using System;
using System.Diagnostics;
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
        private const long DecisionTickInterval = 100; // 100ms 决策心跳
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

            // 施法中或冻结中，跳过决策
            FreezeComponent freeze = owner.GetComponent<FreezeComponent>();
            if (freeze != null && freeze.IsFrozen)
            {
                Log.Debug($"[{LogDebugHelper.GetUnitName(owner)}] 决策跳过: 冻结/施法中");
                return;
            }

            CastingComponent casting = owner.GetComponent<CastingComponent>();
            if (casting != null && casting.IsCasting)
            {
                Log.Debug($"[{LogDebugHelper.GetUnitName(owner)}] 决策跳过: 施法锁定中");
                return;
            }

            BattleUnit currentTarget = self.CurrentTarget;
            if (currentTarget != null && currentTarget.IsDead)
            {
                self.CurrentTarget = null;
            }

            if (!BattleSkillHelper.TrySelectBestAutoSkillPlan(owner, self.CurrentTarget, out BattleSkillHelper.AutoCastPlan plan))
            {
                Log.Debug($"[{LogDebugHelper.GetUnitName(owner)}] 无可用技能，停止移动");
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
            Log.Debug($"[{LogDebugHelper.GetUnitName(owner)}] 选技: SkillId={plan.SkillId}, Target={target?.Id}, InRange={inRange}");

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
                if (!plan.EmitterConfig.CanMoveCast)
                {
                    self.PublishStopMove(owner);
                }

                Log.Debug($"[{LogDebugHelper.GetUnitName(owner)}] 范程内，释放: SkillId={plan.SkillId}");
                self.PublishCast(owner, plan.SkillId, target.Id);
            }
            else
            {
                float requiredMove = plan.RequiredMoveDistance;
                LogDebugHelper.Log($"[{LogDebugHelper.GetUnitName(owner)}] 超出射程，移动: RequiredMove={requiredMove:F2}");
                float chaseRange = plan.TargetingConfig.CastRange + plan.TargetingConfig.EdgeDistance;
                self.PublishMove(owner, target.Position, target.Id, chaseRange);
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

        private static void PublishMove(this BattleActionDecisionComponent self, BattleUnit owner, Vector3 position, long chaseTargetId = 0, float chaseAttackRange = 0f)
        {
            EventSystem.Instance.Publish<BattleRoom, RequestMoveEvent>(self.Scene<BattleRoom>()!, new RequestMoveEvent
            {
                Unit = owner,
                TargetPosition = position,
                ChaseTargetId = chaseTargetId,
                ChaseAttackRange = chaseAttackRange,
            });
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
