using System.Numerics;

namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
    {
        protected override async ETTask Run(Unit unit, C2M_StartBattle request, M2C_StartBattle response)
        {
            Scene mapScene = unit.Scene();
            
            // 1. 获取或创建 BattleRoomManagerComponent
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                roomManager = mapScene.AddComponent<BattleRoomManagerComponent>();
            }
            
            // 2. 检查玩家是否已在战斗中
            if (roomManager.IsUnitInBattle(unit.Id))
            {
                response.Error = ErrorCode.ERR_AlreadyInBattle;
                response.Message = "玩家已在战斗中";
                Log.Warning($"玩家 {unit.Id} 已在战斗中，无法开始新战斗");
                return;
            }
            
            // 3. 创建战斗房间（BattleRoom）
            BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
            battleRoom.Fiber = mapScene.Fiber;
            battleRoom.SceneType = SceneType.Battle;
            battleRoom.State = BattleState.Prepare;
            battleRoom.ConfigId = request.battleType; // 使用 battleType 作为配置ID
            battleRoom.PlayerIds.Add(unit.Id);
            
            long battleRoomId = battleRoom.Id;
            
            Log.Info($"创建战斗房间: BattleRoomId={battleRoomId}, PlayerId={unit.Id}, BattleType={request.battleType}");
            
            // 4. 添加房间到管理器
            roomManager.AddBattleRoom(battleRoom);
            roomManager.AddUnitToBattleRoom(unit.Id, battleRoomId);
            
            // 5. 根据战斗类型初始化战斗内容
            try
            {
                switch (request.battleType)
                {
                    case 0: // WaveBattle - 波次战斗
                        await InitWaveBattle(battleRoom, unit, request.totalWaves);
                        break;
                    case 1: // Dungeon - 副本
                        await InitDungeon(battleRoom, unit);
                        break;
                    case 2: // Boss - Boss战
                        await InitBossBattle(battleRoom, unit);
                        break;
                    default:
                        response.Error = ErrorCode.ERR_InvalidBattleType;
                        response.Message = "无效的战斗类型";
                        // 清理已创建的房间
                        roomManager.RemoveBattleRoom(battleRoomId);
                        battleRoom.Dispose();
                        return;
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"初始化战斗失败: {e}");
                response.Error = ErrorCode.ERR_CreateRoomFailed;
                response.Message = "创建战斗房间失败";
                // 清理已创建的房间
                roomManager.RemoveBattleRoom(battleRoomId);
                battleRoom.Dispose();
                return;
            }
            
            // 6. 开始战斗
            battleRoom.State = BattleState.Fighting;
            
            // 7. 响应客户端
            response.Error = ErrorCode.ERR_Success;
            response.Message = "战斗开始";
            response.battleId = battleRoomId;
            
            Log.Info($"玩家 {unit.Id} 开始战斗成功: BattleRoomId={battleRoomId}, BattleType={request.battleType}");
            
            await ETTask.CompletedTask;
        }
        
        /// <summary>
        /// 初始化波次战斗
        /// </summary>
        private async ETTask InitWaveBattle(BattleRoom battleRoom, Unit unit, int totalWaves)
        {
            // 创建玩家的战斗单位
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, 0)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            // 添加波次管理组件
            WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int>(totalWaves);
            
            Log.Info($"初始化波次战斗: BattleRoomId={battleRoom.Id}, TotalWaves={totalWaves}");
            
            // 开始第一波
            await waveManager.StartFirstWave();
        }
        
        /// <summary>
        /// 初始化副本
        /// </summary>
        private async ETTask InitDungeon(BattleRoom battleRoom, Unit unit)
        {
            // 创建玩家的战斗单位
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, 0)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            // TODO: 添加副本管理组件
            // DungeonManagerComponent dungeonManager = battleRoom.AddComponent<DungeonManagerComponent>();
            // await dungeonManager.StartDungeon(battleRoom.ConfigId);
            
            Log.Info($"初始化副本: BattleRoomId={battleRoom.Id}, ConfigId={battleRoom.ConfigId}");
            
            await ETTask.CompletedTask;
        }
        
        /// <summary>
        /// 初始化Boss战
        /// </summary>
        private async ETTask InitBossBattle(BattleRoom battleRoom, Unit unit)
        {
            // 创建玩家的战斗单位
            BattleUnit playerUnit = UnitFactory.CreateHero(
                battleRoom, 
                unit.Id, 
                unit.ConfigId, 
                new Vector3(0, 0, -5)
            );
            
            battleRoom.Units[playerUnit.Id] = playerUnit;
            
            // 创建Boss
            BattleUnit bossUnit = UnitFactory.CreateMonster(
                battleRoom, 
                battleRoom.ConfigId, // 使用 ConfigId 作为 Boss 配置ID
                new Vector3(0, 0, 5)
            );
            
            battleRoom.Units[bossUnit.Id] = bossUnit;
            
            Log.Info($"初始化Boss战: BattleRoomId={battleRoom.Id}, BossConfigId={battleRoom.ConfigId}");
            
            await ETTask.CompletedTask;
        }
    }
}
