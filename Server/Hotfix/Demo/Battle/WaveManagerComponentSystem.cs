using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(WaveManagerComponent))]
    [FriendOf(typeof(WaveManagerComponent))]
    [FriendOf(typeof(BattleRoom))]
    public static partial class WaveManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WaveManagerComponent self, int stageConfigId, List<int> waveConfigIds)
        {
            self.StageConfigId = stageConfigId;
            self.WaveConfigIds = waveConfigIds;
            self.TotalWaves = waveConfigIds.Count;
            self.CurrentWaveIndex = -1;
            self.State = WaveState.None;
            self.CurrentWaveMonsterIds = new List<long>();
            self.WaveInterval = 5000;
            self.AutoStartNextWave = true;
        }
        
        [EntitySystem]
        private static void Destroy(this WaveManagerComponent self)
        {
            self.CurrentWaveMonsterIds.Clear();
            self.WaveConfigIds?.Clear();
        }
        
        public static async ETTask StartFirstWave(this WaveManagerComponent self)
        {
            await self.StartNextWave();
        }
        
        public static async ETTask StartNextWave(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            if (self.CurrentWaveIndex >= self.TotalWaves - 1)
            {
                Log.Info($"所有波次已完成: BattleRoomId={battleRoom.Id}");
                await self.OnAllWavesCompleted();
                return;
            }
            
            self.CurrentWaveIndex++;
            self.State = WaveState.Preparing;
            
            int waveNumber = self.CurrentWaveIndex + 1;
            int waveConfigId = self.WaveConfigIds[self.CurrentWaveIndex];
            
            Log.Info($"准备开始第 {waveNumber}/{self.TotalWaves} 波: BattleRoomId={battleRoom.Id}, WaveConfigId={waveConfigId}");
            
            WaveConfig waveConfig = WaveConfigCategory.Instance.GetOrDefault(waveConfigId);
            int monsterCount = self.GetTotalMonsterCount(waveConfigId);
            
            M2C_WaveStart waveStartMsg = M2C_WaveStart.Create();
            waveStartMsg.battleId = battleRoom.Id;
            waveStartMsg.waveNumber = waveNumber;
            waveStartMsg.totalWaves = self.TotalWaves;
            waveStartMsg.monsterCount = monsterCount;
            
            self.BroadcastToBattleRoom(waveStartMsg);
            
            if (self.CurrentWaveIndex > 0)
            {
                int interval = waveConfig?.WaveInterval ?? self.WaveInterval;
                await self.Root().GetComponent<TimerComponent>().WaitAsync(interval);
            }
            
            await self.SpawnWaveMonsters(waveConfigId);
            
            self.State = WaveState.Fighting;
            self.WaveStartTime = TimeInfo.Instance.ServerFrameTime();
            
            Log.Info($"第 {waveNumber} 波开始战斗: BattleRoomId={battleRoom.Id}, MonsterCount={self.CurrentWaveMonsterIds.Count}");
        }
        
        private static async ETTask SpawnWaveMonsters(this WaveManagerComponent self, int waveConfigId)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            self.CurrentWaveMonsterIds.Clear();
            
            WaveConfig waveConfig = WaveConfigCategory.Instance.GetOrDefault(waveConfigId);
            if (waveConfig == null)
            {
                Log.Error($"找不到波次配置: WaveConfigId={waveConfigId}");
                return;
            }
            
            // 使用新配置系统：SpawnConfig
            await self.SpawnWaveMonstersFromBatches(waveConfig);
        }
        
        private static async ETTask SpawnWaveMonstersFromBatches(this WaveManagerComponent self, WaveConfig waveConfig)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            long waveStartTime = TimeInfo.Instance.ServerFrameTime();
            
            foreach (var batch in waveConfig.Batches)
            {
                SpawnConfig spawnConfig = SpawnConfigCategory.Instance.GetOrDefault(batch.SpawnId);
                if (spawnConfig == null)
                {
                    Log.Error($"找不到刷怪配置: SpawnId={batch.SpawnId}");
                    continue;
                }
                
                if (batch.Delay > 0)
                {
                    await self.Root().GetComponent<TimerComponent>().WaitAsync(batch.Delay);
                }
                
                await self.SpawnFromSpawnConfig(spawnConfig);
            }
            
            await ETTask.CompletedTask;
        }
        
        private static async ETTask SpawnFromSpawnConfig(this WaveManagerComponent self, SpawnConfig spawnConfig)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            // 卷轴战斗中当前只使用 X 坐标，PositionZ 暂时保留但不参与布局。
            float centerX = spawnConfig.PositionX;
            float spreadRange = spawnConfig.SpreadRange;
            
            // 收集本批次创建的怪物信息
            List<BattleUnitInfo> battleUnitInfos = new List<BattleUnitInfo>();
            
            foreach (var monsterInfo in spawnConfig.Monsters)
            {
                for (int i = 0; i < monsterInfo.Count; i++)
                {
                    float offsetX = (RandomGenerator.RandFloat01() * 2 - 1) * spreadRange;

                    Vector3 position = new Vector3(centerX + offsetX, 0, 0);
                    BattleUnit monster = UnitFactory.CreateMonster(battleRoom, monsterInfo.MonsterId, position);
                    
                    battleRoom.Units[monster.Id] = monster;
                    self.CurrentWaveMonsterIds.Add(monster.Id);
                    
                    // 使用统一方法创建单位信息（包含数值）
                    BattleUnitInfo unitInfo = BattleUnitHelper.CreateBattleUnitInfo(monster);
                    battleUnitInfos.Add(unitInfo);
                    
                    Log.Debug($"生成怪物: MonsterId={monster.Id}, ConfigId={monsterInfo.MonsterId}, Position={position}");
                }
            }
            
            // 发送怪物创建消息给所有玩家
            if (battleUnitInfos.Count > 0)
            {
                M2C_CreateBattleUnits createMsg = M2C_CreateBattleUnits.Create();
                createMsg.battleId = battleRoom.Id;
                createMsg.units = battleUnitInfos;
                
                self.BroadcastToBattleRoom(createMsg);
                
                Log.Info($"发送怪物创建消息: BattleId={battleRoom.Id}, Count={battleUnitInfos.Count}");
            }
            
            await ETTask.CompletedTask;
        }
        
        public static async ETTask OnMonsterDead(this WaveManagerComponent self, long monsterId)
        {
            self.CurrentWaveMonsterIds.Remove(monsterId);
            
            Log.Debug($"怪物死亡: MonsterId={monsterId}, 剩余怪物数: {self.CurrentWaveMonsterIds.Count}");
            
            if (self.CurrentWaveMonsterIds.Count == 0 && self.State == WaveState.Fighting)
            {
                await self.OnWaveCompleted();
            }
        }
        
        private static async ETTask OnWaveCompleted(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            self.State = WaveState.Completed;
            long duration = (TimeInfo.Instance.ServerFrameTime() - self.WaveStartTime) / 1000;
            
            int waveNumber = self.CurrentWaveIndex + 1;
            
            Log.Info($"第 {waveNumber} 波完成: BattleRoomId={battleRoom.Id}, 耗时={duration}秒");
            
            M2C_WaveComplete waveCompleteMsg = M2C_WaveComplete.Create();
            waveCompleteMsg.battleId = battleRoom.Id;
            waveCompleteMsg.waveNumber = waveNumber;
            waveCompleteMsg.totalWaves = self.TotalWaves;
            waveCompleteMsg.duration = (int)duration;
            
            self.BroadcastToBattleRoom(waveCompleteMsg);
            
            if (self.CurrentWaveIndex < self.TotalWaves - 1)
            {
                if (self.AutoStartNextWave)
                {
                    await self.StartNextWave();
                }
                else
                {
                    Log.Info($"等待玩家开始下一波: BattleRoomId={battleRoom.Id}");
                }
            }
            else
            {
                await self.OnAllWavesCompleted();
            }
        }
        
        private static async ETTask OnAllWavesCompleted(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            Log.Info($"所有波次完成，战斗胜利: BattleRoomId={battleRoom.Id}");
            
            battleRoom.State = BattleState.End;
            
            M2C_BattleEnd battleEndMsg = M2C_BattleEnd.Create();
            battleEndMsg.battleId = battleRoom.Id;
            battleEndMsg.success = true;
            battleEndMsg.duration = 0;
            
            self.BroadcastToBattleRoom(battleEndMsg);
            
            await self.Root().GetComponent<TimerComponent>().WaitAsync(5000);
            
            Scene mapScene = battleRoom.Scene();
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager != null)
            {
                foreach (long playerId in battleRoom.PlayerIds)
                {
                    roomManager.RemoveUnitFromBattleRoom(playerId);
                }
                roomManager.RemoveBattleRoom(battleRoom.Id);
            }
            
            battleRoom.Dispose();
        }
        
        private static int GetTotalMonsterCount(this WaveManagerComponent self, int waveConfigId)
        {
            WaveConfig config = WaveConfigCategory.Instance.GetOrDefault(waveConfigId);
            if (config == null)
            {
                return 0;
            }
            
            int totalCount = 0;
            foreach (var batch in config.Batches)
            {
                SpawnConfig spawnConfig = SpawnConfigCategory.Instance.GetOrDefault(batch.SpawnId);
                if (spawnConfig != null)
                {
                    foreach (var monsterInfo in spawnConfig.Monsters)
                    {
                        totalCount += monsterInfo.Count;
                    }
                }
            }
            
            return totalCount;
        }
        
        private static void BroadcastToBattleRoom(this WaveManagerComponent self, IMessage message)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            
            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, message);
                }
            }
        }
        
        public static async ETTask ForceCompleteCurrentWave(this WaveManagerComponent self)
        {
            if (self.State != WaveState.Fighting)
            {
                return;
            }
            
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            foreach (long monsterId in self.CurrentWaveMonsterIds.ToArray())
            {
                if (battleRoom.Units.TryGetValue(monsterId, out EntityRef<BattleUnit> monsterRef))
                {
                    BattleUnit monster = monsterRef;
                    monster?.Dispose();
                }
            }
            
            self.CurrentWaveMonsterIds.Clear();
            
            await self.OnWaveCompleted();
        }
    }
}
