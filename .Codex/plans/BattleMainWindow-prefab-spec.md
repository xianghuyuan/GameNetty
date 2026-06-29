# Prefab Spec: BattleMainWindow

## 文档元信息

- 文档类型：Prefab Spec
- 状态：t0 待 Unity 验证
- 使用 Workflow：`.Codex/workflows/ui-prefab-workflow.md`
- 来源：`.codex/plans/battle-main-window.plan.json`
- 上游文档：
  - `.codex/plans/战斗界面-feature-spec.md`
  - `.codex/plans/战斗界面-visual-mockup-spec.md`
  - `.codex/plans/BattleMainWindow-asset-slice-spec.md`
  - `.codex/plans/battle_main_window_hud.asset-manifest.json`
- 下游文档：
  - `.codex/plans/战斗界面-harness.md`
- 最近整理：2026-06-09

## 基本信息

- Prefab 名称：`BattleMainWindow`
- 保存路径：`Unity/Assets/AssetRaw/UI/Battle/BattleMainWindow.prefab`
- 所属系统：战斗 UI / BattleMainUI
- 用途：战斗主 HUD，展示 Boss 血条、波次、玩家状态、底部成长进度、调试入口，并承载发射器/ Buff 相关 Widget。
- 关联代码：
  - `Unity/Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainWindow.cs`
  - `Unity/Assets/GameScripts/HotFix/GameLogic/UI/Gen/BattleMainWindow_Gen.g.cs`

## 输入来源

- 效果图：`.Codex/artifacts/battle-ui/效果图/battle_main_cream_single_baseline_mockup.png`
- 素材目录：
  - `Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/`
- 参考 Prefab：
  - `Unity/Assets/AssetRaw/UI/Battle/BattleEmitterOwnedBarWidget.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleEmitterAddPanelWidget.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleBuffAddPanelWidget.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleGMWidget.prefab`

## 项目规范

- UI 结构必须创建在 Prefab 中，不在业务代码中动态搭核心 HUD 层级。
- 需要业务绑定的节点使用 `m_` 前缀。
- 图片节点使用 `m_img` 前缀，文本节点使用 `m_tmp` 前缀，按钮节点使用 `m_btn` 前缀，容器节点使用 `m_tf` 前缀。
- 根节点必须挂 `GameLogic.UIBindComponent`，并保持绑定顺序与 `BattleMainWindow_Gen.g.cs` 一致。
- 第一版不新增主动技能按钮 / 手动释放入口。
- 第一版核心展示发射器槽位和槽位内 BuffStack；具体槽位展示由 `BattleEmitterOwnedBarWidget` 负责。

## Root 节点

- 节点名：`BattleMainWindow`
- 组件：
  - RectTransform
  - CanvasRenderer
  - GraphicRaycaster
  - GameLogic.UIBindComponent
- 默认状态：显示
- UI 类型：`UIWindow`
- 生成代码路径：`Assets/GameScripts/HotFix/GameLogic/UI/Gen`
- 实现代码路径：`Assets/GameScripts/HotFix/GameLogic/UI`

## 层级结构

```text
BattleMainWindow
  ├── TopLayer
  │   ├── TopCenterStatus
  │   │   ├── top_long_bar
  │   │   ├── m_tfEnemy
  │   │   ├── m_tmpBossName
  │   │   ├── m_tmpBossHp
  │   │   ├── boss_hp_bar_frame
  │   │   └── m_imgBossHp
  │   └── TopButtons
  │       ├── m_btnGear
  │       ├── m_btnPause
  │       └── m_btnBookmark
  ├── BottomLayer
  │   ├── LeftPlayerStatus
  │   │   ├── left_avatar_frame
  │   │   ├── avatar_backplate
  │   │   ├── level_badge
  │   │   ├── hp_bar
  │   │   ├── hp_bar_frame
  │   │   ├── m_imgPlayerHp
  │   │   ├── mp_bar
  │   │   ├── energy_bar
  │   │   ├── energy_bar_frame
  │   │   ├── energy_bar_fill
  │   │   └── m_tmpPlayerHp
  │   ├── BottomProgress
  │   │   ├── bottom_slider
  │   │   ├── exp_bar_frame
  │   │   └── exp_bar_fill
  │   ├── m_tmpWave
  │   ├── m_tmpControlMode
  │   └── m_btnSpawnEnemy
```

说明：发射器拥有栏、发射器添加面板、Buff 添加面板和 GM 面板由 `BattleMainWindow.cs` 通过 `CreateWidgetByType<T>` 创建，Prefab 资源分别维护在对应 Widget Prefab 中。

## 绑定字段

绑定顺序必须与 `BattleMainWindow_Gen.g.cs` 保持一致：

| Index | 字段 | 组件类型 | 用途 |
| ---: | --- | --- | --- |
| 0 | `m_imgBossHp` | Image | Boss HP 填充，代码使用 `fillAmount` |
| 1 | `m_tmpBossHp` | TextMeshProUGUI | Boss HP 数字 |
| 2 | `m_tfEnemy` | Transform | 敌方/Boss 状态容器 |
| 3 | `m_tmpBossName` | TextMeshProUGUI | Boss 名称 |
| 4 | `m_btnPause` | Button | 暂停入口 |
| 5 | `m_btnBookmark` | Button | 书签/调试入口占位 |
| 6 | `m_btnSpawnEnemy` | Button | 生成敌人调试入口 |
| 7 | `m_imgPlayerHp` | Image | 玩家 HP 填充，代码使用 `fillAmount` |
| 8 | `m_tmpPlayerHp` | TextMeshProUGUI | 玩家 HP 数字 |
| 9 | `m_tmpControlMode` | TextMeshProUGUI | 当前战斗模式 |
| 10 | `m_tmpWave` | TextMeshProUGUI | 波次文本 |
| 11 | `m_btnGear` | Button | 设置入口 |

## t0 素材使用

主 HUD t0 素材位于 `Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/`，不再使用 `battle_main/` 子目录：

- 头像：`avatar_backplate.png`、`avatar_frame.png`、`level_badge.png`
- 玩家状态：`hp_bar_bg.png`、`hp_bar_fill.png`、`hp_bar_frame.png`、`energy_bar_bg.png`、`energy_bar_fill.png`、`energy_bar_frame.png`
- 底部进度：`exp_bar_bg.png`、`exp_bar_fill.png`、`exp_bar_frame.png`
- Boss 状态：`boss_hp_bar_bg.png`、`boss_hp_bar_fill.png`、`boss_hp_bar_frame.png`
- 波次：`wave_panel_bg.png`

Minimal 小件同样位于 `Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/`，用于发射器槽位、图标框、按钮底和简版条框：

- `emitter_slot_bg.png`
- `emitter_icon_frame.png`
- `buff_icon_frame.png`
- `icon_button_bg.png`
- `player_hp_frame.png`
- `boss_hp_frame.png`
- `wave_plate.png`

## Widget 接入

- `BattleEmitterOwnedBarWidget`：展示已拥有发射器槽位和 BuffStack；由主界面 `OnCreate` 创建。
- `BattleEmitterAddPanelWidget`：调试添加发射器面板；默认隐藏。
- `BattleBuffAddPanelWidget`：调试添加 Buff 面板；默认隐藏。
- `BattleGMWidget`：调试按钮面板；由主界面创建并绑定回调。

## 交互说明

- Boss 出现时显示 Boss 容器、Boss 名称、Boss HP 数字和 HP 填充。
- Boss 不存在或死亡时隐藏 Boss 容器，并重置 Boss 文案和填充。
- 玩家 HP 由 `m_tmpPlayerHp` 和 `m_imgPlayerHp.fillAmount` 同步展示。
- 波次由 `m_tmpWave` 展示。
- 控制模式由 `m_tmpControlMode` 展示。
- 发射器与 Buff 调试入口通过 Widget 回调驱动，不新增主界面业务字段。

## 验收标准

- [ ] `BattleMainWindow.prefab` 路径存在。
- [ ] 根节点存在 `GameLogic.UIBindComponent`，`className` 为 `BattleMainWindow`。
- [ ] `UIBindComponent.m_components` 数量为 12，顺序与 `BattleMainWindow_Gen.g.cs` 一致。
- [ ] `m_imgBossHp` 与 `m_imgPlayerHp` 的 Image 类型支持 `fillAmount`。
- [ ] `m_tmpBossHp`、`m_tmpBossName`、`m_tmpPlayerHp`、`m_tmpControlMode`、`m_tmpWave` 文本节点不缺失。
- [ ] `m_btnGear`、`m_btnPause`、`m_btnBookmark`、`m_btnSpawnEnemy` 按钮节点不缺失。
- [ ] t0 HUD 素材引用不缺失，无 Missing Sprite / Missing Reference。
- [ ] `BattleEmitterOwnedBarWidget`、`BattleEmitterAddPanelWidget`、`BattleBuffAddPanelWidget`、`BattleGMWidget` 可被 `CreateWidgetByType<T>` 创建。
- [ ] Unity Console 无关键错误。
- [ ] 1280 x 720 下布局不遮挡战斗主体，移动端等比适配后文本不溢出。

## 待确认问题

- `m_btnBookmark` 在正式 HUD 中的定位：保留为调试入口、书签入口，还是隐藏。
- `m_btnSpawnEnemy` 是否仅开发环境显示。
- `BattleEmitterOwnedBarWidget` 是否需要从动态创建改为主 Prefab 内固定挂载。
- t0 素材是否需要设置 Sprite Border 以支持九宫格拉伸。
