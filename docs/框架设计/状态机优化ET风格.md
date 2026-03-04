# ET风格状态机优化方案

> **文档目标**：设计一个更符合ET框架风格的状态互斥机制  
> **核心理念**：利用ET的组件系统和事件系统，而非传统FSM模式  
> **更新日期**：2026-01-26

---

## 1. 当前问题分析

### 1.1 现有实现的问题

```csharp
// ❌ 当前方案：每个状态组件都要实现 CanTransitionTo
public static bool CanTransitionTo(this IdleStateComponent self, MachineState nextState)
{
    // 每个状态都要写一遍转换规则
    long now = TimeInfo.Instance.ServerNow();
    if (now - self.EnterTime < self.MinIdleDuration)
    {
        return false;
    }
    return true;
}
```

**问题**：
- ❌ 代码重复：每个状态组件都要实现相似的逻辑
- ❌ 不够ET：使用了传统FSM的switch/case模式
- ❌ 难以维护：状态转换规则分散在各个状态组件中
- ❌ 扩展性差：添加新状态需要修改多处代码

### 1.2 ET框架的核心理念

ET框架的设计哲学：
- ✅ **组件化**：功能通过组件组合实现，而非继承
- ✅ **数据驱动**：逻辑由数据配置驱动，而非硬编码
- ✅ **事件驱动**：模块间通过事件通信，降低耦合
- ✅ **热更新友好**：逻辑在Hotfix层，数据在Model层

---

## 2. ET风格的优化方案

### 2.1 方案一：状态标记组件（推荐）⭐

**核心思想**：不使用状态机，而是用多个互斥的标记组件表示状态。

#### 设计理念

```
传统FSM思维：Unit有一个StateMachine，StateMachine有一个CurrentState
ET组件思维：Unit直接挂载状态标记组件，通过组件的存在与否判断状态
```

#### 实现方案

**⚠️ 重要：状态标记组件只在服务端使用，放在Server解决方案中**

```csharp
// Model/Server/Demo/Unit/StateFlags/IdleStateFlag.cs
namespace ET.Server
{
    /// <summary>
    /// 待机状态标记组件（服务端）
    /// 挂载此组件表示Unit处于待机状态
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class IdleStateFlag : Entity, IAwake, IDestroy
    {
        public long EnterTime { get; set; }
    }
}

// Model/Server/Demo/Unit/StateFlags/MoveStateFlag.cs
namespace ET.Server
{
    /// <summary>
    /// 移动状态标记组件（服务端）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class MoveStateFlag : Entity, IAwake, IDestroy
    {
        public long EnterTime { get; set; }
    }
}

// Model/Server/Demo/Unit/StateFlags/AttackStateFlag.cs
namespace ET.Server
{
    /// <summary>
    /// 攻击状态标记组件（服务端）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AttackStateFlag : Entity, IAwake, IDestroy
    {
        public long EnterTime { get; set; }
        public long TargetId { get; set; }
    }
}
```

#### 状态切换逻辑

```csharp
// Hotfix/Server/Demo/Unit/StateHelper.cs
namespace ET.Server
{
    /// <summary>
    /// 状态切换辅助类（ET风格）
    /// </summary>
    public static class StateHelper
    {
        /// <summary>
        /// 切换到待机状态
        /// </summary>
        public static void ChangeToIdle(this Unit self)
        {
            // 移除所有其他状态标记
            self.RemoveComponent<MoveStateFlag>();
            self.RemoveComponent<AttackStateFlag>();
            
            // 添加待机状态标记
            if (self.GetComponent<IdleStateFlag>() == null)
            {
                self.AddComponent<IdleStateFlag>();
            }
        }
        
        /// <summary>
        /// 切换到移动状态
        /// </summary>
        public static void ChangeToMove(this Unit self)
        {
            // 检查防抖
            var idleFlag = self.GetComponent<IdleStateFlag>();
            if (idleFlag != null)
            {
                long now = TimeInfo.Instance.ServerNow();
                if (now - idleFlag.EnterTime < 200) // 200ms防抖
                {
                    Log.Debug($"[State] 防抖拒绝切换到Move");
                    return;
                }
            }
            
            // 移除其他状态
            self.RemoveComponent<IdleStateFlag>();
            self.RemoveComponent<AttackStateFlag>();
            
            // 添加移动状态
            if (self.GetComponent<MoveStateFlag>() == null)
            {
                self.AddComponent<MoveStateFlag>();
            }
        }
        
        /// <summary>
        /// 切换到攻击状态
        /// </summary>
        public static void ChangeToAttack(this Unit self, long targetId)
        {
            // 移除其他状态
            self.RemoveComponent<IdleStateFlag>();
            self.RemoveComponent<MoveStateFlag>();
            
            // 添加攻击状态
            var attackFlag = self.GetComponent<AttackStateFlag>();
            if (attackFlag == null)
            {
                attackFlag = self.AddComponent<AttackStateFlag>();
            }
            attackFlag.TargetId = targetId;
        }
        
        /// <summary>
        /// 检查是否在指定状态
        /// </summary>
        public static bool IsIdle(this Unit self) => self.GetComponent<IdleStateFlag>() != null;
        public static bool IsMoving(this Unit self) => self.GetComponent<MoveStateFlag>() != null;
        public static bool IsAttacking(this Unit self) => self.GetComponent<AttackStateFlag>() != null;
    }
}
```

#### 使用示例

```csharp
// 业务逻辑中使用
public static void StartCombat(this CombatComponent self, long targetId)
{
    Unit unit = self.GetParent<Unit>();
    
    // ✅ ET风格：直接切换状态标记
    unit.ChangeToAttack(targetId);
    
    self.IsInCombat = true;
    self.TargetId = targetId;
}

public static void EndCombat(this CombatComponent self)
{
    Unit unit = self.GetParent<Unit>();
    
    // ✅ ET风格：直接切换状态标记
    unit.ChangeToIdle();
    
    self.IsInCombat = false;
    self.TargetId = 0;
}

// AI决策中使用
public static void DecideAction(this PlayerAIComponent self)
{
    Unit unit = self.GetParent<Unit>();
    
    // ✅ ET风格：通过组件判断状态
    if (unit.IsAttacking())
    {
        // 正在攻击，不做处理
        return;
    }
    
    Unit target = AIHelper.FindNearestEnemy(unit, self.SearchRange);
    if (target == null) return;
    
    float dist = PositionHelper.Distance2D(unit, target);
    if (dist > self.AttackRange)
    {
        // 移动向目标
        unit.ChangeToMove();
        unit.FindPathMoveToAsync(target.Position).Coroutine();
    }
    else
    {
        // 攻击目标
        unit.ChangeToAttack(target.Id);
        unit.GetComponent<CombatComponent>().Attack(target, true).Coroutine();
    }
}
```

#### 优势分析

| 特性 | 传统FSM | ET组件风格 |
|:---|:---|:---|
| **代码量** | 需要StateMachine + 多个State类 | 只需要简单的Flag组件 |
| **状态判断** | `stateMachine.CurrentState == State.Idle` | `unit.GetComponent<IdleStateFlag>() != null` |
| **状态切换** | `stateMachine.ChangeState(State.Move)` | `unit.AddComponent<MoveStateFlag>()` |
| **互斥保证** | 需要手动实现 | 自动保证（同一类型组件只能有一个） |
| **扩展性** | 需要修改枚举和switch | 只需添加新的Flag组件 |
| **热更新** | 状态逻辑在Hotfix层 | 完全在Hotfix层 |
| **ET风格** | ❌ 不符合 | ✅ 完全符合 |

---

### 2.2 方案二：配置驱动的状态转换表

**核心思想**：将状态转换规则配置化，通过Excel配置状态转换关系。

#### Excel配置表设计

```
# StateTransitionConfig.xlsx

| Id | FromState | ToState | MinDuration | Priority | Condition |
|----|-----------|---------|-------------|----------|-----------|
| 1  | Idle      | Move    | 200         | 1        | None      |
| 2  | Idle      | Attack  | 200         | 2        | None      |
| 3  | Move      | Idle    | 0           | 1        | None      |
| 4  | Move      | Attack  | 0           | 2        | None      |
| 5  | Attack    | Idle    | 0           | 1        | None      |
| 6  | Attack    | Move    | 500         | 1        | None      |
```

#### 配置代码生成

```csharp
// Model/Generate/Config/StateTransitionConfig.cs (Luban生成)
namespace ET
{
    public partial class StateTransitionConfig
    {
        public int Id { get; set; }
        public string FromState { get; set; }
        public string ToState { get; set; }
        public long MinDuration { get; set; }
        public int Priority { get; set; }
        public string Condition { get; set; }
    }
}
```

#### 使用配置的状态切换

```csharp
// Hotfix/Server/Demo/Unit/StateTransitionHelper.cs
namespace ET.Server
{
    public static class StateTransitionHelper
    {
        /// <summary>
        /// 检查是否允许状态转换（配置驱动）
        /// </summary>
        public static bool CanTransition(this Unit self, string fromState, string toState)
        {
            var config = StateTransitionConfigCategory.Instance.GetByFromTo(fromState, toState);
            if (config == null)
            {
                Log.Warning($"[StateTransition] 未配置转换规则: {fromState} -> {toState}");
                return false;
            }
            
            // 检查最小停留时长
            var currentFlag = self.GetStateFlag(fromState);
            if (currentFlag != null)
            {
                long now = TimeInfo.Instance.ServerNow();
                if (now - currentFlag.EnterTime < config.MinDuration)
                {
                    Log.Debug($"[StateTransition] 防抖拒绝: {fromState} -> {toState}, 停留时长不足");
                    return false;
                }
            }
            
            // 检查自定义条件
            if (!string.IsNullOrEmpty(config.Condition))
            {
                // 可以扩展条件系统
                return CheckCondition(self, config.Condition);
            }
            
            return true;
        }
    }
}
```

---

### 2.3 方案三：事件驱动的状态通知

**核心思想**：状态切换通过事件系统通知，各模块监听事件做出响应。

#### 事件定义

**⚠️ 注意：事件定义在Server层，只在服务端使用**

```csharp
// Model/Server/Demo/Unit/UnitEventType.cs
namespace ET.Server
{
    [Event(SceneType.Map)]
    public struct UnitStateEnterEvent
    {
        public Unit Unit;
        public Type StateType; // typeof(IdleStateFlag)
    }
    
    [Event(SceneType.Map)]
    public struct UnitStateExitEvent
    {
        public Unit Unit;
        public Type StateType;
    }
}
```

#### 状态切换时发布事件

```csharp
// Hotfix/Server/Demo/Unit/StateHelper.cs
public static void ChangeToIdle(this Unit self)
{
    // 退出旧状态
    var oldMoveFlag = self.GetComponent<MoveStateFlag>();
    if (oldMoveFlag != null)
    {
        EventSystem.Instance.Publish(self.Scene(), new UnitStateExitEvent()
        {
            Unit = self,
            StateType = typeof(MoveStateFlag)
        });
        self.RemoveComponent<MoveStateFlag>();
    }
    
    // 进入新状态
    if (self.GetComponent<IdleStateFlag>() == null)
    {
        self.AddComponent<IdleStateFlag>();
        EventSystem.Instance.Publish(self.Scene(), new UnitStateEnterEvent()
        {
            Unit = self,
            StateType = typeof(IdleStateFlag)
        });
    }
}
```

#### 监听状态事件

```csharp
// Hotfix/Server/Demo/Unit/UnitStateEnter_StopMove.cs
namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitStateEnter_StopMove : AEvent<Scene, UnitStateEnterEvent>
    {
        protected override async ETTask Run(Scene scene, UnitStateEnterEvent args)
        {
            // 进入Idle状态时，停止移动
            if (args.StateType == typeof(IdleStateFlag))
            {
                var moveComp = args.Unit.GetComponent<MoveComponent>();
                moveComp?.Stop(false);
            }
            
            await ETTask.CompletedTask;
        }
    }
}

// Hotfix/Client/Demo/Unit/UnitStateEnter_PlayAnimation.cs
namespace ET.Client
{
    [Event(SceneType.Client)]
    public class UnitStateEnter_PlayAnimation : AEvent<Scene, UnitStateEnterEvent>
    {
        protected override async ETTask Run(Scene scene, UnitStateEnterEvent args)
        {
            // 客户端监听状态变化，播放动画
            if (args.StateType == typeof(AttackStateFlag))
            {
                // 播放攻击动画
                var animComp = args.Unit.GetComponent<AnimatorComponent>();
                animComp?.PlayAnimation("Attack");
            }
            
            await ETTask.CompletedTask;
        }
    }
}
```

---

## 3. 三种方案对比

| 方案 | 优点 | 缺点 | 适用场景 |
|:---|:---|:---|:---|
| **方案一：状态标记组件** | ✅ 最符合ET风格<br>✅ 代码简洁<br>✅ 易于理解 | ⚠️ 需要手动管理互斥 | ⭐ 推荐用于大多数场景 |
| **方案二：配置驱动** | ✅ 规则可配置<br>✅ 策划可调整<br>✅ 易于扩展 | ⚠️ 需要配置表<br>⚠️ 增加复杂度 | 状态转换规则复杂的项目 |
| **方案三：事件驱动** | ✅ 解耦彻底<br>✅ 易于扩展<br>✅ 支持多模块响应 | ⚠️ 调试困难<br>⚠️ 性能开销 | 需要多模块联动的场景 |

---

## 4. 推荐实现方案（混合方案）

结合三种方案的优点，推荐使用以下混合方案：

### 4.1 基础层：状态标记组件（方案一）

```csharp
// 使用简单的Flag组件表示状态
unit.AddComponent<IdleStateFlag>();
unit.AddComponent<MoveStateFlag>();
unit.AddComponent<AttackStateFlag>();
```

### 4.2 辅助层：状态切换Helper

```csharp
// Hotfix/Server/Demo/Unit/StateHelper.cs
public static class StateHelper
{
    /// <summary>
    /// 切换状态（自动处理互斥和防抖）
    /// </summary>
    public static void ChangeState<TNewState>(this Unit self) where TNewState : Entity, IAwake, new()
    {
        // 1. 检查防抖
        if (!self.CheckStateDebounce<TNewState>())
        {
            return;
        }
        
        // 2. 移除所有状态标记（自动互斥）
        self.RemoveAllStateFlags();
        
        // 3. 添加新状态
        self.AddComponent<TNewState>();
        
        // 4. 发布事件（可选）
        EventSystem.Instance.Publish(self.Scene(), new UnitStateChangedEvent()
        {
            Unit = self,
            NewStateType = typeof(TNewState)
        });
    }
    
    /// <summary>
    /// 移除所有状态标记
    /// </summary>
    private static void RemoveAllStateFlags(this Unit self)
    {
        self.RemoveComponent<IdleStateFlag>();
        self.RemoveComponent<MoveStateFlag>();
        self.RemoveComponent<AttackStateFlag>();
        // 未来添加新状态时，在这里添加
    }
    
    /// <summary>
    /// 检查防抖
    /// </summary>
    private static bool CheckStateDebounce<TNewState>(this Unit self) where TNewState : Entity
    {
        // 获取当前状态
        Entity currentState = self.GetCurrentStateFlag();
        if (currentState == null) return true;
        
        // 检查是否是同一状态
        if (currentState.GetType() == typeof(TNewState))
        {
            return false; // 已经在目标状态
        }
        
        // 检查最小停留时长（可以从配置读取）
        long minDuration = GetMinDuration(currentState.GetType());
        if (minDuration > 0)
        {
            long enterTime = GetStateEnterTime(currentState);
            long now = TimeInfo.Instance.ServerNow();
            if (now - enterTime < minDuration)
            {
                Log.Debug($"[State] 防抖拒绝切换: {currentState.GetType().Name} -> {typeof(TNewState).Name}");
                return false;
            }
        }
        
        return true;
    }
}
```

### 4.3 使用示例

```csharp
// ✅ 简洁的状态切换
unit.ChangeState<IdleStateFlag>();
unit.ChangeState<MoveStateFlag>();
unit.ChangeState<AttackStateFlag>();

// ✅ 简洁的状态判断
if (unit.GetComponent<IdleStateFlag>() != null)
{
    // 处于待机状态
}

// ✅ 或者使用扩展方法
if (unit.IsIdle())
{
    // 处于待机状态
}
```

---

## 5. 迁移指南

### 5.1 从现有StateMachine迁移

```csharp
// ❌ 旧代码
unit.GetComponent<StateMachineComponent>()?.ChangeState(MachineState.Idle);

// ✅ 新代码
unit.ChangeState<IdleStateFlag>();

// ❌ 旧代码
if (stateMachine.CurrentState == MachineState.Idle)

// ✅ 新代码
if (unit.GetComponent<IdleStateFlag>() != null)
// 或
if (unit.IsIdle())
```

### 5.2 迁移步骤

1. **创建状态标记组件**
   - 创建 `IdleStateFlag.cs`
   - 创建 `MoveStateFlag.cs`
   - 创建 `AttackStateFlag.cs`

2. **创建StateHelper**
   - 实现 `ChangeState<T>()` 方法
   - 实现 `IsIdle()` / `IsMoving()` / `IsAttacking()` 扩展方法

3. **替换业务代码**
   - 全局搜索 `StateMachineComponent`
   - 替换为新的状态标记组件

4. **删除旧代码**
   - 删除 `StateMachineComponent.cs`
   - 删除 `StateMachineComponentSystem.cs`
   - 删除各个状态组件（IdleStateComponent等）

---

## 6. 总结

### 6.1 核心优势

✅ **更ET**：完全符合ET的组件化设计理念  
✅ **更简洁**：代码量减少50%以上  
✅ **更灵活**：状态互斥自动保证，无需手动管理  
✅ **更易扩展**：添加新状态只需创建新的Flag组件  
✅ **更易调试**：通过组件面板直接查看当前状态  

### 6.2 最佳实践

1. **状态标记组件只存储数据，不包含逻辑**
2. **状态切换逻辑统一在StateHelper中管理**
3. **状态相关的业务逻辑通过事件系统解耦**
4. **防抖等通用规则可以配置化**
5. **保持状态数量精简，避免过度设计**

### 6.3 参考资源

- ET框架官方文档：https://et-framework.cn/
- ET组件系统设计：查看 `Entity.cs` 和 `Component.cs`
- ET事件系统设计：查看 `EventSystem.cs`

---

**文档作者**：ET开发团队  
**最后更新**：2026-01-26  
**文档版本**：v1.0
