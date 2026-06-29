# BattleMainWindow UI 标注说明

本文档是战斗主界面效果图的人工语义说明，用于指导工具生成或刷新 Prefab。它不是工具中间产物，`ui_layout.json` 由工具生成和记录，不作为人工修改入口。

## 输入文件

- `image.png`：战斗主界面整体效果图。
- `image-1.png`：Boss 信息栏局部标注图。

正式流程中，同一个界面只保留一张最终效果图，文件名应与界面名一致，例如 `BattleMainWindow.png`。不要使用 `preview`、`effect`、`v2`、`v3` 等后缀区分版本。

效果图和图集迭代时，默认先给用户预览，不自动保存为新的正式版本。只有用户确认“使用这一版”后，才替换当前正式文件。不要因为多次尝试就新建多个版本目录或多个后缀文件，例如 `Generated`、`GeneratedDetailed`、`GeneratedSplit`、`Painterly`、`v2`、`preview`。同一个界面同一类产物只保留当前确认版本。

## Prefab 目标

- Prefab 路径：`Unity/Assets/AssetRaw/UI/Battle/BattleMainWindow.prefab`
- 图片资源目录：`Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/`
- 根节点：`BattleMainWindow`
- 根节点组件：`GameLogic.UIBindComponent`
- 文本组件：`GameLogic.UIText`
- 可点击节点组件：`GameLogic.UIButton`

业务代码只负责刷新数值、注册点击响应和复用列表/模板，不应动态创建 UI 层级。

## 整体布局

界面按 1280x720 设计坐标理解，中心战斗区域需要尽量留空，避免遮挡角色和怪物。

- 顶部居中：Boss 信息栏。
- 左上偏中：玩家状态栏。
- 右上：系统按钮组。
- 右侧中上：波次信息。
- 底部居中：发射器/卡牌栏。
- 中央大面积区域：战斗表现留白，不放常驻 UI。

## Boss 信息栏

对应 `image-1.png`，由以下元素组成：

- Boss 名称底图：装饰底图，不需要绑定。
- Boss 名称文本：绑定为 `m_tmpBossName`。
- Boss 血量框：装饰外框，不需要绑定。
- Boss 血量条：绑定为 `m_imgBossHp`。
- Boss 血量文本：绑定为 `m_tmpBossHp`。

Boss 血量条必须支持运行时缩放：

- `m_imgBossHp` 的锚点和中心点放在左侧。
- 满血宽度等于血量框内可填充宽度。
- 更新血量时只调整 `RectTransform.sizeDelta.x`。
- 左边缘保持不动，右边缘随血量减少向左收缩。
- 血量文本显示格式示例：`5000/5000`。

血量刷新逻辑应由业务代码根据当前血量比例计算宽度，不通过动态创建或替换节点实现。

## 玩家状态栏

玩家状态栏包含头像/生命标识、生命条、生命文本、能量条和控制模式文本。

建议节点语义：

- 生命标识图：装饰或状态图标，不需要绑定。
- 玩家生命条：绑定为 `m_imgPlayerHp`。
- 玩家生命文本：绑定为 `m_tmpPlayerHp`。
- 玩家能量条：绑定为 `m_imgPlayerEnergy`。
- 控制模式文本：绑定为 `m_tmpControlMode`，示例文案为 `Auto`。

玩家生命条和能量条也应采用左锚点伸缩规则：

- 条形图左侧固定。
- 通过 `sizeDelta.x` 改变长度。
- 不在业务代码中创建新的血条或能量条对象。

## 波次信息

波次面板位于右侧中上区域，包含一个底图和文本。

- 波次底图：装饰底图，不需要绑定。
- 波次文本：绑定为 `m_tmpWave`。
- 文本格式示例：`Wave 1/5`。

波次面板不参与点击交互。

## 系统按钮组

右上角有三个圆形按钮：

- 暂停按钮：绑定为 `m_btnPause`，组件为 `GameLogic.UIButton`。
- 图鉴/书签按钮：绑定为 `m_btnBookmark`，组件为 `GameLogic.UIButton`。
- 添加/调试按钮：绑定为 `m_btnGear`，组件为 `GameLogic.UIButton`。

按钮业务事件通过生成绑定中的 `UIButton.SetClick(...)` 注册，不直接使用 Unity 原生 `Button.onClick.AddListener(...)`。

## 发射器栏

底部居中的发射器栏用于展示当前可用的发射器/卡牌槽位。区域整体需要保持在底部，不遮挡主要战斗角色。

建议结构：

- 发射器栏容器：绑定为 `m_tfEmitterBar`。
- 背景底板：装饰底图，不需要绑定。
- 第一个槽位模板：绑定为 `m_itemEmitterSlot`。
- 其他槽位：按模板复制或由工具生成静态槽位。
- 每个槽位下方有 4 个 Buff 插槽装饰点。

`m_itemEmitterSlot` 是复用模板。运行时需要增删或刷新发射器时，应使用项目现有 Widget/Prefab 复用方式，例如 `CreateWidget`、`CreateWidgetByPrefab` 或 `AdjustIconNum`，不要在业务代码中手动搭 UI 层级。

## 发射器冷却表现

当前效果图中，第 5 个槽位展示冷却状态。

冷却表现由两个绑定节点表达：

- 冷却底盘：不绑定，资源为 `emitter_cooldown_bg.png`。
- 冷却进度条：绑定为 `m_imgCooldown05`，资源为 `emitter_cooldown_progress.png`。
- 冷却倒计时文本：绑定为 `m_tmpCooldown05`。

冷却节点应覆盖在对应槽位图标上方，而不是放在槽位底部的小方块位置。小方块没有明确功能表现需求，正式结构中不应保留为独立表现节点。

冷却刷新规则：

- 通过 `m_imgCooldown05` 表示冷却进度，图片类型应为 `Filled/Radial360`。
- 通过 `m_tmpCooldown05` 显示剩余秒数，示例：`6.8`。
- 不同时叠加两套不同的冷却表现，避免出现“两个 CD 表现”。
- 不使用一张合成图同时包含底盘和进度条；底和进度必须拆成两个独立 PNG。

## 资源规则

- `battle_main/` 目录只保留当前布局引用的正式 PNG。
- 效果图迭代后，旧的无引用 PNG 应删除。
- 过程图、临时图、旧预览图不放入正式资源目录。
- 预览中的 imagegen 输出、拆分试验图集和临时 sprite 不建立多个长期目录；未确认的结果只作为预览存在，确认后覆盖当前目录。
- 不从 `_Incoming` 作为正式流程入口；确定性阶段的输入和说明应放在 Unity 项目外的 `docs/` 或 `spec/` 目录。

## 生成规则

工具应读取效果图和本文档，生成或刷新：

- `ui_layout.json`：工具记录文件，不人工修改。
- Prefab 层级和 RectTransform。
- 图片资源引用。
- `UIBindComponent` 绑定列表。
- 绑定代码。

如果本文档与 `ui_layout.json` 不一致，以本文档和当前效果图为准，由工具重新生成 `ui_layout.json`。

## 验收点

- Prefab 主体布局与 `BattleMainWindow.png` 基本一致。
- 中央战斗区域没有被底部 UI 大面积遮挡。
- Boss 血条、玩家血条、玩家能量条都能从左向右按宽度表现进度。
- 文本节点使用 `GameLogic.UIText`。
- 按钮节点使用 `GameLogic.UIButton`。
- 根节点有 `GameLogic.UIBindComponent`。
- 正式素材目录没有旧版本、预览图或未引用 PNG。
