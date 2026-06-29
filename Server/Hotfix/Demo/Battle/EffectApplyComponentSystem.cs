using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(EffectApplyComponent))]
    [FriendOf(typeof(EffectApplyComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class EffectApplyComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EffectApplyComponent self)
        {
        }
        
        [EntitySystem]
        private static void Destroy(this EffectApplyComponent self)
        {
        }
        
        /// <summary>
        /// 应用技能效果组
        /// </summary>
        public static List<EffectResult> ApplyEffects(this EffectApplyComponent self, 
            BattleUnit caster, BattleUnit target, BuffGroupConfig buffGroup, int skillId)
        {
            List<EffectResult> results = new();
            
            if (buffGroup == null || target == null || target.IsDead)
            {
                return results;
            }
            
            foreach (int buffId in buffGroup.BuffIds)
            {
                BuffConfig effectConfig = BuffConfigCategory.Instance.GetOrDefault(buffId);
                if (effectConfig == null)
                {
                    continue;
                }
                
                EffectResult result = self.ApplySingleEffect(caster, target, effectConfig, skillId);
                results.Add(result);
            }
            
            return results;
        }
        
        /// <summary>
        /// 应用单个效果
        /// </summary>
        private static EffectResult ApplySingleEffect(this EffectApplyComponent self,
            BattleUnit caster, BattleUnit target, BuffConfig effectConfig, int skillId)
        {
            EffectResult result = new EffectResult
            {
                EffectType = effectConfig.EffectType,
                Success = false
            };
            
            switch (effectConfig.EffectType)
            {
                case (int)EffectType.Damage:
                {
                    int damage = CalculateDamage(caster, target, effectConfig);
                    result.Value = damage;
                    result.Success = damage > 0;
                    
                    // 发布伤害事件
                    self.PublishDamageEvent(caster, target, damage, skillId);
                    break;
                }
                case (int)EffectType.Freeze:
                {
                    int durationMs = (int)effectConfig.BaseValue;
                    result.Value = durationMs;
                    result.Success = durationMs > 0;
                    
                    // 发布冻结事件
                    self.PublishFreezeEvent(target, caster?.Id ?? 0, durationMs);
                    break;
                }
                case (int)EffectType.Knockback:
                {
                    float distance = effectConfig.BaseValue;
                    float direction = caster != null 
                        ? (caster.Position.X <= target.Position.X ? 1f : -1f) 
                        : 1f;
                    result.FloatValue = distance;
                    result.Success = distance > 0;
                    
                    // 发布击退事件
                    self.PublishKnockbackEvent(target, caster, distance, direction);
                    break;
                }
                case (int)EffectType.Heal:
                {
                    int healAmount = (int)effectConfig.BaseValue;
                    result.Value = healAmount;
                    result.Success = healAmount > 0;
                    
                    // 治疗直接应用
                    target.Heal(healAmount);
                    break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 计算伤害
        /// </summary>
        private static int CalculateDamage(BattleUnit caster, BattleUnit target, BuffConfig effectConfig)
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
        
        /// <summary>
        /// 发布伤害事件
        /// </summary>
        private static void PublishDamageEvent(this EffectApplyComponent self, 
            BattleUnit attacker, BattleUnit target, int damage, int skillId)
        {
            var evt = new DamageEvent
            {
                Attacker = attacker,
                Target = target,
                Damage = damage,
                SkillId = skillId,
                DamageType = 0
            };
            EventSystem.Instance.Publish(target.Root(), evt);
        }
        
        /// <summary>
        /// 发布冻结事件
        /// </summary>
        private static void PublishFreezeEvent(this EffectApplyComponent self, 
            BattleUnit target, long sourceId, int durationMs)
        {
            var evt = new FreezeEvent
            {
                Target = target,
                SourceId = sourceId,
                DurationMs = durationMs
            };
            EventSystem.Instance.Publish(target.Root(), evt);
        }
        
        /// <summary>
        /// 发布击退事件
        /// </summary>
        private static void PublishKnockbackEvent(this EffectApplyComponent self,
            BattleUnit target, BattleUnit attacker, float distance, float direction)
        {
            var evt = new KnockbackEvent
            {
                Target = target,
                Attacker = attacker,
                Distance = distance,
                Direction = direction
            };
            EventSystem.Instance.Publish(target.Root(), evt);
        }
    }
}
