using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 决策组件 - 周期性决策，负责选目标、选技能、发布移动/施法指令
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleActionDecisionComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前锁定的目标
        /// </summary>
        public EntityRef<BattleUnit> CurrentTarget { get; set; }

        /// <summary>
        /// 决策定时器ID
        /// </summary>
        public long DecisionTimerId { get; set; }

        /// <summary>
        /// 上次决策目标ID
        /// </summary>
        public long LastTargetId { get; set; }

        /// <summary>
        /// 上次是否在施法范围内
        /// </summary>
        public bool LastInSkillRange { get; set; }

        /// <summary>
        /// 上次目标位置（用于判断是否需要重新发布移动指令）
        /// </summary>
        public Vector3 LastTargetPosition { get; set; }
    }
}
