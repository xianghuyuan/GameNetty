# Prefab Spec: <PrefabName>

## 基本信息

- Prefab 名称：
- 保存路径：
- 所属系统：
- 用途：
- 关联 Workflow：`.Codex/workflows/ui-prefab-workflow.md`
- 关联 Harness：

## 输入来源

- 策划案：
- 效果图 / 截图：
- 参考 Prefab：
- 资源清单：

## 项目规范

- UI 图片节点使用 `m_img` 前缀
- 文本节点使用 `m_tmp` 前缀，优先使用 `GameLogic.UIText`
- 按钮节点使用 `m_btn` 前缀
- Transform 容器节点使用 `m_tf` 前缀
- 需要业务绑定的节点使用 `m_` 前缀
- 使用 `GameLogic.UIBindComponent` 管理绑定
- UI 层级创建在 Prefab 中，不在业务代码中动态创建核心 UI 节点

## Root 节点

- 节点名：
- 组件：
  - RectTransform
  - CanvasRenderer
  - GameLogic.UIBindComponent
- 尺寸：
- Anchor：
- Pivot：
- 默认状态：

## 层级结构

```text
<PrefabName>
  ├── m_imgBg
  ├── m_tfContent
  └── m_btnClose
```

## 节点明细

### m_imgBg

- 组件：Image
- 作用：
- 资源：
- 是否绑定：是
- 默认显示：是
- RectTransform：

### m_tfContent

- 组件：RectTransform
- 作用：
- 是否绑定：是
- 默认显示：是
- RectTransform：

### m_btnClose

- 组件：Image, Button
- 作用：
- 资源：
- 是否绑定：是
- 默认显示：是
- RectTransform：

## 绑定字段

- `m_imgBg`: Image
- `m_tfContent`: RectTransform
- `m_btnClose`: Button

## 资源需求

- 

## 交互说明

- 

## 状态说明

- 默认状态：
- 空状态：
- 选中状态：
- 禁用状态：
- 加载状态：

## 验收标准

- [ ] Prefab 路径存在
- [ ] 层级符合 Spec
- [ ] 组件完整
- [ ] 绑定字段完整
- [ ] 资源引用不缺失，或缺失项有占位说明
- [ ] 无 Missing Reference
- [ ] Unity Console 无关键错误
- [ ] 截图或人工视觉检查通过

## 待确认问题

- 
