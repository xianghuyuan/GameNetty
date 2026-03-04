# GameNetty 高性能 AI 系统设计 (Roguelike/Survivor Style)

## 1. 目标
针对“割草类”游戏（如《吸血鬼幸存者》），实现同屏上千个 AI 实体的流畅运行。

---

## 2. 核心挑战
- **同屏实体多**: 超过 1000+ 个怪物的逻辑更新。
- **寻路开销大**: 每个怪物的 A* 寻路会拖垮 CPU。
- **渲染瓶颈**: 大量 DrawCall。

---

## 3. GameNetty 优化方案

### 3.1 逻辑分帧与分块 (Fibers)
利用 ET8.1 的 Fiber 模型，将怪物的 AI 逻辑分布在不同的 `Logic Fiber` 中。
- **高频逻辑 (Update)**: 移动、位移计算。
- **低频逻辑 (LateUpdate)**: 目标寻找、状态决策。

### 3.2 向量寻路 (Flow Field)
放弃传统的 A* 寻路，采用 **Flow Field (流场)** 或简单的 **Vector Steering (向量操控)**。
- 所有的怪物共享一张全局的“向玩家移动”的重力图。
- 每个怪物只需读取当前网格的向量方向即可完成移动。

### 3.3 实体池 (Pools)
利用 ET 的 `ObjectPool` 和 `EntityPool`。
- 怪物的创建与销毁不再触发 GC。
- 所有的 `Component` 均在池中循环使用。

---

## 4. AI 状态机实现 (StateMachine)

### 定义状态
在 `Model` 层定义状态枚举和组件。
```csharp
public enum AIState
{
    Idle,
    Chasing,
    Attacking,
    Dying
}

[ComponentOf(typeof(Unit))]
public class AIComponent : Entity, IAwake, IUpdate
{
    public AIState CurrentState;
    public long TargetUnitId;
}
```

### 状态切换逻辑 (Hotfix 层)
```csharp
[EntitySystemOf(typeof(AIComponent))]
public static partial class AIComponentSystem
{
    [EntitySystem]
    private static void Update(this AIComponent self)
    {
        switch (self.CurrentState)
        {
            case AIState.Chasing:
                self.HandleChasing();
                break;
            // ...
        }
    }
}
```

---

## 5. 视图表现优化
- **GPU Instancing**: 使用支持实例化渲染的材质。
- **Simple Animation**: 背景层怪物使用纹理动画或简单的顶点位移，减少 Spine 的计算量。
- **LOD (Level of Detail)**: 远处的怪物不显示特效，不计算复杂的物理碰撞。
