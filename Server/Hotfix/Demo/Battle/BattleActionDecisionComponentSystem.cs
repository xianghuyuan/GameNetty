using System;
using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleActionDecisionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleActionDecisionComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BattleActionDecisionComponent self)
        {
            self.CurrentTarget = null;
        }

        /// <summary>
        /// 执行决策：选目标、选技能、发指令
        /// </summary>
        public static void MakeDecision(this BattleActionDecisionComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                return;
            }

            // 懒检查当前目标有效性
            BattleUnit currentTarget = self.CurrentTarget;
            if (currentTarget != null && currentTarget.IsDead)
            {
                self.CurrentTarget = null;
                currentTarget = null;
            }

            //没有选到目标
            if (!BattleSkillHelper.TrySelectBestAutoSkillPlan(owner, currentTarget, out BattleSkillHelper.AutoCastPlan autoCastPlan))
            {
                if (currentTarget != null)
                {
                    EventSystem.Instance.Publish<BattleRoom, RequestStopMoveEvent>(self.Scene<BattleRoom>()!, new RequestStopMoveEvent { Unit = owner });
                    self.CurrentTarget = null;
                }
                return;
            }

            BattleUnit newTarget = autoCastPlan.Target;

            // 目标变化
            if (currentTarget == null || currentTarget.Id != newTarget.Id)
            {
                long oldTargetId = currentTarget?.Id ?? 0;
                self.CurrentTarget = newTarget;
                EventSystem.Instance.Publish<BattleRoom, TargetChangedEvent>(self.Scene<BattleRoom>()!, new TargetChangedEvent
                {
                    UnitId = owner.Id,
                    OldTargetId = oldTargetId,
                    NewTargetId = newTarget.Id,
                });
            }

            // 判断是否在施法范围内
            if (BattleSkillHelper.IsInSkillRange(owner, newTarget, autoCastPlan.TargetingConfig))
            {
                // 在范围内，停止移动，施法
                EventSystem.Instance.Publish<BattleRoom, RequestStopMoveEvent>(self.Scene<BattleRoom>()!, new RequestStopMoveEvent { Unit = owner });
                EventSystem.Instance.Publish<BattleRoom, RequestCastEvent>(self.Scene<BattleRoom>()!, new RequestCastEvent
                {
                    Unit = owner,
                    SkillId = autoCastPlan.SkillId,
                    TargetId = newTarget.Id,
                });
            }
            else
            {
                // 不在范围，计算期望施法位置，移动过去
                Vector3 desiredPosition = BattleSkillHelper.ComputeDesiredCastPosition(owner, newTarget, autoCastPlan.TargetingConfig);
                EventSystem.Instance.Publish<BattleRoom, RequestMoveEvent>(self.Scene<BattleRoom>()!, new RequestMoveEvent
                {
                    Unit = owner,
                    TargetPosition = desiredPosition,
                });
            }
        }

        /// <summary>
        /// 重置决策状态
        /// </summary>
        public static void Reset(this BattleActionDecisionComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            BattleUnit currentTarget = self.CurrentTarget;
            if (owner != null && currentTarget != null)
            {
                EventSystem.Instance.Publish<BattleRoom, RequestStopMoveEvent>(self.Scene<BattleRoom>()!, new RequestStopMoveEvent { Unit = owner });
            }
            self.CurrentTarget = null;
        }
    }
}
