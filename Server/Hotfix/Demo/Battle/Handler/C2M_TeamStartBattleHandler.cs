using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class C2M_TeamStartBattleHandler : MessageLocationHandler<Unit, C2M_TeamStartBattle, M2C_TeamStartBattle>
    {
        protected override async ETTask Run(Unit leader, C2M_TeamStartBattle request, M2C_TeamStartBattle response)
        {
            Scene mapScene = leader.Scene();
            
            // 1. 获取房间管理器
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                roomManager = mapScene.AddComponent<BattleRoomManagerComponent>();
            }
            
            // 2. 获取队伍信息
            // TODO: 从队伍系统获取队伍成员
            // 目前使用简化实现：只有队长一个人
            List<long> memberIds = new List<long> { leader.Id };
            
            // 实际应该这样：
            // TeamComponent teamComponent = mapScene.GetComponent<TeamComponent>();
            // Team team = teamComponent.GetTeam(request.teamId);
            // if (team == null || team.LeaderId != leader.Id)
            // {
            //     response.Error = ErrorCode.ERR_NotTeamLeader;
            //     response.Message = "只有队长可以开启战斗";
            //     return;
            // }
            // List<long> memberIds = team.MemberIds.ToList();
            
            // 3. 检查所有队员是否都在空闲状态
            foreach (long memberId in memberIds)
            {
                if (roomManager.IsUnitInBattle(memberId))
                {
                    response.Error = ErrorCode.ERR_TeamMemberInBattle;
                    response.Message = $"队员 {memberId} 已在战斗中";
                    return;
                }
            }
            
            // 4. 创建战斗房间
            BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
            battleRoom.Fiber = mapScene.Fiber;
            battleRoom.SceneType = SceneType.Battle;
            battleRoom.State = BattleState.Prepare;
            battleRoom.ConfigId = request.battleType;
            
            // 添加所有队员
            foreach (long memberId in memberIds)
            {
                battleRoom.PlayerIds.Add(memberId);
            }
            
            long battleRoomId = battleRoom.Id;
            
            Log.Info($"创建组队战斗房间: BattleRoomId={battleRoomId}, TeamId={request.teamId}, MemberCount={memberIds.Count}");
            
            // 5. 添加房间到管理器
            roomManager.AddBattleRoom(battleRoom);
            foreach (long memberId in memberIds)
            {
                roomManager.AddUnitToBattleRoom(memberId, battleRoomId);
            }
            
            // 6. 初始化战斗
            try
            {
                await InitTeamBattle(battleRoom, memberIds, request.battleType, request.totalWaves);
            }
            catch (System.Exception e)
            {
                Log.Error($"初始化组队战斗失败: {e}");
                response.Error = ErrorCode.ERR_CreateRoomFailed;
                response.Message = "创建战斗房间失败";
                // 清理
                roomManager.RemoveBattleRoom(battleRoomId);
                battleRoom.Dispose();
                return;
            }
            
            // 7. 开始战斗
            battleRoom.State = BattleState.Fighting;
            
            // 8. 响应客户端
            response.Error = ErrorCode.ERR_Success;
            response.Message = "组队战斗开始";
            response.battleId = battleRoomId;
            response.memberIds.AddRange(memberIds);
            
            Log.Info($"组队战斗开始成功: BattleRoomId={battleRoomId}, MemberCount={memberIds.Count}");
            
            await ETTask.CompletedTask;
        }
        
        /// <summary>
        /// 初始化组队战斗
        /// </summary>
        private async ETTask InitTeamBattle(BattleRoom battleRoom, List<long> memberIds, int battleType, int totalWaves)
        {
            Scene mapScene = battleRoom.Scene();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            
            // 为每个队员创建战斗单位
            int index = 0;
            foreach (long memberId in memberIds)
            {
                Unit unit = unitComponent.Get(memberId);
                if (unit == null)
                {
                    Log.Warning($"找不到队员 Unit: {memberId}");
                    continue;
                }
                
                // 创建战斗单位，排成一排
                Vector3 position = new (index * 2 - memberIds.Count, 0, 0);
                BattleUnit battleUnit = UnitFactory.CreateHero(battleRoom, unit.Id, unit.ConfigId, position);
                battleRoom.Units[battleUnit.Id] = battleUnit;
                
                index++;
            }
            
            // 根据战斗类型初始化
            switch (battleType)
            {
                case 0: // WaveBattle
                    // TODO: 添加波次管理
                    Log.Info($"初始化组队波次战斗: TotalWaves={totalWaves}");
                    break;
                case 1: // Dungeon
                    // TODO: 添加副本管理
                    Log.Info($"初始化组队副本");
                    break;
                case 2: // Boss
                    // 创建Boss
                    BattleUnit bossUnit = UnitFactory.CreateMonster(
                        battleRoom, 
                        battleRoom.ConfigId, 
                        new Vector3(0, 0, 10)
                    );
                    battleRoom.Units[bossUnit.Id] = bossUnit;
                    Log.Info($"初始化组队Boss战");
                    break;
            }
            
            await ETTask.CompletedTask;
        }
    }
}
