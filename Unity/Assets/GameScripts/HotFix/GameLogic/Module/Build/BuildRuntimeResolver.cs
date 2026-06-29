using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 将玩家局内构筑解析成战斗系统可直接执行的发射器运行时。
    /// </summary>
    public static class BuildRuntimeResolver
    {
        public static BuildRuntime Resolve(VehicleComponent vehicleComponent)
        {
            BuildRuntime buildRuntime = new();
            if (vehicleComponent == null)
            {
                return buildRuntime;
            }

            List<VehicleData> equippedVehicles = vehicleComponent.GetEquippedVehicles();
            foreach (VehicleData vehicle in equippedVehicles)
            {
                EmitterRuntime emitterRuntime = ResolveEmitter(vehicle);
                if (emitterRuntime != null)
                {
                    buildRuntime.Emitters.Add(emitterRuntime);
                }
            }

            return buildRuntime;
        }

        private static EmitterRuntime ResolveEmitter(VehicleData vehicle)
        {
            if (vehicle == null)
            {
                return null;
            }

            List<int> buffGroupIds = new();
            EmitterEffectPackRuntimeHelper.BuildRuntimeStats(
                vehicle,
                out int cooldownMs,
                out float attackRange,
                out float whiteDamageMultiplier,
                out float baseDamage,
                buffGroupIds);

            return new EmitterRuntime
            {
                RuntimeId = vehicle.VehicleId,
                EmitterConfigId = vehicle.VehicleConfigId,
                Level = System.Math.Max(1, vehicle.Level),
                BuffSlotCount = System.Math.Max(0, vehicle.BuffSlotCount),
                CooldownMs = cooldownMs,
                AttackRange = attackRange,
                AttackHitRatio = ResolveAttackHitRatio(vehicle.VehicleConfigId, vehicle.AttackHitRatio),
                BaseDamage = baseDamage,
                WhiteAttackRatio = vehicle.WhiteAttackRatio,
                WhiteDamageMultiplier = whiteDamageMultiplier,
                CanMoveCast = vehicle.CanMoveCast,
                DeliveryType = BattleAttackDeliveryType.Instant,
                PayloadType = BattleAttackPayloadType.VehicleBuff,
                EffectPackIds = vehicle.SlottedEffectPackIds != null ? new List<int>(vehicle.SlottedEffectPackIds) : new List<int>(),
                BuffGroupIds = buffGroupIds,
            };
        }

        private static float ResolveAttackHitRatio(int emitterConfigId, float fallbackRatio)
        {
            EmitterConfig emitterConfig = ConfigHelper.EmitterConfig?.GetOrDefault(emitterConfigId);
            return NormalizeAttackHitRatio(emitterConfig != null ? emitterConfig.AttackHitRatio : fallbackRatio);
        }

        private static float NormalizeAttackHitRatio(float ratio)
        {
            if (ratio <= 0f || ratio > 1f)
            {
                return 0.5f;
            }

            return ratio;
        }
    }
}
