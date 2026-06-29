namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class C2M_BattleReadyHandler : MessageLocationHandler<Unit, C2M_BattleReady, M2C_BattleReady>
    {
        protected override async ETTask Run(Unit unit, C2M_BattleReady request, M2C_BattleReady response)
        {
            Scene mapScene = unit.Scene();
            
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                response.Error = ErrorCode.ERR_NotInBattle;
                response.Message = "未在战斗中";
                return;
            }
            
            BattleRoom battleRoom = roomManager.GetBattleRoomByUnitId(unit.Id);
            if (battleRoom == null)
            {
                response.Error = ErrorCode.ERR_RoomNotFound;
                response.Message = "战斗房间不存在";
                return;
            }

            Log.Info($"玩家 {unit.Id} 战斗准备就绪: BattleRoomId={request.battleId}");

            battleRoom.State = BattleState.Fighting;
            
            battleRoom.ForEachUnit(battleUnit =>
            {
                BattleActionDecisionComponent decisionComponent = battleUnit.GetComponent<BattleActionDecisionComponent>();
                if (decisionComponent != null)
                {
                    decisionComponent.MakeDecision();
                }
            });

            response.Error = ErrorCode.ERR_Success;
            
            await ETTask.CompletedTask;
        }
    }
}
