using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 离线波次组件。
    /// 挂在 Battle 上，负责本地波次推进与当前波敌人跟踪。
    /// </summary>
    [ComponentOf(typeof(Battle))]
    public class OfflineWaveComponent : Entity, IAwake<int, List<int>>, IDestroy
    {
        public int StageId { get; set; }

        public List<int> WaveConfigIds { get; set; }

        public int CurrentWaveIndex { get; set; }

        public OfflineWaveState State { get; set; }

        public List<long> CurrentWaveUnitIds { get; set; }

        public int WaveIntervalMs { get; set; }

        public bool AutoStartNextWave { get; set; }
    }

    public enum OfflineWaveState
    {
        None = 0,
        Preparing = 1,
        Fighting = 2,
        Completed = 3,
    }
}
