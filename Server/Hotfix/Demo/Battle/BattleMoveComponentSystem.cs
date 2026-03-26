using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 移动组件响应请求移动事件
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnRequestMove : AEvent<BattleRoom, RequestMoveEvent>
    {
        protected override async ETTask Run(BattleRoom scene, RequestMoveEvent args)
        {
            BattleUnit unit = args.Unit;
            if (unit == null || unit.IsDead)
            {
                return;
            }

            BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
            moveComponent?.StartMove(args.TargetPosition);

            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 冻结结束事件处理器 - 重新决策
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnFreezeEnd : AEvent<Scene, FreezeEndEvent>
    {
        protected override async ETTask Run(Scene scene, FreezeEndEvent args)
        {
            BattleUnit unit = args.Target;
            if (unit == null || unit.IsDead)
            {
                return;
            }
            
            BattleActionDecisionComponent decisionComponent = unit.GetComponent<BattleActionDecisionComponent>();
            if (decisionComponent != null)
            {
                decisionComponent.MakeDecision();
            }
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 击退事件处理器 - 停止移动并瞬移到击退位置
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class KnockbackEvent_OnKnockback : AEvent<Scene, KnockbackEvent>
    {
        protected override async ETTask Run(Scene scene, KnockbackEvent args)
        {
            BattleUnit target = args.Target;
            if (target == null || target.IsDead)
            {
                return;
            }
            
            BattleMoveComponent moveComp = target.GetComponent<BattleMoveComponent>();
            if (moveComp == null)
            {
                return;
            }
            
            // 停止当前移动
            moveComp.StopMove();
            
            // 计算击退目标位置并直接设置
            System.Numerics.Vector3 targetPosition = new System.Numerics.Vector3(
                target.Position.X + args.Direction * args.Distance,
                target.Position.Y,
                target.Position.Z
            );
            target.Position = targetPosition;
            
            // 广播击退
            BattleUnitHelper.BroadcastKnockback(target, args.Distance, args.Direction);
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 冻结开始事件处理器 - 停止移动
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnFreezeStart : AEvent<Scene, FreezeStartEvent>
    {
        protected override async ETTask Run(Scene scene, FreezeStartEvent args)
        {
            BattleUnit unit = args.Target;
            if (unit == null)
            {
                return;
            }
            
            BattleMoveComponent moveComp = unit.GetComponent<BattleMoveComponent>();
            moveComp?.StopMove();
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 施法结束事件处理器 - 重新决策
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnCastingEnd : AEvent<Scene, CastingEndEvent>
    {
        protected override async ETTask Run(Scene scene, CastingEndEvent args)
        {
            BattleUnit unit = args.Unit;
            if (unit == null || unit.IsDead)
            {
                return;
            }

            BattleActionDecisionComponent decisionComponent = unit.GetComponent<BattleActionDecisionComponent>();
            decisionComponent?.MakeDecision();

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 移动组件响应请求停止移动事件
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnRequestStopMove : AEvent<BattleRoom, RequestStopMoveEvent>
    {
        protected override async ETTask Run(BattleRoom scene, RequestStopMoveEvent args)
        {
            BattleUnit unit = args.Unit;
            if (unit == null)
            {
                return;
            }

            BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
            moveComponent?.StopMove();

            await ETTask.CompletedTask;
        }
    }
    
    [Invoke(TimerInvokeType.BattleMoveTick)]
    public class BattleMoveTimer : ATimer<BattleMoveComponent>
    {
        protected override void Run(BattleMoveComponent self)
        {
            BattleMoveComponentSystem.OnMoveTick(self);
        }
    }

    [EntitySystemOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleMoveComponentSystem
    {
        private const long MoveTickInterval = 100; // 100ms 移动心跳

        [EntitySystem]
        private static void Awake(this BattleMoveComponent self)
        {
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
            self.MoveTimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(MoveTickInterval, TimerInvokeType.BattleMoveTick, self);
        }

        [EntitySystem]
        private static void Destroy(this BattleMoveComponent self)
        {
            long timerId = self.MoveTimerId;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref timerId);
            self.MoveTimerId = 0;
        }

        public static void StartMove(this BattleMoveComponent self, Vector3 targetPosition)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null)
            {
                return;
            }

            NumericComponent numeric = owner.GetComponent<NumericComponent>();
            float moveSpeed = numeric?.GetAsFloat(NumericType.Speed) ?? self.MoveSpeed;
            if (moveSpeed <= 0f)
            {
                moveSpeed = self.MoveSpeed;
            }

            self.MoveSpeed = moveSpeed;
            self.TargetPosition = targetPosition;
            BattleUnitHelper.BroadcastMoveCommand(owner,targetPosition,moveSpeed);
        }

        public static void StopMove(this BattleMoveComponent self)
        {
            if (self.MoveSpeed > 0f)
            {
                BattleUnit owner = self.GetParent<BattleUnit>();
                if (owner != null)
                {
                    BattleUnitHelper.BroadcastPositionSync(owner);
                }
            }

            self.MoveSpeed = 0f;
        }

        /// <summary>
        /// 移动定时器回调 - 每100ms执行一次
        /// </summary>
        internal static void OnMoveTick(BattleMoveComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead || self.MoveSpeed <= 0f)
            {
                return;
            }

            float distance = BattleDistanceHelper.GetDistance(owner.Position, self.TargetPosition);
            if (distance < 0.01f)
            {
                self.StopMove();
                EventSystem.Instance.Publish(self.Scene<BattleRoom>()!, new ReachTargetEvent { Unit = owner });
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            long deltaTime = currentTime - self.LastUpdateTime;
            self.LastUpdateTime = currentTime;

            float moveDistance = self.MoveSpeed * deltaTime / 1000f;
            Vector3 newPosition = BattleDistanceHelper.MoveTowardsX(owner.Position, self.TargetPosition, moveDistance);

            if (BattleDistanceHelper.GetDistance(owner.Position, newPosition) <= 0.0001f)
            {
                return;
            }

            owner.Position = newPosition;
        }
    }
}
