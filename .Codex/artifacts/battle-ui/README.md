# Battle UI Artifacts

这个目录只放战斗 UI 相关产物；`.Codex/plans/` 只放计划、规格和清单文档。

## Directory

- `效果图/`：当前认可的完整战斗界面效果图；旧探索稿统一放入 `效果图/archive/`。
- `待处理素材/`：手动保存的 imagegen 源图、待裁切图、待透明处理图；不是正式素材，不进 Unity，不给 Prefab 引用。
- `正式素材/`：只保留合规正式素材副本。旧 Python/代码绘制素材不得放入此目录。
- `player-status/`：仅作为玩家状态条素材工作目录；当前没有合规批准 PNG。

## Current Anchors

- Battle mockup: `效果图/battle_main_cream_single_baseline_mockup.png`
- Formal background: `正式素材/background/battle_scene_master_no_center_focus.png`
- Loop scene manifest: `正式素材/scene/battle_scene_loop_manifest.json`
- Scene layers: `正式素材/scene/battle_scene_*.png`
- HUD formal assets: none. The previous Python/代码绘制 HUD PNGs have been removed and must be regenerated through Codex `imagegen` or an art tool.

## Formal Unity Assets

当前合规正式运行资源只包括背景整图和 imagegen 背景拆层：

```text
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/battle_scene_master_no_center_focus.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_sky.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_far_hills_loop.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_mid_forest_loop.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_ground_loop.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_foreground_grass_loop.png
```

HUD 正式素材必须通过 Codex `imagegen` 能力生成或由美术工具人工制作，再按规格导入 Unity。禁止使用 Python 脚本或代码绘制方式生成正式美术素材。

`Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/` 当前不保留旧 HUD PNG。重新生成 HUD 前，Prefab 中引用旧 Sprite 的节点应视为待修复状态。

当前背景候选为无中心焦点横版推图方向，符合奶油月夜与单一水平基线方向；但仍是整图背景，不是最终循环分层资源。后续进入跑图生产时，应继续拆成天空、远景、地面、前景等可循环层，并继续压低中心亮区和前景草石细节。

当前候选整图背景：

```text
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/battle_scene_master_no_center_focus.png
```

可循环场景分层资源已写入：

```text
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_sky.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_far_hills_loop.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_mid_forest_loop.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_ground_loop.png
Unity/Assets/AssetRaw/UIRaw/Raw/Battle/Scene/battle_scene_foreground_grass_loop.png
```

`battle_scene_ground_loop.png` 的最终 `baselineY` 必须在 Unity 摆放时按实际脚底接触线确定，不沿用旧脚本资源的 `baselineY=56`。所有玩家、怪物、Boss 的脚底/爪底接触点必须对齐到同一条水平线。`battle_scene_foreground_grass_loop.png` 是可选层；如果遮挡掉落物、技能预警或角色脚点，应关闭或降低透明度。

当前循环层必须由 `imagegen` 或美术工具生产原图。Python 脚本不得用于绘制、合成或生成正式背景层。代码只允许做尺寸、透明通道、纯色抠图底转 alpha、命名、`.meta`、引用关系等非创作性处理和检查。

## Style Anchor

当前方向是中等饱和奶油月夜轻幻想：UI 和场景风格统一，香草灰圆角面板、浅粉 HP、薄荷能量、浅金细边、柔和蓝紫阴影、软草坡/土路横向战斗带。

硬规则：所有战斗单位的脚底/爪底接触点必须在同一条水平基线上。场景只能表现单一横向战斗带，不允许透视道路、上下平台、斜坡、前后深度站位。

不要把风格继续推进到纸板、纸雕、羊皮纸、纸片剧场或厚重舞台布景。整屏背景要服务横版向右推进：中等饱和、低噪声、少细节、可横向延展和循环，避免中央聚光、唯一亮斑、中心空地、中心山谷、中心主树、幕布、吊绳、侧边框和剧场舞台感。

## Rule

新生成的图片、PSD、脚本和临时素材不要放到 `.Codex/plans/` 根目录。
手动保存的 imagegen 预览图先放入 `待处理素材/`，验收、裁切、透明处理后才能进入 `正式素材/` 或 Unity 正式资源目录。
临时绿幕源、失败版本和程序重建实验不长期保留；确认不用后直接清理。
旧风格探索图如需保留，只能放入 `效果图/archive/`，不得作为当前风格锚点引用。
玩家状态条背景按整块管理，不拆成爱心、边框和装饰碎片；HP / energy 只作为运行时填充条单独维护。
禁止新增或恢复 Python 生图脚本；正式素材生产入口只能是 Codex `imagegen` 或人工美术工具。
