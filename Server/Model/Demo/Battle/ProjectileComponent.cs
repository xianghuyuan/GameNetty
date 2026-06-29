using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 投射物组件 - 挂载在投射物 BattleUnit 上，管理投射物的飞行和碰撞
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class ProjectileComponent : Entity, IAwake<int, long, long>, IDestroy
    {
        /// <summary>
        /// 投射物所属施法者ID
        /// </summary>
        public long CasterId { get; set; }

        /// <summary>
        /// 投射物使用的技能ID
        /// </summary>
        public int SkillId { get; set; }

        /// <summary>
        /// 投射物所属阵营（继承施法者阵营）
        /// </summary>
        public UnitCamp Camp { get; set; }

        /// <summary>
        /// 飞行速度（单位/秒）
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// 最大飞行距离（超过后销毁）
        /// </summary>
        public float MaxDistance { get; set; }

        /// <summary>
        /// 碰撞半径（投射物自身的判定范围）
        /// </summary>
        public float CollisionRadius { get; set; }

        /// <summary>
        /// 是否穿透（命中后不销毁，继续飞行）
        /// </summary>
        public bool IsPiercing { get; set; }

        /// <summary>
        /// 最大命中单位数（0=无限，穿透时有用）
        /// </summary>
        public int MaxHitCount { get; set; }

        /// <summary>
        /// 已命中的单位ID集合（避免重复命中）
        /// </summary>
        public HashSet<long> HitSet { get; } = new();

        /// <summary>
        /// 飞行起点位置（用于计算已飞行距离）
        /// </summary>
        public Vector3 StartPosition { get; set; }

        /// <summary>
        /// 上一帧位置（用于线段碰撞检测）
        /// </summary>
        public Vector3 PreviousPosition { get; set; }

        /// <summary>
        /// 飞行方向（1=向右，-1=向左）
        /// </summary>
        public float Direction { get; set; }

        /// <summary>
        /// 上次更新时间
        /// </summary>
        public long LastUpdateTime { get; set; }

        /// <summary>
        /// 定时器ID
        /// </summary>
        public long TimerId { get; set; }

    }
}
