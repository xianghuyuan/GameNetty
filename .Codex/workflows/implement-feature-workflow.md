# Implement Feature Workflow

用于 GameNetty 中“新增完整功能模块”的通用研发流程。它负责编排步骤，不替代具体 skill。

新需求应先经过 `.Codex/workflows/requirement-intake-workflow.md`，形成 Feature Spec 草稿和影响面分析后，再进入本流程。

## 适用场景

- 新增玩法系统、养成系统、UI 功能、战斗功能
- 涉及服务端、客户端、配置、协议、UI、Prefab 中两个以上模块
- 需要阶段验收和中断后继续推进

## 输入

- 用户需求或策划案
- 相关 docs 业务规范
- 参考代码、参考 Prefab、参考配置
- 美术/效果图/资源清单，可选

## 流程

### 1. 需求读取

- 明确功能目标
- 明确玩家体验
- 明确完成标准
- 明确本次不做什么
- 查阅相关 docs 和现有代码

### 2. 生成 Feature Spec

- 生成或更新 `.Codex/plans/features/<feature-name>.feature-spec.md`
- 拆分服务端、客户端、配置、协议、UI、Prefab、资源、测试影响面
- 列出待确认问题

### 3. 技术影响分析

- 是否涉及 Proto / Opcode
- 是否涉及 Luban 配置
- 是否涉及 Server Model / Hotfix
- 是否涉及 Unity GameLogic
- 是否涉及 UI Prefab / Widget / UIBindComponent
- 是否涉及 BattleRoom / Unit / BattleUnit
- 是否涉及测试场景或 Debug 入口

### 4. 子 Skill 编排

按影响面选择具体 skill：

- `proto-config`：协议、Opcode、Luban 配置、生成代码同步
- `unity-ui-prefab`：UI Prefab、Widget、UIBindComponent、列表项模板
- `unity-mcp-orchestrator`：Unity Editor、GameObject、Prefab、场景自动化
- `fix-bug`：实现过程中发现的功能异常修复

### 5. 实现

- 服务端逻辑放在 `Server/Hotfix/Demo/`
- 服务端数据结构放在 `Server/Model/Demo/`
- 客户端逻辑放在 `Unity/Assets/GameScripts/HotFix/GameLogic/Module/`
- UI 结构创建或修改 Prefab，不在业务代码中 `new GameObject(...)`
- Proto 手动同步服务端和客户端生成代码

### 6. 验证

- Share 构建
- Server 构建
- Proto / Opcode 双端一致性检查
- 配置生成和配置引用检查
- Prefab 层级、组件、绑定、资源引用检查
- Unity Console 检查
- 功能验收清单

### 7. 交付

- 汇总改动文件
- 汇总验证结果
- 标记已知问题和剩余风险
- 更新对应 plan / harness 状态

## 输出

- Feature Spec
- 具体实现改动
- 验证记录
- 交付总结
