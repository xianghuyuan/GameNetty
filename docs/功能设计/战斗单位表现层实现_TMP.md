# 战斗单位表现层实现文档

## 1. 概述

在客户端创建战斗单位的表现层，分为两部分：
- **2D 场景表现**：使用 SpriteRenderer 色块（临时方案，后续替换 Spine）
- **UI 层数值**：血条、名称等信息

## 2. 技术方案

### 2.1 表现层分离

```
┌─────────────────────────────────────────────────────────┐
│                    2D 场景表现                           │
│  - SpriteRenderer 色块（临时）                           │
│  - 后续替换为 Spine 动画                                 │
│  - 位于战斗区域坐标 (10000, 0, 0)                        │
└─────────────────────────────────────────────────────────┘
            │ 位置同步
            ▼
┌─────────────────────────────────────────────────────────┐
│                    UI 层数值                             │
│  - 血条、名称、状态                                      │
│  - 跟随 2D 角色位置                                      │
│  - 使用 TEngine UIWindow                                │
└─────────────────────────────────────────────────────────┘
```

### 2.2 数据来源

| 事件 | 触发时机 | 携带数据 |
|------|---------|---------|
| BattleUnitCreated | 收到 M2C_CreateBattleUnits | Battle, BattleUnit |
| BattleUnitDead | 战斗单位死亡 | BattleUnit |

### 2.3 BattleUnit 数据结构

```csharp
public class BattleUnit : Entity, IAwake<int>
{
    public int ConfigId { get; set; }
    public long OwnerId { get; set; }
    public UnitCamp Camp { get; set; }
    public float3 Position { get; set; }
    public float3 Forward { get; set; }
    public bool IsDead { get; set; }
}
```

## 3. 架构设计

### 3.1 文件结构

```
Unity/Assets/GameScripts/HotFix/GameLogic/
├── Module/Battle/
│   └── View/
│       ├── BattleUnitViewComponent.cs       # 表现管理组件
│       ├── BattleUnitViewComponentSystem.cs # 表现逻辑
│       └── BattleUnitView.cs                # 单个单位视图
│
└── UI/
    └── BattleUnitInfoUI/
        └── BattleUnitInfoUI.cs              # 数值 UI
```

### 3.2 类设计

#### BattleUnitViewComponent（管理所有单位表现）

```csharp
[ComponentOf(typeof(Battle))]
public class BattleUnitViewComponent : Entity, IAwake, IDestroy
{
    public Dictionary<long, BattleUnitView> Views { get; } = new();
}
```

#### BattleUnitView（单个单位表现）

```csharp
public class BattleUnitView : Entity, IAwake<long>, IDestroy
{
    public long UnitId { get; set; }
    public UnitCamp Camp { get; set; }
    
    // 2D 场景表现
    public GameObject GameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
    
    // UI 数值表现
    public RectTransform UIRectTransform { get; set; }
}
```

## 4. 实现步骤

### 步骤 1：创建 BattleUnitViewComponent

```csharp
// Model/Battle/View/BattleUnitViewComponent.cs
[ComponentOf(typeof(Battle))]
public class BattleUnitViewComponent : Entity, IAwake, IDestroy
{
    public Dictionary<long, BattleUnitView> Views { get; } = new();
}
```

### 步骤 2：创建 BattleUnitView

```csharp
// Model/Battle/View/BattleUnitView.cs
public class BattleUnitView : Entity, IAwake<long>, IDestroy
{
    public long UnitId { get; set; }
    public UnitCamp Camp { get; set; }
    public GameObject GameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
}
```

### 步骤 3：实现 BattleUnitViewComponentSystem

```csharp
// Hotfix/Battle/View/BattleUnitViewComponentSystem.cs
[EntitySystemOf(typeof(BattleUnitViewComponent))]
[FriendOf(typeof(BattleUnitViewComponent))]
[FriendOf(typeof(BattleUnitView))]
public static partial class BattleUnitViewComponentSystem
{
    [EntitySystem]
    private static void Awake(this BattleUnitViewComponent self)
    {
    }
    
    [EntitySystem]
    private static void Destroy(this BattleUnitViewComponent self)
    {
        foreach (var view in self.Views.Values)
        {
            view.Dispose();
        }
        self.Views.Clear();
    }
    
    public static BattleUnitView CreateView(this BattleUnitViewComponent self, BattleUnit unit)
    {
        BattleUnitView view = self.AddChildWithId<BattleUnitView>(unit.Id);
        view.Camp = unit.Camp;
        
        // 创建 2D 场景表现
        view.GameObject = new GameObject($"Unit_{unit.Id}");
        view.SpriteRenderer = view.GameObject.AddComponent<SpriteRenderer>();
        
        // 设置位置
        Vector3 worldPos = new Vector3(
            unit.Position.x + BattleAreaConfig.BattleAreaCenter.x,
            unit.Position.y,
            unit.Position.z + BattleAreaConfig.BattleAreaCenter.z
        );
        view.GameObject.transform.position = worldPos;
        
        // 阵营颜色
        view.SpriteRenderer.color = unit.Camp == UnitCamp.Friend ? Color.green : Color.red;
        
        self.Views[unit.Id] = view;
        return view;
    }
    
    public static void RemoveView(this BattleUnitViewComponent self, long unitId)
    {
        if (self.Views.TryGetValue(unitId, out BattleUnitView view))
        {
            view.Dispose();
            self.Views.Remove(unitId);
        }
    }
}
```

### 步骤 4：实现 BattleUnitViewSystem

```csharp
// Hotfix/Battle/View/BattleUnitViewSystem.cs
[EntitySystemOf(typeof(BattleUnitView))]
[FriendOf(typeof(BattleUnitView))]
public static partial class BattleUnitViewSystem
{
    [EntitySystem]
    private static void Awake(this BattleUnitView self, long unitId)
    {
        self.UnitId = unitId;
    }
    
    [EntitySystem]
    private static void Destroy(this BattleUnitView self)
    {
        if (self.GameObject != null)
        {
            UnityEngine.Object.Destroy(self.GameObject);
            self.GameObject = null;
        }
    }
    
    public static void UpdatePosition(this BattleUnitView self, float3 position)
    {
        if (self.GameObject != null)
        {
            Vector3 worldPos = new Vector3(
                position.x + BattleAreaConfig.BattleAreaCenter.x,
                position.y,
                position.z + BattleAreaConfig.BattleAreaCenter.z
            );
            self.GameObject.transform.position = worldPos;
        }
    }
}
```

### 步骤 5：创建事件监听

```csharp
// Hotfix/Battle/Event/BattleUnitView_Event.cs
[Event(SceneType.Main)]
public class BattleUnitCreated_View : AEvent<Scene, BattleUnitCreated>
{
    protected override async ETTask Run(Scene scene, BattleUnitCreated args)
    {
        Battle battle = args.Battle;
        BattleUnit unit = args.Unit;
        
        BattleUnitViewComponent viewComponent = battle.GetComponent<BattleUnitViewComponent>();
        if (viewComponent == null)
        {
            viewComponent = battle.AddComponent<BattleUnitViewComponent>();
        }
        
        viewComponent.CreateView(unit);
        
        Log.Info($"创建单位表现: UnitId={unit.Id}, Camp={unit.Camp}, Pos={unit.Position}");
        
        await ETTask.CompletedTask;
    }
}

[Event(SceneType.Main)]
public class BattleUnitDead_View : AEvent<Scene, BattleUnitDead>
{
    protected override async ETTask Run(Scene scene, BattleUnitDead args)
    {
        BattleUnit unit = args.BattleUnit;
        
        Battle battle = unit.GetParent<Battle>();
        BattleUnitViewComponent viewComponent = battle?.GetComponent<BattleUnitViewComponent>();
        
        if (viewComponent != null)
        {
            viewComponent.RemoveView(unit.Id);
        }
        
        Log.Info($"移除单位表现: UnitId={unit.Id}");
        
        await ETTask.CompletedTask;
    }
}
```

## 5. 战斗区域配置

```csharp
// Model/Battle/View/BattleAreaConfig.cs
public static class BattleAreaConfig
{
    /// <summary>
    /// 战斗区域中心坐标（与主世界隔离）
    /// </summary>
    public static readonly Vector3 BattleAreaCenter = new Vector3(10000, 0, 0);
}
```

## 6. SpriteRenderer 色块显示

### 临时方案

| 阵营 | 颜色 | 说明 |
|------|------|------|
| Friend | 绿色 | 友方单位 |
| Enemy | 红色 | 敌方单位 |

### 后续 Spine 替换

```csharp
// 未来替换为 Spine
public static void LoadSpineAsset(this BattleUnitView self, int configId)
{
    // 从配置表获取 Spine 资源路径
    // 加载 Spine 资源
    // 替换 SpriteRenderer
}
```

## 7. UI 数值表现（可选）

后续可在 BattleUnitView 中添加 UI 跟随：

```csharp
public static void UpdateUIPosition(this BattleUnitView self, Camera camera)
{
    if (self.GameObject != null && self.UIRectTransform != null)
    {
        Vector3 screenPos = camera.WorldToScreenPoint(self.GameObject.transform.position);
        self.UIRectTransform.position = screenPos;
    }
}
```

## 8. 测试验证

- [ ] 进入战斗后 SpriteRenderer 色块正确显示
- [ ] 友方绿色，敌方红色
- [ ] 位置在战斗区域 (10000, 0, 0) 附近
- [ ] 单位死亡后色块消失

## 9. 后续优化

1. **Spine 动画**：替换 SpriteRenderer 色块
2. **UI 血条**：添加血条跟随
3. **对象池**：优化大量单位创建/销毁
4. **动画状态机**：Idle/Move/Attack 状态切换
