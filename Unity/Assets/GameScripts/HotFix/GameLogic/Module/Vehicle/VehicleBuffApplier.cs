using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 载具 Buff 施加器。
    /// 负责将载具镶嵌的所有 Buff 施加到目标身上。
    /// 规则：叠加层数、刷新持续时间、按来源载具分别计算。
    /// </summary>
    public static class VehicleBuffApplier
    {
        private const int EffectTypeDamage = 1;
        private const int EffectTypeFreeze = 2;
        private const int EffectTypeKnockback = 3;
        private const int EffectTypeHeal = 4;
        private const int EffectTypeStun = 5;
        private const int EffectTypeSlowDown = 6;
        private const int EffectTypeLifeSteal = 7;
        private const int EffectTypeShield = 8;
        private const int EffectTypeAttackBuff = 9;
        private const int EffectTypeDefenseBuff = 10;
        private const int EffectTypeDOT = 11;

        private const int FormulaTypeAttackMinusDefense = 1;

        /// <summary>
        /// 对目标施加载具的所有镶嵌 Buff。
        /// </summary>
        public static void Apply(Battle battle, BattleUnit caster, VehicleData vehicle, BattleUnit target)
        {
            if (vehicle == null || target == null || target.IsDead) return;

            // 去重：同 EffectType 取 Duration 最长的
            Dictionary<int, int> effectTypeToBuffId = new();
            foreach (int buffGroupId in vehicle.SlottedBuffIds)
            {
                if (buffGroupId == 0) continue;

                BuffGroupConfig buffGroup = ConfigHelper.BuffGroupConfig?.GetOrDefault(buffGroupId);
                if (buffGroup == null || buffGroup.BuffIds == null || buffGroup.BuffIds.Length == 0)
                {
                    Log.Warning($"[VehicleBuffApplier] Ignore invalid buffGroupId={buffGroupId}, group missing or empty.");
                    continue;
                }

                foreach (int buffId in buffGroup.BuffIds)
                {
                    if (buffId == 0)
                    {
                        continue;
                    }

                    BuffConfig config = ConfigHelper.BuffConfigCategory?.GetOrDefault(buffId);
                    if (config == null)
                    {
                        continue;
                    }

                    if (!effectTypeToBuffId.ContainsKey(config.EffectType))
                    {
                        effectTypeToBuffId[config.EffectType] = buffId;
                    }
                    else
                    {
                        // 同 EffectType 取 Duration 较长者
                        BuffConfig existing = ConfigHelper.BuffConfigCategory?.GetOrDefault(effectTypeToBuffId[config.EffectType]);
                        if (existing != null && config.Duration > existing.Duration)
                        {
                            effectTypeToBuffId[config.EffectType] = buffId;
                        }
                    }
                }
            }

            bool hasDamageLikeEffect = false;
            // 逐个执行
            foreach (var kv in effectTypeToBuffId)
            {
                BuffConfig config = ConfigHelper.BuffConfigCategory?.GetOrDefault(kv.Value);
                if (config == null) continue;
                if (config.EffectType == EffectTypeDamage || config.EffectType == EffectTypeLifeSteal || config.EffectType == EffectTypeDOT)
                {
                    hasDamageLikeEffect = true;
                }

                ExecuteBuff(battle, caster, vehicle, target, kv.Value, config);
            }

            if (!hasDamageLikeEffect && effectTypeToBuffId.Count > 0)
            {
                Log.Warning($"[VehicleBuffApplier] No damage-like effect. vehicleId={vehicle.VehicleId} slotBuffIds=[{string.Join(",", vehicle.SlottedBuffIds)}] effectTypes=[{string.Join(",", effectTypeToBuffId.Keys)}]");
            }
        }

        private static void ExecuteBuff(Battle battle, BattleUnit caster, VehicleData vehicle, BattleUnit target, int buffId, BuffConfig config)
        {
            switch (config.EffectType)
            {
                case EffectTypeDamage:
                    ApplyDamage(caster, target, config);
                    break;

                case EffectTypeDOT:
                    ApplyStackingBuff(battle, target, vehicle.VehicleId, buffId, config);
                    break;

                case EffectTypeSlowDown:
                    ApplyStackingBuff(battle, target, vehicle.VehicleId, buffId, config);
                    break;

                case EffectTypeFreeze:
                case EffectTypeStun:
                    ApplyStackingBuff(battle, target, vehicle.VehicleId, buffId, config);
                    break;

                case EffectTypeHeal:
                    ApplyHeal(caster, config);
                    break;

                case EffectTypeLifeSteal:
                    ApplyLifeSteal(caster, target, config);
                    break;

                case EffectTypeAttackBuff:
                case EffectTypeDefenseBuff:
                case EffectTypeShield:
                    ApplyStackingBuff(battle, caster, vehicle.VehicleId, buffId, config);
                    break;
            }
        }

        private static void ApplyDamage(BattleUnit caster, BattleUnit target, BuffConfig config)
        {
            int damage = CalculateValue(caster, target, config);
            if (damage <= 0) return;

            BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
            if (combatComp == null) return;

            combatComp.TakeDamage(damage);
            EventSystem.Instance.Publish(target.Scene(), new BattleUnitDamaged
            {
                Unit = target,
                AttackerId = caster.Id,
                Damage = damage,
                IsCrit = false,
            });
        }

        private static void ApplyHeal(BattleUnit caster, BuffConfig config)
        {
            int healAmount = (int)config.BaseValue;
            if (healAmount <= 0) return;

            BattleUnitCombatComponent combatComp = caster.GetComponent<BattleUnitCombatComponent>();
            if (combatComp == null) return;

            combatComp.Heal(healAmount);
        }

        private static void ApplyLifeSteal(BattleUnit caster, BattleUnit target, BuffConfig config)
        {
            int damage = CalculateValue(caster, target, config);
            if (damage <= 0) return;

            BattleUnitCombatComponent targetCombat = target.GetComponent<BattleUnitCombatComponent>();
            if (targetCombat == null) return;

            targetCombat.TakeDamage(damage);
            EventSystem.Instance.Publish(target.Scene(), new BattleUnitDamaged
            {
                Unit = target,
                AttackerId = caster.Id,
                Damage = damage,
                IsCrit = false,
            });

            int healAmount = (int)(damage * config.BaseValue);
            if (healAmount > 0)
            {
                BattleUnitCombatComponent casterCombat = caster.GetComponent<BattleUnitCombatComponent>();
                casterCombat?.Heal(healAmount);
            }
        }

        /// <summary>
        /// 施加可叠加的持续 Buff。
        /// 查找目标身上同 EffectType + 同来源载具ID 的 BuffEntity，找到则层数+1并刷新持续时间。
        /// 未找到则创建新的 BuffEntity。
        /// </summary>
        private static void ApplyStackingBuff(Battle battle, BattleUnit target, long vehicleId, int buffId, BuffConfig config)
        {
            VehicleBuffComponent vehicleBuffComp = target.GetComponent<VehicleBuffComponent>();
            if (vehicleBuffComp == null)
            {
                vehicleBuffComp = target.AddComponent<VehicleBuffComponent>();
            }

            vehicleBuffComp.ApplyVehicleBuff(battle, vehicleId, buffId, config);
        }

        private static int CalculateValue(BattleUnit caster, BattleUnit target, BuffConfig config)
        {
            float value = config.BaseValue;

            switch (config.FormulaType)
            {
                case FormulaTypeAttackMinusDefense:
                {
                    int attack = caster.GetOrCreateBattleStats()?.Attack ?? 0;
                    int defense = target.GetOrCreateBattleStats()?.Defense ?? 0;
                    value += attack * config.RatioAtk - defense * config.RatioDef;
                    break;
                }
            }

            int finalValue = (int)System.Math.Floor(value);
            if (finalValue < config.MinValue) finalValue = config.MinValue;
            if (config.MaxValue > 0 && finalValue > config.MaxValue) finalValue = config.MaxValue;
            return finalValue;
        }
    }
}
