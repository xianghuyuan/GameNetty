namespace ET
{
    /// <summary>
    /// 单位阵营
    /// </summary>
    public enum UnitCamp
    {
        /// <summary>
        /// 友方（玩家）
        /// </summary>
        Friend = 1,
        
        /// <summary>
        /// 敌方（怪物）
        /// </summary>
        Enemy = 2
    }
    
    /// <summary>
    /// 战斗类型
    /// </summary>
    public enum BattleType
    {
        /// <summary>
        /// 波次战斗
        /// </summary>
        WaveBattle = 0,
        
        /// <summary>
        /// 副本
        /// </summary>
        Dungeon = 1,
        
        /// <summary>
        /// Boss 战
        /// </summary>
        Boss = 2
    }
    
    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum BattleState
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 准备中
        /// </summary>
        Preparing = 1,
        
        /// <summary>
        /// 战斗中
        /// </summary>
        Fighting = 2,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused = 3,
        
        /// <summary>
        /// 已结束
        /// </summary>
        Ended = 4
    }
    
    /// <summary>
    /// 波次状态
    /// </summary>
    public enum WaveState
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 开始中
        /// </summary>
        Starting = 1,
        
        /// <summary>
        /// 战斗中
        /// </summary>
        Fighting = 2,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3
    }
}
