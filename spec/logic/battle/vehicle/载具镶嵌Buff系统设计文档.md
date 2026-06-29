# 载具镶嵌 Buff 系统

> 版本: 2.1 | 日期: 2026-04-14
>
> 核心理念：载具是发射器，Buff 碎片是弹药。玩家自由搭配弹药，打出自己的战斗风格。

---

## 一、设计目标

### 1.1 现状问题

当前效果链路为固定组合：

```
UnitCombatConfig → SkillConfig → BuffGroupConfig → BuffConfig[]
```

`BuffGroupConfig.EffectIds[]` 在 Excel 中写死，玩家获得的是整组效果，无法拆开重组。

### 1.2 目标

- **效果粒度自由**：掉落单个 BuffConfig（如"减速30%"、"中毒5秒"），而非固定组合
- **玩家自主搭配**：将获得的 Buff 镶嵌到载具上，每次发射都携带这些效果
- **重复命中刷新**：同一 Buff 重复命中目标时刷新持续时间
- **兼容现有架构**：复用 BuffConfig 定义和效果执行逻辑，不引入新执行路径

---

## 二、概念定义

| 概念 | 说明 |
|------|------|
| **载具（Vehicle）** | 纯发射器。有一个发射CD（多久打一次）和若干 Buff 镶嵌槽。载具本身不提供任何属性加成 |
| **载具 CD** | 发射节奏，即多久触发一次攻击。例：1000ms = 每秒打一次 |
| **Buff 镶嵌槽** | 载具上的槽位，每个槽位放一个 Buff 碎片 |
| **Buff 碎片（BuffShard）** | 掉落物，对应一个 BuffConfig Id，镶嵌后随载具发射命中目标时生效 |

### 核心模型

```
载具 = 发射器 + Buff容器

  ┌─────────────────────────────────────┐
  │  载具 (VehicleId = 100)              │
  │                                     │
  │  发射CD: 1000ms (每秒打一次)          │
  │                                     │
  │  镶嵌槽:                             │
  │    [槽0: 伤害]     ← BuffConfig Id  │
  │    [槽1: 减速30%]  ← BuffConfig Id  │
  │    [槽2: 中毒]     ← BuffConfig Id  │
  │    [槽4: 空]                         │
  │                                     │
  └─────────────────────────────────────┘
         │
         │ 载具CD到期 → 选取目标 → 命中
         ▼
  ┌─────────────────────────────────────┐
  │  目标身上的效果 (按来源载具区分)       │
  │                                     │
  │  伤害: 即时生效                       │
  │  减速30%: 层数+1，刷新为配置持续时长    │
  │  中毒@载具100: 层数+1，刷新持续时长    │
  │  中毒@载具200: (如果另一个载具也打了)  │
  │              独立计算层数和持续时间     │
  │                                     │
  └─────────────────────────────────────┘
```

### Buff 重复命中规则

**三条核心规则：叠加层数、刷新持续时间、按来源载具分别计算。**

#### 规则 1：叠加层数 + 刷新持续时间

同一载具重复命中同一目标时：层数 +1，持续时间刷新为 BuffConfig 配置的 Duration。

```
载具A(CD=1秒) 镶嵌了 [中毒(持续5秒)]

0s  → 命中目标 → 中毒 层数1，持续到 5s
1s  → 再次命中 → 中毒 层数2，持续到 6s（刷新为配置的5秒）
2s  → 再次命中 → 中毒 层数3，持续到 7s
3s  → 不再命中 → 中毒在 7s 到期消失（层数清零）
```

#### 规则 2：同类型不同持续时间 → 取较长者

同一载具镶嵌了两个相同 EffectType 但 Duration 不同的 Buff（如中毒3秒 + 中毒5秒），目标身上取持续时间长的那个。

```
载具A 镶嵌了 [中毒(持续3秒), 中毒(持续5秒)]

命中目标 → 目标身上只有一个中毒效果，Duration = 5秒（取较长者）
```

#### 规则 3：不同载具分别计算

不同载具触发的相同类型 Buff 各自独立，分别计算层数和持续时间。

```
载具A(CD=1秒) 镶嵌 [中毒(持续5秒)]
载具B(CD=2秒) 镶嵌 [中毒(持续3秒)]

0s → A命中 → 目标身上: [中毒@A, 层数1, 持续到5s]
1s → A命中 → 目标身上: [中毒@A, 层数2, 持续到6s]
2s → A命中 + B命中 → 目标身上:
                      [中毒@A, 层数3, 持续到7s]
                      [中毒@B, 层数1, 持续到5s]  ← 独立计算
3s → A命中 → 目标身上:
             [中毒@A, 层数4, 持续到8s]
             [中毒@B, 层数1, 持续到5s]  ← B没触发，不刷新

两个中毒效果各自 tick 各自的伤害，互不影响。
```

**目标身上的 BuffEntity 需要记录来源载具 ID，用于区分不同载具的同类型 Buff。**

---

## 三、数据模型

### 3.1 载具运行时数据（新增）

```
VehicleData (持久化到玩家数据)
├── VehicleId: long              // 载具实例唯一ID
├── VehicleConfigId: int         // 载具模板配置ID
├── Level: int                   // 载具等级，重复获得同模板载具时提升
├── BuffSlotCount: int           // Buff槽位数量，来自 EmitterConfig
├── BaseDamage: float            // 发射器白值基础伤害
├── WhiteAttackRatio: float      // 发射器白值攻击力系数
├── SlottedBuffIds: List<int>    // 已镶嵌的 BuffGroupConfig.Id 列表，下标就是槽位位置
└── State: VehicleState          // 装备状态
```

### 3.2 发射器配置表（EmitterConfig.xlsx）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 发射器模板ID |
| Name | string | 载具名称 |
| Description | string | 描述 |
| CooldownMs | int | 发射CD（毫秒），即多久触发一次攻击 |
| TargetingConfigId | int | 目标与射程配置ID |
| UpgradeConfigId | int | 升级方案ID，对应 `EmitterUpgradeConfig.UpgradeConfigId` |
| BuffSlotCount | int | Buff槽位数量，决定该发射器最多可装配几个 Buff |
| BaseDamage | float | 发射器白值基础伤害 |
| WhiteAttackRatio | float | 发射器白值攻击力系数 |

发射器未镶嵌任何 Buff 时，仍会结算白值伤害：

```
WhiteDamage = max(0, BaseDamage + Attacker.Attack * WhiteAttackRatio - Target.Defense)
FinalWhiteDamage = floor(WhiteDamage * WhiteDamageMultiplier)
```

`BaseDamage / WhiteAttackRatio` 都为 0 时，不结算白值伤害。

### 3.3 发射器升级配置表（EmitterUpgradeConfig.xlsx）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 升级等级配置ID |
| UpgradeConfigId | int | 升级方案ID |
| Level | int | 等级 |
| Code | string | 代码名 |
| Name | text | 显示名称 |
| CooldownReduceMs | int | 当前等级累计减少的发射 CD 毫秒数 |
| RangeAdd | float | 当前等级累计增加的攻击射程 |
| WhiteDamageMultiplier | float | 当前等级白值伤害倍率 |
| Desc | string | 描述 |

同一个 `UpgradeConfigId` 下按 `Level` 配多行，例如 `12001 Lv.1~Lv.5`。重复获得发射器时直接查当前等级行，不再额外维护最大等级字段或每级增量字段。

> **注意**：载具本身不提供任何属性加成（不加攻击力、防御力、速度）。所有效果都来自镶嵌的 Buff 碎片。

### 3.4 载具升级收益

重复获得同 `VehicleConfigId` 的载具时，不再创建重复实例，而是提升已有实例的 `Level`，最高不超过同一 `UpgradeConfigId` 下已配置的最大 `Level`。

升级后战斗运行时读取 `UpgradeConfigId + Level` 对应行，并按以下规则刷新载具参数：

```
AttackCooldownMs = max(100, CooldownMs - CurrentLevel.CooldownReduceMs)
AttackRange = BaseRange + CurrentLevel.RangeAdd
WhiteDamageMultiplier = max(0.1, CurrentLevel.WhiteDamageMultiplier)
```

`WhiteDamageMultiplier` 只影响发射器白值伤害，不影响镶嵌 Buff 的伤害或 DOT。

### 3.5 Buff 碎片

**Buff 碎片的本质就是一个 BuffConfig Id**，不需要单独的配置表。

掉落时直接掉落 BuffConfig Id，镶嵌时存储该 Id，战斗时按该 Id 读取 BuffConfig 执行效果。

如需碎片自身的展示属性（名称、图标、品质），可新增 `BuffShardConfig.xlsx`：

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 碎片ID（等于对应的 BuffConfig Id） |
| Name | string | 碎片显示名称 |
| Icon | string | 图标资源路径 |
| Rarity | int | 品质 |
| MaxStack | int | 最大堆叠 |

### 3.6 玩家持有数据（新增组件）

```
VehicleComponent (挂载在 Unit 上)
├── EquippedVehicleId: long            // 当前装备的载具实例ID
├── OwnedVehicles: List<VehicleData>   // 拥有的所有载具
└── OwnedShards: Dictionary<int, int>  // 拥有的碎片 <BuffConfigId, 数量>
```

---

## 四、核心流程

### 4.1 载具镶嵌

```
玩家打开镶嵌界面
  │
  ├── 显示当前装备载具的槽位
  ├── 显示背包中的 Buff 碎片
  │
  ├── 拖拽碎片到空槽位
  │   ├── 检查槽位是否为空
  │   ├── 扣除碎片数量 (-1)
  │   ├── 写入 VehicleData.SlottedBuffIds[slotIndex] = buffConfigId
  │   └── 持久化
  │
  └── 拖拽已镶嵌碎片到背包
      ├── 移除 VehicleData.SlottedBuffIds[slotIndex] = 0
      ├── 归还碎片数量 (+1)
      └── 持久化
```

### 4.2 战斗中攻击触发

**载具触发循环**：每次载具CD到期，选取目标并施加所有镶嵌的 Buff 效果。

```
载具触发循环（每 AttackCooldownMs）
  │
  ├── 载具CD就绪
  │
  ├── 选取目标:
  │   └── AI 锁定最近的敌方目标（同现有逻辑）
  │
  ├── 检查射程:
  │   ├── 在射程内 → 继续
  │   └── 超出射程 → AI 移动接近（同现有停移逻辑）
  │
  ├── 对目标施加所有镶嵌的 Buff:
  │   foreach buffId in VehicleData.SlottedBuffIds:
  │       BuffConfig config = BuffConfigCategory.Get(buffId)
  │       // 查找目标身上同类型 + 同来源载具的 Buff
  │       // 找到 → 层数+1，刷新持续时间为 config.Duration
  │       // 未找到 → 新建 BuffEntity，层数=1，记录来源载具ID
  │       // 同载具同类型但不同Duration → 取较长者
  │       switch (config.EffectType):
  │           case Damage:     → 即时伤害
  │           case SlowDown:   → 减速（叠加层数，刷新时长）
  │           case DOT:        → 持续伤害（叠加层数，刷新时长）
  │           case Stun:       → 眩晕（刷新时长）
  │           case Heal:       → 治疗自己
  │           ...
  │
  └── 进入载具CD
```

**命中示例**：

```
载具CD = 1秒，镶嵌 [伤害, 减速30%(持续2秒), 中毒(持续5秒)]

第1次命中: 伤害 + 减速(层数1, 2秒) + 中毒(层数1, 5秒)
第2次命中: 伤害 + 减速(层数2, 刷新2秒) + 中毒(层数2, 刷新5秒)
第3次命中: 伤害 + 减速(层数3, 刷新2秒) + 中毒(层数3, 刷新5秒)
停止命中后: 各效果到期消失，层数清零
```

### 4.3 装备/卸下载具

```
装备载具
  │
  ├── VehicleComponent.EquippedVehicleId = vehicleData.VehicleId
  ├── 读取 VehicleConfig 的 AttackCooldownMs，设置发射节奏
  ├── 读取 VehicleConfig 的 AttackRange，设置攻击射程
  └── 战斗中按载具CD + 镶嵌Buff自动攻击

卸下载具
  │
  ├── VehicleComponent.EquippedVehicleId = 0
  └── 恢复默认战斗行为
```

---

## 五、与现有架构的关系

### 5.1 模型对比

```
现有（固定组合）:
  UnitCombatConfig → SkillConfig → BuffGroupConfig → BuffConfig[]
  效果组合在配置表中写死

新增（载具动态组合）:
  VehicleConfig → SlottedBuffIds[] → BuffConfig (逐个)
  效果组合由玩家在运行时决定
```

**关键**：不引入新的效果执行路径。镶嵌的 Buff 和固定 Buff 走同一套 `switch (EffectType)` 分发逻辑。

### 5.2 兼容性

| 场景 | 处理方式 |
|------|----------|
| 怪物/NPC | 仍使用固定 UnitCombatConfig + Skill + BuffGroup，无变化 |
| 玩家无载具 | 回退到默认战斗行为，与现有一致 |
| 玩家有载具但无镶嵌 | 载具触发但不产生任何效果（空打） |
| 玩家有载具有镶嵌 | 载具每次触发时对所有命中目标施加镶嵌的 Buff |
| 重复获得同模板载具 | 不新增重复实例，提升已有 `VehicleData.Level`，刷新 CD / 射程 / 伤害倍率，并保留原镶嵌槽 |

### 5.3 离线模式

离线模式与联网模式核心逻辑一致：

| 环节 | 联网模式 | 离线模式 |
|------|----------|----------|
| 镶嵌操作 | 客户端发请求，服务端验证并持久化 | 客户端本地直接操作 |
| 效果执行 | 服务端 ApplyEffects() | 客户端本地模拟 |
| 数据存储 | 数据库（MongoDB） | 本地内存/存档 |

---

## 六、数据流图

```
┌─────────────────────────────────────────────────────────────┐
│  背包层                                                      │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ 掉落 Buff碎片 │    │ 载具实例列表  │    │ 镶嵌操作     │   │
│  │ (BuffConfigId)│───▶│ (VehicleData)│◀───│ (Buff槽位)   │   │
│  └──────────────┘    └──────┬───────┘    └──────────────┘   │
│                             │                                │
└─────────────────────────────┼────────────────────────────────┘
                              │ 装备载具
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  战斗层                                                      │
│                                                              │
│  载具触发循环 (每 AttackCooldownMs)                           │
│    │                                                         │
│    ├─ AI 选取目标（最近的敌方）                                │
│    │                                                         │
│    ├─ 检查射程（在射程内才触发）                               │
│    │                                                         │
│    └─ 命中目标，施加所有镶嵌 Buff:                             │
│        foreach buffId in SlottedBuffIds:                      │
│            BuffConfig config = Get(buffId)                    │
│            ApplyEffect(caster, target, config)                │
│            ├─ 即时效果: 直接执行                               │
│            └─ 持续效果: 挂载或刷新持续时间                      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 七、实现步骤（按优先级排序）

### Phase 1：数据模型（服务端 Model 层）

1. 新增 `VehicleConfig.xlsx` 配置表及 Luban 生成
2. 新增 `VehicleData` 数据结构（可序列化）
3. 新增 `VehicleComponent` 组件（挂载在 Unit 上）
4. 新增 `BuffShardConfig.xlsx`（可选，Phase 1 可复用 BuffConfig.Desc）

### Phase 2：镶嵌操作

1. 新增镶嵌/卸下 RPC（`C2M_VehicleSlotOp`）
2. 实现镶嵌逻辑（验证、扣除、持久化）
3. 客户端镶嵌界面 UI

### Phase 3：战斗集成

1. 新增载具触发组件（`VehicleAttackComponent`），管理载具CD + 目标选取 + Buff 施加
2. 修改效果执行逻辑，支持"重复命中刷新持续时长"
3. 玩家进入战斗时，从 `VehicleComponent` 读取载具数据
4. 离线模式同步实现

### Phase 4：掉落系统

1. 配置掉落表（关卡/怪物 → Buff 碎片）
2. 战斗结算时生成掉落
3. 玩家拾取/自动获取碎片

---

## 八、设计原则

1. **载具是纯发射器**：不提供属性加成，没有 Skill 插槽，只管"多久打一次"和"带什么 Buff"
2. **BuffConfig 是唯一的效果定义**：无论配置表还是镶嵌，效果执行都读 BuffConfig
3. **叠加层数 + 刷新持续时间**：同一载具重复命中，层数+1，持续时间刷新为配置值
4. **按来源载具分别计算**：不同载具的同类型 Buff 各自独立维护层数和持续时间
5. **同载具同类型取较长持续时间**：一个载具镶嵌了两个不同 Duration 的同类型 Buff，取长者
6. **向后兼容**：怪物/NPC 仍走固定配置，无载具玩家行为不变
7. **载具是可选层**：没有载具时整个系统透明，不影响现有功能
8. **镶嵌是持久化操作**：镶嵌结果存入数据库，跨战斗保留

---

**文档版本**: v2.1
**创建日期**: 2026-04-14
