using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 离线战斗刷怪辅助类。
    /// 使用配置表在客户端本地创建敌方 BattleUnit。
    /// </summary>
    public static class OfflineBattleSpawnHelper
    {
        public static BattleUnit SpawnMonster(Battle battle, int configId, float3 position)
        {
            MonsterUnitConfig monsterConfig = ConfigHelper.MonsterUnitConfig?.GetOrDefault(configId);
            if (monsterConfig == null)
            {
                Log.Error($"OfflineBattleSpawn: 找不到 MonsterUnitConfig, configId={configId}");
                return null;
            }

            BattleUnit unit = battle.AddChild<BattleUnit, int>(configId);
            unit.Camp = UnitCamp.Enemy;
            unit.OwnerId = 0;
            unit.Position = BattleAreaConfig.WithBattleUnitSpawnY(position);
            unit.Forward = float3.zero;
            unit.FaceDirection = -1f;
            unit.IsBoss = monsterConfig.Type == 3;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            numeric.Set(NumericType.Hp, monsterConfig.MaxHp);
            numeric.Set(NumericType.MaxHp, monsterConfig.MaxHp);
            numeric.Set(NumericType.Attack, monsterConfig.Attack);
            numeric.Set(NumericType.Defense, monsterConfig.Defense);
            numeric.Set(NumericType.Speed, monsterConfig.Speed);
            unit.GetOrCreateBattleStats().SetCore(monsterConfig.MaxHp, monsterConfig.MaxHp, monsterConfig.Attack, monsterConfig.Defense, monsterConfig.Speed, false);

            if (unit.IsBoss)
            {
                unit.AddComponent<BattleUnitCombatComponent, float>(1.5f);
                unit.AddComponent<BattleAttackComponent>();
                unit.AddComponent<VehicleComponent>();
                unit.AddComponent<ClientPlayerAIComponent>();
            }
            else
            {
                BattleUnitHelper.SetupMinionEmitter(unit, configId, unit.Id);
            }

            BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
            view.InitViewAsync().Forget();

            BattleUIHelper.RefreshUnit(unit);
            return unit;
        }

    }
}   
