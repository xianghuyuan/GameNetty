using System;
using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    public static class BattleSkillHelper
    {
        private const int SelectTypeLockedTarget = 1;
        private const int SelectTypeNearestEnemy = 2;
        private const int SelectTypeAllEnemiesInRange = 3;

        private const int TargetCampEnemy = 1;
        private const int TargetCampAlly = 2;
        private const int TargetCampSelf = 3;
        private const int TargetCampAny = 4;

        private const int SortRuleNone = 1;
        private const int SortRuleNearest = 2;
        private const int SortRuleLowestHp = 3;

        private const int EffectTypeDamage = 1;

        private const int FormulaTypeAttackMinusDefense = 1;
        private const int FormulaTypeFixedValue = 2;

        private const int MonsterTypeElite = 2;
        private const int MonsterTypeBoss = 3;

        public struct SkillExecutionResult
        {
            public int Error;
            public string Message;
            public int SkillId;
            public BattleUnit MainTarget;
            public int HitTargetsCount;
            public int TotalDamage;
            public long CooldownEnd;
        }

        public struct AutoCastPlan
        {
            public int SkillId;
            public SkillConfig SkillConfig;
            public SkillTargetingConfig TargetingConfig;
            public BattleUnit Target;
            public Vector3 DesiredCastPosition;
            public float DesiredCastDistance;
            public float RequiredMoveDistance;
        }

        public static void ApplyNormalAttackConfig(BattleUnit unit, BattleUnitCombatComponent combat)
        {
            if (unit == null || combat == null)
            {
                return;
            }

            UnitCombatConfig unitCombatConfig = UnitCombatConfigCategory.Instance.GetOrDefault(unit.ConfigId);
            if (unitCombatConfig == null)
            {
                return;
            }

            SkillConfig skillConfig = unitCombatConfig.NormalAttackSkillIdConfig;
            SkillTargetingConfig targetingConfig = skillConfig?.TargetingConfigIdConfig;

            if (skillConfig != null)
            {
                combat.AttackCooldown = skillConfig.CooldownMs;
            }

            if (targetingConfig != null)
            {
                combat.AttackRange = targetingConfig.CastRange;
            }

            NumericComponent numeric = unit.GetComponent<NumericComponent>();
            if (numeric != null && unitCombatConfig.MoveSpeed > 0)
            {
                numeric.Set(NumericType.Speed, unitCombatConfig.MoveSpeed);
            }
        }

        public static bool IsInNormalAttackRange(BattleUnit caster, BattleUnit target)
        {
            if (caster == null || target == null)
            {
                return false;
            }

            UnitCombatConfig unitCombatConfig = UnitCombatConfigCategory.Instance.GetOrDefault(caster.ConfigId);
            SkillConfig skillConfig = unitCombatConfig?.NormalAttackSkillIdConfig;
            SkillTargetingConfig targetingConfig = skillConfig?.TargetingConfigIdConfig;
            if (targetingConfig == null)
            {
                BattleUnitCombatComponent combat = caster.GetComponent<BattleUnitCombatComponent>();
                return combat != null && GetDistance(caster.Position, target.Position) <= combat.AttackRange;
            }

            return IsInSkillRange(caster, target, targetingConfig);
        }

        /// <summary>
        /// 遍历所有单位选目标
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="preferredTarget"></param>
        /// <param name="plan"></param>
        /// <returns></returns>
        public static bool TrySelectBestAutoSkillPlan(BattleUnit caster, BattleUnit preferredTarget, out AutoCastPlan plan)
        {
            plan = default;

            if (caster == null || caster.IsDead)
            {
                return false;
            }

            BattleRoom battleRoom = caster.GetParent<BattleRoom>();
            BattleUnitCombatComponent combat = caster.GetComponent<BattleUnitCombatComponent>();
            UnitCombatConfig unitCombatConfig = UnitCombatConfigCategory.Instance.GetOrDefault(caster.ConfigId);
            if (battleRoom == null || combat == null || unitCombatConfig == null)
            {
                return false;
            }

            AutoBattleStrategyConfig strategyConfig = unitCombatConfig.AutoBattleStrategyIdConfig;

            List<int> autoSkillIds = GetAutoSkillIds(unitCombatConfig);
            if (autoSkillIds.Count == 0)
            {
                return false;
            }

            if (TryBuildAutoCastPlan(caster, battleRoom, combat, autoSkillIds, strategyConfig, preferredTarget, true, out plan))
            {
                return true;
            }

            if (strategyConfig != null && !strategyConfig.AllowPreMoveOnCooldown)
            {
                return false;
            }

            return TryBuildAutoCastPlan(caster, battleRoom, combat, autoSkillIds, strategyConfig, preferredTarget, false, out plan);
        }

        public static BattleUnit FindPlayerBattleUnit(BattleRoom battleRoom, long ownerId)
        {
            if (battleRoom == null)
            {
                return null;
            }

            foreach (EntityRef<BattleUnit> unitRef in battleRoom.Units.Values)
            {
                BattleUnit unit = unitRef;
                if (unit != null && unit.OwnerId == ownerId && unit.Camp == UnitCamp.Friend)
                {
                    return unit;
                }
            }

            return null;
        }

        public static bool TryExecuteNormalAttack(BattleUnit caster, long explicitTargetId, out SkillExecutionResult result)
        {
            result = default;

            UnitCombatConfig unitCombatConfig = UnitCombatConfigCategory.Instance.GetOrDefault(caster.ConfigId);
            if (unitCombatConfig == null)
            {
                result.Error = ErrorCode.ERR_SkillNotFound;
                result.Message = $"No UnitCombatConfig for ConfigId={caster.ConfigId}";
                return false;
            }

            return TryExecuteSkill(caster, unitCombatConfig.NormalAttackSkillId, explicitTargetId, out result);
        }

        public static bool TryExecuteSkill(BattleUnit caster, int skillId, long explicitTargetId, out SkillExecutionResult result,
            bool ignoreAutoModeLimit = false)
        {
            result = default;

            if (caster == null || caster.IsDead)
            {
                result.Error = ErrorCode.ERR_UnitIsDead;
                result.Message = "Caster is dead";
                return false;
            }

            BattleRoom battleRoom = caster.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                result.Error = ErrorCode.ERR_BattleNotFound;
                result.Message = "Battle room not found";
                return false;
            }

            BattleUnitCombatComponent combat = caster.GetComponent<BattleUnitCombatComponent>();
            if (combat == null)
            {
                result.Error = ErrorCode.ERR_BattleAttackNotReady;
                result.Message = "No combat component";
                return false;
            }

            SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                result.Error = ErrorCode.ERR_SkillNotFound;
                result.Message = $"Skill not found: {skillId}";
                return false;
            }

            SkillTargetingConfig targetingConfig = skillConfig.TargetingConfigIdConfig;
            SkillCastCheckConfig castCheckConfig = skillConfig.CastCheckConfigIdConfig;
            SkillEffectGroupConfig effectGroupConfig = skillConfig.EffectGroupIdConfig;
            if (targetingConfig == null || castCheckConfig == null || effectGroupConfig == null)
            {
                result.Error = ErrorCode.ERR_SkillNotFound;
                result.Message = $"Skill config incomplete: {skillId}";
                return false;
            }

            combat.AttackCooldown = skillConfig.CooldownMs;
            combat.AttackRange = targetingConfig.CastRange;

            if (castCheckConfig.CheckCanAttack && !combat.CanAttack)
            {
                result.Error = ErrorCode.ERR_BattleAttackNotReady;
                result.Message = "Caster cannot attack";
                return false;
            }

            if (castCheckConfig.CheckCooldown && !combat.IsSkillReady(skillConfig))
            {
                result.Error = castCheckConfig.FailErrorCode != 0 ? castCheckConfig.FailErrorCode : ErrorCode.ERR_SkillCooldown;
                result.Message = "Skill is cooling down";
                return false;
            }

            if (!ignoreAutoModeLimit && castCheckConfig.CheckAutoModeLimit)
            {
                PlayerCombatModeComponent modeComponent = caster.GetComponent<PlayerCombatModeComponent>();
                if (modeComponent != null && modeComponent.IsAutoBattle)
                {
                    result.Error = ErrorCode.ERR_BattleInAutoMode;
                    result.Message = "In auto battle mode";
                    return false;
                }
            }

            List<BattleUnit> targets = SelectTargets(caster, battleRoom, skillConfig, targetingConfig, explicitTargetId, out int error, out string message);
            if (targets.Count == 0)
            {
                result.Error = error;
                result.Message = message;
                return false;
            }

            BattleUnit mainTarget = targets[0];
            BattleUnitHelper.BroadcastSkillCast(caster, skillId, mainTarget.Id, mainTarget.Position);

            int totalDamage = 0;
            foreach (BattleUnit target in targets)
            {
                totalDamage += ApplyEffects(caster, target, effectGroupConfig, skillConfig);
            }

            long cooldownEnd = combat.StartSkillCooldown(skillConfig);

            result.SkillId = skillId;
            result.MainTarget = mainTarget;
            result.HitTargetsCount = targets.Count;
            result.TotalDamage = totalDamage;
            result.CooldownEnd = cooldownEnd;
            return true;
        }

        private static bool TryBuildAutoCastPlan(BattleUnit caster, BattleRoom battleRoom, BattleUnitCombatComponent combat,
            List<int> autoSkillIds, AutoBattleStrategyConfig strategyConfig, BattleUnit preferredTarget, bool readyOnly, out AutoCastPlan bestPlan)
        {
            bestPlan = default;
            bool found = false;

            foreach (int skillId in autoSkillIds)
            {
                SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(skillId);
                SkillTargetingConfig targetingConfig = skillConfig?.TargetingConfigIdConfig;
                SkillCastCheckConfig castCheckConfig = skillConfig?.CastCheckConfigIdConfig;
                if (!CanUseSkillForAuto(caster, combat, skillConfig, targetingConfig, castCheckConfig, readyOnly))
                {
                    continue;
                }

                foreach (EntityRef<BattleUnit> unitRef in battleRoom.Units.Values)
                {
                    BattleUnit target = unitRef;
                    if (!IsTargetValid(caster, target, targetingConfig))
                    {
                        continue;
                    }

                    float distance = GetDistance(caster.Position, target.Position);

                    float desiredCastDistance = GetDesiredCastDistance(caster, target, targetingConfig);
                    float requiredMoveDistance = System.MathF.Max(0f, distance - desiredCastDistance);
                    Vector3 desiredCastPosition = ComputeDesiredCastPosition(caster, target, targetingConfig);

                    AutoCastPlan candidate = new AutoCastPlan
                    {
                        SkillId = skillId,
                        SkillConfig = skillConfig,
                        TargetingConfig = targetingConfig,
                        Target = target,
                        DesiredCastPosition = desiredCastPosition,
                        DesiredCastDistance = desiredCastDistance,
                        RequiredMoveDistance = requiredMoveDistance,
                    };

                    if (!found || IsBetterAutoCastPlan(caster, candidate, bestPlan, strategyConfig, preferredTarget))
                    {
                        bestPlan = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool CanUseSkillForAuto(BattleUnit caster, BattleUnitCombatComponent combat, SkillConfig skillConfig,
            SkillTargetingConfig targetingConfig, SkillCastCheckConfig castCheckConfig, bool readyOnly)
        {
            if (caster == null || combat == null || skillConfig == null || targetingConfig == null || castCheckConfig == null)
            {
                return false;
            }

            if (!skillConfig.IsEnabled || (castCheckConfig.CheckCanAttack && !combat.CanAttack))
            {
                return false;
            }

            if (readyOnly && castCheckConfig.CheckCooldown && !combat.IsSkillReady(skillConfig))
            {
                return false;
            }

            return true;
        }

        private static bool IsBetterAutoCastPlan(BattleUnit caster, AutoCastPlan candidate, AutoCastPlan current,
            AutoBattleStrategyConfig strategyConfig, BattleUnit preferredTarget)
        {
            if (TryCompareCurrentTargetPreference(candidate, current, strategyConfig, preferredTarget, out bool candidateWinsByCurrentTarget))
            {
                return candidateWinsByCurrentTarget;
            }

            int candidateTargetWeight = GetTargetTypeWeight(candidate.Target, strategyConfig);
            int currentTargetWeight = GetTargetTypeWeight(current.Target, strategyConfig);
            if (candidateTargetWeight != currentTargetWeight)
            {
                return candidateTargetWeight > currentTargetWeight;
            }

            if (TryCompareSkillSelectionRule(candidate, current, strategyConfig, out bool candidateWinsBySkillRule))
            {
                return candidateWinsBySkillRule;
            }

            if (TryCompareTargetSelectionRule(caster, candidate, current, strategyConfig, out bool candidateWinsByTargetRule))
            {
                return candidateWinsByTargetRule;
            }

            if (candidate.RequiredMoveDistance + 0.0001f < current.RequiredMoveDistance)
            {
                return true;
            }

            if (candidate.RequiredMoveDistance > current.RequiredMoveDistance + 0.0001f)
            {
                return false;
            }

            float candidateTargetDistance = GetDistance(candidate.DesiredCastPosition, candidate.Target.Position);
            float currentTargetDistance = GetDistance(current.DesiredCastPosition, current.Target.Position);
            if (candidateTargetDistance + 0.0001f < currentTargetDistance)
            {
                return true;
            }

            if (candidateTargetDistance > currentTargetDistance + 0.0001f)
            {
                return false;
            }

            return (candidate.SkillConfig?.Priority ?? 0) > (current.SkillConfig?.Priority ?? 0);
        }

        private static bool TryCompareCurrentTargetPreference(AutoCastPlan candidate, AutoCastPlan current,
            AutoBattleStrategyConfig strategyConfig, BattleUnit preferredTarget, out bool candidateWins)
        {
            candidateWins = false;

            if (preferredTarget == null || preferredTarget.IsDead || strategyConfig == null || !strategyConfig.PreferCurrentTarget)
            {
                return false;
            }

            bool candidateIsPreferredTarget = candidate.Target?.Id == preferredTarget.Id;
            bool currentIsPreferredTarget = current.Target?.Id == preferredTarget.Id;
            if (candidateIsPreferredTarget == currentIsPreferredTarget)
            {
                return false;
            }

            float tolerance = MathF.Max(0f, strategyConfig.TargetSwitchTolerance);
            if (candidateIsPreferredTarget && candidate.RequiredMoveDistance <= current.RequiredMoveDistance + tolerance)
            {
                candidateWins = true;
                return true;
            }

            if (currentIsPreferredTarget && current.RequiredMoveDistance <= candidate.RequiredMoveDistance + tolerance)
            {
                candidateWins = false;
                return true;
            }

            return false;
        }

        private static bool TryCompareSkillSelectionRule(AutoCastPlan candidate, AutoCastPlan current,
            AutoBattleStrategyConfig strategyConfig, out bool candidateWins)
        {
            candidateWins = false;

            AutoBattleSkillSelectRule rule = (AutoBattleSkillSelectRule)(strategyConfig?.SkillSelectRule ?? (int)AutoBattleSkillSelectRule.ShortestMoveThenPriority);
            switch (rule)
            {
                case AutoBattleSkillSelectRule.HighestPriorityThenMove:
                {
                    int candidatePriority = candidate.SkillConfig?.Priority ?? 0;
                    int currentPriority = current.SkillConfig?.Priority ?? 0;
                    if (candidatePriority != currentPriority)
                    {
                        candidateWins = candidatePriority > currentPriority;
                        return true;
                    }

                    if (MathF.Abs(candidate.RequiredMoveDistance - current.RequiredMoveDistance) > 0.0001f)
                    {
                        candidateWins = candidate.RequiredMoveDistance < current.RequiredMoveDistance;
                        return true;
                    }

                    return false;
                }
                case AutoBattleSkillSelectRule.ShortestMoveThenPriority:
                default:
                {
                    if (MathF.Abs(candidate.RequiredMoveDistance - current.RequiredMoveDistance) > 0.0001f)
                    {
                        candidateWins = candidate.RequiredMoveDistance < current.RequiredMoveDistance;
                        return true;
                    }

                    int candidatePriority = candidate.SkillConfig?.Priority ?? 0;
                    int currentPriority = current.SkillConfig?.Priority ?? 0;
                    if (candidatePriority != currentPriority)
                    {
                        candidateWins = candidatePriority > currentPriority;
                        return true;
                    }

                    return false;
                }
            }
        }

        private static bool TryCompareTargetSelectionRule(BattleUnit caster, AutoCastPlan candidate, AutoCastPlan current,
            AutoBattleStrategyConfig strategyConfig, out bool candidateWins)
        {
            candidateWins = false;

            AutoBattleTargetSelectRule rule = (AutoBattleTargetSelectRule)(strategyConfig?.TargetSelectRule ?? (int)AutoBattleTargetSelectRule.NearestEnemy);
            switch (rule)
            {
                case AutoBattleTargetSelectRule.LowestHp:
                {
                    int candidateHp = GetCurrentHp(candidate.Target);
                    int currentHp = GetCurrentHp(current.Target);
                    if (candidateHp != currentHp)
                    {
                        candidateWins = candidateHp < currentHp;
                        return true;
                    }

                    break;
                }
                case AutoBattleTargetSelectRule.KeepCurrentThenNearest:
                case AutoBattleTargetSelectRule.NearestEnemy:
                default:
                    break;
            }

            float candidateDistance = GetDistance(caster.Position, candidate.Target.Position);
            float currentDistance = GetDistance(caster.Position, current.Target.Position);
            if (MathF.Abs(candidateDistance - currentDistance) > 0.0001f)
            {
                candidateWins = candidateDistance < currentDistance;
                return true;
            }

            return false;
        }

        private static int GetTargetTypeWeight(BattleUnit target, AutoBattleStrategyConfig strategyConfig)
        {
            if (target == null || strategyConfig == null)
            {
                return 0;
            }

            int weight = 0;
            int monsterType = GetMonsterType(target);
            if (strategyConfig.PreferBoss && monsterType == MonsterTypeBoss)
            {
                weight += 200;
            }

            if (strategyConfig.PreferElite && monsterType == MonsterTypeElite)
            {
                weight += 100;
            }

            return weight;
        }

        private static int GetMonsterType(BattleUnit unit)
        {
            MonsterUnitConfig monsterConfig = MonsterUnitConfigCategory.Instance.GetOrDefault(unit.ConfigId);
            return monsterConfig?.Type ?? 0;
        }

        private static List<int> GetAutoSkillIds(UnitCombatConfig unitCombatConfig)
        {
            List<int> result = new List<int>();
            if (unitCombatConfig == null)
            {
                return result;
            }

            foreach (int skillId in unitCombatConfig.AutoSkillIds)
            {
                if (skillId != 0 && !result.Contains(skillId))
                {
                    result.Add(skillId);
                }
            }

            if (unitCombatConfig.NormalAttackSkillId != 0 && !result.Contains(unitCombatConfig.NormalAttackSkillId))
            {
                result.Add(unitCombatConfig.NormalAttackSkillId);
            }

            return result;
        }

        private static List<BattleUnit> SelectTargets(BattleUnit caster, BattleRoom battleRoom, SkillConfig skillConfig,
            SkillTargetingConfig targetingConfig, long explicitTargetId, out int error, out string message)
        {
            error = ErrorCode.ERR_BattleTargetNotFound;
            message = "No valid target";

            List<BattleUnit> targets = new List<BattleUnit>();
            bool hasEnemyCandidate = false;

            BattleUnit explicitTarget = explicitTargetId > 0 ? FindBattleUnitById(battleRoom, explicitTargetId) : null;
            if (explicitTarget != null)
            {
                if (!IsTargetValid(caster, explicitTarget, targetingConfig))
                {
                    error = ErrorCode.ERR_InvalidSkillTarget;
                    message = "Invalid target";
                    return targets;
                }

                if (!IsInSkillRange(caster, explicitTarget, targetingConfig))
                {
                    error = ErrorCode.ERR_BattleTargetOutOfRange;
                    message = "Target out of range";
                    return targets;
                }

                targets.Add(explicitTarget);
                return targets;
            }

            if (skillConfig.NeedExplicitTarget || targetingConfig.SelectType == SelectTypeLockedTarget)
            {
                error = ErrorCode.ERR_InvalidSkillTarget;
                message = "Explicit target required";
                return targets;
            }

            BattleUnit nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (EntityRef<BattleUnit> unitRef in battleRoom.Units.Values)
            {
                BattleUnit target = unitRef;
                if (!IsTargetValid(caster, target, targetingConfig))
                {
                    continue;
                }

                hasEnemyCandidate = true;

                float distance = GetDistance(caster.Position, target.Position);

                switch (targetingConfig.SelectType)
                {
                    case SelectTypeAllEnemiesInRange:
                        if (IsInSkillRange(caster, target, targetingConfig))
                        {
                            targets.Add(target);
                        }
                        break;
                    case SelectTypeNearestEnemy:
                    default:
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestEnemy = target;
                        }
                        break;
                }
            }

            if (targetingConfig.SelectType != SelectTypeAllEnemiesInRange)
            {
                if (nearestEnemy == null)
                {
                    error = hasEnemyCandidate ? ErrorCode.ERR_BattleTargetOutOfRange : ErrorCode.ERR_BattleTargetNotFound;
                    message = hasEnemyCandidate ? "No valid target in cast range" : "No target found";
                    return targets;
                }

                if (!IsInSkillRange(caster, nearestEnemy, targetingConfig))
                {
                    error = ErrorCode.ERR_BattleTargetOutOfRange;
                    message = "Target out of range";
                    return targets;
                }

                targets.Add(nearestEnemy);
            }

            if (targets.Count == 0)
            {
                error = hasEnemyCandidate ? ErrorCode.ERR_BattleTargetOutOfRange : ErrorCode.ERR_BattleTargetNotFound;
                message = hasEnemyCandidate ? "No target in skill range" : "No target found";
                return targets;
            }

            SortTargets(caster, targets, targetingConfig.SortRule);
            LimitTargets(targets, targetingConfig.MaxTargetCount);
            return targets;
        }

        private static int ApplyEffects(BattleUnit caster, BattleUnit target, SkillEffectGroupConfig effectGroupConfig, SkillConfig skillConfig)
        {
            int totalDamage = 0;

            foreach (int effectId in effectGroupConfig.EffectIds)
            {
                SkillEffectConfig effectConfig = SkillEffectConfigCategory.Instance.GetOrDefault(effectId);
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
                        target.TakeDamage(damage);
                        BattleUnitHelper.BroadcastDamage(caster, target, damage, GetDamageType(skillConfig));
                        if (wasAlive && target.IsDead)
                        {
                            BattleUnitHelper.BroadcastUnitDead(target, caster.Id);
                        }

                        totalDamage += damage;
                        break;
                    }
                }
            }

            return totalDamage;
        }

        private static int CalculateDamage(BattleUnit caster, BattleUnit target, SkillEffectConfig effectConfig)
        {
            NumericComponent attackerNumeric = caster.GetComponent<NumericComponent>();
            NumericComponent targetNumeric = target.GetComponent<NumericComponent>();

            float damageValue = effectConfig.BaseValue;

            switch (effectConfig.FormulaType)
            {
                case FormulaTypeAttackMinusDefense:
                {
                    int attack = attackerNumeric?.GetAsInt(NumericType.Attack) ?? 0;
                    int defense = targetNumeric?.GetAsInt(NumericType.Defense) ?? 0;
                    damageValue += attack * effectConfig.RatioAtk - defense * effectConfig.RatioDef;
                    break;
                }
                case FormulaTypeFixedValue:
                default:
                    break;
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

        private static int GetDamageType(SkillConfig skillConfig)
        {
            if (skillConfig == null)
            {
                return 1;
            }

            return skillConfig.SkillKind == 1 ? 0 : 1;
        }

        private static bool IsTargetValid(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            if (caster == null || target == null)
            {
                return false;
            }

            if (targetingConfig.RequireAlive && target.IsDead)
            {
                return false;
            }

            return IsTargetCampMatched(caster, target, targetingConfig.TargetCampRelation);
        }

        private static bool IsTargetCampMatched(BattleUnit caster, BattleUnit target, int targetCampRelation)
        {
            switch (targetCampRelation)
            {
                case TargetCampEnemy:
                    return caster.Camp != target.Camp;
                case TargetCampAlly:
                    return caster.Camp == target.Camp && caster.Id != target.Id;
                case TargetCampSelf:
                    return caster.Id == target.Id;
                case TargetCampAny:
                    return true;
                default:
                    return caster.Camp != target.Camp;
            }
        }

        public static bool IsInSkillRange(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            return GetDistance(caster.Position, target.Position) <= GetAllowedCastDistance(caster, target, targetingConfig);
        }

        private static float GetAllowedCastDistance(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            float allowedDistance = targetingConfig.CastRange + targetingConfig.EdgeDistance;
            if (targetingConfig.UseCollisionRadius)
            {
                allowedDistance += GetCollisionRadius(caster) + GetCollisionRadius(target);
            }

            return allowedDistance;
        }

        private static float GetDesiredCastDistance(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            float allowedDistance = GetAllowedCastDistance(caster, target, targetingConfig);
            float engageBuffer = MathF.Min(0.25f, allowedDistance * 0.2f);
            float slotOffset = GetEngageSlotOffset(caster, target);
            float desiredDistance = allowedDistance - engageBuffer - slotOffset;
            return desiredDistance > 0f ? desiredDistance : allowedDistance;
        }

        private static float GetEngageSlotOffset(BattleUnit caster, BattleUnit target)
        {
            if (caster == null || target == null)
            {
                return 0f;
            }

            int slotIndex = (int)(Math.Abs(caster.Id ^ target.Id) % 4);
            return slotIndex * 0.08f;
        }

        public static Vector3 ComputeDesiredCastPosition(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            float desiredDistance = GetDesiredCastDistance(caster, target, targetingConfig);
            float direction = caster.Position.X <= target.Position.X ? -1f : 1f;
            return new Vector3(target.Position.X + direction * desiredDistance, caster.Position.Y, caster.Position.Z);
        }

        private static void SortTargets(BattleUnit caster, List<BattleUnit> targets, int sortRule)
        {
            switch (sortRule)
            {
                case SortRuleLowestHp:
                    targets.Sort((a, b) => GetCurrentHp(a).CompareTo(GetCurrentHp(b)));
                    break;
                case SortRuleNearest:
                    targets.Sort((a, b) => GetDistance(caster.Position, a.Position).CompareTo(GetDistance(caster.Position, b.Position)));
                    break;
                case SortRuleNone:
                default:
                    break;
            }
        }

        private static void LimitTargets(List<BattleUnit> targets, int maxTargetCount)
        {
            if (maxTargetCount > 0 && targets.Count > maxTargetCount)
            {
                targets.RemoveRange(maxTargetCount, targets.Count - maxTargetCount);
            }
        }

        private static BattleUnit FindBattleUnitById(BattleRoom battleRoom, long unitId)
        {
            if (battleRoom == null || !battleRoom.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                return null;
            }

            BattleUnit unit = unitRef;
            return unit;
        }

        private static int GetCurrentHp(BattleUnit unit)
        {
            return unit?.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Hp) ?? 0;
        }

        private static float GetCollisionRadius(BattleUnit unit)
        {
            return 0f;
        }

        private static float GetDistance(Vector3 from, Vector3 to)
        {
            return MathF.Abs(from.X - to.X);
        }
    }
}
