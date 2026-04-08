using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(OfflineWaveManagerComponent))]
    [FriendOf(typeof(OfflineWaveManagerComponent))]
    [FriendOf(typeof(Battle))]
    public static partial class OfflineWaveManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OfflineWaveManagerComponent self, int stageConfigId)
        {
            self.StageConfigId = stageConfigId;
            self.CurrentWaveIndex = -1;
            self.State = WaveState.None;
            self.AliveMonsterIds = new List<long>();

            StageConfig stageConfig = ConfigHelper.StageConfig?.GetOrDefault(stageConfigId);
            if (stageConfig != null)
            {
                self.TotalWaves = stageConfig.TotalWaves;
                self.WaveConfigIds = stageConfig.WaveList;
            }
            else
            {
                // 没有关卡配置时，使用所有 WaveConfig 按 WaveNumber 排序
                var waveConfigs = ConfigHelper.WaveConfig?.DataList;
                if (waveConfigs != null && waveConfigs.Count > 0)
                {
                    var sorted = new List<WaveConfig>(waveConfigs);
                    sorted.Sort((a, b) => a.WaveNumber.CompareTo(b.WaveNumber));
                    self.TotalWaves = sorted.Count;
                    self.WaveConfigIds = new int[sorted.Count];
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        self.WaveConfigIds[i] = sorted[i].Id;
                    }
                }
            }
        }

        [EntitySystem]
        private static void Destroy(this OfflineWaveManagerComponent self)
        {
            self.AliveMonsterIds?.Clear();
        }

        /// <summary>
        /// 启动第一波（创建玩家单位后调用）
        /// </summary>
        public static async ETTask StartFirstWave(this OfflineWaveManagerComponent self)
        {
            await self.StartNextWave();
        }

        private static async ETTask StartNextWave(this OfflineWaveManagerComponent self)
        {
            if (self.CurrentWaveIndex >= self.TotalWaves - 1)
            {
                await self.OnAllWavesCompleted();
                return;
            }

            self.CurrentWaveIndex++;
            self.State = WaveState.Preparing;

            Battle battle = self.GetParent<Battle>();
            int waveConfigId = self.WaveConfigIds[self.CurrentWaveIndex];

            battle.CurrentWave = self.CurrentWaveIndex;
            battle.TotalWaves = self.TotalWaves;

            EventSystem.Instance.Publish(battle.Scene(), new WaveStart
            {
                Battle = battle,
                WaveNumber = self.CurrentWaveIndex,
            });

            await self.SpawnWaveMonsters(waveConfigId);

            self.State = WaveState.Fighting;
            self.WaveStartTime = TimeInfo.Instance.ClientNow();
        }

        private static async ETTask SpawnWaveMonsters(this OfflineWaveManagerComponent self, int waveConfigId)
        {
            WaveConfig waveConfig = ConfigHelper.WaveConfig?.GetOrDefault(waveConfigId);
            if (waveConfig == null)
            {
                Log.Error($"OfflineWave: WaveConfig not found: id={waveConfigId}");
                return;
            }

            Battle battle = self.GetParent<Battle>();

            foreach (var batch in waveConfig.Batches)
            {
                SpawnConfig spawnConfig = ConfigHelper.SpawnConfig?.GetOrDefault(batch.SpawnId);
                if (spawnConfig == null)
                {
                    Log.Error($"OfflineWave: SpawnConfig not found: SpawnId={batch.SpawnId}");
                    continue;
                }

                if (batch.Delay > 0)
                {
                    await self.Root().GetComponent<TimerComponent>().WaitAsync(batch.Delay);
                }

                self.SpawnFromSpawnConfig(spawnConfig, battle);
            }
        }

        /// <summary>
        /// 从 SpawnConfig 生成怪物，复用 M2C_SpawnWaveHandler 的逻辑
        /// </summary>
        private static void SpawnFromSpawnConfig(this OfflineWaveManagerComponent self, SpawnConfig spawnConfig, Battle battle)
        {
            float centerX = spawnConfig.PositionX;
            float spreadRange = spawnConfig.SpreadRange;

            foreach (var monsterInfo in spawnConfig.Monsters)
            {
                MonsterUnitConfig monsterConfig = ConfigHelper.MonsterUnitConfig?.GetOrDefault(monsterInfo.MonsterId);
                if (monsterConfig == null)
                {
                    Log.Error($"OfflineWave: MonsterUnitConfig not found: id={monsterInfo.MonsterId}");
                    continue;
                }

                // 跳过 Boss 类型（离线模式 v1 不支持）
                if (monsterConfig.Type == 3)
                {
                    continue;
                }

                for (int i = 0; i < monsterInfo.Count; i++)
                {
                    long unitId = IdGenerater.Instance.GenerateInstanceId();
                    float offsetX = (UnityEngine.Random.Range(0f, 1f) * 2f - 1f) * spreadRange;
                    float posX = centerX + offsetX;

                    BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, monsterInfo.MonsterId);
                    unit.Camp = UnitCamp.Enemy;
                    unit.Position = new float3(posX, 0, 0);
                    unit.Forward = new float3(-1f, 0, 0);

                    NumericComponent numeric = unit.AddComponent<NumericComponent>();
                    numeric.Set(NumericType.Hp, monsterConfig.MaxHp);
                    numeric.Set(NumericType.MaxHp, monsterConfig.MaxHp);
                    numeric.Set(NumericType.Attack, monsterConfig.Attack);
                    numeric.Set(NumericType.Defense, monsterConfig.Defense);
                    numeric.Set(NumericType.Speed, monsterConfig.Speed);

                    unit.AddComponent<BattleUnitCombatComponent, float>(1.5f);
                    unit.AddComponent<ClientMinionAIComponent>();

                    BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
                    view.InitViewAsync().Forget();

                    BattleUIHelper.CreateUnitUI(unit);

                    self.AliveMonsterIds.Add(unitId);
                }
            }

            // 确保挂载杂兵 AI Tick
            if (battle.GetComponent<ClientMinionAITickComponent>() == null)
            {
                battle.AddComponent<ClientMinionAITickComponent>();
            }
        }

        /// <summary>
        /// 每帧检查波次状态，所有怪物死亡则触发波次完成
        /// </summary>
        public static void CheckWaveState(this OfflineWaveManagerComponent self)
        {
            if (self.State != WaveState.Fighting)
            {
                return;
            }

            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State != BattleState.Fighting)
            {
                return;
            }

            // 清理已死亡的怪物 ID
            for (int i = self.AliveMonsterIds.Count - 1; i >= 0; i--)
            {
                long id = self.AliveMonsterIds[i];
                BattleUnit unit = battle.GetChild<BattleUnit>(id);
                if (unit == null || unit.IsDisposed || unit.IsDead)
                {
                    self.AliveMonsterIds.RemoveAt(i);
                }
            }

            if (self.AliveMonsterIds.Count == 0)
            {
                self.OnWaveCompleted().Coroutine();
            }
        }

        /// <summary>
        /// 检查玩家是否存活
        /// </summary>
        public static void CheckPlayerAlive(this OfflineWaveManagerComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State != BattleState.Fighting)
            {
                return;
            }

            if (battle.GetAliveBattleUnits(UnitCamp.Friend).Count == 0)
            {
                battle.End(false);
            }
        }

        private static async ETTask OnWaveCompleted(this OfflineWaveManagerComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            self.State = WaveState.Completed;

            int waveNumber = self.CurrentWaveIndex + 1;

            EventSystem.Instance.Publish(battle.Scene(), new WaveComplete
            {
                Battle = battle,
                WaveNumber = waveNumber,
            });

            if (self.CurrentWaveIndex < self.TotalWaves - 1)
            {
                await self.Root().GetComponent<TimerComponent>().WaitAsync(3000);
                await self.StartNextWave();
            }
            else
            {
                await self.OnAllWavesCompleted();
            }
        }

        private static async ETTask OnAllWavesCompleted(this OfflineWaveManagerComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            battle.End(true);
            await ETTask.CompletedTask;
        }
    }
}
