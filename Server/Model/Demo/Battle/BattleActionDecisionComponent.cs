namespace ET.Server
{
    /// <summary>
    /// 决策组件 - 事件驱动，负责选目标、选技能、发布移动/施法指令
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleActionDecisionComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前锁定的目标
        /// </summary>
        public EntityRef<BattleUnit> CurrentTarget { get; set; }
    }
}
