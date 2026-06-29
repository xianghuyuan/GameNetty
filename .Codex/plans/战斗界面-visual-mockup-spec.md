# Visual Mockup Spec: 战斗界面

## 文档元信息

- 文档类型：Visual Mockup Spec
- 状态：草稿
- 使用 Workflow：`.Codex/workflows/requirement-intake-workflow.md`
- 上游文档：
  - `.Codex/plans/战斗界面-feature-spec.md`
  - `.Codex/plans/战斗界面-wireframe.svg`
  - `.Codex/plans/局内器件构筑系统-美术需求清单.md`
  - `.Codex/plans/局内成长HUD-主美资产拆分规范.md`
- 下游文档：
  - 待生成 `BattleMainWindow-prefab-spec.md`
- 最近整理：2026-06-04

## 目标

在进入 Unity Prefab Spec 之前，先生成一张战斗 HUD 最终尺寸视觉稿，用来确认风格、信息层级、区域比例和资产拆分方向。

这张图不是最终切图，不直接作为 Unity Prefab 使用；它是 Prefab Spec 和美术资产拆分的视觉依据。

## 当前视觉稿

- 设计基准版本：`.Codex/artifacts/battle-ui/效果图/battle_main_cream_single_baseline_mockup.png`
- 状态：当前推荐用于 Prefab Spec 和素材 prompt 的美术锚点
- 说明：
  - 中等饱和奶油月夜轻幻想战斗 UI 与场景统一方向成立
  - 比旧纸片剧场方向更轻、更圆润、更亲和，但后续不能继续使用纸板、纸雕或羊皮纸材质
  - 保留蓝紫月夜、单一水平战斗带、Boss 条、玩家状态条和右侧卡片的信息层级
  - 后续素材生成应以本版本的圆角面板、香草灰底板、浅粉/薄荷/浅金点缀、柔和蓝紫阴影和简化场景大形为准
  - 所有战斗单位的脚底/爪底接触点必须在同一条水平基线上
  - 旧风格探索图和中间版本不再作为素材依据，只能归档参考

## 设计基准尺寸

- 第一版设计基准尺寸：1280 x 720
- 宽高比：16:9
- 用途：战斗 HUD 视觉稿 / UI mockup
- 安全区：四周保留约 40px，不贴边

说明：

- `1280 x 720` 用作第一版视觉稿和 UI 参数推导基准。
- 它不是唯一最终设备尺寸。
- 后续可按 16:9 等比适配到 `1920 x 1080`。
- UI 资产应优先按可缩放、可九宫格、可填充方式拆分。
- 如后续目标比例或参考分辨率变化，应先更新本文件，再重新生成视觉稿。

## 风格方向

整体风格沿用项目当前确认方向：

- 中等饱和奶油月夜轻幻想战斗 UI
- 蓝紫月夜轻幻想森林，适合横版推进和横向循环
- 香草灰圆角面板、浅粉血条、薄荷绿能量条、浅金细边
- 柔和描边、低对比蓝紫阴影、受控高光、细腻手绘 2D 游戏质感
- 哑光、圆润、干净的游戏 HUD 与场景材质，不是纸张工艺
- 场景细节克制，只保留大形、少量节奏点和干净战斗通道
- 低疲劳阅读
- 温柔亲和但仍保留战斗 HUD 信息层级

避免：

- 厚重暗黑写实西幻
- 脏旧纸板 / 厚纸雕 / 羊皮纸 / 纸片剧场 / 重木框
- 现代枪械 / 赛博 UI
- 高饱和糖果色
- 纯甜品主题 / 过度软萌 / 大面积过曝纯白 / 发白发糊
- 金属机甲感
- 传统 RPG 装备栏
- 复杂厚重面板压住战斗主体
- 舞台框、幕布、吊绳、挂饰、侧边框、聚光灯式地面高光
- 不可循环的唯一裂缝、唯一亮斑、中央焦点大月亮或城堡
- 中间空地、中间山谷、中间开口、中间主树、中间亮区、左右树木框住中间的插画式构图
- 透视道路、上下平台、斜坡、前后深度站位、怪物脚点上下错位

背景额外约束：

- 背景服务于横版向右推进，不做静态舞台插画。
- 地面必须是侧视单一水平战斗带，低高光、哑光、低噪声、少细节，能横向循环或拆成循环段。
- 角色脚底、怪物脚底/爪底、掉落物落点必须共享同一条水平基线；不能用透视道路制造前后深度站位。
- 画面中间 40%-50% 不能成为独立观景区或留白舞台；远山、森林、灌木和地面节奏必须横向连续穿过中心。
- 远景采用蓝紫山体和深青绿树影分层，方便后续做视差。
- UI 是视觉主角，背景整体应比 HUD 更暗、更低对比。

## 布局来源

以 `.Codex/plans/战斗界面-wireframe.svg` 为布局来源。

已确认结构：

- 中心大面积留给战斗主体
- 顶部中间显示波次
- Boss 信息在顶部偏中区域，有 Boss 时显示
- 左下显示玩家状态
- 右侧显示发射器槽位
- 每个发射器槽位内部显示自己的 BuffStack
- 右上为暂停、设置、Debug 入口
- 左上或边缘区域可保留开发调试入口
- 第一版不包含主动技能按钮 / 手动释放入口

## 视觉稿内容

### 必须表现

- 玩家 HP 区域
- 波次区域
- Boss HP 区域
- 发射器槽位区域
- 每个发射器槽位内部的 BuffStack
- 暂停 / 设置 / Debug 入口
- 开发调试区域的低权重表现

### 可以弱化

- 控制模式文本
- 候选成长进度
- GM 面板内容

### 不要表现

- 主动技能按钮
- 传统装备栏
- 完整背包列表
- 大面积战斗结算面板
- 真实可读的最终数值文本

## 动态内容规则

以下内容不要烘成不可修改的大图：

- HP 数字
- 波次数字
- Boss 名称
- Buff 数量
- 发射器等级
- Debug 文案

视觉稿可以用短占位文本表达位置，但最终 Prefab 中这些内容必须由程序文本或动态组件渲染。

## 小图尺寸建议

以下尺寸以第一版设计基准 `1280 x 720` 战斗 HUD 为基准，单位为像素。第一版优先用于视觉稿、切图规划和后续 Prefab Spec，不代表最终不可调整。

### 玩家状态区

| 资产 | 建议尺寸 | 说明 |
| --- | ---: | --- |
| 玩家头像框 `avatar_frame` | 64 x 64 | 左下玩家状态头像外框 |
| 玩家头像遮罩 `avatar_mask` | 56 x 56 | 程序放角色头像 |
| 玩家 HP 底条 `player_hp_bg` | 180 x 18 | 可做 9-slice |
| 玩家 HP 填充 `player_hp_fill` | 180 x 18 | horizontal fill |
| 玩家 HP 外框 `player_hp_frame` | 184 x 22 | 覆盖底条和填充 |

### Boss 区

| 资产 | 建议尺寸 | 说明 |
| --- | ---: | --- |
| Boss HP 底条 `boss_hp_bg` | 360 x 18 | 顶部 Boss 血条 |
| Boss HP 填充 `boss_hp_fill` | 360 x 18 | horizontal fill |
| Boss HP 外框 `boss_hp_frame` | 366 x 24 | 可带纸带或木框感 |
| Boss 名称底牌 `boss_name_plate` | 120 x 28 | 可选，文本程序渲染 |

### 波次区

| 资产 | 建议尺寸 | 说明 |
| --- | ---: | --- |
| 波次底牌 `wave_plate` | 260 x 48 | 顶部中间，文本程序渲染 |
| 候选进度底条 `candidate_progress_bg` | 220 x 10 | 可选 |
| 候选进度填充 `candidate_progress_fill` | 220 x 10 | 可选 horizontal fill |
| 候选 ready 标记 `candidate_ready_marker` | 32 x 32 | 可选提示图标 |

### 发射器槽位

| 资产 | 建议尺寸 | 说明 |
| --- | ---: | --- |
| 发射器槽位底卡 `emitter_slot_bg` | 78 x 100 | 单个发射器卡片，内部含 BuffStack |
| 发射器图标框 `emitter_icon_frame` | 48 x 48 | 放在槽位下半部 |
| 发射器图标遮罩 `emitter_icon_mask` | 42 x 42 | 程序放发射器图标 |
| 发射器冷却遮罩 `emitter_cooldown_fill` | 42 x 42 | Image Filled，覆盖图标 |
| 发射器等级角标底 `emitter_level_tag` | 26 x 18 | 可选，文本程序渲染 |
| 发射器溢出标记 `emitter_more_tag` | 26 x 22 | `+N`，文本程序渲染 |

### BuffStack

| 资产 | 建议尺寸 | 说明 |
| --- | ---: | --- |
| Buff 小图标框 `buff_icon_frame` | 22 x 22 | 每个发射器槽位内最多直显 2-3 个 |
| Buff 图标遮罩 `buff_icon_mask` | 18 x 18 | 程序放 Buff 图标 |
| Buff 层数角标底 `buff_stack_tag` | 18 x 14 | 可选，文本程序渲染 |
| Buff 溢出标记 `buff_more_tag` | 22 x 20 | `+N`，文本程序渲染 |

### 系统与调试入口

| 资产 | 建议尺寸 | 说明 |
| --- | ---: | --- |
| 圆角小按钮底 `icon_button_bg` | 36 x 32 | 暂停 / 设置 / Debug 共用 |
| 暂停图标 `icon_pause` | 20 x 20 | 图标，不含按钮底 |
| 设置图标 `icon_gear` | 20 x 20 | 图标，不含按钮底 |
| Debug 图标 `icon_debug` | 22 x 20 | 图标，不含按钮底 |
| 调试面板底 `debug_panel_bg` | 210 x 118 | 开发期可见，可做 9-slice |

### 资源拆分原则

- 文本、数值、等级、`+N` 不烘进图片，由程序文本渲染。
- HP、候选进度、冷却遮罩使用可填充 Image，不切固定进度死图。
- 发射器槽位底卡、Buff 图标框、按钮底优先做可复用小图。
- 若美术产能有限，第一版可以先只做底卡、图标框、血条、按钮底，其余使用占位。

## 生成 Prompt

```text
Use case: ui-mockup
Asset type: 2D game battle HUD visual mockup, final size 1280x720, 16:9.
Primary request: Create a polished low-to-mid fidelity visual mockup for a side-scrolling ARPG battle HUD. Follow the provided wireframe layout: keep the center mostly clear for battle action, put wave information at top center, boss name and HP bar near the top, player HP status at bottom left, system buttons at top right, a low-priority debug area near the left edge, and an emitter panel on the right.
Style: medium-saturation creamy moonlit battle UI and scene, side-scrolling light-fantasy forest background, vanilla-gray rounded HUD panels, soft peach-pink HP accents, mint-green energy accents, pale gold thin trims, soft blue-violet shadows, matte hand-painted 2D game UI edges, restrained highlights, readable and low-fatigue. The background should feel like a reusable horizontal game level: a single flat side-view combat lane, aligned foot/paw baseline for all actors, low-highlight matte ground, blue-violet distant hills, teal forest silhouettes, parallax-friendly layers, no unique central spotlight.
Emitter panel: show three emitter slots. Each emitter slot is a small card with its own BuffStack inside the slot: tiny buff icons above or attached to the emitter icon, with +N for overflow. Make the ownership relationship clear: buffs belong to each emitter slot. Do not make a global row of buffs.
Composition: UI elements should be light, compact, and not block the center battle area. Avoid heavy panels. Use simple icon placeholders and bars rather than detailed final art.
Text: use minimal placeholder text only; do not rely on readable final text. No large labels baked into art.
Do not include: active skill buttons, joystick, inventory grid, equipment slots, cyberpunk style, heavy dark fantasy metal UI, dirty cardboard, thick paper-theater props, parchment, paper craft, theater stage, curtains, hanging ropes, side frames, perspective road, diagonal ground, slopes, upper/lower lanes, different monster foot heights, central ground spotlight, unique floor highlight, pure dessert theme, overexposed white UI, washed-out milky blur, bright candy colors, photorealism, watermark.
```

## 验收标准

- [x] 尺寸为 1280 x 720
- [x] 所有战斗单位脚底/爪底在同一条水平基线上
- [x] 中心战斗区域保持清晰
- [x] 发射器槽位和 BuffStack 归属关系清楚
- [ ] 没有主动技能按钮
- [ ] 没有传统装备栏感
- [ ] 风格符合暗调奶油月夜轻幻想战斗 UI
- [ ] 动态文本没有被当作最终切图烘死
- [ ] 可以作为 `BattleMainWindow-prefab-spec.md` 的视觉依据

## 待确认问题

- [已确认] 第一版设计基准尺寸为 1280 x 720，不作为唯一最终设备尺寸。
- 是否需要同时生成移动端安全区版本？
- Debug 入口是左侧独立块，还是右上 Debug 按钮展开？
