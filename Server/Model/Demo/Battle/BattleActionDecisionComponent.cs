namespace ET.Server
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleActionDecisionComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<BattleUnit> CurrentTarget { get; set; }
        public BattleActionState State { get; set; }
    }

    public enum BattleActionState
    {
        Idle = 0,
        Chasing = 1,
        Attacking = 2,
    }
}
