# 客户端 Battle 模块架构设计文档

## 1. 系统概述

本文档定义了客户端 Battle 模块的完整架构设计，包括虚拟 Scene、独立 Fiber、多人组队支持等核心设计。

### 1.1 设计原则

- **参考服务端架构**：客户端 Battle 模块与服务端 BattleRoom 架构对应
- **虚拟 Scene 隔离**：Battle 是独立的虚拟场景，有独立的 Fiber
- **事件系统清晰**：Battle 内部事件和 Main Scene 事件分离
- **支持多人组队**：BattleComponent 管理多个 Battle 实例
- **数据隔离**：Unit 和 BattleUnit 职责分明

### 1.2 核心设计决策

| 决策项 | 方案 | 理由 |
|------|------|------|
| BattleComponent 管理 | 多个 Battle | 支持多人组队 |
| Battle 类型 | 虚拟 Scene（IScene） | 与服务端对应，事件隔离 |
| Fiber 管理 | 独立 Fiber | 性能隔离，事件清晰 |
| BattleUnit | 需要 | 与服务端对应，数据隔离 |
| WaveManagerComponent | 挂载在 Battle 上 | 波次管理属于战斗逻辑 |
| 坐标系统 | 固定战斗区域 | 简单清晰，支持多人 |

---

## 2. 架构设计

### 2.1 整体架构

```
Main Scene (主场景 - 持久)
  ├── Fiber: Main Fiber
  ├── SceneType: Main
  ├── UnitComponent (玩家管理)
  ├── BattleComponent (战斗管理)
  │   └── Dictionary<long, Battle> Battles
  └── BattleTransitionComponent (过渡管理)

Battle (虚拟战斗场景 - 临时)
  ├── Fiber: Battle Fiber (独立)
  ├── SceneType: Battle
  ├── PlayerIds (玩家ID列表)
  ├── Units (BattleUnit 字典)
  ├── WaveManagerComponent (波次管理)
  └── BattleState (战斗状态)
```

### 2.2 与服务端的对应关系

| 服务端 | 客户端 | 说明 |
|------|------|------|
| BattleRoom | Battle | 虚拟战斗场景 |
| BattleRoomManagerComponent | BattleComponent | 战斗管理 |
| BattleUnit | BattleUnit | 战斗单位 |
| WaveManagerComponent | WaveManagerComponent | 波次管理 |
| Unit (Map Scene) | Unit (Main Scene) | 玩家主实体 |

---

## 3. 核心类设计

### 3.1 Battle（虚拟战斗场景）

```csharp
[ChildOf(typeof(BattleComponent))]
public class Battle : Entity, IScene, IAwake<long, int>
{
    /// <summary>
    /// 战斗 ID
    /// </summary>
    public long BattleId { get; set; }
    
    /// <summary>
    /// 战斗类型（0=WaveBattle, 1=Dungeon, 2=Boss）
    /// </summary>
    public int BattleType { get; set; }
    
    /// <summary>
    /// 场景类型
    /// </summary>
    public SceneType SceneType { get; set; } = SceneType.Battle;
    
    /// <summary>
    /// 独立的 Fiber
    /// </summary>
    public Fiber Fiber { get; set; }
    
    /// <summary>
    /// 玩家 ID 列表
    /// </summary>
    public List<long> PlayerIds { get; set; } = new();
    
    /// <summary>
    /// 战斗单位字典
    /// </summary>
    public Dictionary<long, EntityRef<BattleUnit>> Units { get; set; } = new();
    
    /// <summary>
    /// 波次管理组件
    /// </summary>
    public WaveManagerComponent WaveManager { get; set; }
    
    /// <summary>
    /// 战斗状态
    /// </summary>
    public BattleState State { get; set; }
    
    /// <summary>
    /// 总波数
    /// </summary>
    public int TotalWaves { get; set; }
    
    /// <summary>
    /// 当前波次
    /// </summary>
    public int CurrentWave { get; set; }
}
```

### 3.2 BattleComponent（改造为支持多个 Battle）

```csharp
[ComponentOf(typeof(Scene))]
public class BattleComponent : Entity, IAwake, IDestroy
{
    /// <summary>
    /// 管理多个战斗实例
    /// </summary>
    public Dictionary<long, EntityRef<Battle>> Battles { get; set; } = new();
    
    /// <summary>
    /// 战斗 Fiber 字典（用于管理每个 Battle 的 Fiber）
    /// </summary>
    public Dictionary<long, int> BattleFiberIds { get; set; } = new();
}
```

### 3.3 BattleComponentSystem

```csharp
[EntitySystemOf(typeof(BattleComponent))]
[FriendOf(typeof(BattleComponent))]
public static partial class BattleComponentSystem
{
    [EntitySystem]
    private static void Awake(this BattleComponent self)
    {
        self.Battles = new Dictionary<long, EntityRef<Battle>>();
    }
    
    [EntitySystem]
    private static void Destroy(this BattleComponent self)
    {
        // 清理所有战斗实例
        foreach (var battle in self.Battles.Values.ToList())
        {
            battle?.Dispose();
        }
        self.Battles.Clear();
    }
    
    /// <summary>
    /// 创建战斗实例
    /// </summary>
    public static async ETTask<Battle> CreateBattle(this BattleComponent self, long battleId, int battleType)
    {
        // 如果已存在相同 ID 的战斗，先清理
        if (self.Battles.TryGetValue(battleId, out var existingBattle))
        {
            Log.Warning($"Battle already exists: {battleId}, disposing old one");
            existingBattle?.Dispose();
            
            // 清理对应的 Fiber
            if (self.BattleFiberIds.TryGetValue(battleId, out var fiberId))
            {
                await FiberManager.Instance.Remove(fiberId);
                self.BattleFiberIds.Remove(battleId);
            }
        }
        
        // 通过 FiberManager 创建独立的 Fiber
        int battleFiberId = await FiberManager.Instance.Create(
            SchedulerType.Main,  // 使用 Main 线程调度器
            self.Fiber().Zone,   // 使用相同的 Zone
            SceneType.Battle,    // 场景类型为 Battle
            $"Battle_{battleId}" // Fiber 名称
        );
        
        // 获取创建的 Fiber
        Fiber battleFiber = FiberManager.Instance.Get(battleFiberId);
        
        // 创建 Battle 虚拟 Scene
        Battle battle = self.AddChildWithId<Battle, long, int>(battleId, battleId, battleType);
        battle.Fiber = battleFiber;
        battle.SceneType = SceneType.Battle;
        
        // 添加 WaveManagerComponent
        WaveManagerComponent waveManager = battle.AddComponent<WaveManagerComponent>();
        battle.WaveManager = waveManager;
        
        // 保存到字典
        self.Battles[battleId] = battle;
        self.BattleFiberIds[battleId] = battleFiberId;
        
        Log.Info($"Battle created: BattleId={battleId}, Type={battleType}, FiberId={battleFiberId}");
        
        return battle;
    }
    
    /// <summary>
    /// 获取战斗实例
    /// </summary>
    public static Battle GetBattle(this BattleComponent self, long battleId)
    {
        return self.Battles.TryGetValue(battleId, out var battle) ? battle : null;
    }
    
    /// <summary>
    /// 移除战斗实例
    /// </summary>
    public static async ETTask RemoveBattle(this BattleComponent self, long battleId)
    {
        if (self.Battles.TryGetValue(battleId, out var battle))
        {
            battle?.Dispose();
            self.Battles.Remove(battleId);
            
            // 清理对应的 Fiber
            if (self.BattleFiberIds.TryGetValue(battleId, out var fiberId))
            {
                await FiberManager.Instance.Remove(fiberId);
                self.BattleFiberIds.Remove(battleId);
            }
            
            Log.Info($"Battle removed: BattleId={battleId}");
        }
    }
    
    /// <summary>
    /// 获取当前战斗（单人场景用）
    /// </summary>
    public static Battle GetCurrentBattle(this BattleComponent self)
    {
        return self.Battles.Count > 0 ? self.Battles.Values.First() : null;
    }
    
    /// <summary>
    /// 获取所有活跃战斗
    /// </summary>
    public static List<Battle> GetAllBattles(this BattleComponent self)
    {
        return self.Battles.Values.ToList();
    }
}
```

### 3.4 BattleUnit（战斗单位）

```csharp
[ChildOf(typeof(Battle))]
public class BattleUnit : Entity, IAwake<long, int>
{
    /// <summary>
    /// 战斗单位 ID
    /// </summary>
    public long BattleUnitId { get; set; }
    
    /// <summary>
    /// 关联的 Unit ID（0 表示怪物）
    /// </summary>
    public long OwnerId { get; set; }
    
    /// <summary>
    /// 单位类型（玩家/怪物）
    /// </summary>
    public BattleUnitType UnitType { get; set; }
    
    /// <summary>
    /// 最大血量
    /// </summary>
    public int MaxHp { get; set; }
    
    /// <summary>
    /// 当前血量
    /// </summary>
    public int CurrentHp { get; set; }
    
    /// <summary>
    /// 攻击力
    /// </summary>
    public int Attack { get; set; }
    
    /// <summary>
    /// 防御力
    /// </summary>
    public int Defense { get; set; }
    
    /// <summary>
    /// 位置
    /// </summary>
    public Vector3 Position { get; set; }
    
    /// <summary>
    /// 是否死亡
    /// </summary>
    public bool IsDead { get; set; }
}
```

---

## 4. 事件系统设计

### 4.1 Battle 内部事件（在 Battle Fiber 中处理）

```csharp
/// <summary>
/// 波次开始事件
/// </summary>
public struct BattleWaveStart
{
    public Battle Battle;
    public int WaveNumber;
}

/// <summary>
/// 波次完成事件
/// </summary>
public struct BattleWaveComplete
{
    public Battle Battle;
    public int WaveNumber;
}

/// <summary>
/// 战斗单位死亡事件
/// </summary>
public struct BattleUnitDead
{
    public BattleUnit Unit;
}

/// <summary>
/// 战斗结束事件
/// </summary>
public struct BattleEnd
{
    public Battle Battle;
    public bool Success;
}
```

### 4.2 Main Scene 事件（过渡事件，在 Main Fiber 中处理）

```csharp
/// <summary>
/// 战斗过渡开始事件（进入战斗）
/// </summary>
public struct BattleTransitionStart
{
    public Battle Battle;
}

/// <summary>
/// 战斗过渡完成事件（已进入战斗）
/// </summary>
public struct BattleTransitionComplete
{
    public Battle Battle;
}

/// <summary>
/// 战斗退出开始事件（开始返回主界面）
/// </summary>
public struct BattleExitStart
{
    public Battle Battle;
}

/// <summary>
/// 战斗退出完成事件（已返回主界面）
/// </summary>
public struct BattleExitComplete
{
    public Battle Battle;
}
```

### 4.3 事件处理流程

```
消息到达（Main Fiber）
  ↓
消息处理器（Main Fiber）
  ├── 获取 Battle 实例
  └── 在 Battle Fiber 中发布事件
      ↓
Battle Fiber 处理事件
  ├── 业务逻辑处理
  └── 发布相关事件
      ↓
Battle 内的事件监听器处理
  ├── UI 更新
  ├── 音效播放
  └── 特效显示
```

---

## 5. 生命周期流程

### 5.1 进入战斗流程

```
玩家点击"开始战斗"
    ↓
BattleHelper.StartBattle() 发送 C2M_StartBattle
    ↓
收到 M2C_StartBattle 响应（Main Fiber）
    ├── 获取 BattleComponent
    ├── BattleComponent.CreateBattle(battleId, battleType)
    │   ├── 创建独立的 Fiber
    │   ├── 创建 Battle 虚拟 Scene
    │   └── 添加 WaveManagerComponent
    └── 发布 BattleTransitionStart 事件（Main Fiber）
        ↓
过渡流程（Main Fiber）
    ├── 主界面 UI 隐退动画
    ├── 屏幕 Fade Out
    ├── 相机切换到战斗区域
    ├── 播放 Spine 战斗开始动画
    ├── 屏幕 Fade In
    └── 战斗 UI 显示
        ↓
发布 BattleTransitionComplete 事件（Main Fiber）
    ↓
战斗进行中（Battle Fiber）
    ├── 收到 M2C_WaveStart（Main Fiber 接收）
    ├── M2C_WaveStartHandler 在 Battle Fiber 中发布 BattleWaveStart 事件
    ├── WaveManagerComponent 处理
    └── 波次进行...
```

### 5.2 退出战斗流程

```
收到 M2C_BattleEnd（Main Fiber 接收）
    ↓
M2C_BattleEndHandler 在 Battle Fiber 中发布 BattleEnd 事件
    ↓
Battle 内的事件监听器处理
    ├── 播放胜利/失败动画
    └── 显示结算界面
        ↓
玩家确认结算
    ↓
发布 BattleExitStart 事件（Main Fiber）
    ├── 屏幕 Fade Out
    ├── 关闭战斗 UI
    ├── 清理 Battle 实例
    │   ├── 销毁 Battle Fiber
    │   └── 销毁 Battle 虚拟 Scene
    ├── BattleComponent.RemoveBattle(battleId)
    ├── 相机切换回主世界
    ├── 屏幕 Fade In
    └── 主界面 UI 恢复
        ↓
发布 BattleExitComplete 事件（Main Fiber）
    ↓
主界面（可重复进入战斗）
```

---

## 6. 消息处理改造

### 6.1 M2C_WaveStartHandler 改造

```csharp
[MessageHandler(SceneType.Main)]
public class M2C_WaveStartHandler : MessageHandler<Scene, M2C_WaveStart>
{
    protected override async ETTask Run(Scene root, M2C_WaveStart message)
    {
        BattleComponent battleComponent = root.GetComponent<BattleComponent>();
        if (battleComponent == null)
        {
            Log.Error("M2C_WaveStart: BattleComponent not found");
            return;
        }

        Battle battle = battleComponent.GetBattle(message.battleId);
        if (battle == null)
        {
            Log.Error($"M2C_WaveStart: Battle not found, BattleId={message.battleId}");
            return;
        }

        // 在 Battle Fiber 中发布事件
        battle.Fiber.Fire(new BattleWaveStart
        {
            Battle = battle,
            WaveNumber = message.waveNumber
        });

        await ETTask.CompletedTask;
    }
}
```

### 6.2 M2C_WaveCompleteHandler 改造

```csharp
[MessageHandler(SceneType.Main)]
public class M2C_WaveCompleteHandler : MessageHandler<Scene, M2C_WaveComplete>
{
    protected override async ETTask Run(Scene root, M2C_WaveComplete message)
    {
        BattleComponent battleComponent = root.GetComponent<BattleComponent>();
        if (battleComponent == null)
        {
            Log.Error("M2C_WaveComplete: BattleComponent not found");
            return;
        }

        Battle battle = battleComponent.GetBattle(message.battleId);
        if (battle == null)
        {
            Log.Error($"M2C_WaveComplete: Battle not found, BattleId={message.battleId}");
            return;
        }

        // 在 Battle Fiber 中发布事件
        battle.Fiber.Fire(new BattleWaveComplete
        {
            Battle = battle,
            WaveNumber = message.waveNumber
        });

        await ETTask.CompletedTask;
    }
}
```

### 6.3 M2C_BattleEndHandler 改造

```csharp
[MessageHandler(SceneType.Main)]
public class M2C_BattleEndHandler : MessageHandler<Scene, M2C_BattleEnd>
{
    protected override async ETTask Run(Scene root, M2C_BattleEnd message)
    {
        BattleComponent battleComponent = root.GetComponent<BattleComponent>();
        if (battleComponent == null)
        {
            Log.Error("M2C_BattleEnd: BattleComponent not found");
            return;
        }

        Battle battle = battleComponent.GetBattle(message.battleId);
        if (battle == null)
        {
            Log.Error($"M2C_BattleEnd: Battle not found, BattleId={message.battleId}");
            return;
        }

        // 在 Battle Fiber 中发布事件
        battle.Fiber.Fire(new BattleEnd
        {
            Battle = battle,
            Success = message.success
        });

        await ETTask.CompletedTask;
    }
}
```

---

## 7. BattleHelper 改造

### 7.1 StartBattle 方法改造

```csharp
public static async ETTask<long> StartBattle(Scene scene, int battleType = 0, int totalWaves = 1)
{
    C2M_StartBattle request = C2M_StartBattle.Create();
    request.battleType = battleType;
    request.totalWaves = totalWaves;
    
    M2C_StartBattle response = await scene.GetComponent<ClientSenderComponent>().Call(request) as M2C_StartBattle;
    
    if (response.Error != ErrorCode.ERR_Success)
    {
        Log.Error($"开始战斗失败: {response.Message}");
        return 0;
    }
    
    // 创建 Battle 实例（异步）
    BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
    if (battleComponent == null)
    {
        Log.Error("BattleComponent not found");
        return 0;
    }
    
    Battle battle = await battleComponent.CreateBattle(response.battleId, battleType);
    battle.TotalWaves = totalWaves;
    
    Log.Info($"开始战斗成功, BattleId: {response.battleId}");
    return response.battleId;
}
```

---

## 8. CurrentSceneFactory 改造

### 8.1 在 Scene 创建时添加 BattleComponent

```csharp
public static Scene Create(...)
{
    Scene currentScene = ...;
    
    // 添加 BattleComponent（常驻组件）
    currentScene.AddComponent<BattleComponent>();
    
    return currentScene;
}
```

---

## 9. 文件结构规划

```
Module/Battle/
├── Battle.cs                           # 虚拟战斗场景实体
├── BattleComponent.cs                  # 战斗管理组件（改造）
├── BattleComponentSystem.cs            # 战斗管理系统（新建）
├── BattleUnit.cs                       # 战斗单位实体（新建）
├── BattleUnitSystem.cs                 # 战斗单位系统（新建）
├── BattleSystem.cs                     # 战斗系统（已存在）
├── BattleHelper.cs                     # 战斗辅助方法（改造）
├── BattleEventType.cs                  # 事件定义（补充）
├── BattleEnum.cs                       # 枚举定义（已存在）
├── Handler/                            # 消息处理器
│   ├── M2C_WaveStartHandler.cs        # 改造
│   ├── M2C_WaveCompleteHandler.cs     # 改造
│   └── M2C_BattleEndHandler.cs        # 改造
├── Transition/                         # 过渡模块（待创建）
│   ├── BattleTransitionComponent.cs
│   ├── BattleTransitionSystem.cs
│   ├── BattleAreaConfig.cs
│   └── EventListener/
└── WaveManager/                        # 波次管理（待创建）
    ├── WaveManagerComponent.cs
    └── WaveManagerSystem.cs
```

---

## 10. 实施步骤规划

### 阶段 1：基础框架（优先级：高）

- [ ] 改造 BattleComponent 支持多个 Battle
- [ ] 创建 BattleComponentSystem
- [ ] 创建 Battle 虚拟 Scene 实体
- [ ] 创建 BattleUnit 实体
- [ ] 创建 BattleUnitSystem
- [ ] 补充 BattleEventType 事件定义
- [ ] 在 CurrentSceneFactory 中添加 BattleComponent

### 阶段 2：消息处理（优先级：高）

- [ ] 改造 M2C_WaveStartHandler 适配新架构
- [ ] 改造 M2C_WaveCompleteHandler
- [ ] 改造 M2C_BattleEndHandler
- [ ] 改造 BattleHelper.StartBattle()

### 阶段 3：过渡系统（优先级：中）

- [ ] 创建 BattleTransitionComponent
- [ ] 创建 BattleTransitionSystem
- [ ] 创建过渡事件监听器

### 阶段 4：波次管理（优先级：中）

- [ ] 创建客户端 WaveManagerComponent
- [ ] 创建 WaveManagerSystem

### 阶段 5：表现层（优先级：低）

- [ ] 创建 Spine 动画特效
- [ ] 接入音效系统
- [ ] 创建战斗 UI

---

## 11. 关键设计要点

### 11.1 Fiber 的使用

```csharp
// ❌ 错误：在 Main Fiber 中发布 Battle 事件
EventSystem.Instance.Publish(root, new BattleWaveStart { ... });

// ✅ 正确：在 Battle Fiber 中发布 Battle 事件
battle.Fiber.Fire(new BattleWaveStart { ... });
```

### 11.2 消息处理器的职责

```csharp
// 消息处理器总是在 Main Fiber 中运行
[MessageHandler(SceneType.Main)]
public class M2C_WaveStartHandler : MessageHandler<Scene, M2C_WaveStart>
{
    // 1. 获取 Battle 实例
    // 2. 在 Battle Fiber 中发布事件
    // 3. 不要在这里处理业务逻辑
}
```

### 11.3 Battle 的生命周期

```csharp
// 创建（异步）
Battle battle = await BattleComponent.CreateBattle(battleId, battleType);

// 销毁（异步）
await BattleComponent.RemoveBattle(battleId);

// 不要在其他地方创建或销毁 Battle
```

---

## 12. 多人组队支持

### 12.1 架构支持

当前设计已经支持多人组队：

```
BattleComponent
  ├── Battle 1 (Fiber 1)
  │   ├── PlayerIds: [A, B]
  │   └── Units: [BattleUnit A, BattleUnit B, Monster 1, ...]
  ├── Battle 2 (Fiber 2)
  │   ├── PlayerIds: [C, D]
  │   └── Units: [BattleUnit C, BattleUnit D, Monster 1, ...]
  └── Battle 3 (Fiber 3)
      ├── PlayerIds: [E]
      └── Units: [BattleUnit E, Monster 1, ...]
```

### 12.2 事件隔离

每个 Battle 有独立的 Fiber，事件不会相互影响：

```
Battle 1 Fiber: BattleWaveStart → 处理 Battle 1 的波次
Battle 2 Fiber: BattleWaveStart → 处理 Battle 2 的波次
Battle 3 Fiber: BattleWaveStart → 处理 Battle 3 的波次

互不影响
```

---

## 14. Fiber 创建详解

### 14.1 为什么不能直接 new Fiber()

Fiber 的构造函数是 `internal`，不能从外部直接创建：

```csharp
// ❌ 错误：构造函数是 internal
Fiber battleFiber = new Fiber();

// ✅ 正确：通过 FiberManager 创建
int fiberId = await FiberManager.Instance.Create(
    SchedulerType.Main,
    zone,
    SceneType.Battle,
    "Battle_1"
);
```

### 14.2 FiberManager.Create 方法

```csharp
public async ETTask<int> Create(
    SchedulerType schedulerType,  // 调度器类型（Main/Thread/ThreadPool）
    int zone,                      // 区域 ID
    SceneType sceneType,           // 场景类型（Battle）
    string name                    // Fiber 名称
)
```

**参数说明**：
- `schedulerType`: 使用 `SchedulerType.Main`（主线程调度器）
- `zone`: 使用 `self.Fiber().Zone`（继承父 Fiber 的 Zone）
- `sceneType`: 使用 `SceneType.Battle`（虚拟战斗场景）
- `name`: 使用 `$"Battle_{battleId}"`（便于调试）

### 14.3 创建流程

```
调用 FiberManager.Instance.Create()
    ↓
FiberManager 生成新的 fiberId
    ↓
创建 Fiber 实例（internal 构造函数）
    ↓
添加到 FiberManager 的字典中
    ↓
在 Fiber 线程中执行 FiberInit 事件
    ↓
返回 fiberId
    ↓
通过 FiberManager.Instance.Get(fiberId) 获取 Fiber 实例
```

### 14.4 销毁流程

```csharp
// 销毁 Fiber（异步）
await FiberManager.Instance.Remove(fiberId);
```

**注意**：
- 销毁必须是异步的
- 销毁会在 Fiber 线程中执行
- 销毁后 Fiber 会被从字典中移除

### 14.5 完整的创建和销毁示例

```csharp
// 创建
int battleFiberId = await FiberManager.Instance.Create(
    SchedulerType.Main,
    self.Fiber().Zone,
    SceneType.Battle,
    $"Battle_{battleId}"
);

Fiber battleFiber = FiberManager.Instance.Get(battleFiberId);

// 使用
battle.Fiber = battleFiber;

// 销毁
await FiberManager.Instance.Remove(battleFiberId);
```

---

## 15. 关键改动总结

### 15.1 BattleComponent 的改动

```csharp
// 新增字段：管理 Battle 对应的 Fiber ID
public Dictionary<long, int> BattleFiberIds { get; set; } = new();

// CreateBattle 改为异步
public static async ETTask<Battle> CreateBattle(...)

// RemoveBattle 改为异步
public static async ETTask RemoveBattle(...)
```

### 15.2 BattleHelper 的改动

```csharp
// StartBattle 需要 await CreateBattle
Battle battle = await battleComponent.CreateBattle(response.battleId, battleType);
```

### 15.3 过渡系统的改动

```csharp
// BattleExitStart 事件处理中需要 await RemoveBattle
await BattleComponent.RemoveBattle(battleId);
```

---

## 16. 总结

### 16.1 核心改造

1. **BattleComponent**：从单个 Battle 改为管理多个 Battle
2. **Battle**：创建虚拟 Scene，通过 FiberManager 拥有独立的 Fiber
3. **BattleUnit**：新增战斗单位实体
4. **消息处理**：改造为跨 Fiber 事件发布
5. **CurrentSceneFactory**：添加 BattleComponent
6. **Fiber 管理**：使用 FiberManager 创建和销毁 Fiber

### 16.2 异步操作

以下操作都是异步的，需要 await：

```csharp
// 创建 Battle
Battle battle = await battleComponent.CreateBattle(battleId, battleType);

// 销毁 Battle
await battleComponent.RemoveBattle(battleId);

// 启动战斗
long battleId = await BattleHelper.StartBattle(scene, battleType, totalWaves);
```

### 16.3 优势

- ✅ 与服务端架构对应
- ✅ 事件系统清晰
- ✅ 支持多人组队
- ✅ 性能隔离
- ✅ 易于维护和扩展
- ✅ 正确使用 FiberManager 管理 Fiber 生命周期

### 16.4 下一步

确认本文档后，开始实施阶段 1 的基础框架改造。
