using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleRoom))]
    public static partial class BattleRoomSystem
    {
        [EntitySystem]
        private static void Awake(this BattleRoom self)
        {
            self.State = BattleState.Prepare;
            self.SceneType = SceneType.Battle;
        }
        
        [EntitySystem]
        private static void Update(this BattleRoom self)
        {
            if (self.State != BattleState.Fighting)
            {
                return;
            }
            
            WaveManagerComponent waveManager = self.GetComponent<WaveManagerComponent>();
            if (waveManager == null || waveManager.State != WaveState.Fighting)
            {
                return;
            }
            
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit == null || unit.IsDead)
                {
                    continue;
                }
                
                SimpleAIComponent ai = unit.GetComponent<SimpleAIComponent>();
                ai?.Update();
            }
        }
        
        [EntitySystem]
        private static void Destroy(this BattleRoom self)
        {
            self.PlayerIds.Clear();
            self.Units.Clear();
            self.State = BattleState.End;
        }
        
        public static void AddPlayer(this BattleRoom self, long playerId)
        {
            if (!self.PlayerIds.Contains(playerId))
            {
                self.PlayerIds.Add(playerId);
            }
        }
        
        public static void RemovePlayer(this BattleRoom self, long playerId)
        {
            self.PlayerIds.Remove(playerId);
        }
        
        public static BattleUnit GetUnit(this BattleRoom self, long unitId)
        {
            if (self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                return unitRef;
            }
            return null;
        }
        
        public static void RemoveUnit(this BattleRoom self, long unitId)
        {
            if (self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                BattleUnit unit = unitRef;
                unit?.Dispose();
                self.Units.Remove(unitId);
            }
        }
        
        public static List<BattleUnit> GetAllUnits(this BattleRoom self)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead)
                {
                    units.Add(unit);
                }
            }
            return units;
        }
        
        public static List<BattleUnit> GetUnitsByCamp(this BattleRoom self, UnitCamp camp)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead && unit.Camp == camp)
                {
                    units.Add(unit);
                }
            }
            return units;
        }
    }
}
