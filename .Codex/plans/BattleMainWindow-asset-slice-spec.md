# Asset Slice Spec: BattleMainWindow

## 文档元信息

- 文档类型：Asset Slice Spec
- 状态：草稿
- 使用 Workflow：`.Codex/workflows/ui-prefab-workflow.md`
- 上游文档：
  - `.Codex/plans/战斗界面-visual-mockup-spec.md`
  - `.Codex/artifacts/battle-ui/效果图/battle_main_cream_single_baseline_mockup.png`
- 下游文档：
  - 待生成 `BattleMainWindow-prefab-spec.md`
- 最近整理：2026-06-06

## 目标

从战斗 HUD 视觉稿和视觉规格中拆出第一版 Unity 可用 UI 素材清单。本文档只定义素材，不创建 Prefab。

原则：

- 不直接把整张视觉稿当 UI 使用。
- 不从视觉稿硬裁不可复用大图。
- 优先输出透明 PNG 小素材、可九宫格底图、可填充条和图标框。
- 文本、数字、等级、`+N` 等动态内容由程序文本渲染。

## 输出目录建议

```text
Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/
```

该目录是 Battle 图集源图目录，素材文件直接平铺在该目录下，不再按 Common / Bars / Emitter / Buff / Buttons / Debug 分子目录。

## 最小必需素材

第一批先输出支撑 Prefab 结构的核心素材。

| 素材名 | 路径 | 尺寸 | 类型 | 透明 | 九宫格 | 程序控制 | 说明 |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| `emitter_slot_bg` | `emitter_slot_bg.png` | 78 x 100 | 背板 | 是 | 否 | 否 | 单个发射器槽位底卡 |
| `emitter_icon_frame` | `emitter_icon_frame.png` | 48 x 48 | 图标框 | 是 | 否 | 否 | 发射器图标外框 |
| `buff_icon_frame` | `buff_icon_frame.png` | 22 x 22 | 图标框 | 是 | 否 | 否 | Buff 小图标外框 |
| `icon_button_bg` | `icon_button_bg.png` | 36 x 32 | 按钮底 | 是 | 是 | 否 | 暂停 / 设置 / Debug 共用按钮底 |
| `player_hp_frame` | `player_hp_frame.png` | 184 x 22 | 血条框 | 是 | 是 | 否 | 玩家 HP 条外框 |
| `boss_hp_frame` | `boss_hp_frame.png` | 366 x 24 | 血条框 | 是 | 是 | 否 | Boss HP 条外框 |
| `wave_plate` | `wave_plate.png` | 260 x 48 | 底牌 | 是 | 是 | 否 | 顶部波次底牌，文本程序渲染 |

## 第二批素材

| 素材名 | 路径 | 尺寸 | 类型 | 透明 | 九宫格 | 程序控制 | 说明 |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| `avatar_frame` | `avatar_frame.png` | 64 x 64 | 头像框 | 是 | 否 | 否 | 玩家头像外框 |
| `boss_name_plate` | `boss_name_plate.png` | 120 x 28 | 底牌 | 是 | 是 | 否 | Boss 名称底牌 |
| `emitter_level_tag` | `emitter_level_tag.png` | 26 x 18 | 角标底 | 是 | 否 | 文本 | 发射器等级角标底 |
| `emitter_more_tag` | `emitter_more_tag.png` | 26 x 22 | 角标底 | 是 | 否 | 文本 | 发射器溢出 `+N` |
| `buff_stack_tag` | `buff_stack_tag.png` | 18 x 14 | 角标底 | 是 | 否 | 文本 | Buff 层数角标底 |
| `buff_more_tag` | `buff_more_tag.png` | 22 x 20 | 角标底 | 是 | 否 | 文本 | Buff 溢出 `+N` |
| `debug_panel_bg` | `debug_panel_bg.png` | 210 x 118 | 面板底 | 是 | 是 | 否 | 开发调试入口背景 |
| `candidate_ready_marker` | `candidate_ready_marker.png` | 32 x 32 | 提示图标 | 是 | 否 | 显隐 | 候选 ready 提示，可选 |

## 填充与遮罩素材

这些素材不能烘死进度，必须由 Unity Image fill 或尺寸缩放控制。

| 素材名 | 路径 | 尺寸 | 类型 | 透明 | 九宫格 | 程序控制 | 说明 |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| `player_hp_fill` | `player_hp_fill.png` | 180 x 18 | 填充条 | 是 | 否 | fillAmount / width | 玩家 HP 填充 |
| `boss_hp_fill` | `boss_hp_fill.png` | 360 x 18 | 填充条 | 是 | 否 | fillAmount / width | Boss HP 填充 |
| `candidate_progress_fill` | `candidate_progress_fill.png` | 220 x 10 | 填充条 | 是 | 否 | fillAmount / width | 候选进度，可选 |
| `emitter_cooldown_fill` | `emitter_cooldown_fill.png` | 42 x 42 | 遮罩 | 是 | 否 | Image Filled | 发射器冷却遮罩 |

## 图标素材

这些可以先用占位图标，后续由配置指定真实图标。

| 素材名 | 路径 | 尺寸 | 类型 | 透明 | 说明 |
| --- | --- | ---: | --- | --- | --- |
| `icon_pause` | `icon_pause.png` | 20 x 20 | 图标 | 是 | 暂停按钮图标 |
| `icon_gear` | `icon_gear.png` | 20 x 20 | 图标 | 是 | 设置按钮图标 |
| `icon_debug` | `icon_debug.png` | 22 x 20 | 图标 | 是 | Debug 按钮图标 |
| `emitter_placeholder_icon` | `emitter_placeholder_icon.png` | 42 x 42 | 图标 | 是 | 发射器占位图标 |
| `buff_placeholder_icon` | `buff_placeholder_icon.png` | 18 x 18 | 图标 | 是 | Buff 占位图标 |

## 不输出为图片的内容

以下内容必须在 Prefab 中用文本或动态节点表现：

- 玩家 HP 数字
- Boss 名称
- Boss HP 数字
- 波次数字
- 发射器等级数字
- Buff 层数数字
- `+N`
- Debug 文案
- 控制模式文本

## 生成风格提示

所有透明 PNG 小素材应遵循：

- 暗调奶油月夜轻幻想战斗 UI
- 蓝紫月夜氛围下的香草灰圆角面板
- 浅粉、薄荷绿、浅蓝和浅金点缀
- 柔和描边、低对比蓝紫阴影、受控轻微高光
- 哑光、圆润、干净的手绘 2D 游戏 UI 质感，信息层级清晰
- 不要金属机甲、赛博、厚重暗黑装备感
- 不要脏旧纸板、厚纸雕、羊皮纸、纸片剧场、重木框或纯甜品主题
- 不要烘入文字或数字
- 不要投射大阴影
- 背景透明

## 生成 Manifest 三要素

每个素材对人工可配置的字段只保留三类：

| 字段 | 含义 | 示例 |
| --- | --- | --- |
| `prompt` | 描述这张素材长什么样、比例、用途和禁忌 | `a small vertical emitter slot background card...` |
| `size` | 最终透明 PNG 的画布尺寸 | `78x100` |
| `path` | 最终输出到项目中的路径 | `Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/emitter_slot_bg.png` |

`source_size`、`content_box`、`fit_mode` 不再作为 Python 生图脚本参数维护。若某个素材反复生成不理想，应回到 prompt、参考图和人工裁切规则调整，不能恢复脚本生图流程。

## Resize / Pad 内部规则

AI 生成图通常先得到高清透明源图，再整理成 Unity 目标尺寸。

推荐流程：

```text
高清透明源图
  ↓
裁掉多余透明边界，可选
  ↓
按 content box 等比缩放，除非素材明确允许拉伸
  ↓
放入 target canvas
  ↓
按 anchor 对齐
  ↓
输出最终 PNG
```

字段说明：

- `source_size`：建议 API 生成源图尺寸。
- `size`：最终输出 PNG 尺寸。
- `content_box`：素材主体在最终画布中建议占用的最大区域。
- `fit_mode`：
  - `contain`：等比缩放到 content box 内，不变形。
  - `stretch`：允许拉伸到目标尺寸，适合纯色填充条。
  - `nine_slice_source`：生成时保持可九宫格边缘，落地后在 Unity 设置九宫格。
- `padding`：最终画布透明边距。
- `anchor`：素材主体对齐方式，默认居中。

## Resize / Pad 参考参数

以下参数是导出工具的默认推导参考，不作为日常 manifest 必填项。

### 最小必需素材

| 素材名 | source_size | size | content_box | fit_mode | padding | anchor | 说明 |
| --- | --- | ---: | ---: | --- | --- | --- | --- |
| `emitter_slot_bg` | 1024 x 1024 | 78 x 100 | 72 x 94 | contain | L3 R3 T3 B3 | center | 保持卡片完整，不贴边 |
| `emitter_icon_frame` | 1024 x 1024 | 48 x 48 | 44 x 44 | contain | 2px all | center | 正方形图标框 |
| `buff_icon_frame` | 1024 x 1024 | 22 x 22 | 20 x 20 | contain | 1px all | center | 小图标框，边缘必须清晰 |
| `icon_button_bg` | 1024 x 1024 | 36 x 32 | 32 x 28 | contain | L2 R2 T2 B2 | center | 小按钮底，避免过厚阴影 |
| `player_hp_frame` | 1536 x 1024 | 184 x 22 | 184 x 22 | nine_slice_source | 0px | center | 横向血条框，Unity 九宫格 |
| `boss_hp_frame` | 1536 x 1024 | 366 x 24 | 366 x 24 | nine_slice_source | 0px | center | 横向 Boss 血条框，Unity 九宫格 |
| `wave_plate` | 1536 x 1024 | 260 x 48 | 252 x 42 | contain | L4 R4 T3 B3 | center | 顶部波次底牌，文本不烘入 |

### 第二批素材

| 素材名 | source_size | size | content_box | fit_mode | padding | anchor | 说明 |
| --- | --- | ---: | ---: | --- | --- | --- | --- |
| `avatar_frame` | 1024 x 1024 | 64 x 64 | 60 x 60 | contain | 2px all | center | 头像外框 |
| `boss_name_plate` | 1536 x 1024 | 120 x 28 | 116 x 24 | contain | L2 R2 T2 B2 | center | Boss 名称底牌 |
| `emitter_level_tag` | 1024 x 1024 | 26 x 18 | 24 x 16 | contain | L1 R1 T1 B1 | center | 只出角标底，不含数字 |
| `emitter_more_tag` | 1024 x 1024 | 26 x 22 | 24 x 18 | contain | L1 R1 T2 B2 | center | 只出底，不含 `+N` |
| `buff_stack_tag` | 1024 x 1024 | 18 x 14 | 16 x 12 | contain | L1 R1 T1 B1 | center | 只出底，不含层数 |
| `buff_more_tag` | 1024 x 1024 | 22 x 20 | 20 x 16 | contain | L1 R1 T2 B2 | center | 只出底，不含 `+N` |
| `debug_panel_bg` | 1536 x 1024 | 210 x 118 | 202 x 110 | nine_slice_source | L4 R4 T4 B4 | center | 调试面板背景，Unity 九宫格 |
| `candidate_ready_marker` | 1024 x 1024 | 32 x 32 | 28 x 28 | contain | 2px all | center | 可选 ready 提示图标 |

### 填充与遮罩素材

| 素材名 | source_size | size | content_box | fit_mode | padding | anchor | 说明 |
| --- | --- | ---: | ---: | --- | --- | --- | --- |
| `player_hp_fill` | 1024 x 1024 | 180 x 18 | 180 x 18 | stretch | 0px | center | 简洁填充条，程序控制宽度或 fill |
| `boss_hp_fill` | 1024 x 1024 | 360 x 18 | 360 x 18 | stretch | 0px | center | Boss HP 填充 |
| `candidate_progress_fill` | 1024 x 1024 | 220 x 10 | 220 x 10 | stretch | 0px | center | 候选进度填充，可选 |
| `emitter_cooldown_fill` | 1024 x 1024 | 42 x 42 | 42 x 42 | stretch | 0px | center | 用于 Image Filled 的覆盖图 |

### 图标素材

| 素材名 | source_size | size | content_box | fit_mode | padding | anchor | 说明 |
| --- | --- | ---: | ---: | --- | --- | --- | --- |
| `icon_pause` | 1024 x 1024 | 20 x 20 | 18 x 18 | contain | 1px all | center | 暂停图标 |
| `icon_gear` | 1024 x 1024 | 20 x 20 | 18 x 18 | contain | 1px all | center | 设置图标 |
| `icon_debug` | 1024 x 1024 | 22 x 20 | 20 x 18 | contain | L1 R1 T1 B1 | center | Debug 图标 |
| `emitter_placeholder_icon` | 1024 x 1024 | 42 x 42 | 38 x 38 | contain | 2px all | center | 发射器占位图标 |
| `buff_placeholder_icon` | 1024 x 1024 | 18 x 18 | 16 x 16 | contain | 1px all | center | Buff 占位图标 |

## Resize / Pad 示例

以 `emitter_slot_bg` 为例：

```text
source_size: 1024 x 1024
size: 78 x 100
content_box: 72 x 94
fit_mode: contain
padding: L3 R3 T3 B3
anchor: center
```

含义：

```text
1. 生成高清透明源图。
2. 去掉源图外部多余透明边界。
3. 等比缩放主体，使其最大不超过 72 x 94。
4. 放入 78 x 100 透明画布中央。
5. 输出最终 PNG。
```

## 生成顺序

第一轮建议只生成最小必需素材：

1. `emitter_slot_bg`
2. `emitter_icon_frame`
3. `buff_icon_frame`
4. `icon_button_bg`
5. `player_hp_frame`
6. `boss_hp_frame`
7. `wave_plate`

确认风格统一后，再生成第二批、填充条和图标。

## 验收标准

- [ ] 所有最小必需素材尺寸正确
- [ ] 所有最小必需素材符合 Resize / Pad 参数
- [ ] 输出为透明 PNG
- [ ] 无文字、数字、`+N` 烘入
- [ ] 可在 Unity UGUI 中复用
- [ ] 九宫格素材边缘适合拉伸
- [ ] 发射器槽位和 Buff 图标框风格统一
- [ ] 与 v2 视觉稿风格一致

## 待确认问题

- 第一轮是否只生成最小必需素材 7 个？
- 图标类是否先用占位，还是直接生成正式图标？
- 九宫格边距是否统一按 8px 预留？
