using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    public sealed class BattleDebugEmitterSpec
    {
        public int CooldownMs = 1000;
        public float Range = 3f;
        public bool CanMoveCast;
        public BattleAttackPayloadType PayloadType = BattleAttackPayloadType.VehicleBuff;
        public List<int> BuffGroupIds = new();
    }

    /// <summary>
    /// 战斗调试生成辅助类
    /// 在任意战斗中生成纯客户端怪物，属性由面板手动指定，不依赖配置表
    /// </summary>
    public static class BattleDebugSpawnHelper
    {
        /// <summary>
        /// 在战斗中生成怪物（纯客户端，属性手动指定）
        /// </summary>
        public static List<BattleUnit> SpawnMonster(
            Battle battle,
            int hp,
            int atk,
            int def,
            float speed,
            int count,
            float offsetFromCamera,
            float spreadRange,
            int emitterCount,
            int emitterCooldownMs,
            float emitterRange,
            bool emitterCanMoveCast)
        {
            int safeEmitterCount = System.Math.Max(1, emitterCount);
            List<BattleDebugEmitterSpec> specs = new(safeEmitterCount);
            for (int i = 0; i < safeEmitterCount; i++)
            {
                specs.Add(new BattleDebugEmitterSpec
                {
                    CooldownMs = emitterCooldownMs,
                    Range = emitterRange,
                    CanMoveCast = emitterCanMoveCast,
                    PayloadType = BattleAttackPayloadType.VehicleBuff,
                    BuffGroupIds = new List<int> { 61021 },
                });
            }

            return SpawnMonster(battle, hp, atk, def, speed, count, offsetFromCamera, spreadRange, specs);
        }

        /// <summary>
        /// 在战斗中生成怪物，并按规格挂载一组调试发射器。
        /// </summary>
        public static List<BattleUnit> SpawnMonster(
            Battle battle,
            int hp,
            int atk,
            int def,
            float speed,
            int count,
            float offsetFromCamera,
            float spreadRange,
            IReadOnlyList<BattleDebugEmitterSpec> emitterSpecs)
        {
            List<BattleUnit> spawnedUnits = new();
            if (battle == null || battle.State != BattleState.Fighting) return spawnedUnits;

            // 基于相机右边界偏移生成，怪物始终出现在屏幕右侧外
            float cameraRight = BattleCameraHelper.GetCameraRightBound();
            float centerX = cameraRight + offsetFromCamera;
            int emitterCount = System.Math.Max(1, emitterSpecs?.Count ?? 0);

            for (int i = 0; i < count; i++)
            {
                long unitId = IdGenerater.Instance.GenerateInstanceId();
                float offsetX = (UnityEngine.Random.Range(0f, 1f) * 2f - 1f) * spreadRange;
                float posX = centerX + offsetX;

                BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, 0);
                unit.Camp = UnitCamp.Enemy;
                unit.Position = new float3(posX, BattleAreaConfig.BattleUnitSpawnY, 0);
                unit.Forward = new float3(-1f, 0, 0);
                unit.FaceDirection = -1f;

                NumericComponent numeric = unit.AddComponent<NumericComponent>();
                numeric.Set(NumericType.Hp, hp);
                numeric.Set(NumericType.MaxHp, hp);
                numeric.Set(NumericType.Attack, atk);
                numeric.Set(NumericType.Defense, def);
                numeric.Set(NumericType.Speed, speed);
                unit.GetOrCreateBattleStats().SetCore(hp, hp, atk, def, speed, false);

                BattleAttackComponent battleAttack = unit.GetComponent<BattleAttackComponent>() ?? unit.AddComponent<BattleAttackComponent>();
                battleAttack.Attacks.Clear();
                float maxAttackRange = 0f;
                for (int emitterIndex = 0; emitterIndex < emitterCount; emitterIndex++)
                {
                    BattleDebugEmitterSpec spec = emitterSpecs != null && emitterIndex < emitterSpecs.Count
                        ? emitterSpecs[emitterIndex]
                        : null;
                    float range = spec?.Range ?? 3f;
                    if (range > maxAttackRange)
                    {
                        maxAttackRange = range;
                    }

                    battleAttack.AddAttack(new BattleAttackRuntime
                    {
                        AttackRuntimeId = IdGenerater.Instance.GenerateInstanceId(),
                        CooldownMs = spec?.CooldownMs ?? 1000,
                        AttackRange = range,
                        CanMoveCast = spec?.CanMoveCast ?? false,
                        DeliveryType = BattleAttackDeliveryType.Instant,
                        PayloadType = spec?.PayloadType ?? BattleAttackPayloadType.VehicleBuff,
                        BuffGroupIds = spec?.BuffGroupIds != null ? new List<int>(spec.BuffGroupIds) : new List<int>(),
                    });
                }

                unit.AddComponent<BattleUnitCombatComponent, float>(maxAttackRange > 0f ? maxAttackRange : 3f);
                unit.AddComponent<ClientMinionAIComponent>();

                BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
                view.InitViewAsync().Forget();
                spawnedUnits.Add(unit);
            }

            if (battle.GetComponent<ClientMinionAITickComponent>() == null)
            {
                battle.AddComponent<ClientMinionAITickComponent>();
            }

            Log.Info($"BattleDebugSpawn: {count}x monsters (HP:{hp} ATK:{atk} DEF:{def} SPD:{speed}, Emitters:{emitterCount})");
            return spawnedUnits;
        }
    }
}
