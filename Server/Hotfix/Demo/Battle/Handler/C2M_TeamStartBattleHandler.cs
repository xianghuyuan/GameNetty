using System.Collections.Generic;

namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_TeamStartBattleHandler : MessageLocationHandler<Unit, C2M_TeamStartBattle, M2C_TeamStartBattle>
    {
        protected override async ETTask Run(Unit leader, C2M_TeamStartBattle request, M2C_TeamStartBattle response)
        {
            Scene mapScene = leader.Scene();

            BattleRoomManagerComponent roomManager =
                mapScene.GetComponent<BattleRoomManagerComponent>() ??
                mapScene.AddComponent<BattleRoomManagerComponent>();

            List<long> memberIds = new List<long> { leader.Id };

            foreach (long memberId in memberIds)
            {
                if (roomManager.IsUnitInBattle(memberId))
                {
                    response.Error = ErrorCode.ERR_TeamMemberInBattle;
                    response.Message = $"Team member {memberId} is already in battle";
                    return;
                }
            }

            int configId = request.battleType;
            BattleRoom battleRoom = mapScene.AddChild<BattleRoom, int>(configId);

            foreach (long memberId in memberIds)
            {
                battleRoom.AddPlayer(memberId);
            }

            roomManager.AddBattleRoom(battleRoom);
            foreach (long memberId in memberIds)
            {
                roomManager.AddUnitToBattleRoom(memberId, battleRoom.Id);
            }

            battleRoom.InitTeamBattle(mapScene, memberIds, request.battleType);
            response.battleId = battleRoom.Id;
            response.memberIds.AddRange(memberIds);

            Log.Info($"Team battle started: BattleRoomId={battleRoom.Id}, MemberCount={memberIds.Count}");

            battleRoom.StartFirstWave().Coroutine();
            await ETTask.CompletedTask;
        }
    }
}
