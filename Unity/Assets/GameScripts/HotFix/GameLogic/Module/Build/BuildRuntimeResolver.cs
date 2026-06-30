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
                out float baseDamage,
                out float attackRatio,
                buffGroupIds);

            return new EmitterRuntime
            {
                RuntimeId = vehicle.VehicleId,
                EmitterConfigId = vehicle.VehicleConfigId,
                Level = System.Math.Max(1, vehicle.Level),
                BuffSlotCount = System.Math.Max(0, vehicle.BuffSlotCount),
                CooldownMs = cooldownMs,
                AttackRange = attackRange,
                BaseDamage = baseDamage,
                WhiteAttackRatio = attackRatio,
                CanMoveCast = vehicle.CanMoveCast,
                DeliveryType = BattleAttackDeliveryType.Instant,
                PayloadType = BattleAttackPayloadType.VehicleBuff,
                EffectPackIds = vehicle.SlottedEffectPackIds != null ? new List<int>(vehicle.SlottedEffectPackIds) : new List<int>(),
                BuffGroupIds = buffGroupIds,
            };
        }
    }
}
