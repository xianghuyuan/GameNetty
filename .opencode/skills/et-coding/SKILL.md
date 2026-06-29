---
name: et-coding
description: ET框架Model/Hotfix分层规则、文件命名规范、核心Attribute用法
---

## Model/Hotfix 分层

| 层级 | 放置内容 | 可热更 |
|------|---------|--------|
| Model | Entity/Component 定义、数据字段、接口声明、枚举 | ❌ |
| Hotfix | System 类、生命周期实现、业务逻辑、Handler | ✅ |

### 文件命名规范

| Model 层 | Hotfix 层 |
|----------|-----------|
| `Player.cs` | `PlayerSystem.cs` |
| `BagComponent.cs` | `BagComponentSystem.cs` |
| - | `C2G_XxxHandler.cs` |

### 目录结构

```
Server/Model/Demo/Gate/Player.cs           → 数据定义
Server/Hotfix/Demo/Gate/PlayerSystem.cs    → 逻辑实现
Server/Hotfix/Demo/Gate/C2G_LoginHandler.cs → 消息处理
```

---

## 核心 Attribute

### 1. EntitySystemOf + EntitySystem

```csharp
[EntitySystemOf(typeof(Player))]
[FriendOf(typeof(Player))]  // 访问私有成员时需要
public static partial class PlayerSystem
{
    [EntitySystem]
    private static void Awake(this Player self, string account)
    {
        self.Account = account;
    }
    
    [EntitySystem]
    private static void Destroy(this Player self) { }
    
    // 业务方法（不需要 EntitySystem 标记）
    public static void LevelUp(this Player self) { }
}
```

### 2. MessageHandler（服务器间消息）

```csharp
[MessageHandler(SceneType.Gate)]
public class R2G_GetLoginKeyHandler : MessageHandler<Scene, R2G_GetLoginKey, G2R_GetLoginKey>
{
    protected override async ETTask Run(Scene scene, R2G_GetLoginKey request, G2R_GetLoginKey response)
    {
        // 处理逻辑
        
        await ETTask.CompletedTask;  // Handler 结尾必须有
    }
}
```

### 3. MessageLocationHandler（发给 Unit 的消息）

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_TransferMapHandler : MessageLocationHandler<Unit, C2M_TransferMap, M2C_TransferMap>
{
    protected override async ETTask Run(Unit unit, C2M_TransferMap request, M2C_TransferMap response)
    {
        // 处理逻辑
        
        await ETTask.CompletedTask;  // Handler 结尾必须有
    }
}
```

### Handler 规范要点

- 必须使用 `async ETTask`
- 方法结尾必须有 `await ETTask.CompletedTask;`
- 提前返回时直接 `return;`（async 会自动处理）

### 4. Event（事件处理）

```csharp
// 定义事件
public struct PlayerLevelUp
{
    public Player Player;
    public int NewLevel;
}

// 处理器
[Event(SceneType.Gate)]
public class PlayerLevelUp_Handler : AEvent<Scene, PlayerLevelUp>
{
    protected override async ETTask Run(Scene scene, PlayerLevelUp args)
    {
        // 处理逻辑
    }
}

// 发布事件
EventSystem.Instance.Publish(scene, new PlayerLevelUp { Player = player, NewLevel = 10 });
```

### 5. Invoke（Fiber 初始化、Timer 等）

```csharp
// Fiber 初始化
[Invoke((long)SceneType.Gate)]
public class FiberInit_Gate : AInvokeHandler<FiberInit, ETTask>
{
    public override async ETTask Handle(FiberInit fiberInit)
    {
        Scene root = fiberInit.Fiber.Root;
        root.AddComponent<PlayerComponent>();
    }
}

// Timer 回调
[Invoke(TimerInvokeType.SessionIdleChecker)]
public class SessionIdleChecker : ATimer<SessionIdleCheckerComponent>
{
    protected override void Run(SessionIdleCheckerComponent self)
    {
        self.Check();
    }
}
```

---

## ❌ 常见错误

```csharp
// ❌ 错误：在 Model 层写业务逻辑
// Model/Player.cs
public class Player : Entity
{
    public void LevelUp() { Level++; }  // 不要在 Entity 里写方法
}

// ✅ 正确：逻辑放 Hotfix 层
// Hotfix/PlayerSystem.cs
public static partial class PlayerSystem
{
    public static void LevelUp(this Player self) { self.Level++; }
}
```

```csharp
// ❌ 错误：创建 Entity 后手动赋值
BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
battleRoom.ConfigId = configId;
battleRoom.State = BattleState.Prepare;

// ✅ 正确：通过 IAwake 接口传参
// Model/BattleRoom.cs
public class BattleRoom : Entity, IAwake<int> { }

// Hotfix/BattleRoomSystem.cs
[EntitySystem]
private static void Awake(this BattleRoom self, int configId)
{
    self.ConfigId = configId;
    self.State = BattleState.Prepare;
}

// Handler 中使用
BattleRoom battleRoom = mapScene.AddChild<BattleRoom, int>(configId);
```

```csharp
// ❌ 错误：System 类不是 static partial class
public class PlayerSystem  // 缺少 static partial
{
}

// ✅ 正确
public static partial class PlayerSystem { }
```

```csharp
// ❌ 错误：生命周期方法缺少 EntitySystem 标记
[EntitySystemOf(typeof(Player))]
public static partial class PlayerSystem
{
    private static void Awake(this Player self) { }  // 缺少 [EntitySystem]
}

// ✅ 正确
[EntitySystem]
private static void Awake(this Player self) { }
```

```csharp
// ❌ 错误：访问私有成员但没有 FriendOf
[EntitySystemOf(typeof(Player))]
public static partial class PlayerSystem
{
    public static void Test(this Player self)
    {
        self.privateField = 1;  // 编译错误
    }
}

// ✅ 正确：添加 FriendOf
[EntitySystemOf(typeof(Player))]
[FriendOf(typeof(Player))]
public static partial class PlayerSystem { }
```

```csharp
// ❌ 错误：Handler 的泛型参数顺序错误
public class C2G_LoginHandler : MessageHandler<C2G_Login, Scene, G2C_Login>  // 顺序错了

// ✅ 正确：Scene/Unit 在前，Request 在中，Response 在后
public class C2G_LoginHandler : MessageHandler<Scene, C2G_Login, G2C_Login>
```
