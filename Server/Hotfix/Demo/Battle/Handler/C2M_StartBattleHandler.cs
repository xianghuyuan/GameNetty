using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
    {
        protected override async ETTask Run(Unit unit, C2M_StartBattle request, M2C_StartBattle response)
        {
            Scene mapScene = unit.Scene();
            
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                roomManager = mapScene.AddComponent<BattleRoomManagerComponent>();
            }
            
            if (roomManager.IsUnitInBattle(unit.Id))
            {
                response.Error = ErrorCode.ERR_AlreadyInBattle;
                response.Message = "玩家已在战斗中";
                Log.Warning($"玩家 {unit.Id} 已在战斗中，无法开始新战斗");
                return;
            }
            
            BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
            battleRoom.Fiber = mapScene.Fiber;
            battleRoom.SceneType = SceneType.Battle;
            battleRoom.State = BattleState.Prepare;
            battleRoom.ConfigId = request.stageId > 0 ? request.stageId : request.battleType;
            battleRoom.PlayerIds.Add(unit.Id);
            
            long battleRoomId = battleRoom.Id;
            
            Log.Info($"创建战斗房间: BattleRoomId={battleRoomId}, PlayerId={unit.Id}, StageId={request.stageId}, BattleType={request.battleType}");
            
            roomManager.AddBattleRoom(battleRoom);
            roomManager.AddUnitToBattleRoom(unit.Id, battleRoomId);
            
            try
            {
                if (request.stageId > 0)
                {
                    await InitStageBattle(battleRoom, unit, request.stageId);
                }
                else
                {
                    switch (request.battleType)
                    {
                        case 0:
                            await InitWaveBattle(battleRoom, unit);
                            break;
                        case 1:
                            await InitDungeon(battleRoom, unit);
                            break;
                        case 2:
                            await InitBossBattle(battleRoom, unit);
                            break;
                        default:
                            response.Error = ErrorCode.ERR_InvalidBattleType;
                            response.Message = "无效的战斗类型";
                            roomManager.RemoveBattleRoom(battleRoomId);
                            battleRoom.Dispose();
                            return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"初始化战斗失败: {e}");
                response.Error = ErrorCode.ERR_CreateRoomFailed;
                response.Message = "创建战斗房间失败";
                roomManager.RemoveBattleRoom(battleRoomId);
                battleRoom.Dispose();
                return;
            }
            
            battleRoom.State = BattleState.Fighting;
            
            response.Error = ErrorCode.ERR_Success;
            response.Message = "战斗开始";
            response.battleId = battleRoomId;
            
            Log.Info($"玩家 {unit.Id} 开始战斗成功: BattleRoomId={battleRoomId}");
            
            await ETTask.CompletedTask;
        }
        
        private async ETTask InitStageBattle(BattleRoom battleRoom, Unit unit, int stageId)
        {
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, 0)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            StageConfigInfo stageInfo = GetStageConfig(stageId);
            
            WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int, List<int>>(
                stageId, 
                stageInfo.WaveConfigIds
            );
            
            Log.Info($"初始化关卡战斗: BattleRoomId={battleRoom.Id}, StageId={stageId}, TotalWaves={stageInfo.TotalWaves}");
            
            await waveManager.StartFirstWave();
        }
        
        private StageConfigInfo GetStageConfig(int stageId)
        {
            // TODO: 配置表生成后从 StageConfigCategory 读取
            // StageConfig config = StageConfigCategory.Instance.Get(stageId);
            // return new StageConfigInfo
            // {
            //     TotalWaves = config.TotalWaves,
            //     WaveConfigIds = config.WaveList
            // };
            
            // 临时：使用 WaveConfig 构建虚拟关卡
            List<int> waveConfigIds = new List<int>();
            int maxWaveNumber = 0;
            
            foreach (var config in WaveConfigCategory.Instance.DataList)
            {
                waveConfigIds.Add(config.Id);
                if (config.WaveNumber > maxWaveNumber)
                {
                    maxWaveNumber = config.WaveNumber;
                }
            }
            
            waveConfigIds.Sort((a, b) => 
            {
                var configA = WaveConfigCategory.Instance.Get(a);
                var configB = WaveConfigCategory.Instance.Get(b);
                return (configA?.WaveNumber ?? 0).CompareTo(configB?.WaveNumber ?? 0);
            });
            
            return new StageConfigInfo
            {
                TotalWaves = waveConfigIds.Count,
                WaveConfigIds = waveConfigIds
            };
        }
        
        private struct StageConfigInfo
        {
            public int TotalWaves;
            public List<int> WaveConfigIds;
        }
        
        private async ETTask InitWaveBattle(BattleRoom battleRoom, Unit unit)
        {
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, 0)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            StageConfigInfo stageInfo = GetStageConfig(1);
            
            WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int, List<int>>(
                1, 
                stageInfo.WaveConfigIds
            );
            
            Log.Info($"初始化波次战斗: BattleRoomId={battleRoom.Id}, TotalWaves={stageInfo.TotalWaves}");
            
            await waveManager.StartFirstWave();
        }
        
        private async ETTask InitDungeon(BattleRoom battleRoom, Unit unit)
        {
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, 0)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            Log.Info($"初始化副本: BattleRoomId={battleRoom.Id}, ConfigId={battleRoom.ConfigId}");
            
            await ETTask.CompletedTask;
        }
        
        private async ETTask InitBossBattle(BattleRoom battleRoom, Unit unit)
        {
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, -5)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            BattleUnit bossUnit = UnitFactory.CreateMonster(
                battleRoom, 
                battleRoom.ConfigId,
                new Vector3(0, 0, 5)
            );
            
            battleRoom.Units[bossUnit.Id] = bossUnit;
            
            Log.Info($"初始化Boss战: BattleRoomId={battleRoom.Id}, BossConfigId={battleRoom.ConfigId}");
            
            await ETTask.CompletedTask;
        }
    }
}
