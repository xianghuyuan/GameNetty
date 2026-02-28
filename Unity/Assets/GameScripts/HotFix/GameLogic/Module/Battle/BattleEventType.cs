namespace ET
{
    /// <summary>
    /// 战斗事件定义
    /// </summary>
    
    /// <summary>
    /// 战斗开始事件
    /// </summary>
    public struct BattleStart
    {
        public Battle Battle;
    }
    
    /// <summary>
    /// 战斗结束事件
    /// </summary>
    public struct BattleEnd
    {
        public Battle Battle;
        public BattleResult Result;
    }
    
    /// <summary>
    /// 波次开始事件
    /// </summary>
    public struct WaveStart
    {
        public Battle Battle;
        public int WaveNumber;
    }
    
    /// <summary>
    /// 波次完成事件
    /// </summary>
    public struct WaveComplete
    {
        public Battle Battle;
        public int WaveNumber;
    }
    
    /// <summary>
    /// 战斗单位死亡事件
    /// </summary>
    public struct BattleUnitDead
    {
        public BattleUnit BattleUnit;
    }
}
