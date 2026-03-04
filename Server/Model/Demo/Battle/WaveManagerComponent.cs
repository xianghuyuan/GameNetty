using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 波次管理组件
    /// 挂载在 BattleRoom 上，管理波次战斗的流程
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class WaveManagerComponent : Entity, IAwake<int>, IDestroy
    {
        /// <summary>
        /// 总波数
        /// </summary>
        public int TotalWaves { get; set; }
        
        /// <summary>
        /// 当前波次（从1开始）
        /// </summary>
        public int CurrentWave { get; set; }
        
        /// <summary>
        /// 当前波次状态
        /// </summary>
        public WaveState State { get; set; }
        
        /// <summary>
        /// 当前波次开始时间
        /// </summary>
        public long WaveStartTime { get; set; }
        
        /// <summary>
        /// 当前波次的怪物ID列表
        /// </summary>
        public List<long> CurrentWaveMonsterIds { get; set; }
        
        /// <summary>
        /// 波次间隔时间（毫秒）
        /// </summary>
        public int WaveInterval { get; set; } = 5000; // 默认5秒
        
        /// <summary>
        /// 是否自动开始下一波
        /// </summary>
        public bool AutoStartNextWave { get; set; } = true;
    }
    
    /// <summary>
    /// 波次状态
    /// </summary>
    public enum WaveState
    {
        None = 0,
        Preparing = 1,   // 准备中（波次间隔）
        Fighting = 2,    // 战斗中
        Completed = 3,   // 已完成
    }
}
