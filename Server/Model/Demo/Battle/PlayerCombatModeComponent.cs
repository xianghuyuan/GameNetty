namespace ET.Server
{
    /// <summary>
    /// 玩家战斗模式
    /// </summary>
    public enum BattleMode
    {
        Manual = 0,   // 手动模式
        Auto = 1,     // 自动模式
    }
    
    /// <summary>
    /// 玩家战斗模式组件 - 控制玩家是手动操作还是自动战斗
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class PlayerCombatModeComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前战斗模式
        /// </summary>
        public BattleMode Mode { get; set; } = BattleMode.Manual;
        
        /// <summary>
        /// 自动战斗时的AI组件引用
        /// </summary>
        public EntityRef<BattleAIComponent> AutoAI { get; set; }
        
        /// <summary>
        /// 是否启用自动战斗
        /// </summary>
        public bool IsAutoBattle => Mode == BattleMode.Auto;
    }
}
