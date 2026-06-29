namespace ET
{
    /// <summary>
    /// 通用攻击效果执行器。
    /// 负责把统一攻击定义落成具体伤害或 Buff 效果。
    /// </summary>
    public static class BattleAttackExecutor
    {
        public static void Execute(BattleHitContext context)
        {
            BattleAttackRuntime attack = context?.AttackRuntime;
            BattleUnit attacker = context?.Attacker;
            BattleUnit target = context?.Target;
            if (attack == null || attacker == null || target == null || target.IsDead)
            {
                return;
            }

            switch (attack.PayloadType)
            {
                case BattleAttackPayloadType.VehicleBuff:
                    ExecuteVehicleBuff(context.Battle, attacker, attack, target);
                    break;
            }
        }

        private static void ExecuteVehicleBuff(Battle battle, BattleUnit attacker, BattleAttackRuntime attack, BattleUnit target)
        {
            VehicleData vehicleData = new()
            {
                VehicleId = attack.AttackRuntimeId,
                VehicleConfigId = attack.SourceConfigId,
                Level = System.Math.Max(1, attack.Level),
                AttackCooldownMs = attack.CooldownMs,
                AttackRange = attack.AttackRange,
                BaseDamage = attack.BaseDamage,
                WhiteAttackRatio = attack.WhiteAttackRatio,
                WhiteDamageMultiplier = attack.WhiteDamageMultiplier > 0f ? attack.WhiteDamageMultiplier : 1.0f,
                CanMoveCast = attack.CanMoveCast,
                SlottedBuffIds = new System.Collections.Generic.List<int>(attack.BuffGroupIds),
            };

            ApplyBaseDamage(attacker, target, vehicleData);
            VehicleBuffApplier.Apply(battle, attacker, vehicleData, target);
        }

        private static void ApplyBaseDamage(BattleUnit attacker, BattleUnit target, VehicleData vehicle)
        {
            if (attacker == null || target == null || vehicle == null || target.IsDead)
            {
                return;
            }

            if (vehicle.BaseDamage <= 0f && vehicle.WhiteAttackRatio <= 0f)
            {
                return;
            }

            float rawDamage = vehicle.BaseDamage;
            int attack = attacker.GetOrCreateBattleStats()?.Attack ?? 0;
            int defense = target.GetOrCreateBattleStats()?.Defense ?? 0;
            rawDamage += attack * vehicle.WhiteAttackRatio - defense;

            int damage = (int)System.Math.Floor(System.Math.Max(0f, rawDamage) * (vehicle.WhiteDamageMultiplier > 0f ? vehicle.WhiteDamageMultiplier : 1.0f));
            if (damage <= 0)
            {
                return;
            }

            BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
            if (combatComp == null)
            {
                return;
            }

            combatComp.TakeDamage(damage);
            EventSystem.Instance.Publish(target.Scene(), new BattleUnitDamaged
            {
                Unit = target,
                AttackerId = attacker.Id,
                Damage = damage,
                IsCrit = false,
            });
        }
    }
}
