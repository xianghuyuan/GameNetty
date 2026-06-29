# Buff-Skill-Weapon 效果体系设计文档

> 版本: 1.0 | 日期: 2026-04-11
>
> 本文档定义战斗系统中效果体系的核心设计理念、分层职责和扩展规范。

---

## 一、核心理念：Buff 是效果原子

在 GameNetty 中，**Buff 不是传统意义上的"增益/减益状态"**，而是**最小的效果单元（Atom）**。

"Buff"这个命名仅表示它是效果体系的最底层构建块，与"持续/即时"、"正面/负面"、"作用自己/作用目标"无关。

一个 Buff 就是一个**不可再分的效果描述**：做什么事、用什么公式、参数是多少。

### 容易混淆的概念澄清

| 常见误解 | 正确理解 |
|----------|----------|
| Buff = 持续性增益状态 | Buff = 最小效果单元，可以是即时或持续的 |
| Buff 只作用于目标 | Buff 可以作用于目标、自己、或全场任意单位 |
| Buff = 正面效果（增益） | Buff 不区分正面/负面，伤害也是 Buff |
| 吸血、击杀触发是 Skill 层行为 | 吸血、击杀触发等都是 Buff，由 BuffGroup 组合进 Skill |

**判断标准：只要是一个独立的、不可再分的效果，就是 Buff。**

---

## 二、四层组合架构

```
┌─────────────────────────────────────────────────────────┐
│  Weapon (武器)                                           │
│  对应配置: UnitCombatConfig                               │
│  职责: 定义一把武器拥有哪些技能                              │
│  ┌─────────────────────────────────────────────────────┐ │
│  │  Skill (技能)                                        │ │
│  │  对应配置: SkillConfig                                │ │
│  │  职责: 定义施法参数 + 绑定一个效果组                     │ │
│  │  ┌─────────────────────────────────────────────────┐ │ │
│  │  │  BuffGroup (效果组)                               │ │ │
│  │  │  对应配置: BuffGroupConfig                        │ │ │
│  │  │  职责: 将多个 Buff 组合在一起                       │ │ │
│  │  │  ┌─────────────────────────────────────────────┐ │ │ │
│  │  │  │  Buff (效果原子)                               │ │ │ │
│  │  │  │  对应配置: BuffConfig                          │ │ │ │
│  │  │  │  职责: 最小效果单元，定义一个效果行为             │ │ │ │
│  │  │  └─────────────────────────────────────────────┘ │ │ │
│  │  └─────────────────────────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 各层职责定义

| 层级 | 配置表 | 职责 | 不负责 |
|------|--------|------|--------|
| **Buff** | BuffConfig | 定义一个效果原子：效果类型、公式、参数 | 不知道自己属于哪个技能 |
| **BuffGroup** | BuffGroupConfig | 组合多个 Buff 为一个效果组 | 不知道施法者是谁 |
| **Skill** | SkillConfig | 绑定效果组 + 施法参数（CD、射程、目标选取、投射物） | 不知道自己属于哪把武器 |
| **Weapon** | UnitCombatConfig | 组合多个 Skill（普攻 + 自动技能列表） | 不知道效果细节 |

**关键设计原则：每层只与相邻层交互，不跨层依赖。**

---

## 三、配置链路详解

### 3.1 数据引用关系（自底向上组合）

```
UnitCombatConfig (武器)
  ├── NormalAttackSkillId ─────→ SkillConfig (技能1)
  └── AutoSkillIds[] ──────────→ SkillConfig (技能2, 技能3, ...)

SkillConfig (技能)
  ├── BuffGroupId ─────────────→ BuffGroupConfig (效果组)
  ├── TargetingConfigId ───────→ SkillTargetingConfig (目标选取)
  ├── CooldownMs               (CD时间)
  ├── CastType                 (瞬发/投射物)
  └── ProjectileSpeed等        (投射物参数)

BuffGroupConfig (效果组)
  └── EffectIds[] ─────────────→ BuffConfig (效果原子1, 效果原子2, ...)

BuffConfig (效果原子)
  ├── EffectType               (效果类型枚举)
  ├── FormulaType              (计算公式)
  ├── BaseValue / RatioAtk / RatioDef  (参数)
  ├── Duration                 (持续时间，0=即时)
  └── TickInterval             (周期触发间隔)
```

### 3.2 一个完整的武器配置示例

以"冰霜吸血剑"为例：

```
UnitCombatConfig (冰霜吸血剑)
  ├── NormalAttackSkillId = 1001 (冰霜斩)
  └── AutoSkillIds[] = [1002]    (吸血打击)

SkillConfig 1001 (冰霜斩)
  ├── BuffGroupId = 301
  ├── CastType = 0 (瞬发)
  └── CooldownMs = 1000

SkillConfig 1002 (吸血打击)
  ├── BuffGroupId = 302
  ├── CastType = 0 (瞬发)
  └── CooldownMs = 3000

BuffGroupConfig 301 (冰霜斩效果组)
  └── EffectIds = [101, 601]  (伤害 + 减速)

BuffGroupConfig 302 (吸血打击效果组)
  └── EffectIds = [101, 701]  (伤害 + 吸血)

BuffConfig 101 (伤害)
  ├── EffectType = 1 (Damage)
  ├── FormulaType = 1 (ATK*Ratio - DEF*Ratio)
  ├── RatioAtk = 1.0, RatioDef = 0.5
  └── Duration = 0 (即时)

BuffConfig 601 (减速30%)
  ├── EffectType = 6 (SlowDown)
  ├── BaseValue = 0.3
  └── Duration = 2000 (持续2秒)

BuffConfig 701 (吸血20%)
  ├── EffectType = 7 (LifeSteal)
  └── BaseValue = 0.2
```

---

## 四、EffectType 效果类型定义

EffectType 是 BuffConfig 中的核心字段，决定一个 Buff 触发时的行为。

### 4.1 当前已实现

| EffectType | 值 | 时效 | 说明 |
|---|---|---|---|
| Damage | 1 | 即时 | 造成伤害，支持多种公式 |
| Freeze | 2 | 持续 | 冻结目标，停止移动和攻击 |
| Knockback | 3 | 即时 | 击退目标，强制位移 |
| Heal | 4 | 即时 | 恢复目标HP |
| Stun | 5 | 持续 | 眩晕目标 |

### 4.2 待扩展

| EffectType | 值 | 时效 | 说明 |
|---|---|---|---|
| SlowDown | 6 | 持续 | 减速目标 |
| LifeSteal | 7 | 即时 | 造成伤害时按比例回复自身HP |
| Shield | 8 | 持续 | 给目标添加护盾（临时HP） |
| AttackBuff | 9 | 持续 | 增加攻击力 |
| DefenseBuff | 10 | 持续 | 增加防御力 |
| DOT | 11 | 持续 | 持续伤害（每tick触发一次伤害） |
| Reflect | 12 | 持续 | 反伤（受伤时反弹伤害给攻击者） |
| Invincible | 13 | 持续 | 无敌（免疫伤害） |
| Mark | 14 | 持续 | 标记（被标记单位受到额外伤害） |
| Dispel | 15 | 即时 | 驱散目标身上的Buff |

### 4.3 EffectType 与时效的关系

**即时效果（Duration=0）**：在 `BattleSkillHelper.ApplyEffects()` 中直接执行一次，不创建 BuffEntity。

**持续效果（Duration>0）**：注册到 `BuffComponent`，创建 `BuffEntity`，由 `BuffComponentSystem` 的 100ms 心跳驱动 tick 和过期清理。

**注意**：同一个 EffectType 可以同时有即时和持续两种用法。例如 Damage 通常即时，但 DOT 本质上就是"持续的 Damage"，通过 `Duration>0` + `TickInterval>0` 实现。

---

## 五、Buff 的作用对象

Buff 的作用对象由 SkillTargetingConfig 的 `TargetCampRelation` 决定，不是 Buff 自身决定的：

| TargetCampRelation | 值 | 说明 | 示例 |
|---|---|---|---|
| Enemy | 1 | 作用敌方 | 伤害、减速、冻结、击退 |
| Ally | 2 | 作用友方（不含自身） | 群体治疗 |
| Self | 3 | 作用自身 | 吸血、护盾、增攻Buff |
| Any | 4 | 作用任意 | 全场效果 |

因此，吸血可以是一个普通的 Buff，通过 Skill 配置为"目标=Self"即可。

---

## 六、代码执行流程

### 6.1 即时 Buff 的执行路径

```
TryExecuteSkill(caster, skillId, targetId)
  │
  ├── SelectTargets()          ← 选取目标列表
  ├── BroadcastSkillCast()     ← 广播施法动画
  │
  └── ApplyEffects(caster, target, effectGroupConfig, skillConfig)
        │
        └── foreach buffId in effectGroupConfig.EffectIds:
              │
              └── switch (EffectType)           ← 按类型分发
                    ├── Damage → CalculateDamage → TakeDamage → BroadcastDamage
                    ├── Heal → target.Heal
                    ├── Knockback → Publish KnockbackEvent
                    ├── LifeSteal → Damage + caster.Heal
                    └── 持续类型 → buffComponent.AddBuff(...)
```

### 6.2 持续 Buff 的执行路径

```
BuffComponent.AddBuff(buffId, casterId, skillId, config, duration, tickInterval)
  │
  ├── 创建 BuffEntity 子实体
  ├── 设置 ExpireTime = 当前时间 + Duration
  └── 首次执行 → Publish BuffExecuteEvent

BuffComponentSystem.OnBuffTick (每100ms)
  │
  └── foreach BuffEntity:
        ├── 过期 → Dispose
        └── 到达NextTickTime → Publish BuffExecuteEvent
                                    │
                                    └── BuffExecuteEvent_Handler
                                          └── switch (EffectType) 执行对应逻辑
```

---

## 七、扩展新 Buff 的标准流程

当需要新增一种效果时，按以下步骤操作：

### Step 1: EffectType 枚举加值
```csharp
// Server/Model/Demo/Battle/EffectType.cs
public enum EffectType
{
    // ... 已有 ...
    NewEffect = 16,
}
```

### Step 2: Excel 配置
在 `BuffConfig.xlsx` 中新增行，EffectType 填新枚举值，填好参数。

在 `BuffGroupConfig.xlsx` 中组合新 Buff 到效果组。

### Step 3: 即时效果 → 在 BattleSkillHelper.ApplyEffects() 加 case
```csharp
case EffectType.NewEffect:
{
    // 效果逻辑
    break;
}
```

### Step 4: 持续效果 → 在 BuffExecuteEvent_Handler 加 case
```csharp
case (int)EffectType.NewEffect:
{
    // tick 效果逻辑
    break;
}
```

### Step 5: 如需新组件 → 新建 Component
例如减速需要 `SlowDownComponent`，按 ET 框架规范新建 Model + Hotfix 文件。

### Step 6: 客户端表现
在客户端对应的消息处理器中添加视觉效果。

---

## 八、设计原则总结

1. **Buff 是原子**：不可再分的效果单元，不区分正面/负面、即时/持续、作用自己/作用目标
2. **自底向上组合**：Buff → BuffGroup → Skill → Weapon，每层只引用下一层
3. **配置驱动**：新 Buff 类型只需加 Excel 行 + switch case，不改架构
4. **单一职责**：BuffConfig 只管"做什么"，SkillConfig 只管"怎么施放"，UnitCombatConfig 只管"有什么技能"
5. **效果与行为统一**：伤害、治疗、控制、增益、吸血等都是 Buff，没有特殊待遇

---

**文档版本**: v1.0
**创建日期**: 2026-04-11
**维护者**: 项目团队
