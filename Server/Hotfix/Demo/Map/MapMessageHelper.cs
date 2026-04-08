

using System.Collections.Generic;
using System.IO;

namespace ET.Server
{
    public static partial class MapMessageHelper
    {
        public static void NoticeUnitAdd(Unit unit, Unit sendUnit)
        {
            M2C_CreateUnits createUnits = M2C_CreateUnits.Create();
            // 内联 UnitHelper.CreateUnitInfo，避免 MapMessageHelper → UnitHelper 的静态类引用
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = sendUnit.Id;
            unitInfo.ConfigId = sendUnit.ConfigId;
            unitInfo.Type = (int)sendUnit.Type();
            unitInfo.Position = sendUnit.Position;
            unitInfo.Forward = sendUnit.Forward;
            createUnits.Units.Add(unitInfo);
            MapMessageHelper.SendToClient(unit, createUnits);
        }

        public static void NoticeUnitRemove(Unit unit, Unit sendUnit)
        {
            M2C_RemoveUnits removeUnits = M2C_RemoveUnits.Create();
            removeUnits.Units.Add(sendUnit.Id);
            MapMessageHelper.SendToClient(unit, removeUnits);
        }

        public static void Broadcast(Unit unit, IMessage message)
        {
            (message as MessageObject).IsFromPool = false;
            // 内联 UnitHelper.GetBeSeePlayers，直接访问 AOIEntity 组件
            Dictionary<long, EntityRef<AOIEntity>> dict = unit.GetComponent<AOIEntity>().GetBeSeePlayers();
            // 网络底层做了优化，同一个消息不会多次序列化
            MessageLocationSenderOneType oneTypeMessageLocationType = unit.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession);
            foreach (AOIEntity u in dict.Values)
            {
                oneTypeMessageLocationType.Send(u.Unit.Id, message);
            }
        }

        public static void SendToClient(Unit unit, IMessage message)
        {
            unit.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Send(unit.Id, message);
        }

        /// <summary>
        /// 发送协议给Actor
        /// </summary>
        public static void Send(Scene root, ActorId actorId, IMessage message)
        {
            root.GetComponent<MessageSender>().Send(actorId, message);
        }
    }
}