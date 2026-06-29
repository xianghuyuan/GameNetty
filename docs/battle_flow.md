# 战斗模块流程图

> 基于 Server/Hotfix/Demo/Battle/ 及相关代码生成，随代码同步更新。
>
> 最后更新: 2026-04-13

---

## Phase 1: 战斗准备

```
客户端                         服务端
  │                              │
  │  C2M_TeamStartBattle ──────► │  C2M_TeamStartBattleHandler
  │  (battleType=stageId)        │    ├─ 校验玩家状态（是否已在战斗中）
  │                              │    ├─ 获取/创建 BattleRoomManagerComponent
  │                              │    ├─ mapScene.AddChild<BattleRoom>(configId)
  │                              │    │    └─ BattleRoomSystem.Awake:
  │                              │    │       ├─ State = Prepare
  │                              │    │       ├─ AddComponent<SlotManagerComponent>
  │                              │    │       ├─ AddComponent<BattleSpatialGrid>(5f)
  │                              │    │       ├─ AddComponent<SkillTimelineComponent>
  │                              │    │       ├─ AddComponent<BossSyncComponent>
  │                              │    │       └─ AddComponent<BattleUnitRegistryComponent>  ← 统一注册表
  │                              │    ├─ battleRoom.AddPlayer(playerId)
  │                              │    ├─ roomManager 注册映射
  │                              │    └─ battleRoom.InitTeamBattle(mapScene, memberIds, battleType)
  │                              │
  │  ◄──── M2C_TeamStartBattle   │    response.battleId, response.memberIds
  │        (battleId, memberIds)  │
```

### InitTeamBattle 内部流程

```
InitTeamBattle(mapScene, memberIds, battleType)
│
├─ 遍历 memberIds 创建英雄单位
│   ├─ UnitFactory.CreateHero(battleRoom, unit, position)
│   │   ├─ Camp = Friend
│   │   ├─ AddComponent<NumericComponent>        → 初始化 MaxHp/Attack/Defense/Speed
│   │   ├─ AddComponent<NumericNoticeComponent>
│   │   ├─ AddComponent<BuffComponent>
│   │   ├─ AddComponent<BattleUnitCombatComponent>
│   │   └─ ApplyNormalAttackConfigFromCombatConfig → 从 UnitCombatConfig 读取技能/速度覆盖
│   ├─ registry.Register(heroUnit)              ← 通过 BattleUnitRegistryComponent 注册
│   └─ spatialGrid.Insert(unit.Id, position.X)
│
├─ if battleType == 2 (Boss战):
│   ├─ UnitFactory.CreateMonster(battleRoom, configId, position=EnemySpawnX)
│   │   ├─ Camp = Enemy
│   │   ├─ AddComponent<NumericComponent>        → 从 MonsterUnitConfig 读取属性
│   │   ├─ AddComponent<BattleMoveComponent>     → 100ms 移动tick
│   │   ├─ AddComponent<BattleActionDecisionComponent> → 100ms AI决策tick
│   │   ├─ AddComponent<BuffComponent>
│   │   ├─ AddComponent<BattleUnitCombatComponent>
│   │   ├─ ApplyNormalAttackConfigFromCombatConfig → 从 UnitCombatConfig 读取技能/速度覆盖
│   │   └─ if MonsterType==Boss → Publish BossCreatedEvent → BossSyncComponent.RegisterBoss
│   └─ registry.Register(bossUnit)             ← 通过 BattleUnitRegistryComponent 注册
│
└─ 初始化波次管理器（配置驱动）
    └─ AddComponent<WaveManagerComponent>(stageConfigId, waveConfigIds)
        ├─ StageConfigId, WaveConfigIds (从 StageConfig 获取)
        ├─ CurrentWaveIndex = -1
        ├─ State = WaveState.None
        └─ WaveInterval = 5000ms, AutoStartNextWave = true
```

### InitBattle 内部流程（单人战斗）

```
InitBattle(playerUnit, stageId, battleType)
│
├─ 创建玩家战斗单位
│   ├─ UnitFactory.CreateHero(battleRoom, playerUnit, PlayerSpawnX)
│   ├─ registry.Register(heroUnit)
│   └─ spatialGrid.Insert(heroUnit.Id, heroUnit.Position.X)
│
└─ 初始化波次管理器
    └─ AddComponent<WaveManagerComponent>(stageId, stageInfo.WaveConfigIds)
        └─ 从 StageConfigInfo 获取 WaveConfigIds（按 WaveNumber 排序）
```

---

## Phase 2: 进入战斗

```
服务端内部流程
│
├─ battleRoom.StartFirstWave()
│   ├─ WaitFrameAsync()  // 等一帧确保组件就绪
│   ├─ SendHeroUnits()
│   │   ├─ 收集 Camp==Friend 的 BattleUnit (via GetUnitsByCamp)
│   │   ├─ BattleUnitHelper.CreateBattleUnitInfo(hero)
│   │   └─ 广播 M2C_CreateBattleUnits (battleId, units[])
│   │       → 客户端收到后创建英雄 UI
│   │
│   └─ waveManager.StartFirstWave()
│       └─ StartNextWave()
│           ├─ CurrentWaveIndex++ (0→第1波)
│           ├─ State = WaveState.Preparing
│           ├─ 广播 M2C_WaveStart (battleId, waveNumber, totalWaves, monsterCount)
│           └─ SpawnWaveMonsters(waveConfigId)
│               └─ SpawnWaveMonstersFromBatches(waveConfig)
│                   └─ 遍历 waveConfig.Batches
│                       ├─ 读取 SpawnConfig (PositionX, SpreadRange, Monsters[])
│                       ├─ 支持 batch.Delay 延迟生成
│                       └─ SpawnFromSpawnConfig(spawnConfig)
│                           │
│               ┌───────────┴───────────┐
│               ▼                       ▼
│          Boss/精英                    杂兵 (minion)
│    MonsterType == 3            MonsterType != 3
│               │                       │
│   服务端创建完整实体          服务端创建轻量实体
│   UnitFactory.CreateMonster   UnitFactory.CreateMinion
│   ├─ NumericComp (全属性)     ├─ NumericComp (仅基础属性)
│   ├─ BattleMoveComp           ├─ 无 BattleMoveComp
│   ├─ DecisionComp             ├─ 无 DecisionComp
│   ├─ registry.Register()      ├─ registry.Register()
│   ├─ 注册 SpatialGrid         └─ 注册 SpatialGrid
│   └─ 广播 M2C_CreateBattleUnits       │
│                                   广播 M2C_SpawnWave
│                                   ├─ monsterConfigId
│                                   ├─ count, centerX, spreadRange
│                                   ├─ moveDirX, moveDirY
│                                   └─ startUnitId
│                                        │
│                                   客户端收到后:
│                                   ├─ 从 MonsterUnitConfig 读取属性
│                                   ├─ 本地创建 BattleUnit
│                                   ├─ AddComponent<ClientMinionAI>
│                                   └─ 自动移动+攻击
│
├─ State = WaveState.Fighting
└─ WaveStartTime = currentTime
```

---

## Phase 3: 战斗进行

### 3.1 AI决策循环 (Boss/怪物 — 服务端权威)

```
BattleActionDecisionComponent — 100ms/tick
│
├─ OnDecisionTick() → MakeDecision()
│   ├─ 跳过条件: 死亡 / 冻结中 / 施法中
│   │
│   ├─ BattleSkillHelper.TrySelectBestAutoSkillPlan()
│   │   ├─ 获取 UnitCombatConfig (via caster.ConfigId)
│   │   ├─ GetAutoSkillIds() → 收集自动技能列表
│   │   │   ├─ AutoSkillIds[] 中的技能
│   │   │   └─ 如果 AutoCastNormalAttack → 加入 NormalAttackSkillId
│   │   ├─ 找最近敌人 (battleRoom.ForEachUnit → via BattleUnitRegistryComponent)
│   │   └─ 按优先级遍历技能，检查CD + 射程
│   │       └─ 输出 AutoCastPlan { skillId, target, desiredPosition, requiredMoveDistance }
│   │
│   ├─ if 无可用技能 → PublishStopMoveEvent → StopMove
│   │
│   ├─ if 在射程内 (inRange) → PublishCastEvent
│   │   └─ RequestCastEvent { unit, skillId, targetId }
│   │       → BattleSkillHelper.TryExecuteSkill()
│   │
│   └─ if 超出射程 → PublishMoveEvent
│       └─ RequestMoveEvent { unit, targetPosition, chaseTargetId, chaseAttackRange }
│           → BattleMoveComponent.StartMove()
```

### 3.2 移动系统 (服务端权威单位)

```
BattleMoveComponent — 100ms/tick
│
├─ StartMove(targetPosition, chaseTargetId, chaseAttackRange)
│   ├─ 从 NumericComponent 读取 Speed
│   ├─ 设置 TargetPosition, ChaseTargetId
│   └─ BroadcastMoveCommand (M2C_BattleUnitMoveCommand)
│
├─ OnMoveTick() 每100ms
│   ├─ if 追击模式 (ChaseTargetId != 0)
│   │   ├─ 检测与目标距离
│   │   ├─ if 距离 <= ChaseAttackRange → StopMove + 立即决策
│   │   └─ else → 更新 TargetPosition 到射程边缘
│   │
│   ├─ 计算位移: speed * deltaTime / 1000
│   ├─ 更新 owner.Position
│   └─ 更新 SpatialGrid
│
└─ StopMove()
    ├─ BroadcastPositionSync (M2C_BattleUnitPositionSync)
    └─ 清空状态
```

### 3.3 技能执行流程

```
玩家手动施法                    Boss/AI自动施法
C2M_CastSkill ──►              RequestCastEvent
       │                              │
       ▼                              ▼
  C2M_CastSkillHandler         Event: RequestCastEvent
       │                              │
       └──────────┬───────────────────┘
                  ▼
    BattleSkillHelper.TryExecuteSkill(caster, skillId, targetId)
    │
    ├─ 前置校验
    │   ├─ 施法者存活
    │   ├─ SkillConfig 存在且启用
    │   ├─ TargetingConfig + BuffGroupConfig 完整
    │   ├─ 攻击CD就绪 (combat.IsSkillReady)
    │   └─ 自动模式限制检查 (PlayerCombatModeComponent)
    │
    ├─ 目标选取 SelectTargets()
    │   ├─ 指定目标模式: 直接验证
    │   ├─ 最近敌人模式: battleRoom.ForEachUnit 距离筛选
    │   └─ 范围内所有敌人: 距离筛选 + SortRule 排序 + MaxTargetCount 截断
    │
    ├─ 广播施法 M2C_SkillCast (casterId, skillId, targetId, targetPos)
    │
    ├─ if CastType == Projectile (投射物):
    │   └─ Publish SpawnProjectileEvent
    │       └─ UnitFactory.SpawnProjectile → 投射物飞行+碰撞检测
    │
    └─ else (瞬发):
        └─ ApplyEffects(caster, target, buffGroupConfig, skillConfig)
            │   ← 所有效果统一视为 buff 效果
            ├─ Damage效果:
            │   ├─ CalculateDamage (attack * ratioAtk - defense * ratioDef)
            │   ├─ target.TakeDamage(damage)
            │   ├─ BroadcastDamage (M2C_Damage)
            │   └─ if 死亡 → BroadcastUnitDead
            ├─ Heal效果: target.Heal(amount)
            ├─ Knockback效果: Publish KnockbackEvent → 移动组件处理 + 广播
            ├─ LifeSteal效果: 先造成伤害，再按比例回复施法者
            ├─ Shield效果: ShieldComponent.ApplyShield
            ├─ AttackBuff/DefenseBuff: NumericComponent 加成 + BuffComponent 注册
            ├─ SlowDown效果: SlowDownComponent.ApplySlow
            └─ Freeze/Stun/DOT等持续效果:
                └─ BuffComponent.AddBuff → 创建 BuffEntity (带 Duration + TickInterval)
```

### 3.4 技能时间轴 (SkillTimelineComponent)

```
SkillTimelineComponent — 20ms/tick
│
├─ 技能注册 Hitbox → 加入检测队列
│
├─ 20ms 碰撞检测 tick
│   ├─ 遍历活跃 Hitbox
│   ├─ SpatialGrid 范围查询
│   ├─ 校验目标合法性
│   └─ 累积命中结果
│
├─ 100ms 批量伤害下发
│   ├─ Boss 伤害 → M2C_BossDamage (单独下发)
│   ├─ 杂兵伤害 → M2C_BatchDamage (批量下发)
│   │   └─ 包含 deadUnitIds (杂兵死亡不单独广播)
│   └─ 清空累积结果
```

### 3.5 杂兵系统 (客户端权威)

#### 杂兵 AI (ClientMinionAIComponent — 100ms tick)
```
ClientMinionAIComponent.Tick()
│
├─ FindNearestEnemy() — 遍历 Battle.Children 找最近友方
│
├─ 设置 FaceDirection → 始终朝向目标
│
├─ 在 AttackRange 内:
│   ├─ 在线: Forward = zero (停止移动)
│   ├─ 离线: 仅在真正出手时短暂停步
│   ├─ 检查攻击 CD (AttackCooldown)
│   ├─ CD 好了 → 发送 C2M_ClientBatchHit / 本地命中结算
│   └─ CD 未好 → 在线等待, 离线继续追击
│
└─ 超出 AttackRange:
    ├─ Forward = faceDir (继续移动)
    └─ View 层每帧增量移动靠近目标
```

#### 杂兵攻击距离配置
```
M2C_SpawnWaveHandler → 读取 MonsterUnitConfig
│
├─ 从 UnitCombatConfig[monsterConfigId] 获取 AutoSkillIds / NormalAttackSkillId
├─ 遍历技能的 SkillTargetingConfig.CastRange + EdgeDistance
└─ 取最短射程 → 设置 BattleUnitCombatComponent.AttackRange
```

### 3.6 C2M_ClientBatchHit (双向命中上报)
```
客户端                         服务端
  │                              │
  │  C2M_ClientBatchHit ───────► │  C2M_ClientBatchHitHandler
  │  ├─ battleId                 │    ├─ 通过 ILocationMessage 路由到玩家 Map
  │  ├─ skillId                  │    ├─ 获取 BattleRoom + SkillConfig
  │  ├─ hitUnitIds[]             │    ├─ 遍历 hitUnitIds:
  │  └─ casterId (攻击者ID)      │    │   ├─ 校验: 存活 + 非友方 + 非Boss
  │                              │    │   ├─ ApplyEffects(caster, target, ...)
  │                              │    │   └─ 收集 BatchDamageResult
  │                              │    └─ 累积到 SkillTimelineComponent
  │  ◄── M2C_Damage ──────────── │    100ms 批量下发
  │
  使用场景:
  ├─ 玩家打杂兵: casterId=玩家, hitUnitIds=[杂兵列表]
  └─ 杂兵打玩家: casterId=杂兵, hitUnitIds=[玩家列表]
```

### 3.7 玩家位置同步

```
客户端                         服务端
  │                              │
  │  C2M_PlayerPositionSync ──► │  更新 BattleUnit.Position
  │  (移动开始/停止时触发)       │  → 供 Boss AI 追踪玩家位置
  │                              │
```

### 3.8 Boss 同步 (20Hz)

```
BossSyncComponent — 50ms/tick
│
├─ 收集 Boss 状态
│   ├─ Position (X, Y, Z)
│   ├─ State (Idle/Moving/CastSkill/Frozen/Dead)
│   └─ CurrentSkillId, Hp, MaxHp
│
└─ 广播 M2C_SyncBoss → 所有玩家客户端
```

### 3.9 波次推进

```
WaveManagerComponent (配置驱动: WaveConfig → SpawnConfig → Monsters)
│
├─ 怪物死亡 → OnMonsterDead(monsterId)
│   ├─ 从 CurrentWaveMonsterIds 移除
│   ├─ 从 SpatialGrid 移除
│   ├─ 从 BattleUnitRegistryComponent 移除 (registry.Unregister)
│   └─ if 怪物列表为空 && State == Fighting
│       └─ OnWaveCompleted()
│           ├─ State = WaveState.Completed
│           ├─ 广播 M2C_WaveComplete
│           └─ if 还有下一波 && AutoStartNextWave
│               └─ StartNextWave()  → 回到 Phase 2 的刷怪流程
│
├─ TriggerNextWave() → 手动触发下一波（需 State == Completed）
│
└─ 所有波次完成 → OnAllWavesCompleted()
    ├─ battleRoom.State = End
    ├─ 广播 M2C_BattleEnd
    ├─ WaitAsync(5000) → 清理所有玩家映射 → battleRoom.Dispose()
    └─ 进入 Phase 4
```

---

## Phase 4: 战斗结束

```
服务端                              客户端
  │                                   │
  │  OnAllWavesCompleted()            │
  │   ├─ battleRoom.State = End       │
  │   ├─ 广播 M2C_BattleEnd ────────► │  展示结算界面
  │   │   (success=true)              │
  │   ├─ WaitAsync(5000)              │
  │   │                               │
  │   └─ 清理:                        │
  │       ├─ 遍历 battleRoom.PlayerIds → roomManager.RemoveUnitFromBattleRoom
  │       ├─ roomManager.RemoveBattleRoom(battleRoom.Id)
  │       └─ battleRoom.Dispose()     │
  │                                   │
  │                    OR             │
  │                                   │
  │  玩家主动退出:                     │
  │  ◄── C2M_ExitBattle ──────────── │
  │   C2M_ExitBattleHandler          │
  │   ├─ 校验玩家在战斗中             │
  │   ├─ battleRoom.PlayerIds.Remove │
  │   ├─ roomManager 移除映射        │
  │   ├─ if 房间为空:                 │
  │   │   ├─ roomManager.RemoveBattleRoom
  │   │   └─ battleRoom.Dispose()    │
  │   └─ else:                        │
  │       ├─ 广播 M2C_UnitDead (退出者)
  │       └─ battleRoom.RemoveUnit   │
  │   ─── M2C_ExitBattle ──────────► │
  │                                   │
```

---

## 关键消息汇总

| 方向 | 消息 | 触发时机 |
|------|------|----------|
| C→S | `C2M_TeamStartBattle` | 开始组队战斗 |
| C→S | `C2M_StartBattle` | 开始单人战斗 |
| C→S | `C2M_BattleReady` | 客户端战斗准备就绪 |
| S→C | `M2C_TeamStartBattle` | 战斗房间创建响应 |
| S→C | `M2C_StartBattle` | 单人战斗响应 |
| S→C | `M2C_CreateBattleUnits` | 英雄/Boss创建信息 |
| S→C | `M2C_WaveStart` | 波次开始 |
| S→C | `M2C_SpawnWave` | 杂兵刷怪指令 |
| C→S | `C2M_PlayerPositionSync` | 玩家移动同步 |
| C→S | `C2M_CastSkill` | 手动施法请求 |
| S→C | `M2C_SkillCast` | 广播技能施法 |
| S→C | `M2C_BattleUnitMoveCommand` | 服务端单位移动指令 |
| S→C | `M2C_SyncBoss` | Boss状态同步 (20Hz) |
| C→S | `C2M_ClientBatchHit` | 双向命中上报 (玩家打杂兵 / 杂兵打玩家) |
| S→C | `M2C_BatchDamage` | 批量伤害结果 |
| S→C | `M2C_BossDamage` | Boss伤害结果 |
| S→C | `M2C_Damage` | 单次伤害广播 |
| S→C | `M2C_UnitDead` | 单位死亡 (Boss) |
| S→C | `M2C_UnitKnockback` | 击退 |
| S→C | `M2C_UnitFrozen` | 冻结 |
| S→C | `M2C_ForceCorrectPos` | 位置纠偏 |
| S→C | `M2C_WaveComplete` | 波次完成 |
| S→C | `M2C_BattleEnd` | 战斗结束 |
| C→S | `C2M_ExitBattle` | 退出战斗 |

---

## 双轨权威模型

```
┌───────────────────────────────────────────────────────────────────────┐
│  Track A — 杂兵 (客户端权威 + 服务端验证)                              │
│  ├─ 移动: 客户端 Forward 方向驱动增量移动, 服务端不同步                 │
│  ├─ 攻击: ClientMinionAI 100ms tick 判断射程/CD                       │
│  ├─ 伤害上报: C2M_ClientBatchHit (双向: 玩家→杂兵 + 杂兵→玩家)       │
│  └─ 服务端: 校验合法性 (存活/敌方/非Boss) → ApplyEffects 结算         │
├───────────────────────────────────────────────────────────────────────┤
│  Track B — Boss (服务端权威)                                          │
│  ├─ 服务端: CreateMonster → DecisionComp(100ms) + MoveComp(100ms)     │
│  ├─ 服务端: SkillTimeline(20ms) 碰撞检测 + 伤害结算                   │
│  ├─ 服务端: BossSyncComponent(50ms) → M2C_SyncBoss 广播              │
│  └─ 服务端: M2C_Damage 下发伤害                                      │
├───────────────────────────────────────────────────────────────────────┤
│  玩家英雄                                                            │
│  ├─ 移动: 客户端 Forward 方向驱动, C2M_PlayerPositionSync 同步位置     │
│  ├─ AI: ClientPlayerAIComponent 100ms tick, 射程与CD解耦停移          │
│  ├─ 施法: C2M_CastSkill → 服务端执行技能选目标 + 伤害计算             │
│  └─ 停移: 用最短技能射程停移, 全CD中用所有技能最短射程                │
└───────────────────────────────────────────────────────────────────────┘
```

### 客户端移动系统：方向驱动模型
```
BattleUnit 核心字段:
├─ Forward (float3): 移动意图, 非零时每帧 speed*dt 增量移动
└─ FaceDirection (float): 视觉朝向, 与 Forward 解耦

AI 层 (100ms tick):
├─ ClientPlayerAIComponent: 选目标 → 判断射程 → Forward/FaceDirection
└─ ClientMinionAIComponent: 找最近友方 → 射程判断 → Forward/FaceDirection

View 层 (每帧):
└─ BattleUnitViewSystem.Update: 读 Forward 移动 + 读 FaceDirection 翻转
```

---

## 涉及的关键源文件

| 文件 | 职责 |
|------|------|
| `Server/Hotfix/Demo/Battle/BattleRoomSystem.cs` | 战斗房间生命周期 |
| `Server/Model/Demo/Battle/BattleUnitRegistryComponent.cs` | 战斗单位注册表（Model） |
| `Server/Hotfix/Demo/Battle/BattleUnitRegistryComponentSystem.cs` | 注册/查询/遍历单位 |
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
