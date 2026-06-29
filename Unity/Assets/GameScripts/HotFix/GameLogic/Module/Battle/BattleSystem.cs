using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(Battle))]
    [FriendOf(typeof(Battle))]
    public static partial class BattleSystem
    {
        [EntitySystem]
        private static void Awake(this Battle self, long battleId, int battleType)
        {
            self.BattleId = battleId;
            self.BattleType = battleType;
            self.State = BattleState.Preparing;
            self.CurrentWave = 0;
            self.TotalWaves = 0;
            self.StartTime = 0;
            self.EndTime = 0;
        }
        
        /// <summary>
        /// 开始战斗
        /// </summary>
        public static void Start(this Battle self)
        {
            self.GetOrCreateFlowController().StartBattle();
        }
        
        /// <summary>
        /// 暂停战斗
        /// </summary>
        public static void Pause(this Battle self)
        {
            self.GetOrCreateFlowController().PauseBattle();
        }
        
        /// <summary>
        /// 恢复战斗
        /// </summary>
        public static void Resume(this Battle self)
        {
            self.GetOrCreateFlowController().ResumeBattle();
        }
        
        /// <summary>
        /// 结束战斗
        /// </summary>
        public static void End(this Battle self, bool success)
        {
            self.GetOrCreateFlowController().EndBattle(success);
        }
        
        /// <summary>
        /// 获取所有战斗单位
        /// </summary>
        public static List<BattleUnit> GetAllBattleUnits(this Battle self)
        {
            return self.Children.Values.OfType<BattleUnit>().ToList();
        }
        
        /// <summary>
        /// 根据阵营获取战斗单位
        /// </summary>
        public static List<BattleUnit> GetBattleUnitsByCamp(this Battle self, UnitCamp camp)
        {
            return self.Children.Values.OfType<BattleUnit>()
                .Where(unit => unit.Camp == camp)
                .ToList();
        }
        
        /// <summary>
        /// 根据 OwnerId 获取战斗单位
        /// </summary>
        public static BattleUnit GetBattleUnitByOwner(this Battle self, long ownerId)
        {
            return self.Children.Values.OfType<BattleUnit>()
                .FirstOrDefault(unit => unit.OwnerId == ownerId);
        }
        
        /// <summary>
        /// 获取存活的战斗单位
        /// </summary>
        public static List<BattleUnit> GetAliveBattleUnits(this Battle self, UnitCamp camp)
        {
            return self.Children.Values.OfType<BattleUnit>()
                .Where(unit => unit.Camp == camp && !unit.IsDead)
                .ToList();
        }
        
        /// <summary>
        /// 检查战斗是否结束（所有敌人死亡或所有友方死亡）
        /// </summary>
        public static bool CheckBattleEnd(this Battle self)
        {
            var aliveEnemies = self.GetAliveBattleUnits(UnitCamp.Enemy);
            var aliveFriends = self.GetAliveBattleUnits(UnitCamp.Friend);
            
            // 所有敌人死亡 = 胜利
            if (aliveEnemies.Count == 0)
            {
                return true;
            }
            
            // 所有友方死亡 = 失败
            if (aliveFriends.Count == 0)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Battle 对外暴露 Start/Pause/Resume/End API，
        /// 但真正的会话推进交给 BattleFlowComponent 这个流程控制器。
        /// 这样 Battle 仍是战斗实例，而流程职责被显式收拢到一个组件中。
        /// </summary>
        private static BattleFlowComponent GetOrCreateFlowController(this Battle self)
        {
            return self.GetComponent<BattleFlowComponent>() ?? self.AddComponent<BattleFlowComponent>();
        }
    }
}
