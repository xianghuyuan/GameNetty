using System.Numerics;

namespace ET.Server
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleMoveComponent : Entity, IAwake, IDestroy
    {
        public bool IsMoving { get; set; }
        public float MoveSpeed { get; set; } = 1.0f;
        public Vector3 TargetPosition { get; set; }
        public Vector3 LastMoveCommandTarget { get; set; }
        public bool IsMoveCommandActive { get; set; }
        public long LastUpdateTime { get; set; }
    }
}
