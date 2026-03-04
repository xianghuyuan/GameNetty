# 怪物 AI 系统迁移指南：从 FSM 到 Logic Lock

## 📋 概述

已将 ET 的怪物 AI 从**有限状态机 (FSM)** 重构为**基于计数器的逻辑锁 (Logic Lock)** 系统，以支持高性能割草游戏场景（1000+ 怪物）。

**命名规范**：
- ✅ **Logic 不使用 Share** - 所有 AI 逻辑在 Server 层
- ✅ **客户端类加 Client 前缀** - 清晰区分服务端和客户端

---

## 🔄 核心变化对比

| 特性 | 旧实现 (FSM) | 新实现 (Logic Lock) |
|------|--------------|---------------------|
| **状态管理** | 互斥状态 (Idle/Attack) | 计数器锁 (MoveForbiddenCount) |
| **状态切换** | 显式 ChangeState() | 自动增减计数器 |
| **动画驱动** | M2C_MonsterStateChange | ClientMonsterPlayAnimation 事件 |
| **并发支持** | 同一时间一个状态 | 多行为叠加 (攻击+受击) |
| **错误恢复** | 可能卡死 | finally 确保解锁 |
| **性能优化** | 无 | ServerTimeSlicingComponent 分帧更新 |

---

## 🏗️ 架构变化

### 1. Model 层变化

**MoveComponent** (Share/Module/Move/MoveComponent.cs) - 新增字段
```csharp
public int MoveForbiddenCount;  // 移动禁制计数器
public bool CanMove => MoveForbiddenCount == 0;
```

**MonsterAIComponent** (Server/Demo/Monster/MonsterAIComponent.cs) - 简化字段
```csharp
public bool IsAttacking;        // 替代 State，更简单
public float PerceptionRange;   // 感知范围（新增）
```

**MonsterAIEvents** (Server - 新增)
```csharp
namespace ET.Server
{
    public struct MonsterPlayAnimation
    {
        public Unit Unit;
        public string Name;
    }
}
```

**ClientMonsterAIEvents** (Client - 新增)
```csharp
namespace ET.Client
{
    public struct ClientMonsterPlayAnimation
    {
        public Unit Unit;
        public string Name;
    }
}
```

### 2. Hotfix 层变化

**新增文件：**
- `MonsterAILogicServerHelper.cs` - 异步 AI 行为执行（Server）
- `MonsterAIEvents.cs` - 服务端事件定义（Server）
- `ServerTimeSlicingComponent.cs` - 性能优化组件（Server）
- `ClientMonsterAIEvents.cs` - 客户端事件定义（Client）
- `ClientMonsterAnimEventListenerComponent.cs` - 客户端动画监听（Client）

**修改文件：**
- `MonsterAIComponentSystem.cs` - 移除状态机，改为事件驱动
- `MoveComponentSystem.cs` - 添加逻辑锁检查

### 3. 删除文件
- ~~`MonsterAILogicHelper.cs`~~ → 改为 `MonsterAILogicServerHelper.cs`
- ~~`TimeSlicingComponent.cs`~~ → 改为 `ServerTimeSlicingComponent.cs`
- ~~`MonsterAnimEventListenerComponent.cs`~~ → 改为 `ClientMonsterAnimEventListenerComponent.cs`
- ~~Share/Demo/AI/*~~ → 不使用 Share 层

---

## 🔧 使用方式

### 服务端使用

#### 1. 基础 AI 启动（不变）
```csharp
MonsterAIComponent ai = monster.AddComponent<MonsterAIComponent>();
ai.StartAI();
```

#### 2. 启用分帧优化（新）
```csharp
Room room = ...;
ServerTimeSlicingComponent timeSlicing = room.AddComponent<ServerTimeSlicingComponent, int>(5); // 5 分片
```

#### 3. 手动锁定移动（新）
```csharp
MoveComponent move = unit.GetComponent<MoveComponent>();
move.MoveForbiddenCount++;  // 锁定移动
// ... 执行某些逻辑
move.MoveForbiddenCount--;  // 解锁移动
```

### 客户端使用

#### 1. 添加动画监听器
```csharp
ClientMonsterAnimEventListenerComponent animListener = monster.AddComponent<ClientMonsterAnimEventListenerComponent>();
// 自动监听 ClientMonsterPlayAnimation 事件并播放动画
```

#### 2. 手动触发动画（服务端）
```csharp
EventSystem.Instance.Publish(unit.Scene(), new MonsterPlayAnimation
{
    Unit = unit,
    Name = "Attack"  // "Idle", "Run", "Attack", "Hit", "Die"
});
```

---

## 🎯 行为变化

### 旧实现行为
1. Idle → 找到目标 → Attack
2. Attack → 距离远 → 追击（仍在 Attack 状态）
3. 状态切换需要显式调用 `ChangeState()`

### 新实现行为
1. AITick → 检查目标 → 异步执行攻击/移动
2. 攻击时自动加锁，finally 解锁
3. 支持攻击+受击等叠加状态
4. 动画通过事件自动播放

---

## 🚀 性能优化

### 1. 分帧 AI (Time Slicing)
```csharp
// 5 分片，每帧只更新 20% 的怪物
ServerTimeSlicingComponent timeSlicing = room.AddComponent<ServerTimeSlicingComponent, int>(5);
```

### 2. 动画优化建议
- 距离玩家远的怪物禁用 Animator
- 使用简单的 Sprite 切换代替动画
- 只对视野内的怪物播放动画

---

## ⚠️ 迁移注意事项

### 1. 命名规范（重要！）
- ✅ **Server 层类名**：`MonsterAILogicServerHelper`, `ServerTimeSlicingComponent`
- ✅ **Client 层类名**：`ClientMonsterAnimEventListenerComponent`, `ClientMonsterPlayAnimation`
- ✅ **Logic 不使用 Share**：所有 AI 逻辑在 Server 层

### 2. 兼容性
- **保留**了 `MonsterAIState` 枚举，但不再作为核心逻辑
- **保留**了 `ChangeState()` 方法，但仅用于网络同步

### 3. 配置变化
需要在 `MonsterBaseConfig.xlsx` 中添加 `PerceptionRange` 字段（怪物感知范围）

### 4. 网络消息
`M2C_MonsterStateChange` 仍然保留用于客户端显示，但不再是核心驱动逻辑

### 5. 向后兼容
如果不想立即迁移表现层，可以继续使用 `ChangeState()` 触发网络消息，新系统会同步工作

---

## 📝 代码示例对比

### 旧实现 (FSM)
```csharp
private static void OnAttack(this MonsterAIComponent self)
{
    if (!self.IsTargetValid())
    {
        self.ChangeState(MonsterAIState.Idle);
        return;
    }
    
    float distance = math.distance(unit.Position, target.Position);
    if (distance > combat.AttackRange)
    {
        self.ChangeState(MonsterAIState.Idle);  // 切换状态
        return;
    }
    
    combat.Attack(target).Coroutine();
}
```

### 新实现 (Logic Lock)
```csharp
private static async ETTask ExecuteAILogic(this MonsterAIComponent self)
{
    Unit target = unitComponent.Get(self.TargetId);
    if (target == null)
    {
        self.TargetId = 0;  // 清空目标即可，无需切换状态
        return;
    }
    
    float distance = math.distance(unit.Position, target.Position);
    if (distance <= combat.AttackRange)
    {
        self.IsAttacking = true;  // 标记正在攻击
        await MonsterAILogicServerHelper.ExecuteAttackAsync(unit, target);
        self.IsAttacking = false;  // 自动恢复
    }
    else
    {
        await MonsterAILogicServerHelper.MoveToTargetServerAsync(unit, target, combat.AttackRange * 0.8f);
    }
}
```

---

## ✅ 验证清单

- [ ] 编译通过
- [ ] 怪物正常寻敌
- [ ] 怪物正常攻击
- [ ] 攻击时停止移动
- [ ] 攻击结束后恢复移动
- [ ] 动画正确播放
- [ ] 分帧优化生效
- [ ] 多怪物场景性能测试
- [ ] **命名符合规范（Server/Client 前缀）**
- [ ] **Logic 不使用 Share**

---

## 🎓 最佳实践

1. **使用异步任务包装 AI 行为**，确保 finally 中释放锁
2. **使用事件驱动表现层**，逻辑层不依赖具体动画
3. **启用分帧优化**，避免 1000+ 怪物同时更新
4. **监控 MoveForbiddenCount**，确保不会泄漏（一直 > 0）
5. **遵循命名规范**：
   - Server 层：`MonsterAILogicServerHelper`, `ServerTimeSlicingComponent`
   - Client 层：`ClientMonsterAnimEventListenerComponent`, `ClientMonsterPlayAnimation`
   - 不使用 Share 层存放 AI 逻辑

---

## 📞 问题排查

### 问题：怪物不动
- 检查 `MoveForbiddenCount` 是否一直 > 0
- 确认所有异步任务都有 `finally` 解锁

### 问题：动画不播放
- 确认客户端已添加 `ClientMonsterAnimEventListenerComponent`
- 检查事件是否正确发布（`ClientMonsterPlayAnimation`）

### 问题：性能差
- 启用 `ServerTimeSlicingComponent`
- 减少感知范围 `PerceptionRange`
- 增大 `AITickInterval`

---

## 🔗 相关文档

- 割草游戏高性能 AI 逻辑设计文档：`/docs/割草游戏高性能 AI 逻辑设计文档.md`
- ET 框架 Fiber 单线程模型详解：`/docs/ET框架Fiber单线程模型详解.md`
