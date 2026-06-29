# ET 与 TEngine 事件系统对比与选择

## 一、两套事件系统对比

### 1. ET 事件系统

```csharp
// 定义事件结构体
public struct BattleUnitNumericChange
{
    public BattleUnit BattleUnit;
    public int NumericType;
    public long NewValue;
}

// 发布事件
EventSystem.Instance.Publish(scene, new BattleUnitNumericChange { ... });

// 订阅事件
[Event(SceneType.Main)]
public class BattleUnitNumericChange_UpdateStatus : AEvent<Scene, BattleUnitNumericChange>
{
    protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
    {
        // 处理逻辑
        await ETTask.CompletedTask;
    }
}
```

**特点：**
- 基于 `struct` 的事件数据
- 需要 `Scene` 上下文
- 支持异步处理 (`async ETTask`)
- 通过 `[Event(SceneType)]` 标记订阅者
- 自动注册，无需手动订阅
- 支持多个订阅者
- 适合游戏逻辑层

### 2. TEngine 事件系统

```csharp
// 发布事件
GameEvent.Send("OnPlayerLevelUp", level);
// 或
GameEvent.Send<int>("OnPlayerLevelUp", level);

// 订阅事件
GameEvent.AddEventListener("OnPlayerLevelUp", OnPlayerLevelUpHandler);
// 或
GameEvent.AddEventListener<int>("OnPlayerLevelUp", OnPlayerLevelUpHandler);

// 取消订阅
GameEvent.RemoveEventListener("OnPlayerLevelUp", OnPlayerLevelUpHandler);

// 处理函数
private void OnPlayerLevelUpHandler(int level)
{
    // 更新 UI
    txtLevel.text = level.ToString();
}
```

**特点：**
- 基于字符串 key 的事件
- 支持泛型参数
- 同步处理
- 需要手动订阅/取消订阅
- 适合 UI 表现层

## 二、使用场景划分

### 规则：按照数据流向选择

```
┌─────────────────────────────────────────────────────────────┐
│                      事件系统选择规则                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ET 事件系统 (游戏逻辑层)                                    │
│  ├─ 涉及 ET Entity/Component 的数据变化                     │
│  ├─ 需要访问 Scene 上下文                                   │
│  ├─ 跨模块的游戏逻辑通信                                     │
│  ├─ 需要异步处理的事件                                       │
│  └─ 服务端/客户端游戏逻辑                                    │
│                                                             │
│  TEngine 事件系统 (UI 表现层)                                │
│  ├─ 纯 UI 交互事件                                          │
│  ├─ UI 组件之间的通信                                        │
│  ├─ 不涉及 ET 实体的表现层事件                               │
│  ├─ 同步的 UI 更新                                          │
│  └─ Unity MonoBehaviour 生命周期相关                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 三、具体使用场景

### 场景 1：战斗单位数值变化 → 使用 ET 事件

```csharp
// ❌ 错误：使用 TEngine 事件
public static void Set(this NumericComponent self, int numericType, long value)
{
    self.NumericDic[numericType] = value;
    
    // 不推荐：TEngine 事件无法传递 ET 实体
    GameEvent.Send("OnNumericChange", numericType, value);
}

// ✅ 正确：使用 ET 事件
public static void Set(this NumericComponent self, int numericType, long value)
{
    long oldValue = self.GetByKey(numericType);
    self.NumericDic[numericType] = value;
    
    // 推荐：ET 事件可以传递完整的实体信息
    EventSystem.Instance.Publish(
        self.Scene(), 
        new BattleUnitNumericChange
        {
            BattleUnit = self.GetParent<BattleUnit>(),
            NumericType = numericType,
            OldValue = oldValue,
            NewValue = value
        }
    );
}

// ET 事件订阅者
[Event(SceneType.Main)]
public class BattleUnitNumericChange_UpdatePlayerStatus : AEvent<Scene, BattleUnitNumericChange>
{
    protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
    {
        // 玩家状态区、Boss 状态区等正式 UI 由对应窗口或 widget 订阅并刷新。
        // 不再通过 BattleUIHelper 为每个 BattleUnit 创建信息 widget。
        await ETTask.CompletedTask;
    }
}
```

**原因：**
- 涉及 ET 实体 (`BattleUnit`, `NumericComponent`)
- 需要 `Scene` 上下文
- 可能有多个订阅者（UI、音效、特效等）

### 场景 2：UI 按钮点击 → 使用 TEngine 事件

```csharp
// ✅ 正确：UI 内部通信使用 TEngine 事件
public class BattleMainUI : UIWindow
{
    protected override void OnCreate()
    {
        btnSettings.onClick.AddListener(() =>
        {
            // UI 内部事件
            GameEvent.Send("OnSettingsButtonClick");
        });
    }
}

public class SettingsPanelUI : UIWindow
{
    protected override void OnCreate()
    {
        // 订阅 UI 事件
        GameEvent.AddEventListener("OnSettingsButtonClick", OnSettingsOpen);
    }
    
    protected override void OnDestroy()
    {
        // 取消订阅
        GameEvent.RemoveEventListener("OnSettingsButtonClick", OnSettingsOpen);
    }
    
    private void OnSettingsOpen()
    {
        // 显示设置面板
        this.Visible = true;
    }
}
```

**原因：**
- 纯 UI 交互，不涉及游戏逻辑
- 不需要 ET 实体或 Scene
- 同步处理即可

### 场景 3：技能释放 → 混合使用

```csharp
// 1. UI 层：按钮点击 (TEngine 事件可选，也可以直接调用)
public class SkillBarUI : UIWindow
{
    private async void OnSkillButtonClick(int skillId)
    {
        // 直接调用 ET 逻辑层
        Scene clientScene = Client.Instance.CurrentScene();
        Session session = clientScene.GetComponent<SessionComponent>().Session;
        
        C2M_CastSkill request = C2M_CastSkill.Create();
        request.skillId = skillId;
        
        M2C_CastSkill response = await session.Call(request) as M2C_CastSkill;
        
        if (response.Error != ErrorCode.ERR_Success)
        {
            GameModule.UI.ShowToast(response.Message);
            return;
        }
        
        // 更新技能冷却 UI
        UpdateSkillCooldown(skillId, response.cooldownEnd);
    }
}

// 2. 服务端：技能释放成功后发布 ET 事件
public static async ETTask CastSkill(this SkillComponent self, int skillId)
{
    // 执行技能逻辑
    List<BattleUnit> targets = FindTargets(self, skillId);
    
    // 发布 ET 事件
    EventSystem.Instance.Publish(
        self.Scene(),
        new SkillCast
        {
            Caster = self.GetParent<BattleUnit>(),
            SkillId = skillId,
            Targets = targets
        }
    );
}

// 3. 客户端：ET 事件订阅者处理表现
[Event(SceneType.Main)]
public class SkillCast_PlayEffect : AEvent<Scene, SkillCast>
{
    protected override async ETTask Run(Scene scene, SkillCast args)
    {
        // 播放技能特效
        await PlaySkillEffect(args.SkillId, args.Caster.Position);
        
        // 播放音效 (调用 TEngine)
        GameModule.Audio.PlaySound($"Skill_{args.SkillId}");
        
        // 可选：发布 TEngine 事件通知 UI
        GameEvent.Send("OnSkillCast", args.SkillId);
        
        await ETTask.CompletedTask;
    }
}

// 4. UI 层：TEngine 事件订阅者更新 UI
public class SkillBarUI : UIWindow
{
    protected override void OnCreate()
    {
        // 订阅技能释放事件
        GameEvent.AddEventListener<int>("OnSkillCast", OnSkillCastHandler);
    }
    
    private void OnSkillCastHandler(int skillId)
    {
        // 播放技能按钮动画
        PlaySkillButtonAnimation(skillId);
    }
}
```

**流程：**
```
UI 点击 → ET 网络请求 → 服务端处理 → ET 事件 → 客户端表现 → TEngine 事件 → UI 更新
```

## 四、决策流程图

```
开始
  │
  ▼
是否涉及 ET Entity/Component？
  │
  ├─ 是 ──────────────────────────────> 使用 ET 事件系统
  │                                     EventSystem.Instance.Publish()
  │
  └─ 否
      │
      ▼
    是否需要 Scene 上下文？
      │
      ├─ 是 ────────────────────────> 使用 ET 事件系统
      │
      └─ 否
          │
          ▼
        是否需要异步处理？
          │
          ├─ 是 ──────────────────> 使用 ET 事件系统
          │
          └─ 否
              │
              ▼
            纯 UI 交互？
              │
              ├─ 是 ──────────────> 使用 TEngine 事件系统
              │                     GameEvent.Send()
              │
              └─ 否 ──────────────> 考虑直接调用
```

## 五、常见模式总结

### 模式 1：游戏逻辑 → UI 更新

```
ET 逻辑变化 → ET 事件 → ET 事件处理器 → 调用 TEngine UI API
```

```csharp
// ET 事件处理器
[Event(SceneType.Main)]
public class BattleUnitDead_UpdateUI : AEvent<Scene, BattleUnitDead>
{
    protected override async ETTask Run(Scene scene, BattleUnitDead args)
    {
        // 直接调用 TEngine UI
        var battleUI = await GameModule.UI.GetUIAsyncAwait<BattleMainUI>();
        battleUI?.OnUnitDead(args.BattleUnit.Id);
        
        await ETTask.CompletedTask;
    }
}
```

### 模式 2：UI 交互 → 游戏逻辑

```
TEngine UI 点击 → 直接调用 ET API → 网络请求
```

```csharp
// TEngine UI
public class BattleMainUI : UIWindow
{
    private async void OnAttackButtonClick()
    {
        // 直接调用 ET 逻辑，不需要事件
        Scene scene = Client.Instance.CurrentScene();
        Session session = scene.GetComponent<SessionComponent>().Session;
        
        C2M_AttackTarget request = C2M_AttackTarget.Create();
        M2C_AttackTarget response = await session.Call(request) as M2C_AttackTarget;
        
        // 处理结果
    }
}
```

### 模式 3：UI 组件间通信

```
UI A → TEngine 事件 → UI B
```

```csharp
// UI A
GameEvent.Send("OnInventoryItemSelected", itemId);

// UI B
GameEvent.AddEventListener<int>("OnInventoryItemSelected", OnItemSelected);
```

## 六、最佳实践

### ✅ 推荐做法

1. **ET 事件用于游戏逻辑层**
   - 实体数据变化
   - 跨模块逻辑通信
   - 需要异步处理的场景

2. **TEngine 事件用于 UI 表现层**
   - UI 组件间通信
   - 纯表现层事件
   - 不涉及游戏逻辑的交互

3. **直接调用用于明确的依赖关系**
   - UI → ET 逻辑（网络请求）
   - ET 事件处理器 → TEngine UI API

### ❌ 避免做法

1. **不要混用事件系统**
   ```csharp
   // ❌ 错误：在 ET 事件中发布 TEngine 事件
   [Event(SceneType.Main)]
   public class SomeEvent_Handler : AEvent<Scene, SomeEvent>
   {
       protected override async ETTask Run(Scene scene, SomeEvent args)
       {
           // 不推荐：增加复杂度
           GameEvent.Send("OnSomeEvent");
           await ETTask.CompletedTask;
       }
   }
   ```

2. **不要用 TEngine 事件传递 ET 实体**
   ```csharp
   // ❌ 错误：TEngine 事件无法很好地处理 ET 实体
   GameEvent.Send("OnUnitDead", battleUnit); // battleUnit 是 ET 实体
   ```

3. **不要忘记取消 TEngine 事件订阅**
   ```csharp
   // ❌ 错误：可能导致内存泄漏
   protected override void OnCreate()
   {
       GameEvent.AddEventListener("OnSomeEvent", Handler);
       // 忘记在 OnDestroy 中取消订阅
   }
   
   // ✅ 正确
   protected override void OnDestroy()
   {
       GameEvent.RemoveEventListener("OnSomeEvent", Handler);
   }
   ```

## 七、快速参考表

| 场景 | 使用事件系统 | 示例 |
|------|-------------|------|
| 实体数值变化 | ET | `BattleUnitNumericChange` |
| 实体死亡 | ET | `BattleUnitDead` |
| 技能释放 | ET | `SkillCast` |
| 波次开始/结束 | ET | `WaveStart`, `WaveComplete` |
| 网络消息接收 | ET | `M2C_XXX` 消息处理 |
| UI 按钮点击 | 直接调用 | `OnButtonClick()` |
| UI 面板切换 | TEngine | `GameEvent.Send("OnPanelSwitch")` |
| UI 组件通信 | TEngine | `GameEvent.Send("OnItemSelected")` |
| UI 动画完成 | TEngine | `GameEvent.Send("OnAnimComplete")` |

## 八、总结

**简单记忆法则：**

- **涉及 ET 实体/逻辑** → 用 ET 事件
- **纯 UI 交互** → 用 TEngine 事件
- **明确的调用关系** → 直接调用

**数据流向：**

```
服务端逻辑 → ET 事件 → ET 事件处理器 → TEngine API → UI 显示
     ↑                                              ↓
     └──────────── 网络请求 ←── 直接调用 ←── UI 交互
```

这样可以保持清晰的架构层次，避免混乱！
