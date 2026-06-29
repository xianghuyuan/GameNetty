# UI Prefab Workflow

用于 GameNetty 中新增或修改 Unity UI Prefab。该流程要求先形成 Prefab Spec，再通过 UnityMCP 或人工方式创建 Prefab。

如果输入仍是模糊需求，例如“做一个战斗界面”，应先经过 `.Codex/workflows/requirement-intake-workflow.md` 生成 Feature Spec 草稿和影响面分析，再进入本流程。

## 适用场景

- 新增 UI 面板
- 新增列表项模板
- 修改已有 UI Prefab 层级
- 新增或调整 UIBindComponent 绑定字段

## 输入

- 策划案或 UI 功能说明
- 效果图、截图或视觉描述
- 项目 UI 命名规范
- 参考 Prefab，可选
- 资源路径或资源需求清单

## 流程

### 1. 读取输入

- 明确 UI 用途
- 明确交互行为
- 明确显示状态
- 明确需要脚本绑定的节点

### 2. 生成 Prefab Spec

- 使用 `.Codex/templates/prefab-spec.template.md`
- 输出到 `.Codex/plans/ui/<prefab-name>.prefab-spec.md`
- Spec 中必须包含层级结构、节点明细、绑定字段、资源需求、验收标准

### 3. Review Prefab Spec

- 确认节点命名是否符合项目规范
- 确认组件类型是否符合 GameNetty UI 体系
- 确认绑定字段是否够用且不过度绑定
- 确认缺失资源是否允许使用占位

### 4. 创建或修改 Prefab

- 优先通过 UnityMCP 创建或修改 Prefab
- UI 层级必须落在 Prefab 中
- 不在业务代码中动态创建面板、列表项或核心 UI 节点

### 5. 绑定和脚本接入

- 添加或更新 UIBindComponent
- 更新 Widget 代码绑定字段
- 使用 TEngine `CreateWidget` / `CreateWidgetByPrefab` / `AdjustIconNum` 等项目既有方式复用 UI

### 6. 验证

- Prefab 路径存在
- 层级符合 Prefab Spec
- 组件完整
- 绑定字段完整
- 无 Missing Reference
- Unity Console 无关键错误
- 截图或人工视觉检查通过

## 输出

- Prefab Spec
- Unity Prefab
- 绑定脚本改动
- Prefab Harness 验证记录
