namespace ET
{
    /// <summary>
    /// 客户端杂兵AI组件
    /// 挂载到敌方杂兵 BattleUnit 上，驱动本地AI行为
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class ClientMinionAIComponent : Entity, IAwake, IDestroy
    {
        public long TargetUnitId { get; set; }
        public long LastAttackTime { get; set; }
    }
}
