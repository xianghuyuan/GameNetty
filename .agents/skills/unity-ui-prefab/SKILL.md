---
name: unity-ui-prefab
description: 处理 GameNetty 的 Unity UI Prefab、TEngine Widget、UIBindComponent、列表项模板、图标数量调整和界面绑定问题。用于新增或修改 UI 面板结构、Prefab 层级、绑定组件和 UI 表现异常排查。
---

# Unity UI Prefab Skill

## 适用范围

用于：

- 新增或修改 Unity UI Prefab 层级。
- 调整 TEngine Widget、列表项模板、按钮、文本、图片、图标数量等 UI 绑定。
- 排查 UI 元素为空、绑定失败、列表项复用异常、Prefab 结构不匹配的问题。
- 需要通过 Unity MCP 修改 Prefab、GameObject、组件或场景对象的任务。

不用于：

- 不涉及 UI 的服务端逻辑、协议、配置改动。
- Proto 或 Luban 配置生成；使用 `proto-config`。
- 纯战斗逻辑规则判断；战斗 UI 可结合 `battle-system`。

## 核心规则

- Unity/TEngine UI 结构必须放在 Prefab 中。
- 不要在业务代码中用 `new GameObject(...)` 或 `AddComponent(...)` 创建 UI 面板、列表项或 Prefab 层级。
- 业务代码应通过 TEngine 的 `CreateWidget`、`CreateWidgetByPrefab`、`AdjustIconNum` 等既有方式绑定和复用 UI。
- Prefab 节点命名应遵循现有绑定习惯，例如 `m_` 前缀的可绑定字段。
- 可点击 UI 节点必须挂项目 `GameLogic.UIButton`，不要直接挂载或绑定 Unity 原生 `Button`。生成绑定字段使用 `UIButton`，点击事件使用 `UIButton.SetClick(...)`，不要直接使用 `Button.onClick.AddListener(...)`。
- 修改 UI 结构时，同步检查对应 C# 绑定字段、生成绑定代码、事件注册和生命周期释放。
- 如果任务需要操作 Unity Editor，优先使用 Unity MCP；不要只靠代码猜测 Prefab 真实层级。
- 修改 Unity Prefab 时必须优先使用 Unity MCP。若 Unity MCP 不可用、未连接、无法读取目标 Prefab 或无法完成修改，必须先停止并告知用户，由用户决定是否继续使用直接 YAML 编辑等非 MCP 方案；不得静默绕过 Unity MCP 修改 Prefab 文件。

## 工作流程

1. 明确 UI 改动对象：面板、Widget、列表项、按钮、文本、图片、图标或特效节点。
2. 读取 `AGENTS.md`，再搜索相关 Prefab、UI 类、Widget 创建点和绑定字段。
3. 查看现有相似 UI 的 Prefab 结构和代码绑定方式。
4. 修改 Prefab 层级或组件时，保持与现有 TEngine 绑定风格一致。
5. 修改业务代码时，只处理数据填充、事件响应和 Widget 复用逻辑，不在代码中动态搭 UI 层级。
6. 检查空引用、重复注册事件、列表项回收、生命周期释放等常见问题。

## 验证

- 检查 Prefab 中目标节点和组件确实存在。
- 检查 C# 绑定字段名称与 Prefab 节点名称一致。
- 检查列表项模板能被 `CreateWidget`、`CreateWidgetByPrefab` 或 `AdjustIconNum` 正确复用。
- 如果修改了客户端 C#，尽量通过 Unity 编译或已有构建流程验证；无法验证时说明原因。
- 如果使用 Unity MCP，保存 Prefab 后再次读取层级或组件确认改动落地。

## 冲突处理

- 与流程型 skill 冲突时，流程由 `fix-bug` 或 `implement-feature` 决定，本 skill 负责 UI Prefab 和绑定规则。
- 与 `proto-config` 冲突时，协议和配置同步由 `proto-config` 决定，UI 展示和绑定由本 skill 决定。
- 如果某个实现方案要求在业务代码中动态创建 UI 层级，应改为 Prefab 方案；无法改为 Prefab 时先向用户确认。

## 最终汇报

汇报：

- 修改了哪些 Prefab、UI 类或绑定字段。
- 是否涉及 TEngine Widget 创建或列表项复用。
- 验证了哪些 Prefab 层级、组件和绑定关系。
- 未能在 Unity 中验证的部分和剩余风险。
