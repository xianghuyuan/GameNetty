using System;

namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
    {
        protected override async ETTask Run(Unit unit, C2M_StartBattle request, M2C_StartBattle response)
        {
            Scene mapScene = unit.Scene();
            
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>() ?? mapScene.AddComponent<BattleRoomManagerComponent>();
            
            if (roomManager.IsUnitInBattle(unit.Id))
            {
                response.Error = ErrorCode.ERR_AlreadyInBattle;
                response.Message = "玩家已在战斗中";
                return;
            }
            
            int configId = request.stageId > 0 ? request.stageId : request.battleType;
            
            BattleRoom battleRoom = mapScene.AddChild<BattleRoom, int>(configId);
            battleRoom.AddPlayer(unit.Id);
            
            roomManager.AddBattleRoom(battleRoom);
            roomManager.AddUnitToBattleRoom(unit.Id, battleRoom.Id);
            
            battleRoom.InitBattle(unit, request.stageId, request.battleType);
            battleRoom.State = BattleState.Prepare;
            response.battleId = battleRoom.Id;
            Log.Info($"玩家 {unit.Id} 开始战斗: BattleRoomId={battleRoom.Id}");
            battleRoom.StartFirstWave().Coroutine();
            await ETTask.CompletedTask;
        }
    }
}
