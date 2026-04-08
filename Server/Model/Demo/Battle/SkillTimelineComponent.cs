using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 技能时间轴组件 - 挂载在 BattleRoom 上，管理所有待结算的技能判定框。
    /// 当服务端逻辑时间到达判定框的有效时间窗口时，自动执行碰撞检测和伤害结算。
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class SkillTimelineComponent : Entity, IAwake, IDestroy
    {
        /// <summary>待结算的判定框队列，按结算时间排序</summary>
        public List<HitBoxEntry> PendingEntries { get; } = new();

        /// <summary>批量结算累积器 - 每100ms打包下发一次</summary>
        public long LastBatchSendTime { get; set; }

        /// <summary>累积的伤害结算结果，等待批量下发</summary>
        public List<BatchDamageResult> AccumulatedResults { get; } = new();

        /// <summary>累积的Boss伤害结果</summary>
        public List<BossDamageResult> AccumulatedBossResults { get; } = new();

        /// <summary>批量下发间隔（毫秒）</summary>
        public const long BatchInterval = 100;
    }

    /// <summary>
    /// 判定框条目 - 技能命中判定的时间窗口和空间范围
    /// </summary>
    [EnableClass]
    public class HitBoxEntry
    {
        public long CasterId;
        public int SkillId;
        public long TargetId;
        public float StartTick;   // 判定开始时间（服务端逻辑时间）
        public float EndTick;     // 判定结束时间
        public float HitBoxMinX;  // 判定框X轴最小值
        public float HitBoxMaxX;  // 判定框X轴最大值
        public bool IsProcessed;
    }

    /// <summary>
    /// 批量伤害结算结果 - 用于100ms打包下发
    /// </summary>
    [EnableClass]
    public class BatchDamageResult
    {
        public long AttackerId;
        public int SkillId;
        public long TargetId;
        public int Damage;
        public int DamageType;
        public bool TargetDead;
        public int TargetCurrentHp;
        public int TargetMaxHp;
    }

    /// <summary>
    /// Boss伤害结算结果 - Boss伤害需要单独下发，不等批量
    /// </summary>
    [EnableClass]
    public class BossDamageResult
    {
        public long AttackerId;
        public int SkillId;
        public int Damage;
        public int DamageType;
        public int BossCurrentHp;
        public int BossMaxHp;
    }
}
