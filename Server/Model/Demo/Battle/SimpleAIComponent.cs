using System.Numerics;

namespace ET.Server
{
    [ComponentOf(typeof(BattleUnit))]
    public class SimpleAIComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<BattleUnit> CurrentTarget { get; set; }
        public AIState State { get; set; }
        public float DetectRange { get; set; } = 10.0f;
        public float MoveSpeed { get; set; } = 3.0f;
        public long LastUpdateTime { get; set; }
    }
    
    public enum AIState
    {
        Idle = 0,
        Chasing = 1,
        Attacking = 2,
    }
}
