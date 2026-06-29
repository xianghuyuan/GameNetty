# BattleMain UI 素材任务中心

本文档用于把 BattleMain UI 素材生产拆成一个个可派发、可验收、可追溯的任务。

它的定位类似轻量中台：

- 资源需求文档负责定义“要什么、尺寸是多少”。
- 工作流文档负责定义“怎么生产、怎么验收”。
- 本任务中心负责定义“现在有哪些任务、谁在做、状态是什么、下一步做什么”。

关联文档：

- [战斗主界面资源需求.md](/Users/gxx/Documents/UGit/GameNetty/spec/art/战斗主界面资源需求.md)
- [BattleMain_UI_Workflow.md](/Users/gxx/Documents/UGit/GameNetty/docs/art/ui/BattleMain_UI_Workflow.md)
- [BattleMain_UI_TaskBoard.html](/Users/gxx/Documents/UGit/GameNetty/docs/art/ui/BattleMain_UI_TaskBoard.html)

## 1. 状态定义

| 状态 | 含义 |
| --- | --- |
| `Todo` | 已定义任务，尚未开始 |
| `Generating` | 正在生成 Source 批次 |
| `ReviewSource` | Source 图已生成，等待人工筛选 |
| `Regenerate` | Source 不合格，等待重生 |
| `Cropping` | Source 通过，正在裁切到 Final |
| `ReviewFinal` | Final 已输出，等待尺寸/透明/风格复检 |
| `ImportReady` | 可归档到正式图集 |
| `UnityVerify` | 已归档，等待 Unity 导入和 Prefab 验收 |
| `Done` | 已完成 |
| `Blocked` | 被依赖、接口、资源或设计问题阻塞 |

## 2. 任务卡字段

每个素材任务至少包含：

```text
TaskId：
资源：
是否必需：
final_size：
用途：
当前状态：
Source 批次：
Source 路径：
Final 路径：
正式路径：
验收重点：
下一步：
备注：
```

## 3. 当前批次

已生成批次：

```text
BatchId: p0_round_01
路径: Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/
范围: 首批 HUD 资源
状态: Done
```

下一步：

- 已完成 Source 生成、Final 裁切和正式图集归档。
- 已完成 Unity 导入设置和 `BattleMainWindow` Prefab 绑定验收。
- 首批 HUD 资源已完成。
- 资源需求表内所有未完成资源均为必需生成项。

## 4. 已完成任务池

### BMUI-P0-001 玩家血条底图

```text
资源：hp_bar_bg.png
是否必需：是
final_size：416x56
用途：玩家血条底图，9-slice
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/hp_bar_bg.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/hp_bar_bg.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/hp_bar_bg.png
验收重点：暗色内槽清晰；低高度；中段可拉伸；无文字数字；不含填充。
下一步：完成。
```

### BMUI-P0-002 玩家血条填充

```text
资源：hp_bar_fill.png
是否必需：是
final_size：352x28
用途：玩家 HP 填充，Image Filled
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/hp_bar_fill.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/hp_bar_fill.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/hp_bar_fill.png
验收重点：只保留红色填充条；无边框；横向干净；适合 0% 到 100% 填充。
下一步：完成。
```

### BMUI-P0-003 玩家血条边框

```text
资源：hp_bar_frame.png
是否必需：是
final_size：416x56
用途：玩家血条边框，9-slice
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/hp_bar_frame.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/hp_bar_frame.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/hp_bar_frame.png
验收重点：中间空心；边缘干净；中段可拉伸；不遮挡填充槽。
下一步：完成。
```

### BMUI-P0-004 Boss 血条底图

```text
资源：boss_hp_bar_bg.png
是否必需：是
final_size：960x72
用途：Boss 血条底图，9-slice
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/boss_hp_bar_bg.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/boss_hp_bar_bg.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/boss_hp_bar_bg.png
验收重点：宽薄横条；端部不过高；深色内槽；中段可拉伸；无填充。
下一步：完成。
```

### BMUI-P0-005 Boss 血条填充

```text
资源：boss_hp_bar_fill.png
是否必需：是
final_size：880x36
用途：Boss HP 填充，Image Filled
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/boss_hp_bar_fill.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/boss_hp_bar_fill.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/boss_hp_bar_fill.png
验收重点：长条红色填充；压迫感强但干净；无边框；不带火焰和过度发光。
下一步：完成。
```

### BMUI-P0-006 Boss 血条边框

```text
资源：boss_hp_bar_frame.png
是否必需：是
final_size：960x72
用途：Boss 血条边框，9-slice
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/boss_hp_bar_frame.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/boss_hp_bar_frame.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/boss_hp_bar_frame.png
验收重点：中间空心；端部低矮；中段可拉伸；顶部 HUD 读取清晰。
下一步：完成。
```

### BMUI-P0-007 波次文本底板

```text
资源：wave_panel_bg.png
是否必需：是
final_size：360x96
用途：波次文本底板，9-slice
当前状态：Done
Source 批次：p0_round_01
Source 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Source/p0_round_01/wave_panel_bg.source.png
Final 路径：Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/Final/wave_panel_bg.png
正式路径：Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/wave_panel_bg.png
验收重点：中心留白；小牌匾清晰；不烘焙文字数字；不遮挡战斗区。
下一步：完成。
```

## 5. 待生成 HUD 补充任务池

以下任务保留 `BMUI-P1-*` 历史编号，但不再表示生产层级；全部为必需生成项。

| TaskId | 资源 | final_size | 当前状态 | 下一步 |
| --- | --- | --- | --- | --- |
| `BMUI-P1-001` | `hp_icon.png` | `72x72` | `Todo` | 按已确认血条风格生成 |
| `BMUI-P1-002` | `boss_name_plate.png` | `360x96` | `Todo` | 等 Boss 血条风格确认后生成 |

## 6. 待生成发射器/Buff 任务池

以下任务保留 `BMUI-P2-*` 历史编号，但不再表示生产层级；全部为必需生成项。

| TaskId | 资源 | final_size | 当前状态 | 下一步 |
| --- | --- | --- | --- | --- |
| `BMUI-P2-001` | `emitter_card_bg.png` | `160x224` | `Todo` | 先生成，确定发射器卡基调 |
| `BMUI-P2-002` | `emitter_card_selected.png` | `176x240` | `Todo` | 依赖 `emitter_card_bg.png` |
| `BMUI-P2-003` | `buff_socket_small.png` | `40x40` | `Todo` | 依赖发射器卡底部布局 |
| `BMUI-P2-004` | `buff_popup_bg.png` | `380x200` | `Todo` | 依赖操作弹窗视觉关系 |
| `BMUI-P2-005` | `buff_socket_slot.png` | `88x88` | `Todo` | 依赖 `buff_popup_bg.png` |
| `BMUI-P2-006` | `buff_socket_add.png` | `88x88` | `Todo` | 依赖 `buff_socket_slot.png` |
| `BMUI-P2-007` | `buff_link_ribbon.png` | `240x48` | `Todo` | 依赖选中发射器到弹窗的连接方向 |
| `BMUI-P2-008` | `emitter_rope.png` | `760x80` | `Todo` | 发射器卡确定后补容器装饰 |
| `BMUI-P2-009` | `emitter_cooldown_mask.png` | `128x128` | `Todo` | 发射器卡图标区域确定后生成 |
| `BMUI-P2-010` | `emitter_lock.png` | `128x128` | `Todo` | 发射器卡图标区域确定后生成 |

## 7. 推荐看板

```text
Todo
  - BMUI-P1-001
  - BMUI-P1-002
  - BMUI-P2-001 ... BMUI-P2-010

ReviewSource
  - 空

Cropping
  - 空

ReviewFinal
  - 空

UnityVerify
  - 空

Done
  - BMUI-P0-001 ... BMUI-P0-007
```

## 8. 任务推进规则

- 一个任务只对应一个正式资源文件。
- 资源需求表内所有任务均为必需生成项，不再按生产层级跳过。
- 任务状态变化必须写明依据，例如采用哪个 Source 批次。
- Source 不合格时，不修改原图，不覆盖原批次，创建新批次重生。
- Final 合格后，才允许归档到正式图集。
- 正式归档后，任务进入 `UnityVerify`，不能直接标 `Done`。
- 只有 Unity 导入和 Prefab 验收都通过，任务才标 `Done`。
