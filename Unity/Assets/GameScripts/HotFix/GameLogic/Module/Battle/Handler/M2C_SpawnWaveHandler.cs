using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_SpawnWaveHandler : MessageHandler<Scene, M2C_SpawnWave>
    {
        protected override async ETTask Run(Scene root, M2C_SpawnWave message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_SpawnWave: BattleComponent not found");
                return;
            }

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null)
            {
                Log.Error("M2C_SpawnWave: 当前没有进行中的战斗");
                return;
            }

            MonsterUnitConfig monsterConfig = ConfigHelper.MonsterUnitConfig.Get(message.monsterConfigId);
            if (monsterConfig == null)
            {
                Log.Error($"M2C_SpawnWave: 找不到怪物配置 monsterConfigId={message.monsterConfigId}");
                return;
            }

            for (int i = 0; i < message.count; i++)
            {
                long unitId = message.startUnitId + i;
                float offsetX = (UnityEngine.Random.Range(0f, 1f) * 2f - 1f) * message.spreadRange;
                float posX = message.centerX + offsetX;

                BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, message.monsterConfigId);
                unit.Camp = UnitCamp.Enemy;
                unit.Position = new float3(posX, BattleAreaConfig.BattleUnitSpawnY, 0);
                unit.Forward = new float3(message.moveDirX, message.moveDirY, 0);
                unit.FaceDirection = message.moveDirX >= 0f ? 1f : -1f;

                NumericComponent numeric = unit.AddComponent<NumericComponent>();
                numeric.Set(NumericType.Hp, monsterConfig.MaxHp);
                numeric.Set(NumericType.MaxHp, monsterConfig.MaxHp);
                numeric.Set(NumericType.Attack, monsterConfig.Attack);
                numeric.Set(NumericType.Defense, monsterConfig.Defense);
                numeric.Set(NumericType.Speed, monsterConfig.Speed);
                unit.GetOrCreateBattleStats().SetCore(monsterConfig.MaxHp, monsterConfig.MaxHp, monsterConfig.Attack, monsterConfig.Defense, monsterConfig.Speed, false);

                BattleUnitHelper.SetupMinionEmitter(unit, message.monsterConfigId, unitId);

                BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
                view.InitViewAsync().Forget();
            }

            // 确保 Battle 上挂载 AI Tick 组件
            if (battle.GetComponent<ClientMinionAITickComponent>() == null)
            {
                battle.AddComponent<ClientMinionAITickComponent>();
            }

            Log.Info($"M2C_SpawnWave: 创建 {message.count} 个杂兵, configId={message.monsterConfigId}, waveId={message.waveId}");
            await ETTask.CompletedTask;
        }
    }
}
