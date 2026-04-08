using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 离线波次管理组件 - 挂载在 Battle 上
    /// 读取 WaveConfig 和 SpawnConfig 配置表，本地管理波次推进
    /// </summary>
    [ComponentOf(typeof(Battle))]
    public class OfflineWaveManagerComponent : Entity, IAwake<int>, IDestroy
    {
        public int StageConfigId { get; set; }

        public int TotalWaves { get; set; }

        public int CurrentWaveIndex { get; set; }

        public int[] WaveConfigIds { get; set; }

        public List<long> AliveMonsterIds { get; set; } = new();

        public WaveState State { get; set; }

        public long WaveStartTime { get; set; }
    }
}
