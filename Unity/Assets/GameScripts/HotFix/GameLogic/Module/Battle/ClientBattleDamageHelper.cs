using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 客户端战斗伤害辅助类 - 杂兵系统
    /// 在客户端执行本地碰撞检测和伤害结算（客户端权威路径），
    /// 同时发送 C2M_ClientBatchHit 给服务端做验证。
    /// </summary>
    public static class ClientBattleDamageHelper
    {
        private const int FormulaTypeAttackMinusDefense = 1;
        private const int EffectTypeDamage = 1;
        private const int SelectTypeAllEnemiesInRange = 3;

        /// <summary>
        /// 在玩家释放技能时，本地执行命中检测和伤害结算，并发送命中包给服务端。
        /// 只对非Boss敌军（杂兵）生效，Boss走服务端权威路径。
        /// </summary>
        public static void ApplySkillOnMinions(Battle battle, BattleUnit caster, int skillId)
        {
            if (battle == null || caster == null || caster.IsDead)
            {
                return;
            }

            SkillConfig skillConfig = ConfigHelper.SkillConfig?.GetOrDefault(skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return;
            }

            SkillTargetingConfig targetingConfig = skillConfig.TargetingConfigId_Ref;
            BuffGroupConfig effectGroupConfig = skillConfig.BuffGroupId_Ref;
            if (targetingConfig == null || effectGroupConfig == null)
            {
                return;
            }

            float castRange = targetingConfig.CastRange + targetingConfig.EdgeDistance;

            // 客户端本地碰撞检测：找出射程内的非Boss敌军
            List<long> hitUnitIds = new List<long>();

            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit target) continue;
                if (target.IsDead) continue;
                if (target.Camp == caster.Camp) continue;
                if (target.IsBoss) continue;

                float distance = math.abs(caster.Position.x - target.Position.x);
                if (distance > castRange)
                {
                    continue;
                }

                // AOE类型技能（SelectType=3）命中范围内所有杂兵，否则只命中主目标
                if (targetingConfig.SelectType != SelectTypeAllEnemiesInRange && hitUnitIds.Count > 0)
                {
                    continue;
                }

                int damage = CalculateDamage(caster, target, effectGroupConfig);
                if (damage <= 0) continue;

                hitUnitIds.Add(target.Id);

                // 本地立刻扣血
                BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
                if (combatComp != null)
                {
                    combatComp.TakeDamage(damage);
                }

                // 本地发布受击事件（驱动受击特效、飘字等）
                EventSystem.Instance.Publish(battle.Root(), new BattleUnitDamaged
                {
                    Unit = target,
                    AttackerId = caster.Id,
                    Damage = damage,
                    IsCrit = false,
                });

                // 限制最大目标数
                if (targetingConfig.MaxTargetCount > 0 && hitUnitIds.Count >= targetingConfig.MaxTargetCount)
                {
                    break;
                }
            }

            // 发送命中包给服务端验证
            if (hitUnitIds.Count > 0)
            {
                SendClientBatchHit(battle, caster, skillId, hitUnitIds, castRange);
            }
        }

        /// <summary>
        /// 发送 C2M_ClientBatchHit 给服务端
        /// </summary>
        private static void SendClientBatchHit(Battle battle, BattleUnit caster, int skillId,
            List<long> hitUnitIds, float castRange)
        {
            C2M_ClientBatchHit message = C2M_ClientBatchHit.Create();
            message.battleId = battle.BattleId;
            message.skillId = skillId;
            message.casterId = caster.Id;
            message.hitUnitIds = hitUnitIds;

            float faceDir = caster.FaceDirection;
            float casterX = caster.Position.x;
            message.hitBoxMinX = faceDir >= 0f ? casterX : casterX - castRange;
            message.hitBoxMaxX = faceDir >= 0f ? casterX + castRange : casterX;

            long nowMs = TimeInfo.Instance.ClientNow();
            message.hitStartTick = nowMs;
            message.hitEndTick = nowMs;

            Log.Debug($"[ClientBattleDamage] SendClientBatchHit: skillId={skillId} hitCount={hitUnitIds.Count} hitUnitIds=[{string.Join(",", hitUnitIds)}]");
            battle.Root().GetComponent<ClientSenderComponent>()?.Send(message);
        }

        /// <summary>
        /// 计算技能对目标的伤害（复制服务端 BattleSkillHelper 的伤害公式）
        /// </summary>
        private static int CalculateDamage(BattleUnit caster, BattleUnit target, BuffGroupConfig effectGroupConfig)
        {
            int totalDamage = 0;

            foreach (int effectId in effectGroupConfig.EffectIds)
            {
                BuffConfig effectConfig = ConfigHelper.SkillEffectConfig?.GetOrDefault(effectId);
                if (effectConfig == null) continue;

                if (effectConfig.EffectType != EffectTypeDamage) continue;

                float damageValue = effectConfig.BaseValue;

                if (effectConfig.FormulaType == FormulaTypeAttackMinusDefense)
                {
                    int attack = caster.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Attack) ?? 0;
                    int defense = target.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Defense) ?? 0;
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

                totalDamage += damage;
            }

            return totalDamage;
        }
    }
}
