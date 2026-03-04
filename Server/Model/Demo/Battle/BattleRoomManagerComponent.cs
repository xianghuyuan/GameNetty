using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 战斗房间管理器组件
    /// 挂载在 Map Scene 上，管理所有战斗房间
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class BattleRoomManagerComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 玩家ID到战斗房间ID的映射
        /// Key: UnitId, Value: BattleRoomId
        /// </summary>
        public Dictionary<long, long> UnitIdToBattleRoomId { get; set; }
        
        /// <summary>
        /// 战斗房间ID到战斗房间的映射
        /// Key: BattleRoomId, Value: BattleRoom EntityRef
        /// 使用 EntityRef 避免循环引用
        /// </summary>
        public Dictionary<long, EntityRef<BattleRoom>> BattleRoomIdToBattleRoom { get; set; }
    }
}
