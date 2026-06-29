# GameNetty 高性能 AI 系统设计 (Roguelike/Survivor Style)

> 最后更新: 2026-04-13

## 1. 目标
针对"割草类"游戏（如《吸血鬼幸存者》/《英雄没有闪》），实现同屏上千个 AI 实体的流畅运行。

---

## 2. 核心挑战
- **同屏实体多**: 超过 1000+ 个怪物的逻辑更新。
- **寻路开销大**: 每个怪物的 A* 寻路会拖垮 CPU。
- **状态互斥问题**: 传统 FSM 中移动、攻击、受击等状态互斥，无法表达"边走边打"等复合行为。

---

## 3. GameNetty 方案概览

本项目的 AI 系统**不使用传统有限状态机 (FSM)**，而是采用**逻辑锁计数器 (Logic Lock Counters)** + **方向驱动移动** + **分角色双轨架构**。

### 3.1 核心设计原则

| 原则 | 说明 |
|------|------|
| **逻辑锁计数器** | 用数值计数器控制行为（如冻结计数、施法计数），支持非互斥状态共存 |
| **方向驱动移动** | 通过 `Forward` 字段驱动增量移动，不使用寻路 |
| **Tick 驱动** | 所有 AI 以 100ms 固定频率 Tick，不依赖 Update |
| **双轨分离** | 客户端权威 AI（玩家/杂兵）与服务端权威 AI（Boss）独立运行 |
| **配置驱动** | 技能选择、攻击范围、CD 等全部从配置表读取 |

### 3.2 三个 AI 组件

| 组件 | 运行端 | 挂载对象 | 职责 |
|------|--------|----------|------|
| `ClientPlayerAIComponent` | 客户端 | 玩家 BattleUnit | 自动选目标 + 自动释放技能 + 增量移动 |
| `ClientMinionAIComponent` | 客户端 | 杂兵 BattleUnit | 找最近敌人 + 追逐 + 本地攻击 |
| `BattleActionDecisionComponent` | 服务端 | Boss/精英 BattleUnit | 选目标 + 选技能 + 发布移动/施法事件 |

---

## 4. 逻辑锁计数器（非 FSM）

### 4.1 设计理念

传统 FSM 将 AI 状态定义为互斥的枚举值（Idle → Chasing → Attacking → Dying），切换时需要处理复杂的退出/进入逻辑。

逻辑锁计数器使用独立的数值标志：
- 单位可以**同时**处于多个"状态"（移动中 + 施法中 + 受击中）
- View 层根据计数器组合驱动动画（非互斥动画混合）
- 避免状态机爆炸，支撑 1000+ 敌人高效运行

### 4.2 当前实现中的逻辑锁

在服务端 AI (`BattleActionDecisionComponent`) 中：
- **冻结锁**: `FreezeComponent.IsFrozen` → 跳过决策
- **施法锁**: `CastingComponent.IsCasting` → 跳过决策
- **死亡**: `BattleUnit.IsDead` → 直接跳过

在客户端 AI 中：
- **CD 锁**: `SkillCooldownEnd[skillId] > nowMs` → 技能不可用
- **攻击 CD 锁**: `nowMs - LastAttackTime < attackCooldownMs` → 攻击未就绪

### 4.3 与 FSM 的对比

```
传统 FSM:
  Idle → Chase → Attack → (被打断) → Hit → Dead
  状态互斥，同一时刻只能处于一种状态

逻辑锁计数器:
  MovingCount=1, CastingCount=0, HitCount=1, FrozenCount=0
  多个计数器同时有效，View 层自由组合表现
```

---

## 5. 客户端玩家 AI (ClientPlayerAIComponent)

**文件**: `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/ClientPlayerAIComponentSystem.cs`
**Tick 驱动**: `ClientPlayerAITickComponent` (100ms)

### 5.1 组件定义

```csharp
[ComponentOf(typeof(BattleUnit))]
public class ClientPlayerAIComponent : Entity, IAwake, IDestroy
{
    public long CurrentTargetId { get; set; }
    public long LastAttackTimeMs { get; set; }
    public Dictionary<int, long> SkillCooldownEnd { get; } = new();
}
```

### 5.2 Tick 流程

```
Tick(battle, nowMs) — 每 100ms
│
├─ 清理过期 CD (CleanupCooldowns)
│
├─ FindNearestEnemy — 遍历 battle.Children 找最近敌方
│   └─ 距离按 X 轴绝对值计算（横版游戏）
│
├─ 无目标 → Forward = zero, return
│
├─ 遍历 AutoSkillIds:
│   ├─ 检查 SkillConfig.IsEnabled
│   ├─ 检查 TargetingConfig 完整性
│   ├─ 计算 CastRange + EdgeDistance
│   ├─ 统计 shortestReadyRange (CD就绪技能中最短射程)
│   ├─ 统计 shortestAllRange (所有技能中最短射程)
│   └─ 找第一个在射程内且 CD 就绪的技能
│
├─ 设置 FaceDirection (视觉朝向，与 Forward 解耦)
│
├─ 停移判定:
│   ├─ 优先用 CD就绪技能的最短射程
│   └─ 全部CD中则用所有技能的最短射程
│
├─ 在射程内:
│   ├─ Forward = zero (停止移动)
│   └─ 有可用技能 → ExecutePlayerSkill + 设定 CD
│
└─ 超出射程:
    ├─ Forward = faceDir (设定移动方向)
    └─ SyncPlayerPosition → C2M_PlayerPositionSync (供 Boss AI 追踪)
```

### 5.3 关键设计

- **停移解耦**: 停移用最短技能射程，不依赖某个具体技能的 CD
- **CD 就绪优先**: CD 好的技能优先决定停移距离，避免"走到射程内但 CD 没好"的尴尬
- **方向驱动**: `Forward` 设定意图方向，`BattleUnitViewSystem.Update` 每帧增量移动 `speed * dt`

---

## 6. 客户端杂兵 AI (ClientMinionAIComponent)

**文件**: `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/ClientMinionAIComponentSystem.cs`
**Tick 驱动**: `ClientMinionAITickComponent` (100ms)

### 6.1 组件定义

```csharp
[ComponentOf(typeof(BattleUnit))]
public class ClientMinionAIComponent : Entity, IAwake, IDestroy
{
    public long TargetUnitId { get; set; }
    public long LastAttackTime { get; set; }
}
```

### 6.2 Tick 流程

```
Tick(battle, nowMs) — 每 100ms
│
├─ FindNearestEnemy — 遍历 battle.Children 找最近敌方（不同 Camp）
│
├─ 无目标 → Forward = zero, return
│
├─ 设置 FaceDirection → 始终朝向目标
│
├─ 获取 AttackRange (BattleUnitCombatComponent.AttackRange)
│
├─ 在 AttackRange 内:
│   ├─ Forward = zero (停止移动)
│   ├─ 检查攻击 CD (nowMs - LastAttackTime < attackCooldownMs)
│   ├─ CD 未好 → return
│   └─ CD 好了 → 本地计算伤害 → 播放攻击动画 → OnHit 回调中:
│       ├─ TakeDamage (本地扣血)
│       └─ Publish BattleUnitDamaged 事件
│
└─ 超出 AttackRange:
    └─ Forward = faceDir (移动方向)
        └─ BattleUnitViewSystem.Update 每帧增量移动
```

### 6.3 关键设计

- **纯本地伤害**: 杂兵攻击玩家在客户端本地计算伤害（不经过服务端）
- **动画命中点**: 通过 `BattleUnitView.PlayAttackFeedback(OnHit)` 在动画命中帧触发伤害
- **简单公式**: `damage = attack - defense` (最小为1)

---

## 7. 服务端 Boss AI (BattleActionDecisionComponent)

**文件**: `Server/Hotfix/Demo/Battle/BattleActionDecisionComponentSystem.cs`
**Timer**: `BattleDecisionTimer` (100ms, `TimerInvokeType.BattleDecisionTick`)

### 7.1 组件定义

```csharp
[ComponentOf(typeof(BattleUnit))]
public class BattleActionDecisionComponent : Entity, IAwake, IDestroy
{
    public EntityRef<BattleUnit> CurrentTarget { get; set; }
    public long DecisionTimerId { get; set; }
    public long LastTargetId { get; set; }
    public bool LastInSkillRange { get; set; }
    public Vector3 LastTargetPosition { get; set; }
}
```

### 7.2 决策流程

```
MakeDecision() — 每 100ms
│
├─ 跳过条件 (逻辑锁检查):
│   ├─ owner.IsDead → return
│   ├─ FreezeComponent.IsFrozen → return
│   └─ CastingComponent.IsCasting → return
│
├─ 清理死亡目标 (CurrentTarget.IsDead → CurrentTarget = null)
│
├─ TrySelectBestAutoSkillPlan(owner, CurrentTarget, out plan)
│   ├─ GetAutoSkillIds → 收集自动技能列表
│   ├─ 找最近敌人 (battleRoom.ForEachUnit)
│   └─ 按优先级遍历技能，检查 CD + 射程
│       └─ 输出 AutoCastPlan { skillId, target, desiredPosition, requiredMoveDistance }
│
├─ 无可用技能 → PublishStopMoveEvent
│
├─ 目标变化 → Publish TargetChangedEvent
│
├─ 在射程内 (inRange):
│   └─ Publish RequestCastEvent → BattleSkillHelper.TryExecuteSkill
│       └─ 不停止移动，边走边打
│
└─ 超出射程:
    └─ Publish RequestMoveEvent → BattleMoveComponent.StartMove
        ├─ ChaseTargetId 追击模式
        └─ ChaseAttackRange 射程边缘停距
```

### 7.3 关键设计

- **事件驱动**: 决策结果通过事件（RequestMoveEvent/RequestCastEvent/RequestStopMoveEvent）传递，解耦决策与执行
- **边走边打**: 在射程内不停止移动，避免"到达→停顿→攻击"的迟钝感
- **目标锁定**: 使用 `CurrentTarget` 保持目标一致性，目标死亡后才重新选择
- **通过 BattleRoom.ForEachUnit**: 目标查找通过 `BattleUnitRegistryComponent` 统一查询

---

## 8. 三个 AI 组件对比

| 特性 | ClientPlayerAI | ClientMinionAI | BattleActionDecision |
|------|---------------|----------------|---------------------|
| **运行端** | 客户端 | 客户端 | 服务端 |
| **挂载对象** | 玩家 BattleUnit | 杂兵 BattleUnit | Boss/精英 BattleUnit |
| **Tick 频率** | 100ms | 100ms | 100ms |
| **目标选择** | 最近敌方 (battle.Children) | 最近敌方 (battle.Children) | 最近敌方 (ForEachUnit) |
| **技能选取** | AutoSkillIds + CD + 射程 | N/A (固定攻击) | AutoSkillIds + CD + 射程 |
| **移动方式** | Forward 方向驱动 | Forward 方向驱动 | RequestMoveEvent → MoveComponent |
| **伤害计算** | BattleSkillExecutionHelper | 本地 attack - defense | BattleSkillHelper.ApplyEffects |
| **停移逻辑** | 最短技能射程 | AttackRange | 不停移(边走边打) |
| **位置同步** | C2M_PlayerPositionSync | N/A | BossSync 20Hz |
| **状态控制** | CD 计数器 | 攻击 CD 计数器 | 逻辑锁 (Freeze/Casting/Dead) |

---

## 9. 性能优化

### 9.1 向量操控（非寻路）
所有 AI 放弃 A* 寻路，使用方向驱动的向量操控：
- `Forward = faceDir` 设定移动意图
- `BattleUnitViewSystem.Update` 每帧增量移动 `speed * deltaTime`
- 无寻路计算开销，O(1) 决策

### 9.2 低频 Tick
所有 AI 以 100ms 频率运行，非每帧更新：
- 客户端通过 `ClientPlayerAITickComponent` / `ClientMinionAITickComponent` 定时触发
- 服务端通过 `TimerComponent.NewRepeatedTimer` 注册
- 1000 个杂兵的 Tick 总开销约 1-2ms

### 9.3 配置驱动
- 技能列表、CD、射程等全部从配置表读取，无硬编码
- `UnitCombatConfig.AutoSkillIds` 定义自动释放技能
- `SkillTargetingConfig.CastRange + EdgeDistance` 定义射程

### 9.4 视图表现优化
- **GPU Instancing**: 使用支持实例化渲染的材质
- **Simple Animation**: 背景层怪物使用纹理动画或简单的顶点位移，减少 Spine 计算量
- **LOD**: 远处怪物不显示特效，不计算复杂碰撞
- **实体池**: ET 框架内置 `ObjectPool`/`EntityPool` 复用组件

---

## 10. 涉及的关键源文件

| 文件 | 职责 |
|------|------|
| `Unity/.../ClientPlayerAIComponent.cs` | 玩家 AI 组件定义 |
| `Unity/.../ClientPlayerAIComponentSystem.cs` | 玩家 AI Tick 逻辑 |
| `Unity/.../ClientPlayerAITickComponentSystem.cs` | 玩家 AI 100ms Tick 驱动 |
| `Unity/.../ClientMinionAIComponent.cs` | 杂兵 AI 组件定义 |
| `Unity/.../ClientMinionAIComponentSystem.cs` | 杂兵 AI Tick 逻辑 |
| `Unity/.../ClientMinionAITickComponentSystem.cs` | 杂兵 AI 100ms Tick 驱动 |
| `Server/Model/Demo/Battle/BattleActionDecisionComponent.cs` | Boss AI 组件定义 |
| `Server/Hotfix/Demo/Battle/BattleActionDecisionComponentSystem.cs` | Boss AI 决策逻辑 |
| `Server/Hotfix/Demo/Battle/BattleSkillHelper.cs` | 技能选择/执行 (TrySelectBestAutoSkillPlan) |
| `Unity/.../BattleSkillExecutionHelper.cs` | 客户端技能执行 |
