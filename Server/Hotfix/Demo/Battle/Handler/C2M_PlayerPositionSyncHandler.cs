using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 客户端同步玩家位置 - 客户端权威移动模式下，
    /// 服务端需要知道玩家实时位置以供Boss AI追击使用。
    /// </summary>
    [MessageLocationHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_PlayerPositionSyncHandler : MessageLocationHandler<Unit, C2M_PlayerPositionSync>
    {
        protected override async ETTask Run(Unit unit, C2M_PlayerPositionSync message)
        {
            Scene mapScene = unit.Scene();

            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                return;
            }

            BattleRoom battleRoom = roomManager.GetBattleRoomByUnitId(unit.Id);
            if (battleRoom == null)
            {
                return;
            }

            BattleUnit battleUnit = battleRoom.GetUnit(unit.Id);
            if (battleUnit == null || battleUnit.IsDead)
            {
                return;
            }

            battleUnit.Position = new Vector3(message.position.x,message.position.y,message.position.z);

            await ETTask.CompletedTask;
        }
    }
}
