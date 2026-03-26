# Battle系统重构文档 - 事件驱动架构

## 1. 重构目标

将原有的**轮询Update驱动**改为**事件驱动 + 定时器**架构，使效果应用逻辑更扁平化，组件间通信更解耦。

## 2. 架构

```text
BattleRoom.Update()
    → 遍历所有 BattleUnit
        → BattleMoveComponent.Update()           # 每帧更新位置
        → BattleAIComponent.Update()             # 每帧入口判断
            → BattleActionDecisionComponent.Update()  # 决策选目标/技能/追击

技能命中 → EffectApplyComponent.ApplyEffects()
    → 发布细分事件 (DamageEvent / FreezeEvent / KnockbackEvent)
        → DamageEvent  → DamageEvent_OnDamage    → 扣血、广播伤害
        → FreezeEvent  → FreezeEvent_OnFreeze    → 添加/应用 FreezeComponent
                          FreezeComponent         → FreezeStartEvent → 中断移动
                                                  → FreezeEndTimer  → EndFreeze
                                                  → FreezeEndEvent  → 恢复移动
        → KnockbackEvent → KnockbackEvent_OnKnockback → 击退单位
```

## 3. 组件说明

### 事件定义（`BattleEvents.cs`）

| 事件 | 说明 |
|------|------|
| `SkillHitEvent` | 技能命中目标时触发 |
| `DamageEvent` | 效果应用时触发，携带伤害量 |
| `FreezeEvent` | 冻结效果应用时触发 |
| `FreezeStartEvent` | FreezeComponent 开始冻结时触发（通知移动组件中断） |
| `FreezeEndEvent` | 冻结时间到期时触发（通知移动组件恢复） |
| `KnockbackEvent` | 击退效果应用时触发 |

### 效果应用组件（`EffectApplyComponent`）

- 职责：将技能效果组（`SkillEffectGroupConfig`）拆分为细分效果，发布对应事件。
- 支持：`Damage`、`Freeze`、`Knockback`、`Heal`、`Stun`（枚举定义，可扩展）
- 返回 `List<EffectResult>`，调用方可根据结果做额外处理。

### 冻结组件（`FreezeComponent`）

- 按需挂载：首次冻结时由 `FreezeEvent_OnFreeze` 动态添加。
- 使用 `TimerInvokeType.FreezeEnd` 定时器触发 `EndFreeze()`。
- 冻结期间移动中断/恢复通过 `FreezeStartEvent` / `FreezeEndEvent` 与 `BattleMoveComponent` 解耦通信。
- `BattleMoveComponent.InterruptedCommand` 保存被中断的移动命令（普通移动或跟随移动），冻结结束后恢复。

### 移动命令（`MoveCommand`）

封装移动请求，支持中断恢复：

```csharp
[EnableClass]
public class MoveCommand
{
    public Vector3 TargetPosition      // 目标位置
    public long FollowTargetUnitId     // 跟随目标ID（0=固定点移动）
    public float FollowDistance        // 期望跟随距离
    public float FollowDirectionSign   // 站位方向（-1左/1右）
    public float MoveSpeed             // 移速
}
```

## 4. 事件处理器

| 处理器 | 订阅事件 | 职责 |
|--------|----------|------|
| `DamageEvent_OnDamage` | `DamageEvent` | 调用 `TakeDamage`，广播 `M2C_Damage`，死亡时广播 `M2C_UnitDead` |
| `FreezeEvent_OnFreeze` | `FreezeEvent` | 获取或添加 `FreezeComponent`，调用 `ApplyFreeze` |
| `KnockbackEvent_OnKnockback` | `KnockbackEvent` | 调用 `BattleMoveComponent.Knockback` |
| `MoveComponent_OnFreezeStart` | `FreezeStartEvent` | 保存当前移动命令到 `InterruptedCommand`，调用 `StopMove` |
| `MoveComponent_OnFreezeEnd` | `FreezeEndEvent` | 读取 `InterruptedCommand`，恢复跟随或普通移动 |
| `BattleUnitDead_Event` | `BattleUnitDead` | 怪物死亡通知波次管理器；英雄全灭触发战斗失败流程 |

## 5. 客户端消息

| 消息 | 消息ID | 触发时机 |
|------|--------|---------|
| `M2C_BattleUnitMoveCommand` | 10137 | 移动状态变化（固定点/跟随/停止） |
| `M2C_Damage` | 10130 | 单位受到伤害 |
| `M2C_UnitDead` | 10138 | 单位死亡 |
| `M2C_SkillCast` | 10139 | 技能释放 |
| `M2C_UnitFrozen` | 10142 | 单位被冻结 |
| `M2C_UnitKnockback` | 10143 | 单位被击退 |

## 6. 文件结构

```
Server/
├── Model/Demo/Battle/
│   ├── BattleEvents.cs               # 事件定义
│   ├── EffectType.cs                 # 效果类型枚举
│   ├── EffectApplyComponent.cs       # 效果应用组件（数据）
│   ├── FreezeComponent.cs            # 冻结组件（数据）
│   ├── MoveCommand.cs                # 移动命令（中断恢复用）
│   ├── BattleActionDecisionComponent.cs
│   ├── BattleMoveComponent.cs
│   ├── BattleUnitCombatComponent.cs
│   ├── BattleAIComponent.cs
│   └── PlayerCombatModeComponent.cs
│
└── Hotfix/Demo/Battle/
    ├── EffectApplyComponentSystem.cs  # 效果应用逻辑
    ├── FreezeComponentSystem.cs       # 冻结逻辑（含 FreezeEndTimer）
    ├── BattleMoveComponentSystem.cs   # 移动逻辑（含击退）
    ├── BattleUnitCombatComponentSystem.cs
    ├── BattleActionDecisionComponentSystem.cs
    ├── BattleAIComponentSystem.cs
    ├── BattleSkillHelper.cs
    ├── BattleUnitHelper.cs
    └── Event/
        ├── DamageEvent_OnDamage.cs
        ├── FreezeEvent_OnFreeze.cs
        ├── KnockbackEvent_OnKnockback.cs
        ├── MoveComponent_OnFreezeStart.cs
        ├── MoveComponent_OnFreezeEnd.cs
        └── BattleUnitDead_Event.cs
```

## 7. 后续扩展建议

### 7.1 更多效果类型

`EffectType.cs` 中已定义 `Stun`，可参照 `Freeze` 的事件/组件模式实现：新增 `StunEvent`、`StunComponent`、`StunEndTimer`。

### 7.2 Buff系统

当前冻结是即时定时效果，可扩展为通用 Buff 系统：持续时间、叠加层数、刷新机制。

### 7.3 AI 决策改为事件驱动

当前 AI 仍为每帧轮询。可考虑：攻击完成后发布触发决策事件，移动完成后重新决策，定时器兜底（如每 200ms）。这样可在低战斗密度时减少 CPU 开销。

---

**最后更新**: 2026-03-23
