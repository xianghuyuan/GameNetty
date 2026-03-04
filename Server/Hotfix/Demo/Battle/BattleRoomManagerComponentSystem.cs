using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleRoomManagerComponent))]
    [FriendOf(typeof(BattleRoomManagerComponent))]
    public static partial class BattleRoomManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleRoomManagerComponent self)
        {
            self.UnitIdToBattleRoomId = new Dictionary<long, long>();
            self.BattleRoomIdToBattleRoom = new Dictionary<long, EntityRef<BattleRoom>>();
        }
        
        [EntitySystem]
        private static void Destroy(this BattleRoomManagerComponent self)
        {
            self.UnitIdToBattleRoomId.Clear();
            self.BattleRoomIdToBattleRoom.Clear();
        }
        
        /// <summary>
        /// 添加战斗房间
        /// </summary>
        public static void AddBattleRoom(this BattleRoomManagerComponent self, BattleRoom battleRoom)
        {
            self.BattleRoomIdToBattleRoom[battleRoom.Id] = battleRoom;
            Log.Debug($"添加战斗房间: BattleRoomId={battleRoom.Id}");
        }
        
        /// <summary>
        /// 移除战斗房间
        /// </summary>
        public static void RemoveBattleRoom(this BattleRoomManagerComponent self, long battleRoomId)
        {
            self.BattleRoomIdToBattleRoom.Remove(battleRoomId);
            Log.Debug($"移除战斗房间: BattleRoomId={battleRoomId}");
        }
        
        /// <summary>
        /// 添加玩家到战斗房间映射
        /// </summary>
        public static void AddUnitToBattleRoom(this BattleRoomManagerComponent self, long unitId, long battleRoomId)
        {
            self.UnitIdToBattleRoomId[unitId] = battleRoomId;
            Log.Debug($"添加玩家到战斗房间映射: UnitId={unitId}, BattleRoomId={battleRoomId}");
        }
        
        /// <summary>
        /// 从战斗房间映射移除玩家
        /// </summary>
        public static void RemoveUnitFromBattleRoom(this BattleRoomManagerComponent self, long unitId)
        {
            self.UnitIdToBattleRoomId.Remove(unitId);
            Log.Debug($"从战斗房间映射移除玩家: UnitId={unitId}");
        }
        
        /// <summary>
        /// 检查玩家是否在战斗中
        /// </summary>
        public static bool IsUnitInBattle(this BattleRoomManagerComponent self, long unitId)
        {
            return self.UnitIdToBattleRoomId.ContainsKey(unitId);
        }
        
        /// <summary>
        /// 根据玩家ID获取战斗房间
        /// </summary>
        public static BattleRoom GetBattleRoomByUnitId(this BattleRoomManagerComponent self, long unitId)
        {
            if (!self.UnitIdToBattleRoomId.TryGetValue(unitId, out long battleRoomId))
            {
                return null;
            }
            
            return self.GetBattleRoomById(battleRoomId);
        }
        
        /// <summary>
        /// 根据房间ID获取战斗房间
        /// </summary>
        public static BattleRoom GetBattleRoomById(this BattleRoomManagerComponent self, long battleRoomId)
        {
            if (!self.BattleRoomIdToBattleRoom.TryGetValue(battleRoomId, out EntityRef<BattleRoom> battleRoomRef))
            {
                return null;
            }
            
            // EntityRef 隐式转换为 BattleRoom
            BattleRoom battleRoom = battleRoomRef;
            return battleRoom;
        }
        
        /// <summary>
        /// 获取所有活跃的战斗房间
        /// </summary>
        public static List<BattleRoom> GetActiveBattleRooms(this BattleRoomManagerComponent self)
        {
            List<BattleRoom> activeBattleRooms = new List<BattleRoom>();
            
            foreach (EntityRef<BattleRoom> battleRoomRef in self.BattleRoomIdToBattleRoom.Values)
            {
                BattleRoom battleRoom = battleRoomRef;
                if (battleRoom != null && battleRoom.State == BattleState.Fighting)
                {
                    activeBattleRooms.Add(battleRoom);
                }
            }
            
            return activeBattleRooms;
        }
    }
}
