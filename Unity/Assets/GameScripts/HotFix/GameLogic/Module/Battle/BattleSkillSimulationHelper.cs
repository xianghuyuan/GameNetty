using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 战斗技能模拟器。
    /// 统一负责本地目标筛选、效果数值计算与本地结果落地。
    /// </summary>
    public static class BattleSkillSimulationHelper
    {
        private const int SelectTypeLockedTarget = 1;
        private const int SelectTypeNearestEnemy = 2;
        private const int SelectTypeAllEnemiesInRange = 3;

        private const int TargetCampEnemy = 1;
        private const int TargetCampAlly = 2;
        private const int TargetCampSelf = 3;
        private const int TargetCampAny = 4;

        public sealed class SkillSimulation
        {
            public int SkillId { get; set; }
            public EmitterConfig EmitterConfig { get; set; }
            public SkillTargetingConfig TargetingConfig { get; set; }
            public float CastRange { get; set; }
            public List<PredictedTargetImpact> Impacts { get; } = new List<PredictedTargetImpact>();
        }

        public struct PredictedTargetImpact
        {
            public long TargetId;
            public bool IsBoss;
            public int TotalDamage;
            public int TotalHeal;
        }

        public static bool TrySimulate(Battle battle, BattleUnit caster, int skillId, long explicitTargetId, out SkillSimulation simulation)
        {
            simulation = null;

            if (battle == null || caster == null || caster.IsDead)
            {
                return false;
            }

            EmitterConfig skillConfig = ConfigHelper.EmitterConfig?.GetOrDefault(skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return false;
            }

            SkillTargetingConfig targetingConfig = ConfigHelper.SkillTargetingConfig?.GetOrDefault(skillConfig.TargetingConfigId);
            if (targetingConfig == null)
            {
                return false;
            }

            simulation = new SkillSimulation
            {
                SkillId = skillId,
                EmitterConfig = skillConfig,
                TargetingConfig = targetingConfig,
                CastRange = targetingConfig.CastRange + targetingConfig.EdgeDistance,
            };

            List<BattleUnit> targets = SelectTargets(battle, caster, targetingConfig, explicitTargetId, simulation.CastRange);
            foreach (BattleUnit target in targets)
            {
                PredictedTargetImpact impact = BuildImpact(caster, target, skillConfig);
                if (impact.TotalDamage <= 0 && impact.TotalHeal <= 0)
                {
                    continue;
                }

                simulation.Impacts.Add(impact);
            }

            return true;
        }

        public static void ApplyLocalImpacts(Battle battle, BattleUnit caster, SkillSimulation simulation, bool includeBossTargets)
        {
            if (battle == null || caster == null || simulation == null)
            {
                return;
            }

            foreach (PredictedTargetImpact impact in simulation.Impacts)
            {
                BattleUnit target = battle.GetChild<BattleUnit>(impact.TargetId);
                if (target == null || target.IsDisposed || target.IsDead)
                {
                    continue;
                }

                if (!includeBossTargets && target.IsBoss)
                {
                    continue;
                }

                BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
                if (combatComp == null)
                {
                    continue;
                }

                if (impact.TotalDamage > 0)
                {
                    combatComp.TakeDamage(impact.TotalDamage);
                    EventSystem.Instance.Publish(battle.Root(), new BattleUnitDamaged
                    {
                        Unit = target,
                        AttackerId = caster.Id,
                        Damage = impact.TotalDamage,
                        IsCrit = false,
                    });
                }

                if (impact.TotalHeal > 0 && !target.IsDead)
                {
                    combatComp.Heal(impact.TotalHeal);
                }
            }
        }

        private static List<BattleUnit> SelectTargets(Battle battle, BattleUnit caster, SkillTargetingConfig targetingConfig,
            long explicitTargetId, float castRange)
        {
            List<BattleUnit> targets = new List<BattleUnit>();
            BattleUnit explicitTarget = GetValidExplicitTarget(battle, caster, explicitTargetId, targetingConfig, castRange);

            if (explicitTarget != null)
            {
                targets.Add(explicitTarget);
            }

            bool multiTarget = targetingConfig.SelectType == SelectTypeAllEnemiesInRange;
            if (!multiTarget)
            {
                if (targets.Count > 0)
                {
                    return targets;
                }

                BattleUnit nearest = FindNearestTarget(battle, caster, targetingConfig, castRange);
                if (nearest != null)
                {
                    targets.Add(nearest);
                }

                return targets;
            }

            if (targetingConfig.MaxTargetCount > 0 && targets.Count >= targetingConfig.MaxTargetCount)
            {
                return targets;
            }

            List<BattleUnit> candidates = new List<BattleUnit>();
            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit target)
                {
                    continue;
                }

                if (explicitTarget != null && target.Id == explicitTarget.Id)
                {
                    continue;
                }

                if (!IsTargetSelectable(caster, target, targetingConfig, castRange))
                {
                    continue;
                }

                candidates.Add(target);
            }

            candidates.Sort((left, right) =>
            {
                float leftDistance = math.abs(caster.Position.x - left.Position.x);
                float rightDistance = math.abs(caster.Position.x - right.Position.x);
                return leftDistance.CompareTo(rightDistance);
            });

            foreach (BattleUnit candidate in candidates)
            {
                if (targetingConfig.MaxTargetCount > 0 && targets.Count >= targetingConfig.MaxTargetCount)
                {
                    break;
                }

                targets.Add(candidate);
            }

            return targets;
        }

        private static BattleUnit GetValidExplicitTarget(Battle battle, BattleUnit caster, long explicitTargetId,
            SkillTargetingConfig targetingConfig, float castRange)
        {
            if (explicitTargetId == 0)
            {
                return targetingConfig.TargetCampRelation == TargetCampSelf && IsTargetSelectable(caster, caster, targetingConfig, castRange)
                    ? caster
                    : null;
            }

            BattleUnit explicitTarget = battle.GetChild<BattleUnit>(explicitTargetId);
            if (explicitTarget == null || explicitTarget.IsDisposed)
            {
                return null;
            }

            return IsTargetSelectable(caster, explicitTarget, targetingConfig, castRange) ? explicitTarget : null;
        }

        private static BattleUnit FindNearestTarget(Battle battle, BattleUnit caster, SkillTargetingConfig targetingConfig, float castRange)
        {
            BattleUnit nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit target)
                {
                    continue;
                }

                if (!IsTargetSelectable(caster, target, targetingConfig, castRange))
                {
                    continue;
                }

                float distance = math.abs(caster.Position.x - target.Position.x);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }

            return nearest;
        }

        private static bool IsTargetSelectable(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig, float castRange)
        {
            if (caster == null || target == null || target.IsDisposed)
            {
                return false;
            }

            if (targetingConfig.RequireAlive && target.IsDead)
            {
                return false;
            }

            if (!IsCampRelationMatch(caster, target, targetingConfig.TargetCampRelation))
            {
                return false;
            }

            if (target.Id != caster.Id && math.abs(caster.Position.x - target.Position.x) > castRange)
            {
                return false;
            }

            return true;
        }

        private static bool IsCampRelationMatch(BattleUnit caster, BattleUnit target, int relation)
        {
            switch (relation)
            {
                case TargetCampEnemy:
                    return target.Camp != caster.Camp;
                case TargetCampAlly:
                    return target.Camp == caster.Camp;
                case TargetCampSelf:
                    return target.Id == caster.Id;
                case TargetCampAny:
                    return true;
                default:
                    return target.Camp != caster.Camp;
            }
        }

        private static PredictedTargetImpact BuildImpact(BattleUnit caster, BattleUnit target, EmitterConfig emitterConfig)
        {
            PredictedTargetImpact impact = new PredictedTargetImpact
            {
                TargetId = target.Id,
                IsBoss = target.IsBoss,
            };

            impact.TotalDamage = CalculateEmitterDamage(caster, target, emitterConfig);

            return impact;
        }

        private static int CalculateEmitterDamage(BattleUnit caster, BattleUnit target, EmitterConfig emitterConfig)
        {
            if (caster == null || target == null || emitterConfig == null)
            {
                return 0;
            }

            if (emitterConfig.BaseDamage <= 0f && emitterConfig.WhiteAttackRatio <= 0f)
            {
                return 0;
            }

            int attack = caster.GetOrCreateBattleStats()?.Attack ?? 0;
            int defense = target.GetOrCreateBattleStats()?.Defense ?? 0;
            float value = emitterConfig.BaseDamage + attack * emitterConfig.WhiteAttackRatio - defense;
            return (int)System.Math.Floor(System.Math.Max(0f, value));
        }
    }
}
