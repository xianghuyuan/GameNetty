using System;
using System.Numerics;

namespace ET.Server
{
    [Invoke(TimerInvokeType.BattleDecisionTick)]
    public class BattleDecisionTimer : ATimer<BattleActionDecisionComponent>
    {
        protected override void Run(BattleActionDecisionComponent self)
        {
            BattleActionDecisionComponentSystem.OnDecisionTick(self);
        }
    }

    [EntitySystemOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleActionDecisionComponentSystem
    {
        private const long DecisionTickInterval = 200; // 200ms 决策心跳
        private const float TargetPositionThreshold = 0.1f; // 目标位置变化阈值

        [EntitySystem]
        private static void Awake(this BattleActionDecisionComponent self)
        {
            self.DecisionTimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(DecisionTickInterval, TimerInvokeType.BattleDecisionTick, self);
        }

        [EntitySystem]
        private static void Destroy(this BattleActionDecisionComponent self)
        {
            long timerId = self.DecisionTimerId;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref timerId);
            self.DecisionTimerId = 0;
            self.CurrentTarget = null;
        }

        internal static void OnDecisionTick(BattleActionDecisionComponent self)
        {
            self.MakeDecision();
        }

        public static void MakeDecision(this BattleActionDecisionComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                return;
            }

            BattleUnit currentTarget = self.CurrentTarget;
            if (currentTarget != null && currentTarget.IsDead)
            {
                self.CurrentTarget = null;
            }

            if (!BattleSkillHelper.TrySelectBestAutoSkillPlan(owner, self.CurrentTarget, out BattleSkillHelper.AutoCastPlan plan))
            {
                if (self.LastTargetId != 0)
                {
                    self.PublishStopMove(owner);
                    self.LastTargetId = 0;
                    self.LastInSkillRange = false;
                }
                self.CurrentTarget = null;
                return;
            }

            BattleUnit target = plan.Target;
            bool inRange = BattleSkillHelper.IsInSkillRange(owner, target, plan.TargetingConfig);

            // 状态没变化：同一目标
            if (self.LastTargetId == target.Id )
            {
                return;
            }

            // 状态变化，更新记录
            bool targetChanged = self.LastTargetId != target.Id;
            self.LastTargetId = target.Id;
            self.LastInSkillRange = inRange;
            self.LastTargetPosition = target.Position;
            self.CurrentTarget = target;

            if (targetChanged)
            {
                long oldTargetId = self.LastTargetId;
                EventSystem.Instance.Publish<BattleRoom, TargetChangedEvent>(self.Scene<BattleRoom>()!, new TargetChangedEvent
                {
                    UnitId = owner.Id,
                    OldTargetId = oldTargetId,
                    NewTargetId = target.Id,
                });
            }

            if (inRange)
            {
                self.PublishStopMove(owner);
                self.PublishCast(owner, plan.SkillId, target.Id);
            }
            else
            {
                Vector3 interceptPosition = ComputeInterceptPosition(owner, target, plan.TargetingConfig);
                self.PublishMove(owner, interceptPosition);
            }
        }

        private static void PublishStopMove(this BattleActionDecisionComponent self, BattleUnit owner)
        {
            EventSystem.Instance.Publish<BattleRoom, RequestStopMoveEvent>(self.Scene<BattleRoom>()!, new RequestStopMoveEvent { Unit = owner });
        }

        private static void PublishCast(this BattleActionDecisionComponent self, BattleUnit owner, int skillId, long targetId)
        {
            EventSystem.Instance.Publish<BattleRoom, RequestCastEvent>(self.Scene<BattleRoom>()!, new RequestCastEvent
            {
                Unit = owner,
                SkillId = skillId,
                TargetId = targetId,
            });
        }

        private static void PublishMove(this BattleActionDecisionComponent self, BattleUnit owner, Vector3 position)
        {
            EventSystem.Instance.Publish<BattleRoom, RequestMoveEvent>(self.Scene<BattleRoom>()!, new RequestMoveEvent
            {
                Unit = owner,
                TargetPosition = position,
            });
        }

        /// <summary>
        /// var a1targetX = (Mathf.Abs(a1.positionX - b1.positionX) - a1.attackRange) / (a1.speed + b1.speed) * a1.speed;
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="target"></param>
        /// <param name="targetingConfig"></param>
        /// <returns></returns>
        private static Vector3 ComputeInterceptPosition(BattleUnit owner, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            NumericComponent ownerNumeric = owner.GetComponent<NumericComponent>();
            NumericComponent targetNumeric = target.GetComponent<NumericComponent>();

            float distance = MathF.Abs(owner.Position.X - target.Position.X);
            float attackRange = targetingConfig?.CastRange ?? 1f;
            float ownerSpeed = ownerNumeric?.GetAsFloat(NumericType.Speed) ?? 1f;
            float targetSpeed = targetNumeric?.GetAsFloat(NumericType.Speed) ?? 0f;
            
            float effectiveDistance = distance - attackRange;
            float relativeSpeed = ownerSpeed + targetSpeed;
            float interceptX = effectiveDistance / relativeSpeed * ownerSpeed;
            return new Vector3(interceptX, owner.Position.Y, owner.Position.Z);
        }

        public static void Reset(this BattleActionDecisionComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            BattleUnit currentTarget = self.CurrentTarget;
            if (owner != null && currentTarget != null)
            {
                self.PublishStopMove(owner);
            }
            self.CurrentTarget = null;
            self.LastTargetId = 0;
            self.LastInSkillRange = false;
        }
    }
}
