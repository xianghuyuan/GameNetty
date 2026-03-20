# ET 与 TEngine 框架交互流程

## 一、框架职责划分

```
┌─────────────────────────────────────────────────────────────┐
│                      Unity 客户端架构                        │
├─────────────────────────────────────────────────────────────┤
│  TEngine (表现层)                                            │
│  ├─ UI 管理 (UIModule)                                      │
│  ├─ 资源加载 (ResourceModule)                               │
│  ├─ 场景管理 (SceneModule)                                  │
│  ├─ 音频管理 (AudioModule)                                  │
│  └─ 定时器 (TimerModule)                                    │
├─────────────────────────────────────────────────────────────┤
│  ET Framework (逻辑层)                                       │
│  ├─ 网络通信 (NetComponent)                                 │
│  ├─ 实体管理 (Entity/Component)                             │
│  ├─ 事件系统 (EventSystem)                                  │
│  ├─ 消息处理 (MessageHandler)                               │
│  └─ 业务逻辑 (各种 System)                                  │
└─────────────────────────────────────────────────────────────┘
```

## 二、典型交互流程

### 1. 战斗流程示例

```
┌──────────┐         ┌──────────┐         ┌──────────┐
│ TEngine  │         │    ET    │         │  Server  │
│   UI     │         │  Logic   │         │          │
└────┬─────┘         └────┬─────┘         └────┬─────┘
     │                    │                     │
     │ 1. 点击攻击按钮     │                     │
     ├───────────────────>│                     │
     │                    │ 2. 发送攻击消息      │
     │                    ├────────────────────>│
     │                    │                     │
     │                    │ 3. 服务端处理攻击    │
     │                    │    - 查找目标        │
     │                    │    - 计算伤害        │
     │                    │    - 更新数值        │
     │                    │                     │
     │                    │ 4. 返回攻击结果      │
     │                    │<────────────────────│
     │                    │                     │
     │                    │ 5. 发布事件          │
     │                    │ EventSystem.Publish │
     │                    │ (BattleUnitNumericChange)
     │                    │                     │
     │ 6. 更新 UI 显示     │                     │
     │<───────────────────│                     │
     │ (BattleUIHelper)   │                     │
     │                    │                     │
```

### 2. 消息发送流程

```csharp
// 客户端发送消息
public async void OnAttackButtonClick()
{
    // 1. TEngine UI 层捕获点击
    // 2. 调用 ET 逻辑层发送消息
    
    Scene clientScene = Client.Instance.CurrentScene();
    Session session = clientScene.GetComponent<SessionComponent>().Session;
    
    C2M_AttackTarget request = C2M_AttackTarget.Create();
    M2C_AttackTarget response = await session.Call(request) as M2C_AttackTarget;
    
    if (response.Error != ErrorCode.ERR_Success)
    {
        Log.Error($"攻击失败: {response.Message}");
        return;
    }
    
    // 3. 处理返回结果（可选，通常通过事件处理）
    Log.Info($"攻击成功，造成伤害: {response.result.damage}");
}
```

## 三、ET 事件系统详解

### 1. 事件定义

```csharp
// 定义事件结构体
namespace ET
{
    // 战斗单位数值变化事件
    public struct BattleUnitNumericChange
    {
        public BattleUnit BattleUnit;
        public int NumericType;
        public long OldValue;
        public long NewValue;
    }
    
    // 战斗单位死亡事件
    public struct BattleUnitDead
    {
        public BattleUnit BattleUnit;
        public BattleUnit Killer;
    }
}
```

### 2. 事件发布

```csharp
// 服务端：数值变化时发布事件
public static void Set(this NumericComponent self, int numericType, long value)
{
    long oldValue = self.GetByKey(numericType);
    self.NumericDic[numericType] = value;
    
    // 发布数值变化事件
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
```

### 3. 事件订阅（客户端）

```csharp
// 客户端：监听数值变化事件，更新 UI
namespace ET
{
    [Event(SceneType.Main)]
    public class BattleUnitNumericChange_UI : AEvent<Scene, BattleUnitNumericChange>
    {
        protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
        {
            // 更新 TEngine UI
            BattleUIHelper.OnNumericChange(
                args.BattleUnit, 
                args.NumericType, 
                args.NewValue
            );
            
            await ETTask.CompletedTask;
        }
    }
}
```

### 4. 事件订阅（服务端）

```csharp
// 服务端：监听单位死亡事件
namespace ET.Server
{
    [Event(SceneType.Map)]
    public class BattleUnitDead_RemoveFromRoom : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUnit deadUnit = args.BattleUnit;
            BattleRoom battleRoom = deadUnit.GetParent<BattleRoom>();
            
            // 从房间移除
            battleRoom.RemoveUnit(deadUnit.Id);
            
            // 通知波次管理器
            WaveManagerComponent waveManager = battleRoom.GetComponent<WaveManagerComponent>();
            if (deadUnit.Camp == UnitCamp.Enemy)
            {
                await waveManager.OnMonsterDead(deadUnit.Id);
            }
            
            await ETTask.CompletedTask;
        }
    }
}
```

## 四、开发流程示例

### 场景 1：添加新的战斗技能

#### 步骤 1：定义消息（Proto）

```protobuf
// Config/Proto/OuterMessage_C_10001.proto

// ResponseType M2C_CastSkill
message C2M_CastSkill // ISessionRequest
{
    int32 RpcId = 1;
    int32 skillId = 2;      // 技能ID
}

message M2C_CastSkill // ISessionResponse
{
    int32 RpcId = 1;
    int32 Error = 2;
    string Message = 3;
    int64 cooldownEnd = 4;  // 冷却结束时间
}
```

#### 步骤 2：服务端处理器

```csharp
// Server/Hotfix/Demo/Battle/Handler/C2M_CastSkillHandler.cs

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_CastSkillHandler : MessageHandler<Scene, C2M_CastSkill, M2C_CastSkill>
    {
        protected override async ETTask Run(Scene scene, C2M_CastSkill request, M2C_CastSkill response)
        {
            // 1. 查找玩家战斗单位
            BattleUnit caster = FindPlayerBattleUnit(scene);
            
            // 2. 校验技能
            SkillComponent skillComp = caster.GetComponent<SkillComponent>();
            if (!skillComp.CanCastSkill(request.skillId))
            {
                response.Error = ErrorCode.ERR_SkillCooldown;
                return;
            }
            
            // 3. 执行技能
            await skillComp.CastSkill(request.skillId);
            
            // 4. 返回结果
            response.cooldownEnd = skillComp.GetCooldownEnd(request.skillId);
            
            await ETTask.CompletedTask;
        }
    }
}
```

#### 步骤 3：定义事件

```csharp
// Unity/Assets/GameScripts/HotFix/GameProto/EventType.cs

namespace ET
{
    public struct SkillCast
    {
        public BattleUnit Caster;
        public int SkillId;
        public List<BattleUnit> Targets;
    }
}
```

#### 步骤 4：客户端 UI 响应

```csharp
// Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/SkillUIHelper.cs

namespace ET
{
    [Event(SceneType.Main)]
    public class SkillCast_PlayEffect : AEvent<Scene, SkillCast>
    {
        protected override async ETTask Run(Scene scene, SkillCast args)
        {
            // 播放技能特效
            await PlaySkillEffect(args.SkillId, args.Caster.Position);
            
            // 播放音效
            GameModule.Audio.PlaySound($"Skill_{args.SkillId}");
            
            await ETTask.CompletedTask;
        }
    }
}
```

#### 步骤 5：TEngine UI 按钮

```csharp
// Unity/Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs

public class BattleMainUI : UIWindow
{
    private async void OnSkillButtonClick(int skillId)
    {
        // 调用 ET 逻辑层
        Scene clientScene = Client.Instance.CurrentScene();
        Session session = clientScene.GetComponent<SessionComponent>().Session;
        
        C2M_CastSkill request = C2M_CastSkill.Create();
        request.skillId = skillId;
        
        M2C_CastSkill response = await session.Call(request) as M2C_CastSkill;
        
        if (response.Error != ErrorCode.ERR_Success)
        {
            // TEngine 显示错误提示
            GameModule.UI.ShowToast(response.Message);
            return;
        }
        
        // 更新技能冷却 UI
        UpdateSkillCooldown(skillId, response.cooldownEnd);
    }
}
```

## 五、常见模式总结

### 1. 客户端请求-服务端响应

```
TEngine UI → ET Client → Network → ET Server → Response → ET Client → TEngine UI
```

### 2. 服务端推送-客户端更新

```
ET Server → Network → ET Client → Event → TEngine UI
```

### 3. 事件驱动更新

```
Logic Change → EventSystem.Publish → Event Handlers → UI Update
```

## 六、关键注意事项

### 1. 职责分离

- **TEngine**：只负责表现层（UI、音效、特效、资源加载）
- **ET**：负责所有游戏逻辑、网络通信、数据管理

### 2. 通信方式

- **TEngine → ET**：直接调用 ET 的 API
- **ET → TEngine**：通过事件系统解耦

### 3. 数据流向

```
服务端权威 → 客户端同步 → UI 显示
```

### 4. 事件使用原则

- 用于模块间解耦
- 避免循环依赖
- 一个事件可以有多个订阅者
- 事件处理应该是异步的（async ETTask）

## 七、调试技巧

### 1. 查看事件流

```csharp
// 在事件处理器中添加日志
protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
{
    Log.Debug($"[Event] BattleUnitNumericChange: Unit={args.BattleUnit.Id}, Type={args.NumericType}, Value={args.NewValue}");
    // ...
}
```

### 2. 追踪消息流

```csharp
// 在消息处理器中添加日志
protected override async ETTask Run(Scene scene, C2M_AttackTarget request, M2C_AttackTarget response)
{
    Log.Info($"[Message] C2M_AttackTarget received");
    // ...
    Log.Info($"[Message] M2C_AttackTarget response: Error={response.Error}");
}
```

### 3. 检查组件状态

```csharp
// 使用 ComponentView 查看实体组件
var components = entity.Components;
foreach (var comp in components)
{
    Log.Debug($"Component: {comp.GetType().Name}");
}
```

## 八、推荐开发顺序

1. **定义协议**（Proto 文件）
2. **实现服务端逻辑**（Handler + System）
3. **定义事件**（EventType）
4. **实现事件处理**（客户端订阅）
5. **实现 UI 交互**（TEngine UI）
6. **测试验证**

这样可以确保从底层到表现层的完整流程！
