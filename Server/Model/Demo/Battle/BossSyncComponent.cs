using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Boss同步组件 - 挂载在 BattleRoom 上，负责Boss的高频位置/状态同步（20Hz）。
    /// 与杂兵系统不同，Boss必须由服务端绝对权威控制。
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class BossSyncComponent : Entity, IAwake, IDestroy
    {
        /// <summary>Boss单位ID列表</summary>
        public List<long> BossUnitIds { get; } = new();

        /// <summary>同步定时器ID</summary>
        public long SyncTimerId { get; set; }

        /// <summary>上次同步时间</summary>
        public long LastSyncTime { get; set; }

        /// <summary>Boss同步频率（毫秒），50ms = 20Hz</summary>
        public const long SyncInterval = 50;
    }
}
