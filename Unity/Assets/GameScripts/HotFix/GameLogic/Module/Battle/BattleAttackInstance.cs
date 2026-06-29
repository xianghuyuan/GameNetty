namespace ET
{
    public enum BattleAttackInstanceState
    {
        None = 0,
        Created = 1,
        WaitingForHit = 2,
        Resolved = 3,
        Finished = 4,
    }

    /// <summary>
    /// 每次发射产生的攻击实例。Instant 也创建实例，只是生命周期很短。
    /// </summary>
    [ChildOf(typeof(Battle))]
    public partial class BattleAttackInstance : Entity, IAwake<long, long, BattleAttackRuntime>, IDestroy
    {
        public long CasterId { get; set; }
        public long TargetId { get; set; }
        public BattleAttackRuntime AttackRuntime { get; set; }
        public BattleAttackDeliveryType DeliveryType { get; set; }
        public BattleAttackInstanceState State { get; set; }
        public long StartTimeMs { get; set; }
        public long ResolveTimeMs { get; set; }
        public long FinishTimeMs { get; set; }
    }
}
