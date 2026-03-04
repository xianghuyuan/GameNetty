# GameNetty 核心开发指南 (ET8.1 + TEngine)

## 1. 架构理念
GameNetty 是对 ET8.1 的深度封装与解耦版本。其核心目标是解决 **Unity 表现层 (View)** 与 **C# 逻辑层 (Hotfix)** 的高度耦合问题。

### 核心分层
- **Model / Hotfix (Logic Layer)**: 
  - 纯 C# 逻辑，不依赖 `UnityEngine` (或仅依赖数学库)。
  - 运行在 ET 的 Fiber 中，支持热重载。
  - 处理网络消息、状态同步、AI 计算、数值逻辑。
- **ModelView / HotfixView (Visual Layer)**:
  - 依赖 `UnityEngine` 和 `TEngine`。
  - 处理 UI 打开/关闭、特效播放、动画状态机切换、音效。
  - 通过 `EventSystem` 监听 Logic Layer 的状态变化并进行渲染。

---

## 2. 实体 (Entity) 与组件 (Component)
遵循 ET 的 ECS 规范，但引入了 **View 绑定** 机制。

### 定义组件
```csharp
// Logic Component (在 Model 目录)
[ComponentOf(typeof(Unit))]
public class MoveComponent : Entity, IAwake, IUpdate { ... }

// View Component (在 ModelView 目录)
[ComponentOf(typeof(Unit))]
public class MoveViewComponent : Entity, IAwake { 
    public UnityEngine.Transform Transform;
}
```

### 生命周期 (System)
- 所有逻辑必须写在 `[EntitySystemOf]` 标记的静态类中。
- 禁止在 `Model` 集中定义 `Update` 方法，必须使用 `IUpdate` 接口配合 `System`。

---

## 3. UI 系统 (基于 TEngine)
GameNetty 舍弃了 ET 原生的 UI 方案，完全集成 **TEngine**。

- **UI 加载**: 使用 `TUIForm` 基类。
- **UI 消息传递**: 
  - 逻辑层产生数据更新 -> 发布 `Event`。
  - UI 表现层订阅 `Event` -> 更新 UI 控件。
- **UI 组件绑定**: 利用 TEngine 的自动绑定工具生成成员变量（如 `m_btnConfirm`）。

---

## 4. 配表系统 (Luban)
项目内置了 **Luban** 导出工具。

- **表格路径**: `Config/Excel/`。
- **导出脚本**: `Tools/Luban/GenConfig_Client.sh`。
- **代码访问**: 
  ```csharp
  var config = StartConfigCategory.Instance.Get(1);
  ```

---

## 5. 资源管理 (YooAsset)
- 所有的 Prefab、Texture、Audio 均通过 **YooAsset** 进行寻址。
- 建议使用 `Game.Asset.LoadAssetAsync<T>(path)` 进行异步加载，确保不阻塞主线程。

---

## 6. 开发规范
1. **命名空间**: 统一使用 `ET.Client` (客户端) 和 `ET.Server` (服务端)。
2. **异步处理**: 必须使用 `ETTask` 代替传统的 `Task` 或 `Coroutine`。
3. **解耦原则**: Hotfix 层绝对不允许持有 `GameObject` 的直接引用，必须通过 `EntityID` 或 `ViewComponent` 间接操作。
