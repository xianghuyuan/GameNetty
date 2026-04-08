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

        public static async ETTask TriggerNextWave(this WaveManagerComponent self)
        {
            if (self.State != WaveState.Completed)
            {
                return;
            }

            if (self.CurrentWaveIndex >= self.TotalWaves - 1)
            {
                return;
            }

            await self.StartNextWave();
        }
        
        private static async ETTask StartNextWave(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            if (self.CurrentWaveIndex >= self.TotalWaves - 1)
            {
                await self.OnAllWavesCompleted();
                return;
            }
            
            self.CurrentWaveIndex++;
            self.State = WaveState.Preparing;
            
            int waveNumber = self.CurrentWaveIndex;
            int waveConfigId = self.WaveConfigIds[self.CurrentWaveIndex];
            
            WaveConfig waveConfig = WaveConfigCategory.Instance.GetOrDefault(waveConfigId);
            int monsterCount = self.GetTotalMonsterCount(waveConfigId);
            
            M2C_WaveStart waveStartMsg = M2C_WaveStart.Create();
            waveStartMsg.battleId = battleRoom.Id;
            waveStartMsg.waveNumber = waveNumber;
            waveStartMsg.totalWaves = self.TotalWaves;
            waveStartMsg.monsterCount = monsterCount;
            
            self.BroadcastToBattleRoom(waveStartMsg);
            
            //创建怪物
            await self.SpawnWaveMonsters(waveConfigId);
            
            self.State = WaveState.Fighting;
            self.WaveStartTime = TimeInfo.Instance.ServerFrameTime();
        }
        
        private static async ETTask SpawnWaveMonsters(this WaveManagerComponent self, int waveConfigId)
        {
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
                MonsterUnitConfig monsterConfig = MonsterUnitConfigCategory.Instance.GetOrDefault(monsterInfo.MonsterId);
                bool isBoss = monsterConfig != null && monsterConfig.Type == 3;

                if (isBoss)
                {
                    // Boss走服务端权威路径：服务端创建实体，逐个广播
                    for (int i = 0; i < monsterInfo.Count; i++)
                    {
                        float offsetX = (RandomGenerator.RandFloat01() * 2 - 1) * spreadRange;
                        Vector3 position = new Vector3(centerX + offsetX, 0, 0);
                        BattleUnit monster = UnitFactory.CreateMonster(battleRoom, monsterInfo.MonsterId, position);

                        battleRoom.Units[monster.Id] = monster;
                        self.CurrentWaveMonsterIds.Add(monster.Id);

                        BattleUnitInfo unitInfo = BattleUnitHelper.CreateBattleUnitInfo(monster);
                        battleUnitInfos.Add(unitInfo);
                    }
                }
                else
                {
                    // 杂兵走客户端本地刷怪路径：下发波次指令，客户端本地创建
                    // 服务端仍然创建轻量级实体用于碰撞检测和伤害验证
                    long startUnitId = IdGenerater.Instance.GenerateInstanceId();

                    var spawnWaveMsg = M2C_SpawnWave.Create();
                    spawnWaveMsg.battleId = battleRoom.Id;
                    spawnWaveMsg.waveId = self.CurrentWaveIndex;
                    spawnWaveMsg.centerX = centerX;
                    spawnWaveMsg.centerY = 0f;
                    spawnWaveMsg.count = monsterInfo.Count;
                    spawnWaveMsg.monsterConfigId = monsterInfo.MonsterId;
                    spawnWaveMsg.moveDirX = -1f;
                    spawnWaveMsg.moveDirY = 0f;
                    spawnWaveMsg.spreadRange = spreadRange;
                    spawnWaveMsg.startUnitId = startUnitId;

                    self.BroadcastToBattleRoom(spawnWaveMsg);

                    // 服务端创建轻量级实体用于碰撞检测（不广播M2C_CreateBattleUnits）
                    for (int i = 0; i < monsterInfo.Count; i++)
                    {
                        long localUnitId = startUnitId + i;
                        float offsetX = (RandomGenerator.RandFloat01() * 2 - 1) * spreadRange;
                        Vector3 position = new Vector3(centerX + offsetX, 0, 0);

                        BattleUnit monster = UnitFactory.CreateMinion(battleRoom, monsterInfo.MonsterId, position, localUnitId);
                        battleRoom.Units[monster.Id] = monster;
                        self.CurrentWaveMonsterIds.Add(monster.Id);
                    }
                }
            }

            // 仅广播Boss/精英怪的创建消息
            if (battleUnitInfos.Count > 0)
            {
                M2C_CreateBattleUnits createMsg = M2C_CreateBattleUnits.Create();
                createMsg.battleId = battleRoom.Id;
                createMsg.units = battleUnitInfos;

                self.BroadcastToBattleRoom(createMsg);
            }

            await ETTask.CompletedTask;
        }
        
        public static async ETTask OnMonsterDead(this WaveManagerComponent self, long monsterId)
        {
            self.CurrentWaveMonsterIds.Remove(monsterId);

            // 从空间网格移除
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            BattleSpatialGrid spatialGrid = battleRoom?.GetComponent<BattleSpatialGrid>();
            spatialGrid?.Remove(monsterId);

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
                    self.State = WaveState.Completed;
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
                    // 内联 MapMessageHelper.SendToClient，避免 WaveManagerComponentSystem → MapMessageHelper 的静态类引用
                    player.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Send(player.Id, message);
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
