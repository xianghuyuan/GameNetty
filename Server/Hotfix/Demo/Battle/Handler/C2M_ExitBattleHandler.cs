namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class C2M_ExitBattleHandler : MessageLocationHandler<Unit, C2M_ExitBattle, M2C_ExitBattle>
    {
        protected override async ETTask Run(Unit unit, C2M_ExitBattle request, M2C_ExitBattle response)
        {
            Scene mapScene = unit.Scene();
            
            // 1. 获取房间管理器
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                response.Error = ErrorCode.ERR_NotInBattle;
                response.Message = "未在战斗中";
                return;
            }
            
            // 2. 检查玩家是否在战斗中
            if (!roomManager.IsUnitInBattle(unit.Id))
            {
                response.Error = ErrorCode.ERR_NotInBattle;
                response.Message = "未在战斗中";
                return;
            }
            
            // 3. 获取战斗房间
            BattleRoom battleRoom = roomManager.GetBattleRoomByUnitId(unit.Id);
            if (battleRoom == null || battleRoom.Id != request.battleId)
            {
                response.Error = ErrorCode.ERR_RoomNotFound;
                response.Message = "战斗房间不存在";
                return;
            }
            
            // 4. 从房间移除玩家
            battleRoom.PlayerIds.Remove(unit.Id);
            roomManager.RemoveUnitFromBattleRoom(unit.Id);
            
            Log.Info($"玩家 {unit.Id} 退出战斗: BattleRoomId={request.battleId}");
            
            // 5. 检查房间是否为空
            if (battleRoom.PlayerIds.Count == 0)
            {
                // 房间为空，销毁房间
                roomManager.RemoveBattleRoom(battleRoom.Id);
                battleRoom.Dispose();
                Log.Info($"战斗房间已销毁（无玩家）: BattleRoomId={request.battleId}");
            }
            else
            {
                // 通知其他玩家有人退出
                // TODO: 广播玩家退出消息
                Log.Info($"玩家退出，房间剩余玩家数: {battleRoom.PlayerIds.Count}");
            }
            
            // 6. 响应客户端
            response.Error = ErrorCode.ERR_Success;
            response.Message = "退出战斗成功";
            
            await ETTask.CompletedTask;
        }
    }
}
