# Buff 效果系统设计文档

> 最后更新: 2026-04-13

## 1. 概述

Buff 系统是战斗效果的最小执行单元。**技能 = 一组 Buff 的组合**（通过 BuffGroupConfig 关联）。

核心理念：所有战斗效果（伤害、治疗、冻结、护盾等）统一视为 Buff，通过 `BuffConfig` 配置行为，`EffectType` 枚举定义类型。

### 架构分层

```
配置层: BuffConfig.xlsx → BuffGroupConfig.xlsx → SkillConfig.xlsx
   ↓
Model层: BuffEntity + BuffComponent + 各状态组件（FreezeComponent等）
   ↓
Hotfix层: BattleSkillHelper.ApplyEffects → BuffComponentSystem → BuffExecuteEvent_Handler
   ↓
网络层: M2C_Damage / M2C_BatchDamage / M2C_BossDamage / M2C_UnitFrozen
```

## 2. EffectType 效果类型（11种）

| 编号 | 类型 | 分类 | 触发方式 | 说明 |
|------|------|------|----------|------|
| 1 | Damage | 即时 | ApplyEffects直接执行 | 伤害，扣减目标HP |
| 2 | Freeze | 持续 | 注册到BuffComponent | 冻结，停止移动和攻击，支持刷新时长 |
| 3 | Knockback | 即时 | ApplyEffects直接执行 | 击退，强制位移 |
| 4 | Heal | 即时 | ApplyEffects直接执行 | 治疗，恢复HP |
| 5 | Stun | 持续 | 注册到BuffComponent | 眩晕，复用Freeze逻辑 |
| 6 | SlowDown | 持续 | ApplySlow直接执行 | 减速，支持多层叠加（上限90%） |
| 7 | LifeSteal | 即时 | ApplyEffects直接执行 | 吸血，伤害+按比例回复自身 |
| 8 | Shield | 持续 | ApplyShield直接执行 | 护盾，独立字段吸收伤害，不修改MaxHp |
| 9 | AttackBuff | 持续 | 注册到BuffComponent | 增攻，到期自动还原属性 |
| 10 | DefenseBuff | 持续 | 注册到BuffComponent | 增防，到期自动还原属性 |
| 11 | DOT | 持续 | 注册到BuffComponent | 持续伤害，按TickInterval周期触发 |

### 效果分类

- **即时效果**（Duration=0）：在 `BattleSkillHelper.ApplyEffects` 中直接执行，不创建 BuffEntity
- **持续效果**（Duration>0）：创建 BuffEntity 注册到 BuffComponent，由定时器驱动 tick 和过期

## 3. 核心组件

### 3.1 BuffComponent

挂在 BattleUnit 上，管理该单位身上所有 BuffEntity 的生命周期。

- 100ms 心跳定时器驱动 tick 检测和过期清理
- `AddBuff()`: 添加持续 buff，**同名 buff 刷新时长**而非创建新实体
- `FindBuffById()`: 按 buffId 查找已存在的 BuffEntity
- `FindBuffByEffectType()`: 按 EffectType 查找（用于状态交互）
- `RemoveBuff()`: 移除 buff 并发布 BuffRemoveEvent（用于属性还原）

### 3.2 BuffEntity

BuffComponent 的子实体，运行时单个 buff 实例。

| 字段 | 类型 | 说明 |
|------|------|------|
| BuffId | int | BuffConfig 配置ID |
| CasterId | long | 来源施法者ID |
| SkillId | int | 来源技能ID |
| Config | BuffConfig | 配置引用 |
| Duration | int | 持续时间(ms) |
| TickInterval | int | tick间隔(ms)，0=不tick |
| ExpireTime | long | 过期时间戳 |
| MaxStack | int | 最大叠层数 |
| StackCount | int | 当前叠层 |

### 3.3 专用状态组件

| 组件 | 管理的效果 | 特点 |
|------|-----------|------|
| FreezeComponent | Freeze + Stun | 支持刷新时长（移除旧定时器+重新注册） |
| SlowDownComponent | SlowDown | 引用计数+BaseSpeed，支持多层叠加（上限90%） |
| ShieldComponent | Shield | 独立字段 ShieldCurrentAmount，TakeDamage 时先扣护盾 |

## 4. 效果执行流程

### 4.1 即时效果

```
技能命中 → BattleSkillHelper.ApplyEffects()
  → 遍历 BuffGroupConfig.EffectIds
  → 每个 BuffConfig 按 EffectType 进入对应 case
  → 直接执行逻辑（伤害计算、治疗、击退等）
  → 广播结果给客户端
```

### 4.2 持续效果

```
技能命中 → BattleSkillHelper.ApplyPersistentBuffEffect()
  → 从 BuffConfig 读取 Duration、TickInterval
  → BuffComponent.AddBuff(buffId, casterId, skillId, config, duration, tickInterval, maxStack)
  → [同名检测] 已存在 → 刷新时长
  → [新建] 创建 BuffEntity + 首次执行 BuffExecuteEvent
  ↓
100ms 心驱tick
  → 检测过期 → 发布 BuffRemoveEvent（还原属性）→ Dispose
  → 检测 tick → 发布 BuffExecuteEvent（周期效果）
```

### 4.3 属性还原机制

AttackBuff/DefenseBuff 到期时通过 `BuffRemoveEvent_Handler` 自动还原：

```
BuffEntity过期 → BuffComponentSystem.OnBuffTick 发布 BuffRemoveEvent
  → BuffRemoveEvent_Handler 检测 EffectType
  → AttackBuff → 从 NumericComponent 减去 buffAmount
  → DefenseBuff → 从 NumericComponent 减去 buffAmount
```

## 5. 护盾机制

护盾通过独立字段 `ShieldCurrentAmount` 管理，**不修改 MaxHp**：

```
伤害到达 → BattleUnit.TakeDamage(damage)
  → 检查 ShieldComponent.IsActive
  → shieldComp.AbsorbDamage(damage) 返回穿透伤害
  → 穿透伤害 > 0 时才扣 HP
```

支持叠加（增加剩余护盾量+刷新时长），到期自动清除。

## 6. BuffConfig 配置字段

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 效果ID |
| EffectType | int | 效果类型（1-11） |
| FormulaType | int | 公式类型（1=攻防公式，2=固定值） |
| BaseValue | float | 基础值（伤害/治疗量/减速比例/护盾量等） |
| RatioAtk | float | 攻击系数（公式类型1时使用） |
| RatioDef | float | 防御系数（公式类型1时使用） |
| MinValue | int | 最小值 |
| MaxValue | int | 最大值（0=无上限） |
| Duration | int | 持续时间(ms)，0=即时效果 |
| TickInterval | int | tick间隔(ms)，DOT用 |
| MaxStack | int | 最大叠层数 |
| CanCritical | bool | 是否可暴击（预留） |
| ApplyTargetFilter | string | 目标过滤扩展（预留） |
| Desc | string | 描述 |

## 7. 伤害公式

```
FormulaType=1 (攻防公式):
  damage = BaseValue + attack * RatioAtk - defense * RatioDef
  damage = clamp(damage, MinValue, MaxValue)

FormulaType=2 (固定值):
  damage = BaseValue
```

## 8. 配置约定

### BuffConfig ID 分配规则

| 范围 | EffectType | 示例 |
|------|-----------|------|
| 51xxx | Damage | 51001 基础物理伤害, 51011 玩家割草版 |
| 52xxx | Freeze | 52001 冻结0.5秒, 52005 冻结3秒 |
| 53xxx | Knockback | 53001 击退1米, 53003 击退3米 |
| 54xxx | Heal | 54001 治疗100HP |
| 55xxx | Stun | 55001 眩晕0.5秒 |
| 56xxx | SlowDown | 56001 减速30% |
| 57xxx | LifeSteal | 57001 吸血 |
| 58xxx | Shield | 58001 护盾200 |
| 59xxx | AttackBuff | 59001 增攻50 |
| 510xxx | DefenseBuff | 510001 增防30 |
| 511xxx | DOT | 511001 毒伤 |

### BuffGroupConfig ID 分配

从 61001 开始，每个效果组包含一个或多个 EffectId。

## 9. 源文件索引

### Model 层
- `Server/Model/Demo/Battle/EffectType.cs` — 效果类型枚举
- `Server/Model/Demo/Battle/BuffEntity.cs` — Buff运行时实体
- `Server/Model/Demo/Battle/BuffComponent.cs` — Buff管理组件
- `Server/Model/Demo/Battle/BattleEvents.cs` — BuffExecuteEvent、BuffRemoveEvent 等
- `Server/Model/Demo/Battle/FreezeComponent.cs` — 冻结/眩晕状态组件
- `Server/Model/Demo/Battle/SlowDownComponent.cs` — 减速状态组件
- `Server/Model/Demo/Battle/ShieldComponent.cs` — 护盾组件

### Hotfix 层
- `Server/Hotfix/Demo/Battle/BattleSkillHelper.cs` — ApplyEffects 主入口
- `Server/Hotfix/Demo/Battle/BuffComponentSystem.cs` — Buff生命周期管理
- `Server/Hotfix/Demo/Battle/Event/BuffExecuteEvent_Handler.cs` — 持续效果tick处理
- `Server/Hotfix/Demo/Battle/Event/BuffRemoveEvent_Handler.cs` — 过期属性还原
- `Server/Hotfix/Demo/Battle/FreezeComponentSystem.cs` — 冻结逻辑
- `Server/Hotfix/Demo/Battle/SlowDownComponentSystem.cs` — 减速逻辑
- `Server/Hotfix/Demo/Battle/ShieldComponentSystem.cs` — 护盾逻辑

### 配置
- `Config/Excel/GameConfig/BuffConfig.xlsx` — 效果配置表
- `Config/Excel/GameConfig/BuffGroupConfig.xlsx` — 效果组配置表
