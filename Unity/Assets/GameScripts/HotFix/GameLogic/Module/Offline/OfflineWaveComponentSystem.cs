using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(OfflineWaveComponent))]
    [FriendOf(typeof(OfflineWaveComponent))]
    [FriendOf(typeof(Battle))]
    public static partial class OfflineWaveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OfflineWaveComponent self, int stageId, List<int> waveConfigIds)
        {
            self.StageId = stageId;
            self.WaveConfigIds = waveConfigIds;
            self.CurrentWaveIndex = -1;
            self.State = OfflineWaveState.None;
            self.CurrentWaveUnitIds = new List<long>();
            self.WaveIntervalMs = 3000;
            self.AutoStartNextWave = true;

            Battle battle = self.GetParent<Battle>();
            battle.TotalWaves = waveConfigIds?.Count ?? 0;
            battle.CurrentWave = 0;
        }

        [EntitySystem]
        private static void Destroy(this OfflineWaveComponent self)
        {
            self.CurrentWaveUnitIds?.Clear();
            self.WaveConfigIds?.Clear();
        }

        public static async ETTask StartFirstWave(this OfflineWaveComponent self)
        {
            await self.StartNextWave();
        }

        public static async ETTask StartNextWave(this OfflineWaveComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.IsDisposed || battle.State == BattleState.Ended)
            {
                return;
            }

            if (self.WaveConfigIds == null || self.WaveConfigIds.Count == 0)
            {
                battle.End(true);
                return;
            }

            if (self.CurrentWaveIndex >= self.WaveConfigIds.Count - 1)
            {
                battle.End(true);
                return;
            }

            self.CurrentWaveIndex++;
            self.CurrentWaveUnitIds.Clear();
            self.State = OfflineWaveState.Preparing;

            int waveConfigId = self.WaveConfigIds[self.CurrentWaveIndex];
            WaveConfig waveConfig = ConfigHelper.WaveConfig?.GetOrDefault(waveConfigId);
            if (waveConfig == null)
            {
                Log.Error($"OfflineWave: 找不到 WaveConfig, id={waveConfigId}");
                battle.End(false);
                return;
            }

            self.WaveIntervalMs = waveConfig.WaveInterval > 0 ? waveConfig.WaveInterval : self.WaveIntervalMs;
            battle.CurrentWave = waveConfig.WaveNumber > 0 ? waveConfig.WaveNumber : self.CurrentWaveIndex + 1;

            EventSystem.Instance.Publish(battle.Scene(), new WaveStart
            {
                Battle = battle,
                WaveNumber = battle.CurrentWave,
            });

            foreach (BatchInfo batch in waveConfig.Batches)
            {
                if (batch.Delay > 0)
                {
                    await battle.Root().GetComponent<TimerComponent>().WaitAsync(batch.Delay);
                    if (battle.IsDisposed || battle.State == BattleState.Ended)
                    {
                        return;
                    }
                }

                SpawnBatch(self, battle, batch.SpawnId);
            }

            self.State = OfflineWaveState.Fighting;

            if (battle.GetComponent<ClientMinionAITickComponent>() == null)
            {
                battle.AddComponent<ClientMinionAITickComponent>();
            }
        }

        public static async ETTask OnEnemyUnitDead(this OfflineWaveComponent self, long unitId)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.IsDisposed || battle.State == BattleState.Ended)
            {
                return;
            }

            if (!self.CurrentWaveUnitIds.Remove(unitId))
            {
                return;
            }

            if (self.CurrentWaveUnitIds.Count > 0 || self.State != OfflineWaveState.Fighting)
            {
                return;
            }

            self.State = OfflineWaveState.Completed;

            EventSystem.Instance.Publish(battle.Scene(), new WaveComplete
            {
                Battle = battle,
                WaveNumber = battle.CurrentWave,
            });

            if (self.CurrentWaveIndex >= self.WaveConfigIds.Count - 1)
            {
                battle.End(true);
                return;
            }

            if (!self.AutoStartNextWave)
            {
                return;
            }

            int waitMs = self.WaveIntervalMs > 0 ? self.WaveIntervalMs : 3000;
            await battle.Root().GetComponent<TimerComponent>().WaitAsync(waitMs);

            if (battle.IsDisposed || battle.State == BattleState.Ended)
            {
                return;
            }

            await self.StartNextWave();
        }

        private static void SpawnBatch(OfflineWaveComponent self, Battle battle, int spawnConfigId)
        {
            SpawnConfig spawnConfig = ConfigHelper.SpawnConfig?.GetOrDefault(spawnConfigId);
            if (spawnConfig == null)
            {
                Log.Error($"OfflineWave: 找不到 SpawnConfig, id={spawnConfigId}");
                return;
            }

            float centerX = spawnConfig.PositionX;
            float spreadRange = spawnConfig.SpreadRange;

            foreach (MonsterSpawnInfo monsterInfo in spawnConfig.Monsters)
            {
                for (int i = 0; i < monsterInfo.Count; i++)
                {
                    float offsetX = (UnityEngine.Random.Range(0f, 1f) * 2f - 1f) * spreadRange;
                    float3 position = new float3(centerX + offsetX, 0f, 0f);
                    BattleUnit unit = OfflineBattleSpawnHelper.SpawnMonster(battle, monsterInfo.MonsterId, position);
                    if (unit != null)
                    {
                        self.CurrentWaveUnitIds.Add(unit.Id);
                    }
                }
            }
        }

        public static List<int> BuildOfflineWaveConfigIds(int stageId)
        {
            return ConfigHelper.WaveConfig?.DataList
                .OrderBy(config => config.WaveNumber)
                .Select(config => config.Id)
                .ToList() ?? new List<int>();
        }
    }

    [Event(SceneType.Main)]
    public class OfflineBattleUnitDeadHandler : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUnit unit = args.BattleUnit;
            Battle battle = unit?.GetParent<Battle>();
            if (battle == null || battle.IsDisposed)
            {
                return;
            }

            if (battle.GetComponent<OfflineBattleComponent>() == null || battle.State == BattleState.Ended)
            {
                return;
            }

            if (unit.Camp == UnitCamp.Friend)
            {
                if (battle.GetAliveBattleUnits(UnitCamp.Friend).Count == 0)
                {
                    battle.End(false);
                }

                return;
            }

            OfflineWaveComponent waveComponent = battle.GetComponent<OfflineWaveComponent>();
            if (waveComponent != null)
            {
                await waveComponent.OnEnemyUnitDead(unit.Id);
            }
        }
    }
}
