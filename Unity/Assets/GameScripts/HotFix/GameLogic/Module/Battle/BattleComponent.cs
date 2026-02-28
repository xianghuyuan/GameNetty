using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 战斗管理组件
    /// 挂在 Scene 上，管理所有战斗实例
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class BattleComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前战斗（客户端通常只有一个战斗）
        /// </summary>
        public Battle CurrentBattle { get; set; }
    }
    
    [EntitySystemOf(typeof(BattleComponent))]
    [FriendOf(typeof(BattleComponent))]
    public static partial class BattleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleComponent self)
        {
            self.CurrentBattle = null;
        }
        
        [EntitySystem]
        private static void Destroy(this BattleComponent self)
        {
            // 清理当前战斗
            if (self.CurrentBattle != null)
            {
                self.CurrentBattle.Dispose();
                self.CurrentBattle = null;
            }
        }
        
        /// <summary>
        /// 创建战斗
        /// </summary>
        public static Battle CreateBattle(this BattleComponent self, long battleId, int battleType)
        {
            // 如果已有战斗，先清理
            if (self.CurrentBattle != null)
            {
                Log.Warning($"已存在战斗，先清理旧战斗: {self.CurrentBattle.BattleId}");
                self.CurrentBattle.Dispose();
            }
            
            // 创建新战斗
            Battle battle = self.AddChildWithId<Battle, long, int>(battleId, battleId, battleType);
            self.CurrentBattle = battle;
            
            Log.Info($"创建战斗: BattleId={battleId}, Type={battleType}");
            
            return battle;
        }
        
        /// <summary>
        /// 获取战斗
        /// </summary>
        public static Battle GetBattle(this BattleComponent self, long battleId)
        {
            if (self.CurrentBattle != null && self.CurrentBattle.BattleId == battleId)
            {
                return self.CurrentBattle;
            }
            
            return null;
        }
        
        /// <summary>
        /// 移除战斗
        /// </summary>
        public static void RemoveBattle(this BattleComponent self, long battleId)
        {
            if (self.CurrentBattle != null && self.CurrentBattle.BattleId == battleId)
            {
                self.CurrentBattle.Dispose();
                self.CurrentBattle = null;
                
                Log.Info($"移除战斗: BattleId={battleId}");
            }
        }
        
        /// <summary>
        /// 获取当前战斗
        /// </summary>
        public static Battle GetCurrentBattle(this BattleComponent self)
        {
            return self.CurrentBattle;
        }
    }
}
