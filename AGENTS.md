# AGENTS.md

本文件为 coding agents 在本仓库中工作时提供指导。

## 项目概览

GameNetty 是一款横版“割草”ARPG 游戏，玩法类似《英雄没有闪》，基于 **ET8.1** 框架构建，服务端使用 .NET 8.0，客户端使用 Unity。核心玩法是 PvE，大量小怪潮（1000+ 敌人）加 Boss 战。

架构上严格区分逻辑层（Model/Hotfix，纯 C#/ET）和表现层（ModelView/HotfixView，Unity/TEngine）。服务端消息在逻辑层处理，然后通过事件发布转发到表现层。

## 构建与运行

```bash
# 1. 构建共享工具（必须最先执行）
dotnet build Share/Share.sln

# 2. 构建服务端
dotnet build Server/Server.sln

# 3. 运行服务端
./Bin/App.exe   # 或通过 IDE 运行

# 4. 生成配置（修改 Excel 后执行）
./Tools/Luban/GenConfig_Server.sh   # 服务端 C# 代码 + 二进制数据
./Tools/Luban/GenConfig_Client.sh   # 客户端 C# 代码 + 二进制数据
```

Unity 客户端：使用 Unity Editor 打开 `Unity/` 目录（2019.4.12+，推荐 2021.3+）。

本项目没有测试基础设施或 CI/CD。

## 架构

### 框架：ET8.1（Entity-Tree + Actor Model）

- **基于 Fiber 的单线程模型**：每个 Fiber 运行在自己的线程上，不需要锁。
- **Model/Hotfix 拆分**（仅服务端）：`Model/` = 数据结构与组件定义（很少改动）；`Hotfix/` = 业务逻辑（可通过 DLL 替换热更新）。
- **客户端没有 Model 程序集**：所有客户端游戏代码都在 `HotFix/` 中，分为两个程序集边界：`GameLogic.asmdef`（逻辑）和 `GameProto.asmdef`（生成的协议/配置）。
- **Entity-Component**：所有对象都是 Entity + Component，通过 `entity.GetComponent<T>()` 访问。

### 目录结构

| 目录 | 用途 |
|------|------|
| `Server/Model/Demo/` | 服务端数据模型（组件、事件、配置） |
| `Server/Hotfix/Demo/` | 服务端 hotfix 逻辑（系统、处理器） |
| `Server/Model/Generate/` | Luban（配置）和 ProtoGen（消息）自动生成的 C# |
| `Unity/Assets/GameScripts/HotFix/GameLogic/Module/` | 客户端游戏模块（Battle、Unit、AI、UI 等） |
| `Unity/Assets/GameScripts/HotFix/GameProto/Generate/` | 客户端生成的协议/配置代码 |
| `Config/Excel/GameConfig/` | Luban Excel 源配置 |
| `Config/Proto/` | Proto 定义（OuterMessage = 客户端-服务端，InnerMessage = 服务端-服务端） |
| `Config/Generate/` | 生成的二进制配置数据 |
| `Tools/Luban/` | Luban 配置生成脚本 |
| `Share/` | 共享分析器和源码生成器 |
| `docs/` | 设计文档（玩法规格、战斗流程、AI 设计、框架指南） |
| `spec/` | 功能模块规格说明；`spec/logic/` 按功能领域分层存放功能逻辑文档，`spec/art/` 放美术需求与资源规格 |

`spec/logic/` 中的功能逻辑文档必须按领域模块形成层级，例如 `core/`、`battle/ai/`、`battle/view/`、`battle/hud/`、`battle/vehicle/`。每篇文档还必须分别提炼服务端底层逻辑和客户端底层逻辑。服务端部分需要说明 Entity/Component、消息 Handler、Timer/Event、权威校验、配置读取、状态同步和生命周期；客户端部分需要说明 GameLogic 模块、消息入口、本地状态、表现事件、HUD/Prefab 绑定、本地预测与服务端校正。涉及协议和配置时，必须明确双端生成代码、二进制配置和同步边界。

### Unit 与 BattleUnit（关键区别）

- **`Unit`**：持久化实体，存在于 Map Scene 的 `UnitComponent` 中。保存玩家持久数据（背包、属性等），登录时创建。
- **`BattleUnit`**：临时战斗实体，存在于 `BattleRoom` 内。保存战斗内数据（HP、位置、Buff 等），通过 `OwnerId` 引用它所属的 `Unit`。
- 这种分离允许同一个玩家同时存在多个并发战斗实例，且不会互相影响持久数据。
- 完整说明见 `docs/框架设计/BattleRoom架构说明-Unit与BattleUnit.md`。

### 战斗系统：双轨同步

完整规格见 `docs/gameplay.md`，完整战斗生命周期见 `docs/battle_flow.md`。

**轨道 A：小怪（客户端权威 + 服务端校验）**
- 服务端发送 `M2C_SpawnWave`（刷怪意图），客户端使用 `ClientMinionAIComponent` 本地创建小怪。
- 服务端创建轻量级 `BattleUnit` 实体（`CreateMinion`），仅用于伤害校验；不挂 `BattleMoveComponent` 或 `BattleActionDecisionComponent`。
- `C2M_ClientBatchHit` 是**双向**的：玩家→小怪（玩家攻击）和小怪→玩家（小怪攻击）。服务端通过技能系统校验并应用伤害。
- 小怪移动：通过 `Forward` 字段驱动方向，`BattleUnitViewSystem.Update` 每帧执行增量移动（`speed * deltaTime`）。
- 小怪攻击范围：从 `UnitCombatConfig.AutoSkillIds` / `NormalAttackSkillId` 读取，再取 `SkillTargetingConfig.CastRange + EdgeDistance`。

**轨道 B：Boss（服务端权威）**
- 服务端通过 `BattleMoveComponent` + `BattleActionDecisionComponent` 控制 Boss 移动。
- Boss 位置通过 `BossSyncComponent` 广播（20Hz `M2C_SyncBoss`）。
- 伤害仅在服务端计算；客户端等待 `M2C_Damage`。
- 碰撞检测通过 `SkillTimelineComponent`（20ms tick）+ `BattleSpatialGrid` 完成。

**玩家移动**：客户端权威，方向驱动。`ClientPlayerAIComponent`（100ms tick）设置 `Forward`（移动意图）和 `FaceDirection`（视觉朝向，与移动解耦）。停止移动逻辑使用最短技能范围，并与技能 CD 解耦。客户端通过 `C2M_PlayerPositionSync` 同步位置，使 Boss AI 能追踪玩家。

### 关键战斗组件（服务端）

- `BattleRoom`：战斗实例的根实体，持有所有 BattleUnit。
- `BattleUnit`：战斗实体，包含 `NumericComponent`、阵营、位置，以及关联持久 Unit 的 `OwnerId`。
- `BattleActionDecisionComponent`：AI 自动选目标和移动/施法决策（仅用于 Boss 等服务端权威单位）。
- `BattleMoveComponent`：带追击模式的移动组件（仅用于服务端权威单位）。
- `SkillTimelineComponent`：注册 hitbox，以 20ms tick 做碰撞检测，并以 100ms 批量输出伤害结果。
- `WaveManagerComponent`：波次推进；通过服务端路径刷 Boss，通过客户端路径刷小怪。
- `BattleSpatialGrid`：用于碰撞查询的空间分区。
- `BuffComponent` / `BuffEntity`：Buff 系统。
- `BattleHelper`：封装所有战斗网络操作（开始、加入、退出、施法）。客户端-服务端战斗 RPC 应使用它。

### 关键战斗组件（客户端）

- `BattleUnit`：客户端战斗实体，包含 `Forward`（移动意图）、`FaceDirection`（视觉朝向）、`Position`。
- `ClientPlayerAIComponent`：玩家自动战斗 AI（100ms tick），负责目标选择、技能施放和基于范围的停止移动。
- `ClientMinionAIComponent`：小怪 AI（100ms tick），负责追击和通过 `C2M_ClientBatchHit` 攻击。
- `BattleUnitView` / `BattleUnitViewSystem`：每帧基于 `Forward` 做增量移动，基于 `FaceDirection` 做视觉翻转。

### AI 系统：逻辑锁计数器（不是 FSM）

AI 系统**不使用**有限状态机。它使用逻辑锁计数器：数值递增/递减以允许非互斥状态共存（一个单位可以同时处于移动、施法、受击状态）。表现层根据计数器状态驱动动画。这种方式能高效处理 1000+ 敌人。见 `spec/logic/battle/ai/割草游戏高性能 AI 逻辑设计文档.md`。

### 消息流

- `ILocationMessage`：路由到玩家 `Unit` 所在的 Map 服务器；由 `MessageLocationHandler<Unit, T>` 处理。
- `ILocationRequest` / `ILocationResponse`：Location 消息的 RPC 变体。
- `IMessage`：普通消息，不走路由。
- 客户端通过 `scene.GetComponent<ClientSenderComponent>().Send(msg)` 或 `.Call(req)` 发送。
- 消息路径：Model → Hotfix → HotfixView → Unity（逻辑和表现通过事件解耦）。

### Proto 与配置生成

**配置**（Luban，脚本生成）：编辑 `Config/Excel/GameConfig/` 下的 Excel 文件后，运行生成脚本。生成的 C# 代码和二进制数据会自动产出。

**Proto**（手动）：`Config/Proto/OuterMessage_C_10001.proto` 中的 Proto 定义**不会自动生成**。修改 proto 后，必须**手动更新**两端生成的 C# 代码：
- `Server/Model/Generate/Message/OuterMessage_C_10001.cs`（消息类 + opcode）
- `Unity/Assets/GameScripts/HotFix/GameProto/Generate/Message/OuterMessage_C_10001.cs`（相同内容）

Opcode 编号从 10001 开始顺序递增。新增消息使用下一个可用编号。

## 编码约定

- ET 组件系统类使用 `[EntitySystemOf(typeof(X))]` + `[FriendOf(typeof(X))]`。
- 组件扩展方法形式：`public static void Method(this XComponent self, ...)`。
- `[EntitySystem]` 标记生命周期方法（Awake、Destroy）。
- 定时器回调：通过 `TimerComponent.NewRepeatedTimer` 注册，回调类型定义在 `TimerInvokeType`。
- 事件：使用 `EventSystem.Instance.Publish<TContext, TEvent>(context, eventData)`，处理器标记 `[Event]`。
- 组件创建：`entity.AddComponent<T>()`、`entity.AddComponent<T, P1>(param1)`。
- `NumericComponent` 的值以 `long` 存储，通过 `GetAsInt()` / `GetAsFloat()` / `GetByKey()` 扩展方法访问。
- Unity/TEngine UI 结构应放在 Prefab 中。不要在业务代码中通过 `new GameObject(...)` / `AddComponent(...)` 创建 UI 面板、列表项或 prefab 层级；应创建或修改 Prefab，并使用 TEngine 的 `CreateWidget` / `CreateWidgetByPrefab` / `AdjustIconNum` 绑定和复用。
- 可点击 UI 节点必须使用项目内的 `GameLogic.UIButton` 组件，而不是直接绑定 Unity `Button`。生成的 UI 绑定应使用 `UIButton`，点击处理应通过 `UIButton.SetClick(...)` 注册，而不是 `Button.onClick.AddListener(...)`。
- 修改 Unity Prefab 时，必须优先使用 Unity MCP。如果 Unity MCP 不可用、断开连接，或无法检查/修改目标 Prefab，则停止并告知用户；由用户决定是否继续使用直接编辑 YAML 等非 MCP 兜底方案。不要在没有 Unity MCP 确认的情况下静默编辑 Prefab 文件。
