# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GameNetty is a side-scrolling "mowing" ARPG game (similar to 英雄没有闪) built on **ET8.1** framework with .NET 8.0 server and Unity client. Core gameplay: PvE with massive minion waves (1000+ enemies) + Boss fights.

Architecture: strict separation between logic layer (Model/Hotfix, pure C#/ET) and presentation layer (ModelView/HotfixView, Unity/TEngine). Server messages are handled in logic layer, then forwarded to view layer via event publishing.

## Build & Run

```bash
# 1. Build shared tools (must be first)
dotnet build Share/Share.sln

# 2. Build server
dotnet build Server/Server.sln

# 3. Run server
./Bin/App.exe   # or via IDE

# 4. Generate configs (after Excel changes)
./Tools/Luban/GenConfig_Server.sh   # server C# code + binary data
./Tools/Luban/GenConfig_Client.sh   # client C# code + binary data
```

Unity client: open `Unity/` in Unity Editor (2019.4.12+, recommended 2021.3+).

No test infrastructure or CI/CD exists in this project.

## Architecture

### Framework: ET8.1 (Entity-Tree + Actor Model)

- **Fiber-based single-threaded model**: each Fiber runs on its own thread, no locks needed
- **Model/Hotfix split** (server only): `Model/` = data structures & component definitions (rarely changed); `Hotfix/` = business logic (hot-reloadable via DLL swap)
- **Client has NO Model assembly** — all client game code is in `HotFix/` with two assembly boundaries: `GameLogic.asmdef` (logic) and `GameProto.asmdef` (generated proto/config)
- **Entity-Component**: everything is Entity + Component, accessed via `entity.GetComponent<T>()`

### Directory Structure

| Directory | Purpose |
|-----------|---------|
| `Server/Model/Demo/` | Server-side data model (components, events, configs) |
| `Server/Hotfix/Demo/` | Server-side hotfix logic (systems, handlers) |
| `Server/Model/Generate/` | Auto-generated C# from Luban (configs) and ProtoGen (messages) |
| `Unity/Assets/GameScripts/HotFix/GameLogic/Module/` | Client game modules (Battle, Unit, AI, UI, etc.) |
| `Unity/Assets/GameScripts/HotFix/GameProto/Generate/` | Client-side generated proto/config code |
| `Config/Excel/GameConfig/` | Luban Excel source configs |
| `Config/Proto/` | Proto definitions (OuterMessage = client-server, InnerMessage = server-server) |
| `Config/Generate/` | Generated binary config data |
| `Tools/Luban/` | Luban config generation scripts |
| `Share/` | Shared analyzers and source generators |
| `docs/` | Design documents (gameplay spec, battle flow, AI design, framework guides) |

### Unit vs BattleUnit (Critical Distinction)

- **`Unit`**: persistent entity living in Map Scene's `UnitComponent`. Holds player's persistent data (inventory, attributes). Created on login.
- **`BattleUnit`**: temporary entity living inside a `BattleRoom`. Holds battle-only data (HP, position, buffs). References its parent `Unit` via `OwnerId`.
- This separation allows multiple concurrent battle instances for the same player without interfering with persistent data.
- See `docs/框架设计/BattleRoom架构说明-Unit与BattleUnit.md` for full details.

### Battle System: Dual-Track Sync

See `docs/gameplay.md` for the full specification, and `docs/battle_flow.md` for the complete battle lifecycle.

**Track A — Minions (Client-Authoritative + Server Validation)**
- Server sends `M2C_SpawnWave` (wave intent), client creates minions locally with `ClientMinionAIComponent`
- Server creates lightweight `BattleUnit` entities (`CreateMinion`) for damage validation only — no `BattleMoveComponent` or `BattleActionDecisionComponent`
- `C2M_ClientBatchHit` is **bidirectional**: player→minion (player attacks) and minion→player (minion attacks). Server validates and applies damage via skill system.
- Minion movement: direction-driven via `Forward` field, `BattleUnitViewSystem.Update` drives incremental movement each frame (`speed * deltaTime`)
- Minion attack range: read from `UnitCombatConfig.AutoSkillIds` / `NormalAttackSkillId` → `SkillTargetingConfig.CastRange + EdgeDistance`

**Track B — Boss (Server-Authoritative)**
- Server controls Boss movement via `BattleMoveComponent` + `BattleActionDecisionComponent`
- Boss position broadcast via `BossSyncComponent` (20Hz `M2C_SyncBoss`)
- Damage calculated server-side only; client waits for `M2C_Damage`
- Collision detection via `SkillTimelineComponent` (20ms tick) + `BattleSpatialGrid`

**Player Movement**: Client-authoritative, direction-driven. `ClientPlayerAIComponent` (100ms tick) sets `Forward` (move intent) and `FaceDirection` (visual facing, decoupled from movement). Stop-move logic uses shortest skill range, decoupled from skill CD. Client syncs position via `C2M_PlayerPositionSync` so Boss AI can track the player.

### Key Battle Components (Server)

- `BattleRoom` — root entity for a battle instance, holds all BattleUnits
- `BattleUnit` — battle entity with `NumericComponent`, camp, position, `OwnerId` linking to persistent Unit
- `BattleActionDecisionComponent` — AI auto-targeting + move/cast decisions (only on server-authoritative units like Boss)
- `BattleMoveComponent` — movement with chase mode (only on server-authoritative units)
- `SkillTimelineComponent` — registers hitboxes, ticks collision at 20ms, batches damage results at 100ms
- `WaveManagerComponent` — wave progression, spawns Boss via server path, minions via client path
- `BattleSpatialGrid` — spatial partitioning for collision queries
- `BuffComponent` / `BuffEntity` — buff system
- `BattleHelper` — encapsulates all battle network operations (start, join, exit, cast skill). Use this for client-server battle RPCs.

### Key Battle Components (Client)

- `BattleUnit` — client-side battle entity with `Forward` (move intent), `FaceDirection` (visual facing), `Position`
- `ClientPlayerAIComponent` — player auto-battle AI (100ms tick), target selection + skill casting + range-based stop
- `ClientMinionAIComponent` — minion AI (100ms tick), chase + attack via `C2M_ClientBatchHit`
- `BattleUnitView` / `BattleUnitViewSystem` — per-frame incremental movement driven by `Forward`, visual flip driven by `FaceDirection`

### AI System: Logic Lock Counters (Not FSM)

The AI system does **not** use finite state machines. Instead it uses logic lock counters — numeric values that increment/decrement to allow non-exclusive state coexistence (a unit can be moving AND casting AND hit simultaneously). View layer drives animations based on counter states. This enables handling 1000+ enemies efficiently. See `docs/功能设计/割草游戏高性能 AI 逻辑设计文档.md`.

### Message Flow

- `ILocationMessage` — routed to the Map server where the player's Unit is located; handled by `MessageLocationHandler<Unit, T>`
- `ILocationRequest` / `ILocationResponse` — RPC variant of location messages
- `IMessage` — regular message, no routing
- Client sends via `scene.GetComponent<ClientSenderComponent>().Send(msg)` or `.Call(req)`
- Message path: Model → Hotfix → HotfixView → Unity (event-based decoupling between logic and presentation)

### Proto & Config Generation

**Configs** (Luban, script-based): Edit Excel files in `Config/Excel/GameConfig/`, then run generation scripts. Generated C# code and binary data are produced automatically.

**Proto** (manual): Proto definitions in `Config/Proto/OuterMessage_C_10001.proto` are **NOT auto-generated**. After editing proto, you must **manually update** the generated C# code in both:
- `Server/Model/Generate/Message/OuterMessage_C_10001.cs` (message class + opcode)
- `Unity/Assets/GameScripts/HotFix/GameProto/Generate/Message/OuterMessage_C_10001.cs` (same)

Opcode numbers are sequential starting from 10001. New messages get the next available number.

## Coding Conventions

- ET components use `[EntitySystemOf(typeof(X))]` + `[FriendOf(typeof(X))]` on the system class
- Extension methods on components: `public static void Method(this XComponent self, ...)`
- `[EntitySystem]` marks lifecycle methods (Awake, Destroy)
- Timer callbacks: register via `TimerComponent.NewRepeatedTimer`, invoke type defined in `TimerInvokeType`
- Events: `EventSystem.Instance.Publish<TContext, TEvent>(context, eventData)` with `[Event]` handlers
- Component creation: `entity.AddComponent<T>()`, `entity.AddComponent<T, P1>(param1)`
- `NumericComponent` values stored as `long`, access via `GetAsInt()`/`GetAsFloat()`/`GetByKey()` extension methods
