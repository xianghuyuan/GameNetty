# 发射器效果包结构改造

## 需求概述

当前发射器槽位直接存放 `BuffGroupConfig.Id`，只能表达命中目标后的 Buff 组合，无法表达 `CD减少50%` 这类发射器自身属性修饰。改造为槽位存放 `EmitterEffectPackConfig.Id`，效果包可包含单个或多个 `EmitterEffectConfig`，每个效果可修饰发射器属性或引用一个 `BuffGroupConfig`。

## 配置表设计

### EmitterEffectConfig

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | int | 效果 ID |
| Name | text | 展示名称 |
| EffectKind | int | 1=发射器属性修饰，2=命中时应用 BuffGroup |
| TargetStat | int | EffectKind=1 时生效；1=CooldownMs，2=Range，3=WhiteDamage，4=BaseDamage |
| ModifyOp | int | 1=Add，2=Multiply，3=Override |
| Value | float | 修饰值 |
| BuffGroupId | int#ref=BuffGroupConfigCategory | EffectKind=2 时引用的 BuffGroup |
| Desc | string | 描述 |

### EmitterEffectPackConfig

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | int | 效果包 ID |
| Name | text | 展示名称 |
| EffectIds | int[]#ref=EmitterEffectConfigCategory | 效果列表 |
| Desc | string | 描述 |

### BuffGroupConfig 补充

新增纯命中效果组，避免效果包在白值伤害之外重复附加一份伤害：

| Id | BuffIds | 说明 |
| --- | --- | --- |
| 61101 | 53001 | 纯击退1米 |
| 61102 | 52001 | 纯冻结0.5秒 |
| 61103 | 56001 | 纯减速30% |
| 61104 | 511001 | 纯毒伤DOT |

## 协议设计

不新增协议。当前改造只影响客户端调试发射器 UI 和本地战斗发射器运行时数据。

## 服务端代码

新增生成配置类由 Luban 生成：`EmitterEffectConfig`、`EmitterEffectPackConfig` 及 Category。

## 客户端代码

- `VehicleData`：槽位语义改为 `SlottedEffectPackIds`，保留旧 `SlottedBuffIds` 兼容现有代码。
- `BattleAttackRuntime`：增加 `EffectPackIds`，保留 `BuffGroupIds` 作为命中执行后的展开结果。
- `BattleAttackComponentSystem`：同步发射器时根据效果包计算最终 CD、射程、伤害倍率和 BuffGroup 列表。
- `BattleBuffAddPanelWidget` / `BattleBuffOptionItemWidget` / `BattleMainWindow`：列表和点击参数改为效果包配置。
- `ConfigHelper` / `Tables`：新增效果表访问入口。

## 文件清单

- `Config/Excel/GameConfig/__tables__.xlsx`
- `Config/Excel/GameConfig/EmitterEffectConfig.xlsx`
- `Config/Excel/GameConfig/EmitterEffectPackConfig.xlsx`
- `Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config/*EmitterEffect*.cs`
- `Server/Model/Generate/Config/*EmitterEffect*.cs`
- `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Vehicle/VehicleData.cs`
- `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/BattleAttackComponent.cs`
- `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/BattleAttackComponentSystem.cs`
- `Unity/Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/*`
