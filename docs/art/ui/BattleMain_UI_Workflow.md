# BattleMain UI 素材生产工作流

本文档定义 `BattleMainWindow` UI 素材从生图到 Unity Prefab 使用的完整工作流。

目标：

- 每张素材都有明确尺寸、批次、来源和验收结果。
- Source 原图、Final 裁切图、正式图集资源互不混用。
- 不合格资源能快速重生，不污染正式目录。
- 美术、策划、程序能围绕同一套资源状态协作。

## 1. 目录约定

```text
Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main/
├── Source/                         # 生图源图批次，不直接进 Unity 正式引用
│   └── <batch_id>/
│       ├── manifest.tsv
│       ├── <name>.source.png
│       ├── <name>.request.json
│       └── <name>.response.json
├── Final/                          # 已按 final_size 裁切的候选正式图
│   └── <name>.png
└── Review/                         # 评审记录和问题单
    └── battle_main_review_notes.md

Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/
└── <name>.png                      # 验收通过后的正式图集资源
```

## 2. 工作流总览

```text
资源表锁定
  -> 生图批次
  -> Source 初筛
  -> 重生或通过
  -> 裁切到 Final
  -> Final 复检
  -> 正式归档
  -> Unity 导入验收
  -> Prefab 绑定验收
```

## 3. 阶段定义

### 3.1 资源表锁定

输入：

- `spec/art/战斗主界面资源需求.md`
- `Unity/Assets/AssetRaw/UI/Battle/BattleMainWindow.prefab`

动作：

- 确认资源名、`final_size`、是否必需、用途和 Unity 设置。
- 新资源必须先写入资源需求文档，再通过 Codex `imagegen` 或人工美术工具生产。
- 禁止使用 Python 脚本生成正式美术素材，也禁止用代码绘制方式替代生图。

通过标准：

- 每个资源都有稳定 ASCII 文件名。
- 每个资源都有唯一 `final_size`。
- 资源能对应到 Prefab 节点或运行时用途。

### 3.2 生图批次

执行方式：

- 使用 Codex `imagegen` 按单个资源 prompt 生成。
- 或使用 Photoshop、Affinity 等美术工具人工制作。
- 不使用 Python 脚本、shell 脚本或代码绘制方式生成正式美术素材。

输出：

- `Source/<batch_id>/<name>.source.png`
- `Source/<batch_id>/<name>.prompt.md`
- `Source/<batch_id>/<name>.review.md`

通过标准：

- 每个资源都有 source、prompt、review 记录。
- 生成结果不直接放入正式图集。

### 3.3 Source 初筛

检查项：

- 是否符合奶油系轻幻想月夜战斗 UI 风格。
- 是否是单个透明 UI 组件，而不是完整 HUD 截图。
- 是否烘焙了文字、数字、角色、敌人、背景或 UI 标签。
- 横条资源是否按目标比例设计，端部装饰是否过高。
- 发射器卡是否为竖向长牌，底部是否预留 Buff 小标记位置。
- Buff 操作弹窗是否像依附发射器的轻量抽屉，而不是常驻大面板。

处理：

- 通过：进入裁切。
- 轻微边缘问题：允许后处理一次。
- 风格、构图、文字、比例错误：判废并重生。

### 3.4 重生

重生时不覆盖已采用资源，创建新的 Source 记录。使用 Codex `imagegen` 或人工美术工具重新生产，不使用 Python 或 shell 生图脚本。

重生规则：

- 风格不符：改 prompt 风格锚点。
- 构图太小或太大：强化主体占比和 final_size。
- 有文字或数字：强化负面约束，直接重生。
- 横条端部过高：强化低高度、低端盖规则。
- 9-slice 中段被装饰打断：要求中段留出干净可拉伸区域。

### 3.5 裁切到 Final

动作：

- 从通过初筛的 Source 图中裁切目标区域。
- 导出到 `Final/<name>.png`。
- 像素尺寸必须等于资源表的 `final_size`。

要求：

- 不保留 `.source` 后缀。
- 背景必须透明。
- 清理绿边、脏边、孤立像素和半透明噪点。
- Fill 资源只保留填充条，不包含边框。
- Frame 资源中间空心，保留 9-slice 边缘。

### 3.6 Final 复检

检查项：

- 像素尺寸等于 `final_size`。
- Alpha 边缘干净。
- 主体未被裁断。
- 文本区域留白足够。
- 横条资源可拉伸区域干净。
- 发射器卡、Buff 弹窗不会在 HUD 中占用过大面积。

通过后：

- 进入正式归档。

失败后：

- 能通过裁切修正的，重新输出 Final。
- 不能修正的，回到重生阶段。

### 3.7 正式归档

动作：

```text
Final/<name>.png
  -> Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/<name>.png
```

要求：

- 不使用 `Raw/Generated/`。
- 不使用 `.source.png`。
- 不带批次号、尺寸后缀或版本号。
- 覆盖已有正式资源前必须确认旧资源是否还被 Prefab 使用。

### 3.8 Unity 导入验收

检查项：

- `Image Filled`：0%、50%、100% 填充都正常。
- `9-slice`：横向拉伸不破边。
- 透明图没有异常黑边、白边或彩边。
- 资源进入正确图集目录。
- Meta 导入设置与用途一致。

### 3.9 Prefab 绑定验收

检查项：

- 静态装饰节点不绑定 `m_` 字段。
- 代码控制节点使用 `m_` 前缀。
- 发射器槽使用 `BattleEmitterSlotUI`。
- Buff 操作弹窗使用 `BattleEmitterBuffPopupWidget`。
- Buff 操作弹窗默认隐藏，只在点击或长按发射器槽后展开。
- `BattleMainWindow` 不在业务代码里动态创建 UI 层级。

## 4. 评审记录模板

```text
批次：
日期：
评审人：

资源：
Source 路径：
目标 final_size：
结论：通过 / 重生 / 后处理
问题：
处理建议：
采用到 Final：是 / 否
正式归档：是 / 否
备注：
```

## 5. 首批 HUD 推荐执行顺序

1. `hp_bar_bg.png`
2. `hp_bar_fill.png`
3. `hp_bar_frame.png`
4. `boss_hp_bar_bg.png`
5. `boss_hp_bar_fill.png`
6. `boss_hp_bar_frame.png`
7. `wave_panel_bg.png`

原因：

- 先完成玩家血条，能最快验证基础 HUD。
- 再完成 Boss 血条，验证顶部大横条和 9-slice。
- 最后做波次面板，验证文本底板可读性。

## 6. 发射器/Buff 推荐执行顺序

1. `emitter_card_bg.png`
2. `emitter_card_selected.png`
3. `buff_socket_small.png`
4. `buff_popup_bg.png`
5. `buff_socket_slot.png`
6. `buff_socket_add.png`
7. `buff_link_ribbon.png`
8. `emitter_rope.png`
9. `emitter_cooldown_mask.png`
10. `emitter_lock.png`

原因：

- 先确定发射器卡的视觉基调。
- 再确定选中态和 Buff 挂载关系。
- 最后补冷却、锁定和容器装饰。
