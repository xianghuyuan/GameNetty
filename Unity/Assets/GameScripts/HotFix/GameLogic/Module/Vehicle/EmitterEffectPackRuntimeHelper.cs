using System.Collections.Generic;

namespace ET
{
    public static class EmitterEffectPackRuntimeHelper
    {
        private const int EffectKindModifyEmitterStat = 1;
        private const int EffectKindApplyBuffGroup = 2;

        private const int TargetStatCooldownMs = 1;
        private const int TargetStatRange = 2;
        private const int TargetStatWhiteDamage = 3;
        private const int TargetStatBaseDamage = 4;

        private const int ModifyOpAdd = 1;
        private const int ModifyOpMultiply = 2;
        private const int ModifyOpOverride = 3;

        public static void BuildRuntimeStats(
            VehicleData vehicle,
            out int cooldownMs,
            out float attackRange,
            out float whiteDamageMultiplier,
            out float baseDamage,
            List<int> buffGroupIds)
        {
            cooldownMs = 1000;
            attackRange = 2.0f;
            whiteDamageMultiplier = 1.0f;
            baseDamage = 0f;

            if (vehicle == null)
            {
                return;
            }

            float runtimeCooldownMs = vehicle.AttackCooldownMs > 0 ? vehicle.AttackCooldownMs : 1000;
            float runtimeAttackRange = vehicle.AttackRange > 0f ? vehicle.AttackRange : 2.0f;
            float runtimeWhiteDamageMultiplier = vehicle.WhiteDamageMultiplier > 0f ? vehicle.WhiteDamageMultiplier : 1.0f;
            float runtimeBaseDamage = vehicle.BaseDamage;

            buffGroupIds?.Clear();
            List<int> packIds = vehicle.SlottedEffectPackIds;
            if (packIds == null || packIds.Count == 0)
            {
                if (vehicle.SlottedBuffIds != null && buffGroupIds != null)
                {
                    buffGroupIds.AddRange(vehicle.SlottedBuffIds);
                }

            }
            else
            {
                foreach (int packId in packIds)
                {
                    if (packId == 0)
                    {
                        continue;
                    }

                    EmitterEffectPackConfig packConfig = ConfigHelper.EmitterEffectPackConfig?.GetOrDefault(packId);
                    if (packConfig?.EffectIds == null)
                    {
                        Log.Warning($"[EmitterEffectPack] Ignore invalid effectPackId={packId}.");
                        continue;
                    }

                    foreach (int effectId in packConfig.EffectIds)
                    {
                        EmitterEffectConfig effectConfig = ConfigHelper.EmitterEffectConfig?.GetOrDefault(effectId);
                        if (effectConfig == null)
                        {
                            Log.Warning($"[EmitterEffectPack] Ignore invalid effectId={effectId}, packId={packId}.");
                            continue;
                        }

                        switch (effectConfig.EffectKind)
                        {
                            case EffectKindModifyEmitterStat:
                                ApplyEmitterStatEffect(effectConfig, ref runtimeCooldownMs, ref runtimeAttackRange, ref runtimeWhiteDamageMultiplier, ref runtimeBaseDamage);
                                break;
                            case EffectKindApplyBuffGroup:
                                if (effectConfig.BuffGroupId > 0 && buffGroupIds != null)
                                {
                                    buffGroupIds.Add(effectConfig.BuffGroupId);
                                }
                                break;
                        }
                    }
                }
            }

            cooldownMs = System.Math.Max(100, (int)System.Math.Round(runtimeCooldownMs));
            attackRange = System.Math.Max(0.1f, runtimeAttackRange);
            whiteDamageMultiplier = System.Math.Max(0.1f, runtimeWhiteDamageMultiplier);
            baseDamage = System.Math.Max(0f, runtimeBaseDamage);
        }

        public static List<int> ResolveBuffGroupIds(IReadOnlyList<int> packIds)
        {
            List<int> buffGroupIds = new();
            if (packIds == null)
            {
                return buffGroupIds;
            }

            foreach (int packId in packIds)
            {
                EmitterEffectPackConfig packConfig = ConfigHelper.EmitterEffectPackConfig?.GetOrDefault(packId);
                if (packConfig?.EffectIds == null)
                {
                    continue;
                }

                foreach (int effectId in packConfig.EffectIds)
                {
                    EmitterEffectConfig effectConfig = ConfigHelper.EmitterEffectConfig?.GetOrDefault(effectId);
                    if (effectConfig != null && effectConfig.EffectKind == EffectKindApplyBuffGroup && effectConfig.BuffGroupId > 0)
                    {
                        buffGroupIds.Add(effectConfig.BuffGroupId);
                    }
                }
            }

            return buffGroupIds;
        }

        private static void ApplyEmitterStatEffect(EmitterEffectConfig effectConfig, ref float cooldownMs, ref float attackRange, ref float whiteDamageMultiplier, ref float baseDamage)
        {
            switch (effectConfig.TargetStat)
            {
                case TargetStatCooldownMs:
                    cooldownMs = Modify(cooldownMs, effectConfig.ModifyOp, effectConfig.Value);
                    break;
                case TargetStatRange:
                    attackRange = Modify(attackRange, effectConfig.ModifyOp, effectConfig.Value);
                    break;
                case TargetStatWhiteDamage:
                    whiteDamageMultiplier = Modify(whiteDamageMultiplier, effectConfig.ModifyOp, effectConfig.Value);
                    break;
                case TargetStatBaseDamage:
                    baseDamage = Modify(baseDamage, effectConfig.ModifyOp, effectConfig.Value);
                    break;
            }
        }

        private static float Modify(float current, int op, float value)
        {
            return op switch
            {
                ModifyOpAdd => current + value,
                ModifyOpMultiply => current * value,
                ModifyOpOverride => value,
                _ => current,
            };
        }
    }
}
