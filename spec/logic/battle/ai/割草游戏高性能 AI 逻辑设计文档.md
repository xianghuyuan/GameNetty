# 🎮 割草游戏高性能 AI 逻辑设计文档 (ET 框架版)

## 1. 核心设计理念

在处理同屏 **1000+** 怪物时，本方案放弃了传统的**有限状态机 (FSM)**，采用**基于计数器的逻辑锁 (Logic Lock)** 机制。

* **非互斥性**：允许“移动”、“施法”、“受击”等状态同时并存或叠加。
* **数据驱动**：逻辑层只操作数值（计数器），表现层监听数值变化并切换动画。
* **高鲁棒性**：利用 `ETTask` 与 `finally` 代码块确保逻辑锁的成对增减，防止怪物“永久卡死”。

---

## 2. 系统架构预览

| 层次 | 核心组件 / 系统 | 职能描述 |
| --- | --- | --- |
| **Model** | `MoveComponent`, `AIComponent` | 存储移动锁计数、目标 ID、基础速度等原始数据。 |
| **Hotfix** | `MoveSystem`, `SkillSystem` | 处理逻辑锁自增/自减，执行坐标计算，判定攻击触发。 |
| **View** | `GameObjectComponent` | 监听逻辑层数据，驱动 Unity 的 Animator 和特效播放。 |

---

## 3. 代码实现参考

### 3.1 模型定义 (Model 层)

在 `MoveComponent` 中引入 `MoveForbiddenCount` 作为核心控制因子。

```csharp
namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class MoveComponent : Entity, IAwake
    {
        // 核心：移动禁制计数器。当 > 0 时，禁止自主位移
        public int MoveForbiddenCount;
        
        // 移动数据
        public float Speed;
        public Vector3 TargetPos;
    }
}

```

### 3.2 逻辑锁应用 (Hotfix 层)

以“施法逻辑”为例，展示如何通过异步任务确保锁的安全性。

```csharp
public static class SkillHelper
{
    public static async ETTask CastSkillAsync(Unit unit, int skillId)
    {
        var moveComp = unit.GetComponent<MoveComponent>();
        
        // 1. 加上移动锁
        moveComp.MoveForbiddenCount++; 
        
        // 2. 通知表现层播放动画（解耦）
        Game.EventSystem.Publish(new MonsterPlayAnimation { Unit = unit, Name = "Cast" });

        try 
        {
            // 模拟前摇或持续施法过程
            await TimerComponent.Instance.WaitAsync(1000); 
            // 产生伤害...
        }
        finally 
        {
            // 3. 关键：在 finally 中解锁，确保即使任务被 Cancel 也能恢复移动
            moveComp.MoveForbiddenCount--;
            
            // 4. 若解锁后恢复自由，回到 Idle 或 Run
            if (moveComp.MoveForbiddenCount == 0)
                Game.EventSystem.Publish(new MonsterPlayAnimation { Unit = unit, Name = "Idle" });
        }
    }
}

```

### 3.3 移动系统判定 (Hotfix 层)

`UpdateSystem` 会根据锁的状态决定是否执行物理位移。

```csharp
[ObjectSystem]
public class MoveUpdateSystem : UpdateSystem<MoveComponent>
{
    protected override void Update(MoveComponent self)
    {
        // 如果被锁住（如正在施法、眩晕、被击退），则不执行自主寻路位移
        if (self.MoveForbiddenCount > 0) return;

        Unit unit = self.GetParent<Unit>();
        // 执行正常的位移逻辑...
        unit.Position += unit.Forward * self.Speed * Time.DeltaTime;
    }
}

```

---

## 4. 表现层驱动逻辑 (View 层)

表现层不关心具体是谁锁住了移动，它只根据当前的**数据状态**来决定动画方案。

### 动画优先级判定表

| 条件判定 | 播放动画 | 说明 |
| --- | --- | --- |
| `IsDead == true` | **Die** | 最高优先级。 |
| `MoveForbiddenCount > 0` | **Cast / Hit / Idle** | 依据最后一次触发的事件决定。 |
| `MoveForbiddenCount == 0 && Velocity > 0` | **Run** | 只有没被锁且有速度时才跑。 |
| `MoveForbiddenCount == 0 && Velocity == 0` | **Idle** | 自由状态下的待机。 |

---

## 5. 性能压榨建议（割草游戏专攻）

1. **分帧 AI (Time Slicing)**：
不要每帧让 1000 个怪都寻找玩家。利用 `unit.Id % 5` 将怪物分 5 组，每帧只让一组怪更新 `TargetPos`。
2. **空间分区 (Grid/Quadtree)**：
放弃 Unity 物理。使用简单的网格划分，怪物的挤压避障逻辑只针对同一网格内的邻居进行。
3. **动画状态机降级**：
在 View 层，对距离玩家较远的怪物禁用 Animator，改为手动修改 Transform 的位置或使用简单的 `Sprite` 切换，以节省 `Animator.Update` 的 CPU 消耗。
