# 局内成长 HUD - 主美资产拆分规范

## 文档元信息

- 文档类型：Checklist / UI 资产拆分规范
- 状态：资源需求
- 使用 Workflow：`.Codex/workflows/ui-prefab-workflow.md`
- 来源：`.Codex/plans/局内器件构筑系统-美术需求清单.md`
- 上游文档：
  - `.Codex/plans/局内器件构筑系统-美术需求清单.md`
- 下游文档：待补充 HUD Prefab Spec
- 最近整理：2026-06-04

## 1. 定位

本文档基于《局内成长美术需求清单》和当前确认的 HUD 效果图方向，定义第一版局内成长 HUD 的资产拆分方式。

目标不是把效果图矩形裁开，而是从主美和程序装配角度确定：

- 哪些元素应该成为独立 UI 资产
- 哪些元素应该由程序动态渲染
- 哪些元素需要支持拉伸、填充、状态替换
- 哪些视觉内容只属于效果图，不应作为独立切图

## 2. 核心原则

- 不切整块 HUD 大图。
- 不把文本、数值、等级烘进图片。
- 不把状态条切成死图，必须拆底板、填充和外框。
- 不把成长项切成整张卡，必须拆图标、品质框、等级角标和状态叠层。
- 背景舞台不从 HUD 效果图里裁，背景应按 `L0-L5` 单独生产。
- 第一版优先保证程序能动态装配，细节装饰宁可少，不可死。

## 3. HUD 结构

当前局内成长 HUD 按三组装配：

```text
[左侧角色状态] [中部局内钱币 + 候选进度] [右侧代表成长摘要]
```

三组之间保留呼吸感，不使用整条厚底板。

## 4. 左侧角色状态资产

### 4.1 头像组件

| 资产名 | 用途 | 类型 | 说明 |
| --- | --- | --- | --- |
| `hud_avatar_frame` | 头像外框 | 固定 Sprite | 奶油系圆形/徽章框 |
| `hud_avatar_backplate` | 头像底托 | 固定 Sprite | 香草色轻量底托，可带浅金或浅粉小装饰 |
| `hud_avatar_mask` | 头像遮罩 | Mask | 程序放角色头像 |
| `hud_avatar_trait_badge` | 角色特性小标 | 可选 Sprite | 后续表现角色特性 |

头像本体由角色资源或头像图动态指定，不应烘进 HUD 图。

### 4.2 HP / 能量 / 经验条

每条状态条都拆为：

| 资产名 | 用途 | 类型 | 程序控制 |
| --- | --- | --- | --- |
| `hud_bar_hp_bg` | HP 底板 | 9-slice 或 fixed | 否 |
| `hud_bar_hp_fill` | HP 填充 | horizontal fill | 是 |
| `hud_bar_hp_frame` | HP 外框 | 9-slice 或 fixed | 否 |
| `hud_bar_energy_bg` | 能量底板 | 9-slice 或 fixed | 否 |
| `hud_bar_energy_fill` | 能量填充 | horizontal fill | 是 |
| `hud_bar_energy_frame` | 能量外框 | 9-slice 或 fixed | 否 |
| `hud_bar_exp_bg` | 经验底板 | 9-slice 或 fixed | 否 |
| `hud_bar_exp_fill` | 经验填充 | horizontal fill | 是 |
| `hud_bar_exp_frame` | 经验外框 | 9-slice 或 fixed | 否 |
| `hud_level_badge` | 等级徽章底 | fixed | 文本由程序渲染 |

要求：

- 填充条只负责颜色和材质，不带固定数值。
- 等级数字由 TMP 或项目 UI 文本渲染。
- HP、能量、经验的外框风格一致，但颜色区分明确。

## 5. 中部局内钱币与候选进度

### 5.1 钱币组件

| 资产名 | 用途 | 类型 | 程序控制 |
| --- | --- | --- | --- |
| `hud_coin_icon` | 局内钱币图标 | fixed Sprite | 否 |
| `hud_coin_label_bg` | 数字底牌 | 9-slice 或 fixed | 否 |
| `hud_coin_gain_fx` | 钱币增加提示 | 可选 Sprite/特效 | 是 |

要求：

- 金币数字由程序文本渲染，不能烘进图片。
- 金币图标风格为王国铸币 / 远征金币，不做现代商城币。
- 钱币区域是中部核心信息之一，但视觉权重低于角色状态。

### 5.2 候选进度组件

候选进度对应程序里的“质量权重累积 / 下一次候选阈值”。

| 资产名 | 用途 | 类型 | 程序控制 |
| --- | --- | --- | --- |
| `hud_candidate_progress_bg` | 进度底条 | 9-slice 或 fixed | 否 |
| `hud_candidate_progress_fill` | 进度填充 | horizontal fill | 是 |
| `hud_candidate_progress_frame` | 进度外框 | fixed | 否 |
| `hud_candidate_progress_node` | 刻度节点 | repeated/fixed | 可选 |
| `hud_candidate_ready_marker` | 可触发提示 | optional Sprite | 是 |

要求：

- 视觉可以是纸带、星点、月光刻度或远征记录条。
- 不应被误读成技能冷却或经验条。
- 若设计上不希望暴露精确进度，可只显示弱化刻度和 ready 状态。

## 6. 右侧代表成长摘要

右侧不是装备栏，也不是固定三槽。程序上建议以 `BuildSummary` 或 `RepresentativeGrowthItems` 理解。

### 6.1 单个代表成长项拆分

每个代表成长项由以下层组成：

| 资产名 | 用途 | 类型 | 程序控制 |
| --- | --- | --- | --- |
| `hud_growth_badge_base` | 小物底托 | fixed Sprite | 否 |
| `hud_growth_quality_common` | 普通品质框 | overlay Sprite | 按品质切换 |
| `hud_growth_quality_fine` | 精良品质框 | overlay Sprite | 按品质切换 |
| `hud_growth_quality_rare` | 稀有品质框 | overlay Sprite | 按品质切换 |
| `hud_growth_icon_mask` | 图标遮罩 | Mask | 程序放图标 |
| `hud_growth_level_tag` | 等级角标底 | fixed Sprite | 文本由程序渲染 |
| `hud_growth_new_marker` | 新获得标记 | optional Sprite | 状态控制 |
| `hud_growth_selected_marker` | 当前高亮标记 | optional Sprite | 状态控制 |

要求：

- `Icon + Quality + Level` 是右侧展示的最小信息单元。
- 道具图标由成长项配置指定。
- 等级数字由程序文本渲染。
- 品质框颜色遵守普通 / 精良 / 稀有三档。
- 不做等距方格槽位，不画成传统装备格。

### 6.2 多个代表项的布局规则

默认展示规则：

```text
最多展示 3 个代表成长项
第 1 个为主代表项，尺寸 100%
第 2 个为次代表项，尺寸 85%-90%
第 3 个为补充代表项，尺寸 75%-85%，可半叠放
剩余数量用 +N 小纸签显示
```

需要的资产：

| 资产名 | 用途 | 类型 | 程序控制 |
| --- | --- | --- | --- |
| `hud_growth_stack_shadow` | 叠放阴影 | fixed Sprite | 否 |
| `hud_growth_more_tag` | `+N` 小纸签底 | fixed Sprite | 文本由程序渲染 |
| `hud_growth_empty_hint` | 弱占位 | optional Sprite | 无代表项时显示 |

布局要求：

- 三个代表项不能等距、等尺寸、等框。
- 通过大小、层级、半遮挡表达“摘要”而不是“槽位”。
- 右侧整体应比角色状态区弱，不抢战斗主体。

## 7. 候选弹窗资产边界

候选弹窗不在常驻 HUD 中显示，但第一版最低资产集需要预留。

弹窗组件建议拆为：

| 资产名 | 用途 | 类型 |
| --- | --- | --- |
| `growth_popup_bg` | 弹窗底板 | 9-slice |
| `growth_popup_title_plate` | 标题牌 | fixed |
| `growth_card_large_base` | 大卡底板 | fixed/9-slice |
| `growth_card_icon_mask` | 大卡图标遮罩 | Mask |
| `growth_compare_row_bg` | 对比行底 | 9-slice |
| `growth_arrow_up` | 上升箭头 | fixed |
| `growth_arrow_down` | 下降箭头 | fixed |
| `growth_arrow_neutral` | 中性标记 | fixed |
| `growth_recycle_coin_icon` | 回收金币图标 | fixed |
| `growth_btn_primary` | 主要按钮 | 9-slice |
| `growth_btn_secondary` | 次要按钮 | 9-slice |

要求：

- 弹窗不是当前 HUD 的一部分，不能为了效果图把弹窗内容烘进 HUD。
- 所有文本、数值、旧值/新值由程序渲染。

## 8. 道具图标生产规则

第一版图标不做“功能模块感”，应做成局内成长选择的视觉承载。

图标建议分三组生产，但不要在图标上硬写分类：

- 旅途小物：月光钥匙、旧地图角、布袋护符、旅行铃铛。
- 王国 / 圣堂礼器：圣堂小瓶、蜡封纹章、折页诗篇、旧王冠碎片。
- 屠龙纪念物：龙鳞护符、灰烬瓶、骨质小章、誓印碎片。

每个图标交付：

- `icon_growth_<name>.png`
- 透明背景
- 主体完整
- 小尺寸下轮廓清楚
- 不带品质框、不带等级、不带文字

## 9. 不应切出的内容

以下内容不应从效果图中裁成最终资源：

- 完整底部 HUD 大图
- 带数字的金币牌
- 带等级数字的成长项
- 带固定填充比例的 HP / 能量 / 经验条
- 带背景的矩形裁块
- 舞台背景整图
- 已经合成阴影、文字、图标、边框的整张成长卡

这些只能作为效果评审图或参考图。

## 10. 第一批透明组件建议

第一批真正可进 Unity 的透明组件应优先生成：

1. `hud_avatar_frame`
2. `hud_bar_hp_bg / fill / frame`
3. `hud_bar_energy_bg / fill / frame`
4. `hud_bar_exp_bg / fill / frame`
5. `hud_coin_icon`
6. `hud_coin_label_bg`
7. `hud_candidate_progress_bg / fill / frame`
8. `hud_growth_badge_base`
9. `hud_growth_quality_common / fine / rare`
10. `hud_growth_level_tag`
11. `hud_growth_more_tag`
12. 9 个起步道具图标

## 11. 验收标准

- 程序能动态替换图标、品质、等级、数值和进度。
- HUD 看起来像局内成长摘要，不像传统装备栏。
- 右侧代表项能表达 `Icon + Quality + Level`，但不产生固定三槽感。
- 组件拆分后仍保持奶油系轻幻想月夜战斗 UI 风格。
- 任何数值变化都不需要重新出图。
