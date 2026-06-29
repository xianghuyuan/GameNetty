using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 载具 Buff 管理组件 - 挂载在 BattleUnit 上
    /// 管理由载具施加的持续 Buff，按 (EffectType + VehicleId) 去重。
    /// 支持：叠加层数、刷新持续时间、按来源载具分别计算。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class VehicleBuffComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 活跃的载具 Buff 列表
        /// </summary>
        public List<VehicleBuffEntry> ActiveBuffs { get; } = new();

        /// <summary>
        /// 心跳定时器ID
        /// </summary>
        public long TickTimerId { get; set; }
    }

    /// <summary>
    /// 载具 Buff 条目 - 运行时数据
    /// </summary>
    public class VehicleBuffEntry
    {
        /// <summary>来源载具ID</summary>
        public long VehicleId;

        /// <summary>BuffConfig Id</summary>
        public int BuffId;

        /// <summary>效果类型（缓存）</summary>
        public int EffectType;

        /// <summary>持续时间（毫秒）</summary>
        public int DurationMs;

        /// <summary>到期时间（毫秒）</summary>
        public long ExpireTimeMs;

        /// <summary>层数</summary>
        public int StackCount;

        /// <summary>上次 tick 时间（用于 DOT 等周期效果）</summary>
        public long LastTickTimeMs;

    }
}
