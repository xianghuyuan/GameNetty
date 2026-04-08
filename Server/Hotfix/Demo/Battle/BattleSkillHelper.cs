using System;
using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 战斗技能辅助工具类，提供技能选择、目标选取、伤害计算、技能执行等功能。
    /// 同时服务于自动战斗决策（AI）和手动释放技能两种场景。
    /// </summary>
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


        private const int MonsterTypeElite = 2;
        private const int MonsterTypeBoss = 3;

        private const int CastTypeInstant = 0;     // 瞬发技能（直接生效）
        private const int CastTypeProjectile = 1;   // 投射物技能（生成投射物飞行后命中）

        private const int EffectTypeDamage = 1;
        private const int EffectTypeHeal = 4;
        private const int EffectTypeKnockback = 3;
        private const int EffectTypeFreeze = 2;
        private const int EffectTypeStun = 5;
        private const int FormulaTypeAttackMinusDefense = 1;
        private const int FormulaTypeFixedValue = 2;

        /// <summary>
        /// 技能执行结果，包含技能ID、主目标、命中数、总伤害、冷却结束时间及可能的错误信息。
        /// </summary>
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

        /// <summary>
        /// 自动施法方案，描述一次自动战斗决策选出的最优技能、目标及移动方案。
        /// 由 TrySelectBestAutoSkillPlan 输出，供决策组件执行移动或施法。
        /// </summary>
        public struct AutoCastPlan
        {
            public int SkillId;
            public SkillConfig SkillConfig;
            public SkillTargetingConfig TargetingConfig;
            public BattleUnit Target;
            /// <summary>施法者应移动到的目标位置</summary>
            public Vector3 DesiredCastPosition;
            /// <summary>期望施法距离（扣除 engage buffer 和 slot offset）</summary>
            public float DesiredCastDistance;
            /// <summary>施法者需要移动的距离（distance - desiredCastDistance，最小为0）</summary>
            public float RequiredMoveDistance;
        }

        /// <summary>
        /// 判断目标是否在施法者的普攻范围内。
        /// 优先使用 TargetingConfig 中的 CastRange，配置缺失时回退到 combat.AttackRange。
        /// </summary>
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
        /// 自动战斗选技方法。找到最近的敌人，按优先级遍历技能，返回第一个可用的施法方案。
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="preferredTarget">上一次锁定的目标</param>
        /// <param name="plan">输出的施法方案</param>
        /// <returns>是否找到了可用的施法方案</returns>
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

            List<int> autoSkillIds = GetAutoSkillIds(unitCombatConfig);
            if (autoSkillIds.Count == 0)
            {
                return false;
            }

            // 找最近敌人
            BattleUnit nearestEnemy = preferredTarget;
            if (nearestEnemy == null || nearestEnemy.IsDead)
            {
                float nearestDistance = float.MaxValue;
                foreach (EntityRef<BattleUnit> unitRef in battleRoom.Units.Values)
                {
                    BattleUnit unit = unitRef;
                    if (unit == null || unit.IsDead || unit.Camp == caster.Camp)
                    {
                        continue;
                    }
                    
                    float dist = GetDistance(caster.Position, unit.Position);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearestEnemy = unit;
                    }
                }
            }

            if (nearestEnemy == null)
            {
                return false;
            }

            // 按优先级遍历技能，找到第一个可用的
            foreach (int skillId in autoSkillIds)
            {
                SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(skillId);
                SkillTargetingConfig targetingConfig = skillConfig?.TargetingConfigIdConfig;
                if (!CanUseSkillForAuto(caster, combat, skillConfig, targetingConfig, true))
                {
                    continue;
                }

                float distance = GetDistance(caster.Position, nearestEnemy.Position);
                float desiredCastDistance = GetDesiredCastDistance(caster, nearestEnemy, targetingConfig);
                float requiredMoveDistance = System.MathF.Max(0f, distance - desiredCastDistance);
                Vector3 desiredCastPosition = ComputeDesiredCastPosition(caster, nearestEnemy, targetingConfig);

                plan = new AutoCastPlan
                {
                    SkillId = skillId,
                    SkillConfig = skillConfig,
                    TargetingConfig = targetingConfig,
                    Target = nearestEnemy,
                    DesiredCastPosition = desiredCastPosition,
                    DesiredCastDistance = desiredCastDistance,
                    RequiredMoveDistance = requiredMoveDistance,
                };
                return true;
            }

            return false;
        }

        /// <summary>
        /// 在战斗房间中查找指定玩家拥有的友方战斗单位。
        /// </summary>
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

        /// <summary>
        /// 尝试执行普通攻击。从 UnitCombatConfig 中获取普攻技能ID，委托给 TryExecuteSkill。
        /// </summary>
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

        /// <summary>
        /// 检查指定技能是否允许在自动战斗模式下施放。
        /// 非自动模式下所有技能均可施放；自动模式下仅允许配置中标记为自动释放的技能。
        /// </summary>
        public static bool CanAutoCastSkill(BattleUnit caster, int skillId)
        {
            if (caster == null)
            {
                return false;
            }

            PlayerCombatModeComponent modeComponent = caster.GetComponent<PlayerCombatModeComponent>();
            if (modeComponent == null || !modeComponent.IsAutoBattle)
            {
                return true;
            }

            UnitCombatConfig unitCombatConfig = UnitCombatConfigCategory.Instance.GetOrDefault(caster.ConfigId);
            if (unitCombatConfig == null)
            {
                return false;
            }

            if (IsNormalAttackSkill(unitCombatConfig, skillId))
            {
                return unitCombatConfig.AutoCastNormalAttack;
            }

            foreach (int autoSkillId in unitCombatConfig.AutoSkillIds)
            {
                if (autoSkillId == skillId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 技能执行主入口。依次进行：前置检查（死亡、配置完整性、冷却、自动模式限制）
        /// → 目标选取 → 广播施法 → 执行效果（瞬发直接结算 / 投射物生成弹道） → 启动冷却。
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="skillId">技能配置ID</param>
        /// <param name="explicitTargetId">玩家指定的目标ID，0表示自动选目标</param>
        /// <param name="result">执行结果</param>
        /// <param name="ignoreAutoModeLimit">是否忽略自动模式限制（服务端主动施放时为true）</param>
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
            BuffGroupConfig effectGroupConfig = skillConfig.BuffGroupIdConfig;
            if (targetingConfig == null || effectGroupConfig == null)
            {
                result.Error = ErrorCode.ERR_SkillNotFound;
                result.Message = $"Skill config incomplete: {skillId}";
                LogDebugHelper.Log($"[SkillExec] FAIL skillId={skillId} targetingConfig={(targetingConfig==null?"NULL":"OK")} effectGroupConfig={(effectGroupConfig==null?"NULL":"OK")}");
                return false;
            }

            combat.AttackCooldown = skillConfig.CooldownMs;
            combat.AttackRange = targetingConfig.CastRange;

            if (!combat.CanAttack)
            {
                result.Error = ErrorCode.ERR_BattleAttackNotReady;
                result.Message = "Caster cannot attack";
                return false;
            }

            if (!combat.IsSkillReady(skillConfig))
            {
                result.Error = ErrorCode.ERR_SkillCooldown;
                result.Message = "Skill is cooling down";
                return false;
            }

            if (!ignoreAutoModeLimit)
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

            LogDebugHelper.Log($"[SkillExec] skillId={skillId} CastType={skillConfig.CastType} BuffGroupId={skillConfig.BuffGroupId} EffectIds=[{string.Join(",", effectGroupConfig.EffectIds)}] TargetCount={targets.Count}");

            // 投射物类型技能：生成投射物，由投射物飞行过程中进行碰撞检测和伤害结算
            if (skillConfig.CastType == CastTypeProjectile)
            {
                LogDebugHelper.Log($"[SkillExec] skillId={skillId} -> Projectile path (CastType=1), SpawnProjectileEvent");
                EventSystem.Instance.Publish(battleRoom.Root(), new SpawnProjectileEvent
                {
                    Caster = caster,
                    SkillConfig = skillConfig,
                    Target = mainTarget,
                });
            }
            else
            {
                // 非投射物类型：直接生效
                LogDebugHelper.Log($"[SkillExec] skillId={skillId} -> Instant path (CastType=0), calling ApplyEffects");
                int totalDamage = 0;
                foreach (BattleUnit target in targets)
                {
                    totalDamage += ApplyEffects(caster, target, effectGroupConfig, skillConfig);
                }

                LogDebugHelper.Log($"[SkillExec] skillId={skillId} ApplyEffects done, totalDamage={totalDamage}");
                result.TotalDamage = totalDamage;
            }

            // 技能生效（伤害/投射物已生成）后再启动CD
            long cooldownEnd = combat.StartSkillCooldown(skillConfig);

            result.SkillId = skillId;
            result.MainTarget = mainTarget;
            result.HitTargetsCount = targets.Count;
            return true;
        }

        /// <summary>
        /// 判断指定技能是否为该单位的普攻技能。
        /// </summary>
        private static bool IsNormalAttackSkill(UnitCombatConfig unitCombatConfig, int skillId)
        {
            return unitCombatConfig != null && unitCombatConfig.NormalAttackSkillId != 0 && unitCombatConfig.NormalAttackSkillId == skillId;
        }

        /// <summary>
        /// 检查技能是否可用于自动战斗。校验配置完整性、技能是否启用、能否攻击、以及冷却状态。
        /// </summary>
        /// <param name="readyOnly">true时额外检查技能CD是否就绪</param>
        private static bool CanUseSkillForAuto(BattleUnit caster, BattleUnitCombatComponent combat, SkillConfig skillConfig,
            SkillTargetingConfig targetingConfig, bool readyOnly)
        {
            if (caster == null || combat == null || skillConfig == null || targetingConfig == null)
            {
                return false;
            }

            if (!skillConfig.IsEnabled || !combat.CanAttack)
            {
                return false;
            }

            if (readyOnly && !combat.IsSkillReady(skillConfig))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 收集单位可自动释放的技能ID列表。
        /// 包含 AutoSkillIds 中配置的技能，以及根据 AutoCastNormalAttack 决定是否包含普攻。
        /// 自动去重。
        /// </summary>
        private static List<int> GetAutoSkillIds(UnitCombatConfig unitCombatConfig)
        {
            List<int> result = new List<int>();
            if (unitCombatConfig == null)
            {
                return result;
            }

            foreach (int skillId in unitCombatConfig.AutoSkillIds)
            {
                if (skillId == 0)
                {
                    continue;
                }

                if (IsNormalAttackSkill(unitCombatConfig, skillId) && !unitCombatConfig.AutoCastNormalAttack)
                {
                    continue;
                }

                if (!result.Contains(skillId))
                {
                    result.Add(skillId);
                }
            }

            if (unitCombatConfig.AutoCastNormalAttack
                && unitCombatConfig.NormalAttackSkillId != 0
                && !result.Contains(unitCombatConfig.NormalAttackSkillId))
            {
                result.Add(unitCombatConfig.NormalAttackSkillId);
            }

            return result;
        }

        /// <summary>
        /// 为技能选取目标列表。支持三种目标选取方式：
        /// 1. 指定目标（explicitTargetId > 0）：直接验证并返回
        /// 2. 锁定目标/最近敌人：返回距离最近的一个合法目标
        /// 3. 范围内所有敌人：返回射程内所有合法目标
        /// 选取后会按 SortRule 排序并限制最大目标数。
        /// </summary>
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

        /// <summary>
        /// 对目标施加技能效果组中的所有效果。目前仅支持伤害效果。
        /// <summary>
        /// 对目标施加技能的buff组。所有战斗效果统一视为buff：
        /// - 即时效果（Damage/Heal/Knockback）直接执行并广播
        /// - 持续效果（Freeze/Stun/DoT）创建 BuffEntity 注册到 BuffComponent
        /// </summary>
        /// <returns>造成的总即时伤害</returns>
        public static int ApplyEffects(BattleUnit caster, BattleUnit target, BuffGroupConfig effectGroupConfig, SkillConfig skillConfig)
        {
            int totalDamage = 0;

            if (effectGroupConfig == null || target == null)
            {
                return totalDamage;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            BuffComponent buffComponent = target.GetComponent<BuffComponent>();

            foreach (int buffId in effectGroupConfig.EffectIds)
            {
                BuffConfig effectConfig = BuffConfigCategory.Instance.GetOrDefault(buffId);
                if (effectConfig == null)
                {
                    continue;
                }

                switch ((EffectType)effectConfig.EffectType)
                {
                    case EffectType.Damage:
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
                    case EffectType.Heal:
                    {
                        int healAmount = (int)effectConfig.BaseValue;
                        if (healAmount > 0)
                        {
                            target.Heal(healAmount);
                        }
                        break;
                    }
                    case EffectType.Knockback:
                    {
                        float distance = effectConfig.BaseValue;
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
                                CasterId = caster?.Id ?? 0,
                            });
                        }
                        break;
                    }
                    default:
                    {
                        // 持续效果（Freeze/Stun/DoT等）交给 BuffComponent 管理
                        if (buffComponent != null)
                        {
                            int duration = 0;
                            int tickInterval = 0;
                            if ((EffectType)effectConfig.EffectType == EffectType.Freeze || (EffectType)effectConfig.EffectType == EffectType.Stun)
                            {
                                duration = (int)effectConfig.BaseValue;
                            }

                            buffComponent.AddBuff(buffId, caster?.Id ?? 0, skillConfig.Id, effectConfig, duration, tickInterval);
                        }
                        break;
                    }
                }
            }

            return totalDamage;
        }

        /// <summary>
        /// 根据伤害公式计算单次效果伤害。
        /// </summary>
        private static int CalculateDamage(BattleUnit caster, BattleUnit target, BuffConfig effectConfig)
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

        /// <summary>
        /// 根据技能类型获取伤害类型标识。
        /// </summary>
        private static int GetDamageType(SkillConfig skillConfig)
        {
            if (skillConfig == null)
            {
                return 1;
            }

            return skillConfig.SkillKind == 1 ? 0 : 1;
        }

        /// <summary>
        /// 检查目标是否合法：存活状态检查 + 阵营关系检查。
        /// </summary>
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

        /// <summary>
        /// 检查施法者与目标的阵营关系是否匹配配置要求。
        /// 支持敌方、友方（不含自身）、自身、任意阵营。
        /// </summary>
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

        /// <summary>
        /// 判断目标是否在技能射程内。射程 = CastRange + EdgeDistance + 碰撞半径（可选）。
        /// </summary>
        public static bool IsInSkillRange(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            return GetDistance(caster.Position, target.Position) <= GetAllowedCastDistance(caster, target, targetingConfig);
        }

        /// <summary>
        /// 计算允许施法的最大距离 = CastRange + EdgeDistance + 碰撞半径（如果启用）。
        /// </summary>
        private static float GetAllowedCastDistance(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            float allowedDistance = targetingConfig.CastRange + targetingConfig.EdgeDistance;
            if (targetingConfig.UseCollisionRadius)
            {
                allowedDistance += GetCollisionRadius(caster) + GetCollisionRadius(target);
            }

            return allowedDistance;
        }

        /// <summary>
        /// 计算期望施法距离。在允许施法距离基础上扣除 engage buffer（防止浮点抖动）和 slot offset（多单位近战错位）。
        /// </summary>
        private static float GetDesiredCastDistance(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            float allowedDistance = GetAllowedCastDistance(caster, target, targetingConfig);
            float engageBuffer = MathF.Min(0.25f, allowedDistance * 0.2f);
            float slotOffset = GetEngageSlotOffset(caster, target);
            float desiredDistance = allowedDistance - engageBuffer - slotOffset;
            return desiredDistance > 0f ? desiredDistance : allowedDistance;
        }

        /// <summary>
        /// 计算近战错位偏移量。根据施法者和目标的 ID 哈希分配 4 个站位槽位，
        /// 避免多个单位攻击同一目标时重叠在同一位置。
        /// </summary>
        private static float GetEngageSlotOffset(BattleUnit caster, BattleUnit target)
        {
            if (caster == null || target == null)
            {
                return 0f;
            }

            int slotIndex = (int)(Math.Abs(caster.Id ^ target.Id) % 4);
            return slotIndex * 0.08f;
        }

        /// <summary>
        /// 计算施法者应移动到的目标位置。站在目标面向施法者方向的一侧，距离为期望施法距离。
        /// </summary>
        public static Vector3 ComputeDesiredCastPosition(BattleUnit caster, BattleUnit target, SkillTargetingConfig targetingConfig)
        {
            float desiredDistance = GetDesiredCastDistance(caster, target, targetingConfig);
            float direction = caster.Position.X <= target.Position.X ? -1f : 1f;
            return new Vector3(target.Position.X + direction * desiredDistance, caster.Position.Y, caster.Position.Z);
        }

        /// <summary>
        /// 按排序规则对目标列表排序。支持：无排序、按距离最近、按血量最低。
        /// </summary>
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

        /// <summary>
        /// 限制目标数量不超过配置的最大值。超出时截断尾部（排序后优先保留靠前的目标）。
        /// </summary>
        private static void LimitTargets(List<BattleUnit> targets, int maxTargetCount)
        {
            if (maxTargetCount > 0 && targets.Count > maxTargetCount)
            {
                targets.RemoveRange(maxTargetCount, targets.Count - maxTargetCount);
            }
        }

        /// <summary>
        /// 通过单位ID在战斗房间中查找战斗单位。
        /// </summary>
        private static BattleUnit FindBattleUnitById(BattleRoom battleRoom, long unitId)
        {
            if (battleRoom == null || !battleRoom.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                return null;
            }

            BattleUnit unit = unitRef;
            return unit;
        }

        /// <summary>
        /// 获取单位当前生命值。
        /// </summary>
        private static int GetCurrentHp(BattleUnit unit)
        {
            return unit?.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Hp) ?? 0;
        }

        /// <summary>
        /// 获取单位碰撞半径。当前固定返回 0，后续可从配置中读取。
        /// </summary>
        private static float GetCollisionRadius(BattleUnit unit)
        {
            return 0f;
        }

        /// <summary>
        /// 计算两点之间的 X 轴距离。当前战斗为横版，仅使用 X 坐标。
        /// </summary>
        private static float GetDistance(Vector3 from, Vector3 to)
        {
            return MathF.Abs(from.X - to.X);
        }
    }
}
