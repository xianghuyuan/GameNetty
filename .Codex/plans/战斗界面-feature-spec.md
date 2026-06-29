# Feature Spec: 战斗界面

## 文档元信息

- 文档类型：Feature Spec
- 状态：草稿 / 布局已确认
- 使用 Workflow：`.Codex/workflows/requirement-intake-workflow.md`
- 后续候选 Workflow：
  - `.Codex/workflows/implement-feature-workflow.md`
  - `.Codex/workflows/ui-prefab-workflow.md`
- Wireframe：
  - `.Codex/plans/战斗界面-wireframe.svg`
- Visual Mockup Spec：
  - `.Codex/plans/战斗界面-visual-mockup-spec.md`
- 来源需求：用户提出“做一个战斗界面”
- 上游文档：
  - `AGENTS.md`
- 下游文档：
  - `.Codex/plans/战斗界面-visual-mockup-spec.md`
  - 待生成 `BattleMainWindow-prefab-spec.md`
  - 待生成 `战斗界面-harness.md`
- 最近整理：2026-06-04

## 原始需求

> 做一个战斗界面

## 目标

为 GameNetty 的战斗场景定义第一版常驻战斗界面需求，优先围绕现有 `BattleMainWindow` 承接玩家战斗中的核心状态展示、波次信息、Boss 信息、发射器与其上方 Buff 展示，以及开发调试入口。

第一阶段目标不是立即重做 Prefab，而是先明确战斗界面的范围、信息层级和现有 UI 复用边界。

本次已确认第一版定位为：

> 正式 HUD + 开发调试入口的混合版本

## 玩家体验

玩家进入战斗后，应能在不遮挡割草战斗主体的前提下快速读到：

- 自身生命状态
- 当前波次进度
- Boss 名称和生命状态，若有 Boss
- 当前控制模式或调试状态，开发期可见
- 发射器槽位，以及每个发射器上方挂载的 Buff
- 暂停、设置等基础操作入口
- 开发期调试入口，但不应破坏正式 HUD 的信息层级

界面整体应服务长时间战斗阅读，避免传统 RPG 装备栏感和过重面板感。

## 已知现状

项目中已有战斗 UI 基础：

- Prefab：
  - `Unity/Assets/AssetRaw/UI/Battle/BattleMainWindow.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleEmitterOwnedBarWidget.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleEmitterAddPanelWidget.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleBuffAddPanelWidget.prefab`
  - `Unity/Assets/AssetRaw/UI/Battle/BattleGMWidget.prefab`
- 代码：
  - `Unity/Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainWindow.cs`
  - `Unity/Assets/GameScripts/HotFix/GameLogic/UI/Gen/BattleMainWindow_Gen.g.cs`
  - `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/UI/BattleUIHelper.cs`
- 已绑定字段：
  - `m_imgBossHp`
  - `m_tmpBossHp`
  - `m_tfEnemy`
  - `m_tmpBossName`
  - `m_btnPause`
  - `m_btnBookmark`
  - `m_btnSpawnEnemy`
  - `m_imgPlayerHp`
  - `m_tmpPlayerHp`
  - `m_tmpControlMode`
  - `m_tmpWave`
  - `m_btnGear`

## 第一版范围

第一版建议以“整理并增强现有 `BattleMainWindow`”为目标，而不是新建一套完全独立的战斗界面。

必须包含：

- 玩家 HP 条和数值文本
- 波次文本
- Boss 区域，支持无 Boss 时隐藏
- 暂停按钮
- 设置按钮
- 开发期刷怪按钮或 GM 入口，作为混合版本的一部分保留，后续可按构建环境隐藏
- 发射器槽位展示
- 发射器上方 Buff 展示

建议包含：

- 当前发射器列表，复用或改造 `BattleEmitterOwnedBarWidget`
- 每个发射器上方的 Buff 图标或短标签
- 控制模式文本，开发期保留

默认不包含：

- 主动技能按钮 / 手动释放入口

说明：

- “主动技能按钮”指玩家手动点击释放的技能入口，例如普攻、技能 1、技能 2、闪避、大招。
- “发射器”不是主动技能按钮，它属于局内构筑和自动触发执行模型。
- 第一版战斗 HUD 默认不做手动技能按钮，除非后续确认要引入手动释放玩法。
- 发射器相关信息应直接作为战斗 HUD 的核心构筑展示，而不是抽象成装备栏或技能按钮。

开发调试入口原则：

- 调试入口可以存在于第一版 HUD 中
- 调试入口视觉权重低于正式战斗信息
- 调试入口应集中放置，避免散落在正式 HUD 信息区
- 后续应支持按构建环境或 GM 开关隐藏

## 不做什么

- 不在业务代码中动态创建核心 UI 层级
- 不新增服务端协议，除非后续确认现有战斗事件无法满足 UI 数据
- 不在第一版做完整战斗结算界面
- 不在第一版做完整技能轮盘或手动释放技能面板
- 不把 `Emitter` / `Buff` / `Modifier` 作为玩家层主要文案表达

## 规则说明

- 战斗 UI 由 `BattleStart_UI` 打开 `BattleMainWindow`
- `BattleMainWindow.OnCreate` 绑定窗口到 `BattleUIHelper`
- 战斗单位数值、伤害、死亡、波次事件通过 `BattleUIHelper` 刷新 UI
- UI 结构必须在 Prefab 中定义
- Widget 复用应使用 TEngine 现有创建与绑定方式

## 技术影响面

- Server：第一版不涉及
- Client：涉及 `BattleMainWindow`、`BattleUIHelper`、战斗事件 UI 刷新
- Proto：第一版不涉及，除非需要新增 UI 数据推送
- Config：可能涉及局内成长项图标、显示名称、品质等配置
- UI / Prefab：涉及 `BattleMainWindow.prefab` 和相关 Widget Prefab
- Art / Audio：涉及 HUD 资产、按钮图标、血条、成长摘要图标
- Test / Harness：需要战斗入口、离线战斗或测试场景验证

## 数据结构

第一版优先复用现有运行时数据：

- `Battle`
- `BattleUnit`
- `BattleUnitCombatComponent`
- `BattleUnitNumericChange`
- `BattleUnitDamaged`
- `WaveStart`
- `WaveComplete`

发射器与 Buff 展示应再确认复用或新增：

- 当前玩家拥有的发射器列表
- 每个发射器上绑定或影响的 Buff 列表
- 发射器冷却、等级或品质信息，若第一版需要
- Buff 层数、持续时间或等级信息，若第一版需要

发射器 / Buff 展示定义：

- 用于展示玩家本局实际拥有的发射器，以及发射器上方关联的 Buff
- 不等同于装备栏、背包或主动技能按钮
- 发射器可以作为程序层概念进入开发期 HUD，但正式文案可后续题材化
- 第一版优先展示归属关系：每个发射器槽位内部自带 BuffStack
- 若信息过多，优先保留发射器槽位和 Buff 图标，详细描述放到点击或调试面板

## 流程

### 进入战斗

1. 战斗开始发布 `BattleStart`
2. `BattleStart_UI` 打开 `BattleMainWindow`
3. `BattleMainWindow.OnCreate` 创建子 Widget 并绑定 `BattleUIHelper`
4. `BattleUIHelper.OnBattleStarted` 设置当前 Battle
5. UI 初次刷新玩家、Boss、波次和构筑摘要

### 战斗中刷新

1. HP 或 MaxHp 变化时刷新玩家 / Boss 血条
2. 单位受伤时刷新目标对应 UI
3. 波次开始和结束时刷新波次文本
4. 局内成长变化时刷新成长摘要，待确认事件来源

### 退出战斗

1. 关闭 `BattleMainWindow`
2. `BattleUIHelper.ClearAll` 清理窗口引用
3. 子 Widget 释放引用和事件

## UI 需求

- 面板：`BattleMainWindow`
- 低保真布局图：`.Codex/plans/战斗界面-wireframe.svg`
- 视觉稿规格：`.Codex/plans/战斗界面-visual-mockup-spec.md`
- Widget：
  - `BattleEmitterOwnedBarWidget`
  - `BattleEmitterAddPanelWidget`
  - `BattleBuffAddPanelWidget`
  - `BattleGMWidget`
- 绑定字段：
  - 现有字段先保留
  - 新增字段待 Prefab Spec 确认
- 交互：
  - 暂停
  - 设置
  - 开发期刷怪 / GM
  - 发射器查看 / 调试
  - Buff 查看 / 调试

## 配置需求

- 配置表：待确认
- 字段：若展示局内成长图标，需要成长项图标、品质、名称、描述
- 默认值：待确认
- 校验规则：资源引用不缺失，图标和品质框可正确加载

## 协议需求

- 请求：第一版不新增
- 响应：第一版不新增
- 推送：第一版优先复用现有战斗消息和事件
- Opcode：第一版不新增

## 验收清单

- [ ] 需求范围已确认
- [ ] `BattleMainWindow-prefab-spec.md` 已生成并确认
- [ ] Prefab 层级符合 Spec
- [ ] UIBindComponent 绑定完整
- [ ] 玩家 HP 能正确刷新
- [ ] Boss 区域有 Boss 时显示、无 Boss 时隐藏
- [ ] 波次文本正确刷新
- [ ] 暂停、设置、开发按钮行为明确
- [x] 发射器与其 BuffStack 为第一版核心构筑展示
- [x] 主动技能按钮第一版默认不包含
- [ ] Unity Console 无关键错误
- [ ] 手动进入战斗验证通过

## 待确认问题

- [已确认] 战斗界面第一版定位为“正式 HUD + 开发调试入口”的混合版本。
- 是否已有战斗界面效果图或主美方向？
- [已确认] 第一版默认不包含主动技能按钮 / 手动释放入口；发射器归入局内成长摘要或构筑展示。
- [已确认] 第一版核心只展示发射器和每个发射器内部的 BuffStack，不展示抽象局内成长摘要。
- [已确认] Wireframe 布局合理：发射器槽位内部自带 BuffStack。
- 第一版是否需要展示候选成长进度？
- `BattleGMWidget` 和 `m_btnSpawnEnemy` 是否只在开发环境显示？
- Boss 血条是常驻顶部展示，还是仅 Boss 出现时弹出？
- 是否需要适配横屏移动端安全区？
- 是否要把现有 `BattleEmitterOwnedBarWidget` 改造成“发射器 + Buff 关系展示”？
