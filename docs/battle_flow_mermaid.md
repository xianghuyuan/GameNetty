# 战斗模块时序图

> 基于 Server/Hotfix/Demo/Battle/ 及相关代码生成，使用 Mermaid 时序图表示，随代码同步更新。

---

## Phase 1: 战斗准备

```mermaid
sequenceDiagram
    participant Client as 客户端
    participant Server as 服务端 (Map)

    Client->>Server: C2M_TeamStartBattle(battleType, stageId)
    Note over Server: C2M_TeamStartBattleHandler
    Server->>Server: 校验玩家状态 (是否已在战斗中)
    Server->>Server: 获取/创建 BattleRoomManagerComponent
    Server->>Server: mapScene.AddChild<BattleRoom>(configId)
    Note over Server: BattleRoomSystem.Awake<br/>├─ State = Prepare<br/>├─ SlotManagerComponent<br/>├─ BattleSpatialGrid(5f)<br/>├─ SkillTimelineComponent<br/>└─ BossSyncComponent
    Server->>Server: battleRoom.AddPlayer(playerId)
    Server->>Server: roomManager 注册映射
    Server->>Server: battleRoom.InitTeamBattle(mapScene, memberIds, battleType)

    rect rgb(40, 44, 52)
        Note over Server: InitTeamBattle 内部
        Server->>Server: UnitFactory.CreateHero(battleRoom, unitId, configId, pos)<br/>NumericComponent + BuffComponent + CombatComponent
        Server->>Server: spatialGrid.Insert(unit.Id, position.X)
        opt battleType == 2 (Boss战)
            Server->>Server: UnitFactory.CreateMonster(battleRoom, configId, pos)<br/>+ BattleMoveComponent(100ms) + DecisionComponent(100ms)
            Server->>Server: Publish BossCreatedEvent → BossSyncComponent.RegisterBoss
        end
        Server->>Server: AddComponent<WaveManagerComponent>(stageId, waveConfigIds)
    end

    Server-->>Client: M2C_TeamStartBattle(battleId, memberIds)
```

---

## Phase 2: 进入战斗 (刷怪)

```mermaid
sequenceDiagram
    participant Client as 客户端
    participant Server as 服务端 (Map)
    participant WaveMgr as WaveManagerComponent
    participant SpatialGrid as BattleSpatialGrid

    Note over Server: battleRoom.StartFirstWave()
    Server->>Server: WaitFrameAsync() (等一帧确保组件就绪)
    Server->>Server: SendHeroUnits() → 收集 Camp==Friend 的 BattleUnit
    Server-->>Client: M2C_CreateBattleUnits(battleId, units[])
    Note over Client: 创建英雄 BattleUnit + UI

    Server->>WaveMgr: StartFirstWave() → StartNextWave()
    WaveMgr->>WaveMgr: CurrentWaveIndex++ (0→第1波), State = Preparing
    WaveMgr-->>Client: M2C_WaveStart(battleId, waveNumber, totalWaves, monsterCount)

    WaveMgr->>WaveMgr: SpawnWaveMonstersFromBatches(waveConfig)

    par Boss/精英 (MonsterType == 3)
        Note over Server: 服务端创建完整实体
        Server->>Server: UnitFactory.CreateMonster()<br/>+ BattleMoveComponent + DecisionComponent
        Server->>SpatialGrid: Insert(bossUnit.Id, position.X)
        Server->>Server: Publish BossCreatedEvent → RegisterBoss
        Server-->>Client: M2C_CreateBattleUnits(bossUnitInfo)
    and 杂兵 (MonsterType != 3)
        Note over Server: 服务端创建轻量验证实体
        Server->>Server: UnitFactory.CreateMinion()<br/>仅 NumericComponent (无 Move/Decision)
        Server->>SpatialGrid: Insert(minionUnit.Id, position.X)
        Server-->>Client: M2C_SpawnWave(monsterConfigId, count, centerX, spreadRange, startUnitId)
        Note over Client: 从 MonsterUnitConfig 读取属性<br/>创建本地 BattleUnit + ClientMinionAI<br/>计算 AttackRange = min(技能射程)
    end

    WaveMgr->>WaveMgr: State = Fighting, WaveStartTime = now
```

---

## Phase 3.1: 战斗循环总览

```mermaid
sequenceDiagram
    participant Player as 玩家AI<br/>(ClientPlayerAI 100ms)
    participant ClientView as 客户端View<br/>(每帧)
    participant MinionAI as 杂兵AI<br/>(ClientMinionAI 100ms)
    participant BossAI as Boss AI决策<br/>(DecisionComp 100ms)
    participant BossMove as Boss移动<br/>(MoveComp 100ms)
    participant Timeline as SkillTimeline<br/>(20ms碰撞/100ms下发)
    participant BossSync as BossSync<br/>(50ms/20Hz)
    participant Server as 服务端

    loop 战斗主循环
        Player->>Player: 100ms Tick: 选目标 → 判断射程/CD → Forward/FaceDirection
        Player->>Server: C2M_PlayerPositionSync (位置同步, 供Boss追踪)
        MinionAI->>MinionAI: 100ms Tick: 找最近敌人 → 射程/CD判断 → Forward
        BossAI->>BossAI: 100ms Tick: 选技能 → 射程判断
        BossMove->>BossMove: 100ms Tick: 追击移动 → 到达射程停移
        Timeline->>Timeline: 20ms Tick: 碰撞检测
        Timeline->>Timeline: 100ms Tick: 批量下发伤害
        BossSync->>Server: 50ms Tick: 广播 M2C_SyncBoss
        ClientView->>ClientView: 每帧: 读 Forward 增量移动 + FaceDirection 翻转
    end
```

---

## Phase 3.2: 玩家攻击杂兵 (Track A — 客户端权威)

```mermaid
sequenceDiagram
    participant PlayerAI as ClientPlayerAI (100ms)
    participant Client as 客户端
    participant Server as 服务端
    participant Timeline as SkillTimelineComponent

    Note over PlayerAI: 100ms Tick: 发现杂兵在射程内 + 技能CD就绪

    par 客户端本地即时结算
        PlayerAI->>Client: ApplySkillOnMinions()
        Client->>Client: 遍历 BattleUnit: camp≠caster, 非Boss, 距离≤射程
        Client->>Client: 本地伤害计算: attack * ratioAtk - defense * ratioDef
        Client->>Client: BattleUnitCombatComponent.TakeDamage()
        Client->>Client: Publish BattleUnitDamaged (驱动VFX/浮动数字)
    and 上报服务端验证
        PlayerAI->>Server: C2M_CastSkill(skillId)
        PlayerAI->>Server: C2M_ClientBatchHit(battleId, skillId, casterId=玩家, hitUnitIds=[杂兵列表])
    end

    Note over Server: C2M_ClientBatchHitHandler
    Server->>Server: 遍历 hitUnitIds 校验 (存活 + 非友方 + 非Boss)
    Server->>Server: BattleSkillHelper.ApplyEffects() → 伤害结算
    Server->>Timeline: 累积 BatchDamageResult

    Note over Timeline: 100ms 批量下发
    Timeline-->>Client: M2C_BatchDamage(damages[], deadUnitIds[])
    Note over Client: 更新HP + 清理死亡杂兵
```

---

## Phase 3.3: 杂兵攻击玩家 (Track A — 客户端权威)

```mermaid
sequenceDiagram
    participant MinionAI as ClientMinionAI (100ms)
    participant Client as 客户端
    participant PlayerUnit as 玩家BattleUnit

    Note over MinionAI: 100ms Tick: 发现玩家在 AttackRange 内

    MinionAI->>PlayerUnit: 本地伤害计算: attack - defense (min 1)
    MinionAI->>PlayerUnit: BattleUnitCombatComponent.TakeDamage()
    MinionAI->>Client: Publish BattleUnitDamaged (驱动受击VFX)

    Note over Client: 当前实现: 杂兵→玩家伤害仅本地结算<br/>不发送 C2M_ClientBatchHit 到服务端
```

---

## Phase 3.4: 玩家攻击Boss (Track B — 服务端权威)

```mermaid
sequenceDiagram
    participant PlayerAI as ClientPlayerAI (100ms)
    participant Client as 客户端
    participant Server as 服务端

    Note over PlayerAI: 100ms Tick: 发现Boss在射程内 + 技能CD就绪
    PlayerAI->>Server: C2M_CastSkill(skillId, targetId=Boss)

    Note over Server: C2M_CastSkillHandler
    Server->>Server: 前置校验 (存活/SkillConfig/CD/自动模式限制)
    Server->>Server: SelectTargets() — SpatialGrid 查询
    Server-->>Client: M2C_SkillCast(casterId, skillId, targetId, targetPos)
    Server->>Server: ApplyEffects()
    Server->>Server: CalculateDamage(attack * ratioAtk - defense * ratioDef)
    Server->>Server: target.TakeDamage(damage)

    alt 目标存活
        Server-->>Client: M2C_Damage(targetId, damage, currentHp)
    else 目标死亡 (Boss)
        Server-->>Client: M2C_Damage(targetId, damage, currentHp)
        Server-->>Client: M2C_UnitDead(unitId, killerId)
    end
```

---

## Phase 3.5: Boss攻击玩家 (Track B — 服务端权威)

```mermaid
sequenceDiagram
    participant BossDecision as DecisionComp (100ms)
    participant BossMove as MoveComp (100ms)
    participant Server as 服务端
    participant Timeline as SkillTimeline (20ms)
    participant BossSync as BossSync (50ms)
    participant Client as 客户端

    Note over BossDecision: 100ms Tick: MakeDecision()

    alt 超出射程 → 追击
        BossDecision->>BossMove: RequestMoveEvent(targetPosition, chaseTargetId)
        BossMove->>BossMove: 100ms Tick: speed * deltaTime 增量移动
        BossMove->>BossMove: 更新 SpatialGrid
        BossMove-->>Client: M2C_BattleUnitMoveCommand(targetPos, speed, isMoving)
        BossMove->>BossDecision: 到达射程 → StopMove + 立即 MakeDecision()
    else 在射程内 → 施法
        BossDecision->>Server: RequestCastEvent(unit, skillId, targetId)
        Server->>Server: BattleSkillHelper.TryExecuteSkill()
        Server-->>Client: M2C_SkillCast(casterId, skillId, targetId, targetPos)

        alt 瞬发技能
            Server->>Server: ApplyEffects() → TakeDamage()
            Server-->>Client: M2C_Damage(targetId, damage, currentHp)
        else 投射物技能
            Server->>Server: Publish SpawnProjectileEvent → 投射物飞行+碰撞
            Server-->>Client: M2C_ProjectileLaunch / M2C_ProjectileHit
        else Timeline技能 (Hitbox)
            Server->>Timeline: 注册 Hitbox (StartTick, EndTick, X范围)
        end
    else 无可用技能
        BossDecision->>BossMove: PublishStopMoveEvent → StopMove
    end

    loop 50ms Tick (20Hz)
        BossSync->>BossSync: 收集 Boss 状态 (Position, State, Hp, SkillId)
        BossSync-->>Client: M2C_SyncBoss(pos, rotation, state, skillId, hp, maxHp)
    end

    loop 20ms Tick — 碰撞检测
        Timeline->>Timeline: 遍历活跃 Hitbox
        Timeline->>Timeline: SpatialGrid 范围查询 → 校验目标
        Timeline->>Timeline: 累积命中结果
    end

    loop 100ms Tick — 批量下发
        Timeline-->>Client: M2C_BossDamage(totalDamage, currentHp, maxHp)
        Timeline-->>Client: M2C_BatchDamage(damages[], deadUnitIds[])
    end
```

---

## Phase 3.6: Boss 状态中断 (冻结/击退/施法结束)

```mermaid
sequenceDiagram
    participant Event as 事件系统
    participant MoveComp as BattleMoveComponent
    participant DecisionComp as DecisionComp
    participant Client as 客户端
    participant Server as 服务端

    Note over Event: 状态中断事件链

    alt 冻结开始
        Event->>MoveComp: MoveComponent_OnFreezeStart
        MoveComp->>MoveComp: StopMove()
        MoveComp-->>Client: M2C_BattleUnitPositionSync (停移)
        Server-->>Client: M2C_UnitFrozen(unitId, durationMs)
    else 冻结结束
        Event->>MoveComp: MoveComponent_OnFreezeEnd
        MoveComp->>DecisionComp: 触发 MakeDecision() (恢复AI)
    else 施法结束
        Event->>MoveComp: MoveComponent_OnCastingEnd
        MoveComp->>DecisionComp: 触发 MakeDecision()
    else 击退
        Event->>MoveComp: KnockbackEvent_OnKnockback
        MoveComp->>MoveComp: StopMove() + 更新 Position
        MoveComp-->>Client: M2C_UnitKnockback(unitId, distance, direction, newPosition)
        MoveComp-->>Client: M2C_ForceCorrectPos(smoothDuration=0.1s)
    end
```

---

## Phase 3.7: 玩家自动战斗AI (100ms Tick)

```mermaid
sequenceDiagram
    participant TickDriver as ClientPlayerAITickComponent
    participant PlayerAI as ClientPlayerAIComponent
    participant ClientHelper as ClientBattleDamageHelper
    participant Server as 服务端

    TickDriver->>PlayerAI: Update() — 100ms Tick

    PlayerAI->>PlayerAI: FindNearestEnemy() — 遍历 Battle.Children

    alt 有目标
        PlayerAI->>PlayerAI: 计算与目标距离

        alt 在射程内 (最短技能射程)
            PlayerAI->>PlayerAI: Forward = zero (停止移动)
            PlayerAI->>PlayerAI: FaceDirection = 朝向目标

            alt 有技能CD就绪
                PlayerAI->>Server: C2M_CastSkill(skillId) — 服务端权威路径 (Boss)
                PlayerAI->>ClientHelper: ApplySkillOnMinions() — 客户端本地路径 (杂兵)
                Note over ClientHelper: 跳过Boss (target.IsBoss)
            else 全CD中
                Note over PlayerAI: 等待CD, 用所有技能最短射程维持停移
            end
        else 超出射程
            PlayerAI->>PlayerAI: Forward = faceDir (继续移动)
            PlayerAI->>PlayerAI: FaceDirection = 朝向目标
        end
    else 无目标
        PlayerAI->>PlayerAI: Forward = zero (待机)
    end

    Note over PlayerAI: 停移逻辑与CD解耦:<br/>有CD就绪 → 最短就绪技能射程<br/>全CD中 → 所有技能最短射程
```

---

## Phase 3.8: 杂兵自动AI (100ms Tick)

```mermaid
sequenceDiagram
    participant TickDriver as ClientMinionAITickComponent
    participant MinionAI as ClientMinionAIComponent
    participant View as BattleUnitView (每帧)

    TickDriver->>MinionAI: Update() — 100ms Tick

    MinionAI->>MinionAI: FindNearestEnemy() — 遍历 Battle.Children

    alt 有目标
        MinionAI->>MinionAI: FaceDirection = 朝向目标

        alt 在 AttackRange 内
            MinionAI->>MinionAI: Forward = zero (停止移动)

            alt 攻击CD就绪
                MinionAI->>MinionAI: 本地伤害: attack - defense (min 1)
                MinionAI->>MinionAI: TakeDamage() → Publish BattleUnitDamaged
                Note over MinionAI: 当前不发送 C2M_ClientBatchHit
            else CD未就绪
                Note over MinionAI: 等待
            end
        else 超出 AttackRange
            MinionAI->>MinionAI: Forward = faceDir (继续移动)
        end
    else 无目标
        MinionAI->>MinionAI: Forward = zero (待机)
    end

    loop 每帧
        View->>View: 读 Forward → speed * deltaTime 增量移动
        View->>View: 读 FaceDirection → 视觉翻转
    end
```

---

## Phase 4: 波次推进

```mermaid
sequenceDiagram
    participant WaveMgr as WaveManagerComponent
    participant Server as 服务端
    participant Client as 客户端

    Note over WaveMgr: State = Fighting

    Server->>WaveMgr: OnMonsterDead(monsterId)
    WaveMgr->>WaveMgr: 从 CurrentWaveMonsterIds 移除
    WaveMgr->>WaveMgr: 从 SpatialGrid 移除

    alt 怪物列表为空 && State == Fighting
        WaveMgr->>WaveMgr: OnWaveCompleted()
        WaveMgr->>WaveMgr: State = Completed
        WaveMgr-->>Client: M2C_WaveComplete(waveNumber, duration)

        alt 还有下一波 && AutoStartNextWave
            Note over WaveMgr: 延迟 WaveInterval (5000ms)
            WaveMgr->>WaveMgr: StartNextWave()
            WaveMgr-->>Client: M2C_WaveStart(waveNumber, totalWaves, monsterCount)
            Note over Server,Client: → 回到 Phase 2 刷怪流程
        else 所有波次完成
            WaveMgr->>WaveMgr: OnAllWavesCompleted()
            Note over Server,Client: → 进入 Phase 5 战斗结束
        end
    end
```

---

## Phase 5: 战斗结束

```mermaid
sequenceDiagram
    participant Server as 服务端
    participant Client as 客户端

    alt 正常通关
        Server->>Server: OnAllWavesCompleted()
        Server->>Server: battleRoom.State = End
        Server-->>Client: M2C_BattleEnd(success=true, duration)
        Note over Client: 展示结算界面 (统计/掉落/经验/金币)
        Server->>Server: WaitAsync(5000ms)
        Server->>Server: roomManager 移除映射
        Server->>Server: battleRoom.Dispose()
    else 玩家主动退出
        Client->>Server: C2M_ExitBattle
        Note over Server: C2M_ExitBattleHandler
        Server->>Server: 校验玩家在战斗中
        Server->>Server: battleRoom.PlayerIds.Remove

        alt 房间为空
            Server->>Server: roomManager.RemoveBattleRoom
            Server->>Server: battleRoom.Dispose()
        else 房间还有其他玩家
            Server->>Server: 广播 M2C_UnitDead (退出者)
            Server->>Server: battleRoom.RemoveUnit
        end

        Server-->>Client: M2C_ExitBattle
    end
```

---

## 全局 Tick 时序对照

```mermaid
gantt
    title 各系统 Tick 频率对照
    dateFormat X
    axisFormat %Lms

    section 客户端
    View层 (每帧 ~16ms)       :crit, view, 0, 16
    PlayerAI (100ms)          :ai1, 0, 100
    MinionAI (100ms)          :ai2, 0, 100

    section 服务端
    DecisionComp (100ms)      :dec, 0, 100
    MoveComp (100ms)          :mov, 0, 100
    BossSync (50ms)           :sync, 0, 50
    Timeline碰撞 (20ms)       :tick, 0, 20
    Timeline下发 (100ms)      :flush, 0, 100
```

---

## 关键消息汇总

| 方向 | 消息 | 触发时机 | 说明 |
|------|------|----------|------|
| C→S | `C2M_TeamStartBattle` | 开始战斗 | ILocationRequest |
| S→C | `M2C_TeamStartBattle` | 战斗房间创建 | 返回 battleId + memberIds |
| S→C | `M2C_CreateBattleUnits` | 英雄/Boss创建 | 含完整属性 unitId/configId/camp/pos/hp |
| S→C | `M2C_WaveStart` | 波次开始 | waveNumber/totalWaves/monsterCount |
| S→C | `M2C_SpawnWave` | 杂兵刷怪 | configId/count/centerX/spreadRange/startUnitId |
| C→S | `C2M_PlayerPositionSync` | 玩家移动 | ILocationMessage, 供Boss追踪 |
| C→S | `C2M_CastSkill` | 手动施法 | ISessionRequest, 服务端权威路径 |
| S→C | `M2C_SkillCast` | 广播施法 | casterId/skillId/targetId/targetPos |
| S→C | `M2C_BattleUnitMoveCommand` | Boss移动指令 | targetPos/speed/isMoving/duration |
| S→C | `M2C_SyncBoss` | Boss状态同步 | 20Hz: pos/state/skillId/hp |
| C→S | `C2M_ClientBatchHit` | 双向命中上报 | 玩家→杂兵 / 杂兵→玩家 |
| S→C | `M2C_BatchDamage` | 批量伤害 | 100ms: damages[] + deadUnitIds[] |
| S→C | `M2C_BossDamage` | Boss伤害 | 100ms: totalDamage/currentHp/maxHp |
| S→C | `M2C_Damage` | 单次伤害 | 服务端技能执行结果 |
| S→C | `M2C_UnitDead` | 单位死亡 | 仅Boss死亡时单独广播 |
| S→C | `M2C_UnitKnockback` | 击退 | unitId/distance/direction/newPos |
| S→C | `M2C_UnitFrozen` | 冻结 | unitId/durationMs |
| S→C | `M2C_ForceCorrectPos` | 位置纠偏 | smoothDuration=0.1s |
| S→C | `M2C_WaveComplete` | 波次完成 | waveNumber/duration |
| S→C | `M2C_BattleEnd` | 战斗结束 | success/duration |
| C→S | `C2M_ExitBattle` | 退出战斗 | ILocationRequest |

---

## 双轨权威模型总览

```mermaid
graph TB
    subgraph TrackA["Track A — 杂兵 (客户端权威 + 服务端验证)"]
        direction TB
        A1["移动: 客户端 Forward 驱动<br/>每帧 speed*dt 增量移动"]
        A2["AI: ClientMinionAI 100ms tick<br/>射程/CD判断"]
        A3["伤害: C2M_ClientBatchHit 双向<br/>玩家→杂兵 + 杂兵→玩家"]
        A4["服务端: 校验(存活/敌方/非Boss)<br/>→ ApplyEffects 结算"]
        A1 --> A2 --> A3 --> A4
    end

    subgraph TrackB["Track B — Boss (服务端权威)"]
        direction TB
        B1["实体: CreateMonster 完整组件<br/>MoveComp + DecisionComp"]
        B2["AI: DecisionComp 100ms tick<br/>技能选择/射程判断"]
        B3["移动: MoveComp 100ms tick<br/>追击 + 到达射程停移"]
        B4["碰撞: SkillTimeline 20ms tick<br/>Hitbox + SpatialGrid"]
        B5["同步: BossSync 50ms (20Hz)<br/>M2C_SyncBoss 广播"]
        B1 --> B2 --> B3 --> B4 --> B5
    end

    subgraph Player["玩家英雄 (混合权威)"]
        direction TB
        P1["移动: 客户端 Forward 驱动<br/>C2M_PlayerPositionSync 同步"]
        P2["AI: ClientPlayerAI 100ms tick<br/>射程与CD解耦停移"]
        P3["施法: C2M_CastSkill → 服务端<br/>+ 本地 ApplySkillOnMinions"]
        P4["停移: 最短技能射程<br/>全CD用所有技能最短"]
        P1 --> P2 --> P3 --> P4
    end
```

---

## 客户端方向驱动移动模型

```mermaid
graph LR
    subgraph AI层["AI 层 (100ms tick)"]
        PA["ClientPlayerAI<br/>选目标→射程判断→停/移"]
        MA["ClientMinionAI<br/>找敌人→射程判断→停/移"]
    end

    subgraph 数据层["BattleUnit 字段"]
        F["Forward (float3)<br/>移动意图"]
        FD["FaceDirection (float)<br/>视觉朝向"]
    end

    subgraph View层["View 层 (每帧)"]
        V["BattleUnitViewSystem.Update<br/>Forward → speed*dt 位移<br/>FaceDirection → 翻转"]
    end

    PA --> F
    PA --> FD
    MA --> F
    MA --> FD
    F --> V
    FD --> V
```

---

## 涉及的关键源文件

| 文件 | 职责 |
|------|------|
| `Server/Hotfix/Demo/Battle/BattleRoomSystem.cs` | 战斗房间生命周期 |
| `Server/Hotfix/Demo/Battle/BattleActionDecisionComponentSystem.cs` | AI决策循环 |
| `Server/Hotfix/Demo/Battle/BattleMoveComponentSystem.cs` | 服务端移动+事件处理 |
| `Server/Hotfix/Demo/Battle/BattleSkillHelper.cs` | 技能选择/执行/伤害计算 |
| `Server/Hotfix/Demo/Battle/SkillTimelineComponentSystem.cs` | 技能碰撞检测 |
| `Server/Hotfix/Demo/Battle/EffectApplyComponentSystem.cs` | 效果结算 |
| `Server/Hotfix/Demo/Battle/WaveManagerComponentSystem.cs` | 波次管理 |
| `Server/Hotfix/Demo/Battle/PlayerCombatModeComponentSystem.cs` | 自动/手动模式 |
| `Server/Hotfix/Demo/Battle/Event/DamageEvent_OnDamage.cs` | 伤害事件→广播 |
| `Server/Hotfix/Demo/Map/Unit/UnitFactory.cs` | 单位创建工厂 |
| `Server/Hotfix/Demo/Battle/Handler/C2M_TeamStartBattleHandler.cs` | 开始战斗入口 |
| `Server/Hotfix/Demo/Battle/Handler/C2M_CastSkillHandler.cs` | 手动施法入口 |
| `Server/Hotfix/Demo/Battle/Handler/C2M_ClientBatchHitHandler.cs` | 双向命中上报(玩家↔杂兵) |
| `Server/Hotfix/Demo/Battle/Handler/C2M_ExitBattleHandler.cs` | 退出战斗 |
| `Server/Hotfix/Demo/Battle/BattleUnitHelper.cs` | 消息广播工具 |
| `Unity/.../BattleUnit.cs` | 战斗单位实体(Forward/FaceDirection) |
| `Unity/.../ClientPlayerAIComponentSystem.cs` | 客户端玩家AI(方向驱动+射程停移) |
| `Unity/.../ClientMinionAIComponentSystem.cs` | 客户端杂兵AI(方向驱动+攻击上报) |
| `Unity/.../ClientPlayerAITickComponentSystem.cs` | 玩家AI Tick驱动(100ms) |
| `Unity/.../ClientMinionAITickComponentSystem.cs` | 杂兵AI Tick驱动(100ms) |
| `Unity/.../View/BattleUnitViewSystem.cs` | 每帧增量移动+视觉翻转 |
| `Unity/.../Handler/M2C_SpawnWaveHandler.cs` | 客户端杂兵创建+射程配置 |
| `Unity/.../Handler/M2C_BattleUnitMoveCommandHandler.cs` | Boss移动指令处理 |
| `Unity/.../Handler/M2C_CreateBattleUnitsHandler.cs` | 玩家英雄创建 |
