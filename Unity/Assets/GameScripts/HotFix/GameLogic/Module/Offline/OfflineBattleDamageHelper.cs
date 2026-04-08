namespace ET
{
    /// <summary>
    /// 离线伤害计算器 - 复制服务端 BattleSkillHelper 的伤害公式
    /// 供离线模式下玩家技能伤害本地结算使用
    /// </summary>
    public static class OfflineBattleDamageHelper
    {
        private const int FormulaTypeAttackMinusDefense = 1;
        private const int EffectTypeDamage = 1;
        private const int EffectTypeHeal = 4;

        public struct DamageResult
        {
            public int TotalDamage;
            public bool TargetDied;
        }

        /// <summary>
        /// 对目标施放技能效果，返回伤害结果
        /// 只处理 CastType=0（即时）技能
        /// </summary>
        public static DamageResult ApplySkillEffects(BattleUnit caster, BattleUnit target, int skillId)
        {
            SkillConfig skillConfig = ConfigHelper.SkillConfig?.GetOrDefault(skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return default;
            }

            if (skillConfig.CastType != 0)
            {
                return default;
            }

            BuffGroupConfig effectGroup = skillConfig.BuffGroupId_Ref;
            if (effectGroup == null)
            {
                return default;
            }

            int totalDamage = 0;
            bool targetDied = false;

            foreach (int effectId in effectGroup.EffectIds)
            {
                BuffConfig effectConfig = ConfigHelper.SkillEffectConfig?.GetOrDefault(effectId);
                if (effectConfig == null)
                {
                    continue;
                }

                switch (effectConfig.EffectType)
                {
                    case EffectTypeDamage:
                    {
                        bool wasAlive = !target.IsDead;
                        int damage = CalculateDamage(caster, target, effectConfig);

                        BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
                        if (combatComp != null)
                        {
                            combatComp.TakeDamage(damage);
                        }

                        EventSystem.Instance.Publish(target.Scene(), new BattleUnitDamaged
                        {
                            Unit = target,
                            Damage = damage,
                        });

                        targetDied = wasAlive && target.IsDead;
                        totalDamage += damage;
                        break;
                    }
                    case EffectTypeHeal:
                    {
                        int healAmount = (int)effectConfig.BaseValue;
                        if (healAmount > 0)
                        {
                            BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
                            combatComp?.Heal(healAmount);
                        }
                        break;
                    }
                }
            }

            return new DamageResult { TotalDamage = totalDamage, TargetDied = targetDied };
        }

        private static int CalculateDamage(BattleUnit caster, BattleUnit target, BuffConfig effectConfig)
        {
            NumericComponent attackerNumeric = caster.GetComponent<NumericComponent>();
            NumericComponent targetNumeric = target.GetComponent<NumericComponent>();

            float damageValue = effectConfig.BaseValue;

            if (effectConfig.FormulaType == FormulaTypeAttackMinusDefense)
            {
                int attack = attackerNumeric?.GetAsInt(NumericType.Attack) ?? 0;
                int defense = targetNumeric?.GetAsInt(NumericType.Defense) ?? 0;
                damageValue += attack * effectConfig.RatioAtk - defense * effectConfig.RatioDef;
            }

            int damage = (int)System.Math.Floor(damageValue);
            if (damage < effectConfig.MinValue)
            {
                damage = effectConfig.MinValue;
            }

            if (effectConfig.MaxValue > 0 && damage > effectConfig.MaxValue)
            {
                damage = effectConfig.MaxValue;
            }

            return damage;
        }
    }
}
