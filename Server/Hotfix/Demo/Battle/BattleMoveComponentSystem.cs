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
            moveComponent?.StartMove(args.TargetPosition, args.ChaseTargetId, args.ChaseAttackRange);

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
    /// 击退事件处理器 - 停止移动并设置击退位置，带容错纠偏
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class KnockbackEvent_OnKnockback : AEvent<Scene, KnockbackEvent>
    {
        private const float CorrectionThreshold = 0.2f;
        private const float SmoothDuration = 0.1f;

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

            moveComp.StopMove();

            System.Numerics.Vector3 targetPosition = new System.Numerics.Vector3(
                target.Position.X + args.Direction * args.Distance,
                target.Position.Y,
                target.Position.Z
            );
            target.Position = targetPosition;

            BattleRoom battleRoom = target.GetParent<BattleRoom>();
            BattleSpatialGrid spatialGrid = battleRoom?.GetComponent<BattleSpatialGrid>();
            spatialGrid?.UpdatePosition(target.Id, targetPosition.X);

            BattleUnitHelper.BroadcastKnockback(target, args.Distance, args.Direction);

            M2C_ForceCorrectPos correctMsg = M2C_ForceCorrectPos.Create();
            correctMsg.unitId = target.Id;
            correctMsg.correctPosition = new Unity.Mathematics.float3(
                target.Position.X, target.Position.Y, target.Position.Z);
            correctMsg.smoothDuration = SmoothDuration;
            battleRoom?.BroadcastToPlayers(correctMsg);

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

        public static void StartMove(this BattleMoveComponent self, Vector3 targetPosition, long chaseTargetId = 0, float chaseAttackRange = 0f)
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
            self.ChaseTargetId = chaseTargetId;
            self.ChaseAttackRange = chaseAttackRange;
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
            self.ChaseTargetId = 0;
            self.ChaseAttackRange = 0f;
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

            // 追击模式下实时检测是否已进入射程
            if (self.ChaseTargetId != 0 && self.ChaseAttackRange > 0f)
            {
                BattleRoom battleRoom = owner.GetParent<BattleRoom>();
                if (battleRoom != null && battleRoom.Units.TryGetValue(self.ChaseTargetId, out EntityRef<BattleUnit> targetRef))
                {
                    BattleUnit chaseTarget = targetRef;
                    if (chaseTarget != null && !chaseTarget.IsDead)
                    {
                        float distToTarget = BattleDistanceHelper.GetDistance(owner.Position, chaseTarget.Position);
                        if (distToTarget <= self.ChaseAttackRange)
                        {
                            // 已进入射程，停止移动并立即触发决策攻击
                            self.StopMove();
                            BattleActionDecisionComponent decision = owner.GetComponent<BattleActionDecisionComponent>();
                            decision?.MakeDecision();
                            return;
                        }

                        // 更新移动目标为射程边缘位置（紧跟目标）
                        float dir = owner.Position.X <= chaseTarget.Position.X ? 1f : -1f;
                        float edgeX = chaseTarget.Position.X - dir * self.ChaseAttackRange;
                        self.TargetPosition = new Vector3(edgeX, owner.Position.Y, owner.Position.Z);
                    }
                    else
                    {
                        // 目标已死亡，停止追击
                        self.ChaseTargetId = 0;
                        self.ChaseAttackRange = 0f;
                    }
                }
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

            // 更新空间网格
            BattleRoom room = owner.GetParent<BattleRoom>();
            BattleSpatialGrid spatialGrid = room?.GetComponent<BattleSpatialGrid>();
            spatialGrid?.UpdatePosition(owner.Id, newPosition.X);
        }
    }
}
