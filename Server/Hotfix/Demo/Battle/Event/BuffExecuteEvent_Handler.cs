namespace ET.Server
{
    /// <summary>
    /// Buff执行事件处理器 - 处理持续效果的 tick 触发（冻结、治疗、DoT等）。
    /// BuffComponentSystem 通过发布 BuffExecuteEvent 解耦，避免静态类环形依赖。
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    public class BuffExecuteEvent_Handler : AEvent<Scene, BuffExecuteEvent>
    {
        protected override async ETTask Run(Scene scene, BuffExecuteEvent args)
        {
            BattleUnit target = args.Target;
            BuffEntity buffEntity = args.BuffEntity;

            if (target == null || buffEntity == null || buffEntity.Config == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            switch (buffEntity.Config.EffectType)
            {
                case (int)EffectType.Damage:
                {
                    if (buffEntity.CasterId > 0)
                    {
                        BattleRoom battleRoom = target.GetParent<BattleRoom>();
                        BattleUnit caster = battleRoom?.GetUnit(buffEntity.CasterId);

                        int damage = CalculateBuffDamage(caster, target, buffEntity.Config);
                        EventSystem.Instance.Publish(target.Root(), new DamageEvent
                        {
                            Attacker = caster,
                            Target = target,
                            Damage = damage,
                            SkillId = buffEntity.SkillId,
                            CasterId = buffEntity.CasterId,
                        });
                    }
                    break;
                }
                case (int)EffectType.DOT:
                {
                    if (buffEntity.CasterId > 0)
                    {
                        BattleRoom battleRoom = target.GetParent<BattleRoom>();
                        BattleUnit caster = battleRoom?.GetUnit(buffEntity.CasterId);

                        int damage = CalculateBuffDamage(caster, target, buffEntity.Config);
                        EventSystem.Instance.Publish(target.Root(), new DamageEvent
                        {
                            Attacker = caster,
                            Target = target,
                            Damage = damage,
                            SkillId = buffEntity.SkillId,
                            CasterId = buffEntity.CasterId,
                        });
                    }
                    break;
                }
                case (int)EffectType.Freeze:
                {
                    FreezeComponent freeze = target.GetComponent<FreezeComponent>();
                    freeze?.ApplyFreeze((int)buffEntity.Config.BaseValue);
                    break;
                }
                case (int)EffectType.Stun:
                {
                    FreezeComponent freeze = target.GetComponent<FreezeComponent>();
                    freeze?.ApplyFreeze((int)buffEntity.Config.BaseValue);
                    break;
                }
                case (int)EffectType.Heal:
                {
                    int healAmount = (int)buffEntity.Config.BaseValue;
                    if (healAmount > 0)
                    {
                        target.Heal(healAmount);
                    }
                    break;
                }
                case (int)EffectType.Knockback:
                {
                    BattleRoom battleRoom = target.GetParent<BattleRoom>();
                    BattleUnit caster = battleRoom?.GetUnit(buffEntity.CasterId);
                    float distance = buffEntity.Config.BaseValue;
                    float direction = caster != null
                        ? (caster.Position.X <= target.Position.X ? 1f : -1f)
                        : 1f;

                    if (distance > 0)
                    {
                        EventSystem.Instance.Publish(target.Root(), new KnockbackEvent
                        {
                            Target = target,
                            Attacker = caster,
                            Distance = distance,
                            Direction = direction,
                            CasterId = buffEntity.CasterId,
                        });
                    }
                    break;
                }
                case (int)EffectType.SlowDown:
                {
                    SlowDownComponent slowComp = target.GetComponent<SlowDownComponent>();
                    float slowPercent = buffEntity.Config.BaseValue;
                    int slowDuration = buffEntity.Config.Duration > 0 ? buffEntity.Config.Duration : 2000;
                    slowComp?.ApplySlow(slowPercent, slowDuration);
                    break;
                }
            }

            await ETTask.CompletedTask;
        }

        private static int CalculateBuffDamage(BattleUnit caster, BattleUnit target, BuffConfig effectConfig)
        {
            BattleStatsComponent attackerStats = caster?.GetOrCreateBattleStats();
            BattleStatsComponent targetStats = target?.GetOrCreateBattleStats();

            float damageValue = effectConfig.BaseValue;

            const int FormulaTypeAttackMinusDefense = 1;

            switch (effectConfig.FormulaType)
            {
                case FormulaTypeAttackMinusDefense:
                {
                    int attack = attackerStats?.Attack ?? 0;
                    int defense = targetStats?.Defense ?? 0;
                    damageValue += attack * effectConfig.RatioAtk - defense * effectConfig.RatioDef;
                    break;
                }
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
