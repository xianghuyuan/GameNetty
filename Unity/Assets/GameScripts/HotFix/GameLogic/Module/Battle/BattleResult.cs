using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 战斗结算结果
    /// </summary>
    public class BattleResult
    {
        /// <summary>
        /// 是否胜利
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 持续时间（秒）
        /// </summary>
        public int Duration { get; set; }
        
        /// <summary>
        /// 获得经验
        /// </summary>
        public int Exp { get; set; }
        
        /// <summary>
        /// 掉落物品
        /// </summary>
        public List<ItemDrop> Drops { get; set; }
        
        /// <summary>
        /// 玩家伤害统计
        /// </summary>
        public Dictionary<long, int> PlayerDamage { get; set; }
    }
    
    /// <summary>
    /// 物品掉落
    /// </summary>
    public struct ItemDrop
    {
        /// <summary>
        /// 物品配置 ID
        /// </summary>
        public int ConfigId { get; set; }
        
        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }
    }
}
