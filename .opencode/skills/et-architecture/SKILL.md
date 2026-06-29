---
name: et-architecture
description: ET框架实体树状结构、Child与Component区别、父子关系约束规则
---

## 实体树状结构

```
Fiber
  └── Scene (根节点, IScene = this)
        ├── [Components] 组件 (用类型索引，同类型只能一个)
        │     ├── UnitComponent
        │     ├── TimerComponent
        │     └── ...
        └── [Children] 子实体 (用Id索引，同类型可多个)
              └── SubScene
```

## Child vs Component

| 特性 | Child (子实体) | Component (组件) |
|------|---------------|-----------------|
| 添加方法 | `AddChild<T>()` | `AddComponent<T>()` |
| 存储Key | `entity.Id` | `TypeHashCode` |
| 同类型数量 | 可多个 | 只能一个 |
| 约束标记 | `[ChildOf]` | `[ComponentOf]` |

### 使用场景

- **Component**: 功能模块，如 `MoveComponent`、`BagComponent`
- **Child**: 同类型多实例，如 `Player`、`Unit`、`Item`

## 约束 Attribute

```csharp
// 组件：挂载到指定父实体
[ComponentOf(typeof(Unit))]
public class MoveComponent : Entity, IAwake { }

// 组件：可挂载到任意实体
[ComponentOf]
public class ObjectWait : Entity, IAwake { }

// 子实体：只能添加到指定父实体
[ChildOf(typeof(PlayerComponent))]
public class Player : Entity, IAwake<string> { }

// 子实体：可添加到任意实体
[ChildOf]
public class Session : Entity, IAwake { }
```

## IScene 传递规则

- 每个 Entity 的 `IScene` 指向最近的 Scene 祖先
- Scene 自己的 `IScene` 指向自己
- 设置 Parent 时自动继承 IScene

## 生命周期

- 父实体 Dispose 时，递归 Dispose 所有 Children 和 Components
- 支持对象池复用：`AddComponent<T>(isFromPool: true)`

---

## ❌ 常见错误

```csharp
// ❌ 错误：把应该是 Component 的写成 Child
[ChildOf(typeof(Unit))]
public class MoveComponent : Entity { }  // 移动组件应该用 ComponentOf

// ✅ 正确
[ComponentOf(typeof(Unit))]
public class MoveComponent : Entity { }
```

```csharp
// ❌ 错误：把应该是 Child 的写成 Component（需要多个实例时）
[ComponentOf(typeof(BagComponent))]
public class Item : Entity { }  // 背包里有多个道具，应该用 ChildOf

// ✅ 正确
[ChildOf(typeof(BagComponent))]
public class Item : Entity { }
```

```csharp
// ❌ 错误：不写约束 Attribute
public class MyComponent : Entity { }  // 缺少 ComponentOf 或 ChildOf

// ✅ 正确
[ComponentOf(typeof(Scene))]
public class MyComponent : Entity { }
```
