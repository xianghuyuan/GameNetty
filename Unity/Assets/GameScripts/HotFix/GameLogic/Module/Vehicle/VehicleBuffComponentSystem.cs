using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(VehicleBuffComponent))]
    [FriendOf(typeof(VehicleBuffComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class VehicleBuffComponentSystem
    {
        private const long TickIntervalMs = 100;

        [EntitySystem]
        private static void Awake(this VehicleBuffComponent self)
        {
            self.ActiveBuffs.Clear();
            self.TickTimerId = 0;

            // 使用 Unity 的 Update 驱动 tick（客户端没有 TimerComponent）
            // 通过 ClientPlayerAITickComponent 驱动
        }

        [EntitySystem]
        private static void Destroy(this VehicleBuffComponent self)
        {
            // 还原所有效果
            foreach (VehicleBuffEntry entry in self.ActiveBuffs)
            {
                RemoveBuffEffect(self, entry);
            }
            self.ActiveBuffs.Clear();
        }

        /// <summary>
        /// 施加一个载具 Buff。
        /// 查找同 EffectType + 同 VehicleId 的条目，找到则层数+1并刷新持续时间。
        /// 未找到则新建条目。
        /// </summary>
        public static void ApplyVehicleBuff(this VehicleBuffComponent self, Battle battle, long vehicleId, int buffId, BuffConfig config)
        {
            if (config == null) return;

            BattleUnit target = self.GetParent<BattleUnit>();
            if (target == null || target.IsDead) return;

            int effectType = config.EffectType;
            int durationMs = config.Duration > 0 ? config.Duration : 2000;

            // 查找同 EffectType + 同 VehicleId 的现有条目
            VehicleBuffEntry existing = null;
            foreach (VehicleBuffEntry entry in self.ActiveBuffs)
            {
                if (entry.EffectType == effectType && entry.VehicleId == vehicleId)
                {
                    existing = entry;
                    break;
                }
            }

            long nowMs = TimeInfo.Instance.ClientNow();

            if (existing != null)
            {
                // 层数+1，刷新持续时间
                existing.StackCount++;
                existing.ExpireTimeMs = nowMs + durationMs;

                // 如果新 Buff 的 Duration 更长，更新
                if (durationMs > existing.DurationMs)
                {
                    existing.DurationMs = durationMs;
                    existing.ExpireTimeMs = nowMs + durationMs;
                }
            }
            else
            {
                // 新建条目
                VehicleBuffEntry entry = new VehicleBuffEntry
                {
                    VehicleId = vehicleId,
                    BuffId = buffId,
                    EffectType = effectType,
                    DurationMs = durationMs,
                    StackCount = 1,
                    ExpireTimeMs = nowMs + durationMs,
                    LastTickTimeMs = nowMs,
                };
                self.ActiveBuffs.Add(entry);

                // 首次施加效果
                ApplyBuffEffect(self, entry, target);
            }
        }

        /// <summary>
        /// 每100ms tick 一次，处理过期清理和 DOT tick。
        /// </summary>
        public static void OnTick(this VehicleBuffComponent self, long nowMs)
        {
            BattleUnit target = self.GetParent<BattleUnit>();
            if (target == null || target.IsDead)
            {
                self.Dispose();
                return;
            }

            for (int i = self.ActiveBuffs.Count - 1; i >= 0; i--)
            {
                VehicleBuffEntry entry = self.ActiveBuffs[i];

                // 过期检测
                if (nowMs >= entry.ExpireTimeMs)
                {
                    RemoveBuffEffect(self, entry);
                    self.ActiveBuffs.RemoveAt(i);
                    continue;
                }

                // DOT tick 检测
                BuffConfig config = ConfigHelper.BuffConfigCategory?.GetOrDefault(entry.BuffId);
                if (config != null && config.TickInterval > 0)
                {
                    if (nowMs - entry.LastTickTimeMs >= config.TickInterval)
                    {
                        entry.LastTickTimeMs = nowMs;
                        TickBuffEffect(self, entry, target, config);
                    }
                }
            }
        }

        /// <summary>
        /// 首次施加持续效果
        /// </summary>
        private static void ApplyBuffEffect(VehicleBuffComponent self, VehicleBuffEntry entry, BattleUnit target)
        {
            switch (entry.EffectType)
            {
                case 6: // SlowDown
                    ApplySlowDown(target, entry.StackCount);
                    break;
                case 9: // AttackBuff
                    ApplyAttackBuff(target, entry.StackCount);
                    break;
                case 10: // DefenseBuff
                    ApplyDefenseBuff(target, entry.StackCount);
                    break;
            }
        }

        /// <summary>
        /// 移除持续效果（Buff过期时）
        /// </summary>
        private static void RemoveBuffEffect(VehicleBuffComponent self, VehicleBuffEntry entry)
        {
            switch (entry.EffectType)
            {
                case 6: // SlowDown
                    RemoveSlowDown(self.GetParent<BattleUnit>());
                    break;
                case 9: // AttackBuff
                    RemoveAttackBuff(self.GetParent<BattleUnit>());
                    break;
                case 10: // DefenseBuff
                    RemoveDefenseBuff(self.GetParent<BattleUnit>());
                    break;
            }
        }

        /// <summary>
        /// DOT tick 伤害
        /// </summary>
        private static void TickBuffEffect(VehicleBuffComponent self, VehicleBuffEntry entry, BattleUnit target, BuffConfig config)
        {
            if (entry.EffectType == 11) // DOT
            {
                int damage = (int)System.Math.Floor(config.BaseValue);
                if (damage <= 0) return;

                BattleUnitCombatComponent combatComp = target.GetComponent<BattleUnitCombatComponent>();
                if (combatComp == null) return;

                combatComp.TakeDamage(damage);
                EventSystem.Instance.Publish(target.Scene(), new BattleUnitDamaged
                {
                    Unit = target,
                    AttackerId = 0,
                    Damage = damage,
                    IsCrit = false,
                });
            }
        }

        // --- SlowDown ---

        private static void ApplySlowDown(BattleUnit target, int stackCount)
        {
            // 简化实现：读取 BuffConfig 的 BaseValue 作为减速百分比
            // 多层叠加，上限90%
            // 注意：这里在 ApplyVehicleBuff 中已经有层数管理，实际减速百分比 = sum(all slow entries)
            RecalculateSlowDown(target);
        }

        private static void RemoveSlowDown(BattleUnit target)
        {
            RecalculateSlowDown(target);
        }

        private static void RecalculateSlowDown(BattleUnit target)
        {
            // 遍历所有 SlowDown 类型的 VehicleBuffEntry，累加减速百分比
            VehicleBuffComponent vbc = target.GetComponent<VehicleBuffComponent>();
            if (vbc == null) return;

            float totalSlowPercent = 0f;
            float baseSpeed = 0f;
            bool hasBase = false;

            foreach (VehicleBuffEntry entry in vbc.ActiveBuffs)
            {
                if (entry.EffectType != 6) continue;

                BuffConfig config = ConfigHelper.BuffConfigCategory?.GetOrDefault(entry.BuffId);
                if (config == null) continue;

                if (!hasBase)
                {
                    baseSpeed = target.GetOrCreateBattleStats()?.Speed ?? 0f;
                    hasBase = true;
                }

                totalSlowPercent += config.BaseValue;
            }

            if (totalSlowPercent > 0.9f) totalSlowPercent = 0.9f;

            // 如果没有减速了，不修改速度（保持当前值）
            if (totalSlowPercent <= 0f) return;

            // 需要记录原始速度。简化实现：用 NumericComponent 的基础值
            // TODO: 后续改为 Base/Final 分离的 NumericSystem
        }

        // --- AttackBuff ---

        private static void ApplyAttackBuff(BattleUnit target, int stackCount)
        {
            RecalculateAttackBuff(target);
        }

        private static void RemoveAttackBuff(BattleUnit target)
        {
            RecalculateAttackBuff(target);
        }

        private static void RecalculateAttackBuff(BattleUnit target)
        {
            VehicleBuffComponent vbc = target.GetComponent<VehicleBuffComponent>();
            if (vbc == null) return;

            int totalAttackBuff = 0;
            foreach (VehicleBuffEntry entry in vbc.ActiveBuffs)
            {
                if (entry.EffectType != 9) continue;
                BuffConfig config = ConfigHelper.BuffConfigCategory?.GetOrDefault(entry.BuffId);
                if (config == null) continue;
                totalAttackBuff += (int)config.BaseValue;
            }

            // TODO: 应用到 NumericComponent（需要 Base/Final 分离）
        }

        // --- DefenseBuff ---

        private static void ApplyDefenseBuff(BattleUnit target, int stackCount)
        {
            RecalculateDefenseBuff(target);
        }

        private static void RemoveDefenseBuff(BattleUnit target)
        {
            RecalculateDefenseBuff(target);
        }

        private static void RecalculateDefenseBuff(BattleUnit target)
        {
            VehicleBuffComponent vbc = target.GetComponent<VehicleBuffComponent>();
            if (vbc == null) return;

            int totalDefenseBuff = 0;
            foreach (VehicleBuffEntry entry in vbc.ActiveBuffs)
            {
                if (entry.EffectType != 10) continue;
                BuffConfig config = ConfigHelper.BuffConfigCategory?.GetOrDefault(entry.BuffId);
                if (config == null) continue;
                totalDefenseBuff += (int)config.BaseValue;
            }

            // TODO: 应用到 NumericComponent（需要 Base/Final 分离）
        }
    }
}
