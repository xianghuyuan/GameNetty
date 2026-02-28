namespace ET
{
    /// <summary>
    /// 战斗实体
    /// 管理一场战斗的所有数据和状态
    /// </summary>
    [ChildOf(typeof(BattleComponent))]
    public partial class Battle : Entity, IAwake<long, int>
    {
        /// <summary>
        /// 战斗 ID
        /// </summary>
        public long BattleId { get; set; }
        
        /// <summary>
        /// 战斗类型
        /// </summary>
        public int BattleType { get; set; }
        
        /// <summary>
        /// 战斗状态
        /// </summary>
        public BattleState State { get; set; }
        
        /// <summary>
        /// 总波数（波次战斗用）
        /// </summary>
        public int TotalWaves { get; set; }
        
        /// <summary>
        /// 当前波次
        /// </summary>
        public int CurrentWave { get; set; }
        
        /// <summary>
        /// 开始时间（毫秒）
        /// </summary>
        public long StartTime { get; set; }
        
        /// <summary>
        /// 结束时间（毫秒）
        /// </summary>
        public long EndTime { get; set; }
    }
}
