using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleMoveComponent : Entity, IAwake, IDestroy
    {
        public bool IsMoving { get; set; }
        public float MoveSpeed { get; set; }
        public float3 TargetPosition { get; set; }
        public long FollowTargetUnitId { get; set; }
        public int CommandVersion { get; set; }

        /// <summary>
        /// 移动开始时间（客户端 Time.time）
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// 移动所需时间（秒），由服务端下发
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// 移动系数（减速时 >1，加速时 <1），由服务端下发
        /// </summary>
        public float MoveCoefficient { get; set; } = 1f;
    }
}
