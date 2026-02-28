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
            self.State = BattleState.Fighting;
            self.StartTime = TimeInfo.Instance.ClientNow();
            
            Log.Info($"战斗开始: BattleId={self.BattleId}, Type={self.BattleType}");
            
            // 触发战斗开始事件
            EventSystem.Instance.Publish(self.Scene(), new BattleStart { Battle = self });
        }
        
        /// <summary>
        /// 暂停战斗
        /// </summary>
        public static void Pause(this Battle self)
        {
            if (self.State != BattleState.Fighting)
            {
                return;
            }
            
            self.State = BattleState.Paused;
            Log.Info($"战斗暂停: BattleId={self.BattleId}");
        }
        
        /// <summary>
        /// 恢复战斗
        /// </summary>
        public static void Resume(this Battle self)
        {
            if (self.State != BattleState.Paused)
            {
                return;
            }
            
            self.State = BattleState.Fighting;
            Log.Info($"战斗恢复: BattleId={self.BattleId}");
        }
        
        /// <summary>
        /// 结束战斗
        /// </summary>
        public static void End(this Battle self, bool success)
        {
            self.State = BattleState.Ended;
            self.EndTime = TimeInfo.Instance.ClientNow();
            
            int duration = (int)((self.EndTime - self.StartTime) / 1000); // 转换为秒
            
            Log.Info($"战斗结束: BattleId={self.BattleId}, Success={success}, Duration={duration}s");
            
            // 创建战斗结果
            BattleResult result = new BattleResult
            {
                Success = success,
                Duration = duration,
                Exp = success ? 100 : 0, // 临时值
                Drops = new List<ItemDrop>(),
                PlayerDamage = new Dictionary<long, int>()
            };
            
            // 触发战斗结束事件
            EventSystem.Instance.Publish(self.Scene(), new BattleEnd { Battle = self, Result = result });
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
    }
}
