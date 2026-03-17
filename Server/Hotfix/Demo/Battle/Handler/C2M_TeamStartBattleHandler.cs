// namespace ET.Server
// {
//     [MessageLocationHandler(SceneType.Map)]
//     public class C2M_TeamStartBattleHandler : MessageLocationHandler<Unit, C2M_TeamStartBattle, M2C_TeamStartBattle>
//     {
//         protected override async ETTask Run(Unit leader, C2M_TeamStartBattle request, M2C_TeamStartBattle response)
//         {
//             Scene mapScene = leader.Scene();
//             
//             BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>() ?? mapScene.AddComponent<BattleRoomManagerComponent>();
//             
//             // TODO: 从队伍系统获取队伍成员
//             List<long> memberIds = new List<long> { leader.Id };
//             
//             foreach (long memberId in memberIds)
//             {
//                 if (roomManager.IsUnitInBattle(memberId))
//                 {
//                     response.Error = ErrorCode.ERR_TeamMemberInBattle;
//                     response.Message = $"队员 {memberId} 已在战斗中";
//                     return;
//                 }
//             }
//             
//             BattleRoom battleRoom = mapScene.AddChild<BattleRoom, int>(request.battleType);
//             
//             foreach (long memberId in memberIds)
//             {
//                 battleRoom.AddPlayer(memberId);
//             }
//             
//             roomManager.AddBattleRoom(battleRoom);
//             foreach (long memberId in memberIds)
//             {
//                 roomManager.AddUnitToBattleRoom(memberId, battleRoom.Id);
//             }
//             
//             battleRoom.InitTeamBattle(mapScene, memberIds, request.battleType);
//             
//             battleRoom.State = BattleState.Fighting;
//             response.battleId = battleRoom.Id;
//             response.memberIds.AddRange(memberIds);
//             
//             Log.Info($"组队战斗开始: BattleRoomId={battleRoom.Id}, MemberCount={memberIds.Count}");
//             
//             battleRoom.StartFirstWaveDelayed().Coroutine();
//             await ETTask.CompletedTask;
//         }
//     }
// }
