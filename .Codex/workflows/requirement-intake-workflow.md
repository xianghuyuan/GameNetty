# Requirement Intake Workflow

用于把用户的一句目标、策划想法或模糊需求，整理成可执行的 Feature Spec 草稿，并判断后续需要进入哪些具体 workflow 或 skill。

## 适用场景

- 用户提出一个新功能、新界面、新玩法或系统方向
- 需求还不够明确，不能直接进入实现
- 不确定是否涉及 UI、协议、配置、服务端、客户端、Prefab 或美术资源
- 需要先明确范围、完成标准和不做什么

## 输入

- 用户目标或策划想法
- 已有策划案、效果图、参考游戏、参考文档，可选
- 相关项目 docs，可选
- 现有代码、Prefab、配置、协议，可选

## 流程

### 1. 记录原始需求

- 保留用户原话
- 标记需求来源
- 标记创建时间

### 2. 需求澄清

优先明确以下问题：

- 这是新功能、已有功能调整、UI 表现、配置调整，还是 Bug 修复？
- 玩家最终看到或操作到什么？
- 第一版必须包含什么？
- 本次明确不做什么？
- 是否已有设计稿、效果图或参考 Prefab？
- 是否涉及服务端消息、配置表、战斗逻辑或持久化数据？
- 是否需要 Unity Prefab、Widget、资源或美术资产？

如果关键信息缺失，先形成“待确认问题”，不要直接实现。

### 3. 生成 Feature Spec 草稿

使用 `.Codex/templates/feature-spec.template.md`，生成到：

```text
.Codex/plans/<功能名>-feature-spec.md
```

草稿至少包含：

- 目标
- 玩家体验
- 完成标准
- 不做什么
- 技术影响面初判
- 待确认问题

### 4. 影响面分析

根据 Feature Spec 草稿判断后续需要哪些流程：

- 涉及完整新功能：进入 `.Codex/workflows/implement-feature-workflow.md`
- 涉及 UI Prefab / Widget / 绑定：进入 `.Codex/workflows/ui-prefab-workflow.md`
- 涉及 Proto / Opcode / Luban 配置：使用 `proto-config`
- 涉及 Unity Editor / Prefab / 场景自动化：使用 `unity-mcp-orchestrator`
- 涉及功能异常：使用 `fix-bug`

### 5. 形成执行入口

输出本次建议的执行结构：

```text
主 Workflow：
子 Workflow / Skill：
需要生成的 Spec：
需要生成的 Harness：
待确认问题：
```

### 6. 人工确认

在需求不清晰、影响面较大或存在设计分歧时，先让用户确认 Feature Spec 草稿和执行入口，再进入实现。

## 输出

- `<功能名>-feature-spec.md`
- 影响面分析
- 推荐 workflow / skill 编排
- 待确认问题列表

## 验收标准

- [ ] 原始需求已记录
- [ ] 目标明确
- [ ] 第一版范围明确
- [ ] 不做什么明确
- [ ] 完成标准明确
- [ ] 影响面已初步判断
- [ ] 后续 workflow / skill 已选择
- [ ] 待确认问题已列出
