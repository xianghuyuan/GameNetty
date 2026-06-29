# BattleMain UI 美术规格

## 基本信息

```text
ResourceKey: BattleMain
UI 模块: Battle
用途: 战斗主界面 HUD、局内成长信息、血条、能量条、经验条、技能卡、货币与进度提示
来源: 由旧生图工具提示词迁移整理，本文档是后续唯一执行入口
```

职责边界：

- 资源名、`final_size`、用途、是否必需和 Prefab 对应关系，以 [战斗主界面资源需求.md](/Users/gxx/Documents/UGit/GameNetty/spec/art/战斗主界面资源需求.md) 为准。
- 本文档只维护 BattleMain UI 的风格规范、构图规则、Prompt、生成参数和美术验收标准。
- 素材批次、Source/Final 流转和任务状态不在本文档维护，分别见 [BattleMain_UI_Workflow.md](/Users/gxx/Documents/UGit/GameNetty/docs/art/ui/BattleMain_UI_Workflow.md) 和 [BattleMain_UI_TaskCenter.md](/Users/gxx/Documents/UGit/GameNetty/docs/art/ui/BattleMain_UI_TaskCenter.md)。

## 风格要求

```text
界面类型: 战斗 HUD / 图标 / 状态条 / 技能槽 / 进度条 / 信息牌
适配方向: 横屏
风格关键词: 中等饱和奶油月夜轻幻想、蓝紫月夜、横版推进、单一水平战斗基线、香草灰圆角面板、浅粉与薄荷绿点缀、浅金细边、柔和蓝紫阴影、哑光低高光、清晰战斗 HUD、低厚度状态条
识别要求: 移动端战斗尺寸下可读，低视觉疲劳，状态信息优先清晰
```

全局风格 Prompt：

```text
Medium-saturation creamy moonlit battle UI and scene for a 2D side-scrolling ARPG. The interface and background should feel gentle, rounded, readable, and polished, with a blue-violet moonlit fantasy forest mood behind the combat HUD.

Every asset should use soft rounded silhouettes, vanilla-gray or vanilla-cream matte panels, pale peach and soft pink HP accents, mint-green energy accents, pale gold thin trims, low-contrast blue-violet shadows, restrained soft highlights, and clean hand-painted 2D game UI edges. The material should read as soft matte game UI, not paper craft: no visible paper fibers, no thick cardboard edges, no parchment staining, no cut-paper construction.

The style should read as a creamy light-fantasy mobile combat screen, not a dessert illustration, not a paper-theater scene, and not a medieval equipment UI. Keep combat information clear: HP bars, boss bars, skill cards, progress strips, buttons, runtime text areas, character silhouettes, enemy silhouettes, and dropped items must remain readable at mobile size. Palette: blue-violet night sky, lavender cloud shadow, warm vanilla gray, soft peach pink, muted rose red, mint green, pale sky blue, sage green, and restrained pale gold accents. Saturation should be moderate: not gray and dull, not candy-bright.

Functional hierarchy first: readable status, clear combat feedback, low visual fatigue.
```

背景与整屏效果图风格锚点：

```text
Full-screen battle mockups and battle backgrounds must follow the same medium-saturation creamy moonlit UI language, but the background is not a paper stage. Use simplified hand-painted 2D side-scrolling game background layers: a flat side-view soft grass/earth combat lane, blue-violet distant hills, teal forest silhouettes, and controlled low-to-mid contrast moonlit ambience.

The background must support rightward side-scrolling progression and parallax. Ground layers should be horizontally extendable and loop-friendly, with no unique central spotlight, no bright road highlight, no one-off cracks or landmarks that expose tiling, no large moon or castle focal point, no hanging ropes, curtains, side frames, proscenium, or theater props.

Hard gameplay rule: all combat actors share one horizontal foot-contact baseline. Player feet, monster feet/paws, and drop landing points must align to the same horizontal y-line. Do not use perspective roads, diagonal ground, slopes, hills under actors, upper/lower lanes, foreground/background monster placement, or any visual depth cue that implies different playable y positions.

The UI remains the visual anchor; the background should stay simpler and lower contrast than the HUD while still sharing the same creamy palette. Avoid washed-out white, foggy blur, high-key cream scenes, modern flat gradients, realistic illustration lighting, and any paper/cardboard/parchment material cues.
```

负向 Prompt：

```text
No full HUD screenshot. No battle background, no scenery, no stage, no characters, no enemies. No neighboring UI elements. No baked text, no numbers, no icons unless the component explicitly asks for an icon. No fixed HP, energy, experience, coin count, level, cooldown, lock, selected state, or +N value unless it is a dedicated runtime-controlled mask or placeholder.

Avoid photorealistic rendering, realistic 3D render, volumetric realism, airbrushed realism, flat vector art, anime school style, realistic medieval fantasy, epic dark fantasy, medieval leather UI, wood-and-metal RPG frame, rivets, nailed planks, heavy brown leather, heavy wood grain, bulky medieval end caps, wax seals, gothic horror, gritty horror, gore, dirty grunge texture, heavy grain, muddy fog, muddy paper stains, visible paper fibers, thick cardboard cutout, handmade paper craft, paper-theater scenery, dirty old parchment, scroll UI, neon cyberpunk colors, sci-fi panels, modern app UI, overexposed white UI, washed-out milky blur, pure dessert theme, candy-shop theme, overly cute baby style, busy ornamental patterns, blurry output, baked-in text, logo, watermark, glossy metal, industrial machinery, steampunk, gun parts, cyber modules.
```

## 路径契约

```text
临时产出路径:
Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/

正式 UI 大图路径:
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/

正式 UI 小图/图标路径:
Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/

禁止目录:
Unity/Assets/AssetRaw/UIRaw/Raw/Generated/
```

## 生产流程

1. 只生成单个透明 UI 组件，不生成完整 1920x1080 HUD 截图。
2. 生图原图先进 `_Incoming/Battle/battle_main/Source/`。
3. 裁切、清透明边、缩放到目标尺寸后输出到 `_Incoming/Battle/battle_main/Final/`。
4. 验收通过后，小图、图标、按钮、血条、卡牌进入 `Atlas/Battle/battle_main/`。
5. 大图或整屏参考图进入 `Raw/Battle/`，不得进入运行时图集。
6. Unity 中按资源用途配置 `Image`、`Image Filled`、`9-slice`、`Full Rect`。

## 生成参数

小型透明组件：

```json
{
  "model": "gpt-image-2",
  "size": "1024x1024",
  "quality": "medium",
  "output_format": "png",
  "transparent_background": true
}
```

宽条、面板、横向组件：

```json
{
  "model": "gpt-image-2",
  "size": "2048x512",
  "quality": "medium",
  "output_format": "png",
  "transparent_background": true
}
```

如果生图工具不支持 `2048x512`，使用 `2048x1152` 作为原图画布，但组件主体必须居中，四周保留透明留白，方便后续裁切。

## 资产清单

命名规则：

```text
<语义>.png
```

`battle_main` 由文件夹承载，文件名只写资源语义。资源名和文件名保持一致；不加模块前缀、不加尺寸后缀、不加版本号。尺寸以本表的 `尺寸` 字段为准。

### 画布与安全边距

最终 PNG 尺寸就是正式 UI 资源边界，不是截图容器，也不是临时裁切画布。生图和后处理都必须围绕最终尺寸设计主体占比、安全边距和透明区。安全边距会直接影响最终生图质量，包括主体完整度、边缘清洁度、透明区纯净度、Prefab 摆放效果和 9-slice 可用性。

硬性规则：

- 最终画布外不能依赖任何额外像素；所有装饰、阴影、描边必须完整收在最终尺寸内。
- 主体以外区域必须完全透明，不能有绿幕残边、彩色杂边、孤立像素点、半透明脏点或不可见的 alpha 残留。
- 四边安全区必须干净。安全区内允许主体自然边缘进入，但不能出现被裁断的装饰、阴影或脏像素。
- 横向条资源必须按最终画布比例生成，不能生成短条再放进大画布，也不能通过非等比拉伸铺满。
- 如果源图中存在超出目标高度的大装饰，必须重生或裁掉；不能为了保留装饰导致主体在最终画布里变小。

第一批资源安全边距：

| 资源 | 最终尺寸 | 左右安全边距 | 上下安全边距 | 主体占比要求 |
| --- | --- | --- | --- | --- |
| hp_bar_bg.png | 416x56 | 8-16px | 4-6px | 主体宽度 88%-94% |
| hp_bar_fill.png | 352x28 | 12-20px | 5-7px | 主体宽度 88%-92%，必须贴合 hp_bar 内槽 |
| hp_bar_frame.png | 416x56 | 8-16px | 4-6px | 主体宽度 88%-94%，中段可 9-slice |
| boss_hp_bar_bg.png | 960x72 | 16-32px | 4-8px | 主体宽度 88%-94%，中段可 9-slice |
| boss_hp_bar_fill.png | 880x36 | 8-20px | 2-5px | 主体宽度 90%-96% |
| boss_hp_bar_frame.png | 960x72 | 16-32px | 4-8px | 主体宽度 88%-94%，中段可 9-slice |
| wave_panel_bg.png | 360x96 | 8-18px | 6-10px | 主体宽度 82%-92%，中心留白 |

### 第一批最小可用资源

| 资源名 | 文件名 | 尺寸 | 用途 | Unity 设置 |
| --- | --- | --- | --- | --- |
| hp_bar_bg.png | hp_bar_bg.png | 416x56 | 玩家血条底图 | 9-slice |
| hp_bar_fill.png | hp_bar_fill.png | 352x28 | 玩家血量填充，运行时控制 | Image Filled |
| hp_bar_frame.png | hp_bar_frame.png | 416x56 | 玩家血条边框 | 9-slice |
| boss_hp_bar_bg.png | boss_hp_bar_bg.png | 960x72 | Boss 血条底图 | 9-slice |
| boss_hp_bar_fill.png | boss_hp_bar_fill.png | 880x36 | Boss 血量填充，运行时控制 | Image Filled |
| boss_hp_bar_frame.png | boss_hp_bar_frame.png | 960x72 | Boss 血条边框 | 9-slice |
| wave_panel_bg.png | wave_panel_bg.png | 360x96 | 波次文本底板 | 9-slice |

第一批横向条资源的硬性构图规则：

- 资源主体必须按目标画布比例设计，不能生成短条后等比放入大画布。
- Boss 血条类资源目标为 `960x72` 或 `880x36`，主体宽度应占目标宽度的 `88%~94%`，左右只保留少量透明安全边距。
- 横条上下应保留 `4~8px` 透明安全边距，避免阴影、描边或 9-slice 边缘被裁掉。
- 横条资源不能出现向上或向下大幅伸出的挂绳、缎带、吊牌、长蜡封等装饰；所有装饰必须收敛在目标高度内，否则等比缩放会导致整条血条变小。
- Boss 血条端部装饰不能显著高于中段。端部总高度不得超过中段高度的 `1.25x`，禁止大蜡封、大纸牌、大绳结、旗帜状端盖。
- 9-slice 资源必须有明确结构：左端装饰、中间长直可拉伸段、右端装饰。中间可拉伸段应占主体宽度的 `70%+`，不能被复杂装饰打断。
- 去背后边缘必须干净，不允许出现绿幕残边、彩色杂边或半透明脏边。
- Fill 资源必须按对应背景/边框的内槽尺寸适配，不能只是一个铺满画布的亮色矩形。玩家血条填充应位于 `hp_bar_bg/frame` 的内槽内，视觉高度约 `16-18px`，左右保留约 `16px` 空隙，满值状态不应顶住外框端盖。

### 玩家状态

| 资源名 | 文件名 | 尺寸 | 用途 | Unity 设置 |
| --- | --- | --- | --- | --- |
| avatar_frame.png | avatar_frame.png | 256x256 | 头像框 | Image |
| avatar_backplate.png | avatar_backplate.png | 256x256 | 头像底板 | Image |
| level_badge.png | level_badge.png | 96x96 | 等级底牌，数字运行时渲染 | Image |
| hp_bar_bg.png | hp_bar_bg.png | 416x56 | 血条背景 | Image |
| hp_bar_fill.png | hp_bar_fill.png | 352x28 | 血条填充，运行时控制 | Image Filled |
| hp_bar_frame.png | hp_bar_frame.png | 416x56 | 血条边框 | 9-slice |
| energy_bar_bg.png | energy_bar_bg.png | 512x64 | 能量条背景 | Image |
| energy_bar_fill.png | energy_bar_fill.png | 512x48 | 能量条填充，运行时控制 | Image Filled |
| energy_bar_frame.png | energy_bar_frame.png | 512x64 | 能量条边框 | 9-slice |
| exp_bar_bg.png | exp_bar_bg.png | 512x48 | 经验条背景 | Image |
| exp_bar_fill.png | exp_bar_fill.png | 512x32 | 经验条填充，运行时控制 | Image Filled |
| exp_bar_frame.png | exp_bar_frame.png | 512x48 | 经验条边框 | 9-slice |

### 局内记录

| 资源名 | 文件名 | 尺寸 | 用途 | Unity 设置 |
| --- | --- | --- | --- | --- |
| run_coin.png | run_coin.png | 128x128 | 局内货币图标 | Image |
| coin_label_bg.png | coin_label_bg.png | 256x96 | 货币数字底板，数字运行时渲染 | 9-slice |
| candidate_progress_bg.png | candidate_progress_bg.png | 512x64 | 下一成长候选进度背景 | Image |
| candidate_progress_fill.png | candidate_progress_fill.png | 512x48 | 下一成长候选进度填充 | Image Filled |
| candidate_progress_frame.png | candidate_progress_frame.png | 512x64 | 下一成长候选进度边框 | Image |
| candidate_progress_node.png | candidate_progress_node.png | 48x48 | 进度节点 | Image |
| candidate_ready_marker.png | candidate_ready_marker.png | 96x96 | 可触发提示 | Image |

### 成长摘要

| 资源名 | 文件名 | 尺寸 | 用途 | Unity 设置 |
| --- | --- | --- | --- | --- |
| growth_badge_base.png | growth_badge_base.png | 192x224 | 成长物底牌 | Image |
| growth_quality_common.png | growth_quality_common.png | 192x224 | 普通品质叠加框 | Image |
| growth_quality_fine.png | growth_quality_fine.png | 192x224 | 优秀品质叠加框 | Image |
| growth_quality_rare.png | growth_quality_rare.png | 192x224 | 稀有品质叠加框 | Image |
| growth_level_tag.png | growth_level_tag.png | 96x64 | 成长物等级角标，数字运行时渲染 | Image |
| growth_more_tag.png | growth_more_tag.png | 96x64 | 溢出数量标签，文本运行时渲染 | Image |
| growth_empty_hint.png | growth_empty_hint.png | 160x192 | 空槽弱提示 | Image |

### 技能栏

| 资源名 | 文件名 | 尺寸 | 用途 | Unity 设置 |
| --- | --- | --- | --- | --- |
| skill_rope.png | skill_rope.png | 720x80 | 技能栏挂绳 | Image |
| skill_card_red.png | skill_card_red.png | 160x224 | 红色技能长牌 | Image |
| skill_card_blue.png | skill_card_blue.png | 160x224 | 蓝色技能长牌 | Image |
| skill_card_purple.png | skill_card_purple.png | 160x224 | 紫色技能长牌 | Image |
| skill_card_bg.png | skill_card_bg.png | 160x224 | 通用技能槽底 | Image |
| skill_level_badge.png | skill_level_badge.png | 96x64 | 技能等级标签，数字运行时渲染 | Image |
| skill_cooldown_mask.png | skill_cooldown_mask.png | 128x128 | 技能冷却遮罩 | Image Filled |
| skill_lock.png | skill_lock.png | 128x128 | 技能锁定遮罩 | Image |

### 技能与 Buff 图标

| 资源名 | 文件名 | 尺寸 | 用途 | Unity 设置 |
| --- | --- | --- | --- | --- |
| icon_skill_laurel.png | icon_skill_laurel.png | 128x128 | 技能图标 | Image |
| icon_skill_frost_bottle.png | icon_skill_frost_bottle.png | 128x128 | 技能图标 | Image |
| icon_skill_feather.png | icon_skill_feather.png | 128x128 | 技能图标 | Image |
| icon_skill_map.png | icon_skill_map.png | 128x128 | 技能图标 | Image |
| icon_buff_plus_seven.png | icon_buff_plus_seven.png | 128x128 | Buff 图标，数字不烘焙 | Image |

## 组件 Prompt

### AvatarFrame

```text
Create a transparent PNG UI sprite: a round character portrait frame for a creamy light-fantasy moonlit battle HUD. Use vanilla-cream panel material, soft rounded rim, pale gold trim, subtle frosting-like highlight, and gentle blue-violet shadow. The center must be empty/transparent or clearly reserved for a runtime character portrait. No portrait, no text, no numbers. Final target: 256x256.
```

### AvatarBackplate

```text
Create a transparent PNG UI sprite: a soft backing plate behind the avatar frame. Dark-muted creamy moonlit HUD style, vanilla-gray matte panel surface, rounded shield-like silhouette, low-contrast lavender shadow, small pale gold accent, clean mobile HUD readability. No portrait, no text, no numbers. Final target: 256x256.
```

### LevelBadge

```text
Create a transparent PNG UI sprite: a small level badge base for the battle HUD avatar cluster. Leave a clean center area for runtime-rendered level number. Vanilla-cream rounded badge, soft peach edge, pale gold rim, gentle hand-painted highlight. No number, no text. Final target: 96x96.
```

### PlayerStatusBg

```text
Create one transparent PNG UI sprite for the full player status background. It should be one complete bottom-left HUD component, not separated parts: a soft heart badge on the left, a long HP slot on the upper right, a thinner energy slot below, and small pale-gold decorative nodes integrated into the same background. Creamy light-fantasy moonlit battle HUD style, vanilla rounded base, clear recessed slots for runtime fills, soft blue-violet shadow, peach-pink and mint accents, readable at small mobile HUD size. Do not bake fixed HP or energy percentages. No HP value, no text, no numbers. Final target: player_status_bg.png.
```

局部效果图锚点：

```text
source_region: .Codex/artifacts/battle-ui/效果图/battle_main_cream_single_baseline_mockup.png 左下角玩家状态条
approved_asset: none
status: 待重新生成；旧 player-status PNG 已删除，不能作为合规正式素材使用。
style_anchor: 奶油色圆角底板、红粉色血量填充、薄荷绿能量填充、清晰灰色内槽、浅金小节点、柔和蓝紫阴影、低高度扁平长条
negative_style: 不要卷轴、不要横幅、不要两端垂直缎带、不要亮金豪华边框、不要高饱和大红、不要暗木、不要脏旧纸板
source_canvas: 1536x512
crop_rule: 玩家状态条按整块背景管理，保留爱心徽章、HP 槽、能量槽和装饰点位；不要拆成独立爱心、边框、填充条和菱形碎片。
```

### BossHpBarBg

```text
Create one isolated transparent PNG UI sprite source for boss_hp_bar_bg.png.
Final target: 960x72.
The asset itself must be an ultra-wide horizontal boss HP bar background matching the final aspect ratio. Do not generate a short bar centered in a wide canvas.
The visible subject should occupy 88% to 94% of the final width, with only small transparent safety margins on the left and right.
Keep 4-8px transparent safety margin on top and bottom after final crop.
Do not include long hanging ribbons, dangling tags, vertical ropes, tall seals, large wax seals, bulky end caps, flags, large paper plates, or ornaments that make the end caps much taller than the middle bar.
The end caps must stay compact and low-profile; total end-cap height should be no more than 1.25x the middle bar height.
Structure for 9-slice: decorated left cap, very long straight middle recessed channel, decorated right cap. The middle stretchable channel should occupy more than 70% of the visible width and must not be broken by decorations.
Style: creamy light-fantasy moonlit combat HUD, clean hand-painted 2D mobile game UI, soft rounded vanilla frame, subtle frosting-like highlight, clear gray recessed inner channel, muted rose-red combat accent, pale gold and lavender accents, controlled contrast, low noise. Avoid medieval leather, wood planks, rivets, wax seals, thick cardboard, dark old paper, and heavy RPG frame materials.
No red fill amount, no text, no numbers, no boss portrait, no skulls, no characters, no background scene, no full HUD screenshot, no neighboring UI.
```

### EnergyBar Set

```text
Create transparent PNG UI sprites for a slim energy/mana status bar set: background, mint-green or pale blue fill, and frame. Creamy light-fantasy moonlit battle HUD style, vanilla rounded frame, clear dark inner channel, low-contrast soft shadow, readable at small mobile HUD size. The fill sprite must be standalone and controlled by runtime width/fill amount. No value, no text, no numbers. Final targets: 512x64 bg, 512x48 fill, 512x64 frame.
```

### EXPBar Set

```text
Create transparent PNG UI sprites for a thin experience strip set: background, soft lavender fill, and frame. Long narrow rounded vanilla strip, low height, readable but visually secondary to HP and energy. The fill sprite must be standalone and controlled by runtime width/fill amount. No value, no text, no numbers. Final targets: 512x48 bg, 512x32 fill, 512x48 frame.
```

### RunCoin

```text
Create a transparent PNG icon for in-run battle currency. It should look like a small moonlit fantasy coin or cream-style star token, warm pale gold but matte, rounded and clean. Clear at small size, no number, no text. Final target: 128x128.
```

### CoinLabelBg

```text
Create a transparent PNG rounded label background for displaying in-run coin count. Clean center area for runtime-rendered number text. Vanilla-cream panel, pale gold edge, soft lavender shadow, tiny star or folded-corner accent. No number, no text. Source target: 2048x512. Final target: 256x96.
```

### CandidateProgress Set

```text
Create transparent PNG UI sprites for a next-growth candidate progress indicator: background, fill, frame, and optional node. It should feel like a small creamy moonlit progress strip, not a skill cooldown bar. Use rounded vanilla frame, mint or lavender fill, and pale gold node. The fill sprite must be standalone and controlled by runtime value. No text, no numbers. Final targets: 512x64 bg, 512x48 fill, 512x64 frame, 48x48 node.
```

### CandidateReadyMarker

```text
Create a transparent PNG UI sprite for a small ready marker when a growth candidate can trigger. Creamy moon/star charm style, pale gold and vanilla, clear silhouette, readable at small size. No text, no numbers. Final target: 96x96.
```

### GrowthBadgeBase

```text
Create a transparent PNG UI sprite: small rounded keepsake badge base for representative in-run growth items. It should feel like a creamy light-fantasy charm backing, not a square equipment slot. Leave room for a runtime growth icon and a small level corner tag. No icon, no level, no text. Final target: 192x224.
```

### GrowthQualityFrames

```text
Create transparent PNG overlay frames for representative growth item badges. Same rounded charm silhouette across all qualities, creamy light-fantasy style. Common uses cream/gray, fine uses mint/blue, rare uses lavender/pale gold. No icon, no text, no number. Final target: 192x224 for each quality frame.
```

### GrowthLevelTag

```text
Create a transparent PNG tiny level corner tag for representative growth badges. Small rounded vanilla label attached to a keepsake. Leave clean room for runtime-rendered level number. No number, no text. Final target: 96x64.
```

### GrowthMoreTag

```text
Create a transparent PNG tag for overflow count such as +N. Small rounded folded note in creamy light-fantasy moonlit style. Leave center clean for runtime-rendered text. No text baked in. Final target: 96x64.
```

### GrowthEmptyHint

```text
Create a transparent PNG weak placeholder for an empty representative growth slot. Very low contrast, rounded creamy charm silhouette, does not look like a locked item. No icon, no text, no number. Final target: 160x192.
```

### SkillSlot Set

```text
Create transparent PNG UI sprites for a battle skill slot set: optional soft connector strip, vertical rounded cream skill card, cooldown mask, lock mask, and small level badge. Creamy light-fantasy moonlit style, vanilla card surface, pale gold folded corner, soft lavender shadow, mint/blue/pink accent variants. No baked skill icon, no cooldown number, no level number, no text. Final targets: 720x80 connector, 160x224 slot, 128x128 cooldown mask, 128x128 lock mask, 96x64 level badge.
```

局部效果图锚点：

```text
source_region:
- 连接条参考奶油系 v2 右侧卡片的轻量边缘装饰
- 技能槽参考奶油系 v2 右侧圆角技能卡片
style_anchor: 奶油色圆角卡、浅金折角、小标签区、柔和蓝紫阴影、低饱和浅粉/薄荷/浅蓝点缀
negative_style: 不要正方形装备槽、不要重金属框、不要厚重卷轴、不要暗木、不要脏旧纸板、不要悬挂绳索过重
source_canvas:
- connector: 1536x512
- slot: 1024x1536
- cooldown: 1024x1024
crop_rule:
- rope 保持细长横向
- slot 保持竖向长牌比例
- cooldown 只覆盖图标区，不覆盖整张卡
```

### Skill And Buff Icons

```text
Create a transparent PNG icon for a journey growth or battle skill object in a creamy light-fantasy moonlit adventure. It should feel like a small magical story object found during an expedition, not a weapon module or equipment part. Rounded silhouette, soft hand-painted material, gentle highlight, clear at small size. No frame, no level, no text. Final target: 128x128.
```

首批图标：

```text
icon_skill_laurel
icon_skill_frost_bottle
icon_skill_feather
icon_skill_map
icon_buff_plus_seven
icon_growth_moonlit_key
icon_growth_chapel_vial
icon_growth_star_sealed_token
icon_growth_dragon_scale_keepsake
icon_growth_folded_moon_note
icon_growth_royal_brooch
icon_growth_travel_charm
icon_growth_ash_bottle
icon_growth_crowned_moon_fragment
```

## 程序绑定字段

```text
IconPath: Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/{filename}
PrefabPath: 由 UI Prefab 绑定，不在生图阶段生成
AtlasPath: Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/
RawPath: Unity/Assets/AssetRaw/UIRaw/Raw/Battle/
RuntimeText: HP value, energy value, EXP value, coin count, level, +N
RuntimeFill: HPBarFill, EnergyBarFill, EXPBarFill, CandidateProgressFill, SkillCooldownMask
```

## 验收标准

- 输出单独透明 PNG 组件，不是完整 HUD 截图。
- 不包含战斗场景、背景像素、角色、敌人或相邻 UI 元素。
- HP、能量、经验、金币数量、等级、+N、冷却、锁定、选中态等运行时值不能烘焙进基础图。
- 组件在移动端战斗 HUD 尺寸下可读。
- 风格与全局奶油系轻幻想月夜战斗 UI 方向一致，噪声低于旧整图效果图。
- 原图、中间稿、未验收图只放 `_Incoming`，不得直接导入正式图集。
- 最终小图和图标进入 `Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main`。
- 大图或整屏参考图进入 `Assets/AssetRaw/UIRaw/Raw/Battle`。
- 文件名使用稳定 ASCII，简单直接，不加版本号，不加尺寸后缀。
- 优先控制在 2048x2048 BattleMain 图集预算内，必要时最大 4096x4096。
