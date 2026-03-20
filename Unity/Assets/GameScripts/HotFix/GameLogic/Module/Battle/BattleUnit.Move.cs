using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleMoveComponent : Entity, IAwake, IDestroy
    {
        public bool IsMoving { get; set; }
        public float MoveSpeed { get; set; }
        public float3 TargetPosition { get; set; }
        public int CommandVersion { get; set; }
    }
}
