namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_SwitchControlModeHandler : MessageHandler<Scene, C2M_SwitchControlMode, M2C_SwitchControlMode>
    {
        protected override async ETTask Run(Scene scene, C2M_SwitchControlMode request, M2C_SwitchControlMode response)
        {
            BattleRoomManagerComponent roomManager = scene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }
            
            // 找到玩家的战斗单位
            BattleUnit playerUnit = null;
            BattleRoom battleRoom = null;
            
            foreach (var roomRef in roomManager.BattleRoomIdToBattleRoom.Values)
            {
                BattleRoom room = roomRef;
                if (room == null) continue;
                
                foreach (var kv in room.Units)
                {
                    BattleUnit unit = kv.Value;
                    if (unit != null && unit.Camp == UnitCamp.Friend)
                    {
                        playerUnit = unit;
                        battleRoom = room;
                        break;
                    }
                }
                if (playerUnit != null) break;
            }
            
            if (playerUnit == null)
            {
                response.Error = ErrorCode.ERR_BattleUnitNotFound;
                response.Message = "Player unit not found";
                return;
            }
            
            // 获取或添加战斗模式组件
            PlayerCombatModeComponent modeComponent = playerUnit.GetComponent<PlayerCombatModeComponent>();
            if (modeComponent == null)
            {
                modeComponent = playerUnit.AddComponent<PlayerCombatModeComponent>();
            }
            
            // 设置模式
            BattleMode newMode = (BattleMode)request.Mode;
            modeComponent.SetMode(newMode);
            
            response.CurrentMode = (int)modeComponent.Mode;
            
            // 广播模式变化给所有玩家
            M2C_ControlModeChanged broadcastMsg = M2C_ControlModeChanged.Create();
            broadcastMsg.UnitId = playerUnit.Id;
            broadcastMsg.NewMode = (int)modeComponent.Mode;
            battleRoom.BroadcastToPlayers(broadcastMsg);
            
            await ETTask.CompletedTask;
        }
    }
}
