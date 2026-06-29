using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(BattleAttackComponent))]
    [FriendOf(typeof(BattleAttackComponent))]
    [FriendOf(typeof(BattleUnit))]
    [FriendOf(typeof(VehicleComponent))]
    public static partial class BattleAttackComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleAttackComponent self)
        {
            self.CurrentTargetId = 0;
            self.Attacks.Clear();
            self.EmitterCooldownEndTimeById.Clear();
            self.CastMoveLockEndTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this BattleAttackComponent self)
        {
            self.CurrentTargetId = 0;
            self.Attacks.Clear();
            self.EmitterCooldownEndTimeById.Clear();
            self.CastMoveLockEndTime = 0;
        }

        public static bool HasAttacks(this BattleAttackComponent self)
        {
            return self != null && self.Attacks.Count > 0;
        }

        public static bool IsCastMoveLocked(this BattleAttackComponent self, long nowMs)
        {
            return self != null && nowMs < self.CastMoveLockEndTime;
        }

        public static void SyncFromVehicleComponent(this BattleAttackComponent self, VehicleComponent vehicleComponent)
        {
            self.Attacks.Clear();

            if (vehicleComponent == null)
            {
                self.EmitterCooldownEndTimeById.Clear();
                return;
            }

            BuildRuntime buildRuntime = BuildRuntimeResolver.Resolve(vehicleComponent);
            foreach (EmitterRuntime emitterRuntime in buildRuntime.Emitters)
            {
                BattleAttackRuntime attack = ToBattleAttackRuntime(emitterRuntime);
                if (attack != null)
                {
                    self.Attacks.Add(attack);
                }
            }

            self.TrimStaleTriggerRecords();
        }

        public static void ResetEmitterCooldown(this BattleAttackComponent self, long attackRuntimeId)
        {
            self?.EmitterCooldownEndTimeById.Remove(attackRuntimeId);
        }

        public static void SetSingleEffectAttack(this BattleAttackComponent self, long attackRuntimeId, int cooldownMs, float attackRange, bool canMoveCast, int buffGroupId)
        {
            self.SetSingleEffectAttack(attackRuntimeId, 0, cooldownMs, attackRange, 0.5f, canMoveCast, buffGroupId);
        }

        public static void SetSingleEffectAttack(this BattleAttackComponent self, long attackRuntimeId, int cooldownMs, float attackRange, float attackHitRatio, bool canMoveCast, int buffGroupId)
        {
            self.SetSingleEffectAttack(attackRuntimeId, 0, cooldownMs, attackRange, attackHitRatio, canMoveCast, buffGroupId);
        }

        public static void SetSingleEffectAttack(this BattleAttackComponent self, long attackRuntimeId, int sourceConfigId, int cooldownMs, float attackRange, float attackHitRatio, bool canMoveCast, int buffGroupId)
        {
            self.Attacks.Clear();
            self.AddEffectAttack(attackRuntimeId, sourceConfigId, cooldownMs, attackRange, attackHitRatio, canMoveCast, buffGroupId);

            self.TrimStaleTriggerRecords();
        }

        public static void AddEffectAttack(this BattleAttackComponent self, long attackRuntimeId, int cooldownMs, float attackRange, bool canMoveCast, int buffGroupId)
        {
            self.AddEffectAttack(attackRuntimeId, cooldownMs, attackRange, 0.5f, canMoveCast, buffGroupId);
        }

        public static void AddEffectAttack(this BattleAttackComponent self, long attackRuntimeId, int cooldownMs, float attackRange, float attackHitRatio, bool canMoveCast, int buffGroupId)
        {
            self.AddEffectAttack(attackRuntimeId, 0, cooldownMs, attackRange, attackHitRatio, canMoveCast, buffGroupId);
        }

        public static void AddEffectAttack(this BattleAttackComponent self, long attackRuntimeId, int sourceConfigId, int cooldownMs, float attackRange, float attackHitRatio, bool canMoveCast, int buffGroupId)
        {
            self.AddAttack(new BattleAttackRuntime
            {
                AttackRuntimeId = attackRuntimeId,
                SourceConfigId = sourceConfigId,
                Level = 1,
                BuffSlotCount = buffGroupId > 0 ? 1 : 0,
                CooldownMs = cooldownMs > 0 ? cooldownMs : 1000,
                AttackRange = attackRange > 0f ? attackRange : 1.5f,
                AttackHitRatio = attackHitRatio,
                BaseDamage = 0f,
                WhiteAttackRatio = 0f,
                WhiteDamageMultiplier = 1.0f,
                CanMoveCast = canMoveCast,
                DeliveryType = BattleAttackDeliveryType.Instant,
                PayloadType = BattleAttackPayloadType.VehicleBuff,
                EffectPackIds = new List<int>(),
                BuffGroupIds = buffGroupId > 0 ? new List<int> { buffGroupId } : new List<int>(),
            });
        }

        public static void AddAttack(this BattleAttackComponent self, BattleAttackRuntime attack)
        {
            if (attack == null)
            {
                return;
            }

            attack.CooldownMs = attack.CooldownMs > 0 ? attack.CooldownMs : 1000;
            attack.AttackRange = attack.AttackRange > 0f ? attack.AttackRange : 1.5f;
            attack.AttackHitRatio = NormalizeAttackHitRatio(attack.AttackHitRatio);
            attack.EffectPackIds ??= new List<int>();
            attack.BuffGroupIds ??= new List<int>();
            self.Attacks.Add(attack);
        }

        public static bool HasReadyAttackOutOfRange(this BattleAttackComponent self, float distance, long nowMs)
        {
            if (self == null)
            {
                return false;
            }

            foreach (BattleAttackRuntime attack in self.Attacks)
            {
                if (attack == null || !self.IsAttackReady(attack, nowMs))
                {
                    continue;
                }

                if (distance > attack.AttackRange)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasReadyAttackInRange(this BattleAttackComponent self, float distance, long nowMs)
        {
            if (self == null)
            {
                return false;
            }

            foreach (BattleAttackRuntime attack in self.Attacks)
            {
                if (attack == null || !self.IsAttackReady(attack, nowMs))
                {
                    continue;
                }

                if (distance <= attack.AttackRange)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryTriggerReadyAttacks(this BattleAttackComponent self, Battle battle, BattleUnit target, long nowMs)
        {
            return self.TryTriggerReadyAttacks(battle, target, nowMs, out _);
        }

        public static bool TryTriggerReadyAttacks(this BattleAttackComponent self, Battle battle, BattleUnit target, long nowMs, out bool shouldStopForCast)
        {
            shouldStopForCast = false;
            BattleUnit attacker = self.GetParent<BattleUnit>();
            if (battle == null || attacker == null || attacker.IsDead || target == null || target.IsDead)
            {
                return false;
            }

            long targetId = target.Id;
            self.CurrentTargetId = targetId;
            float distance = math.abs(attacker.Position.x - target.Position.x);
            List<BattleAttackInstance> attackInstances = null;

            foreach (BattleAttackRuntime attack in self.Attacks)
            {
                if (attack == null)
                {
                    continue;
                }

                if (distance > attack.AttackRange || !self.IsAttackReady(attack, nowMs))
                {
                    continue;
                }

                BattleAttackInstance attackInstance = battle.AddChild<BattleAttackInstance, long, long, BattleAttackRuntime>(
                    attacker.Id,
                    targetId,
                    CopyRuntime(attack));
                attackInstance.MarkWaitingForHit();

                attackInstances ??= new List<BattleAttackInstance>();
                attackInstances.Add(attackInstance);
                shouldStopForCast |= !attack.CanMoveCast;

                self.EmitterCooldownEndTimeById[attack.AttackRuntimeId] = nowMs + attack.CooldownMs;
            }

            if (attackInstances == null || attackInstances.Count == 0)
            {
                return false;
            }

            int hitDelayMs = 0;
            BattleUnitView view = attacker.GetComponent<BattleUnitView>();
            if (view != null && !view.IsDisposed)
            {
                view.PlayAttackFeedback(null, attackInstances[0].AttackRuntime.AttackHitRatio);
                hitDelayMs = (int)System.Math.Max(0, view.AttackHitTime * 1000f);
            }

            if (shouldStopForCast)
            {
                int lockDurationMs = System.Math.Max(100, hitDelayMs);
                self.CastMoveLockEndTime = System.Math.Max(self.CastMoveLockEndTime, nowMs + lockDurationMs);
                attacker.Forward = float3.zero;
            }

            ResolveAttackInstancesAfterDelay(attacker.Scene(), attackInstances, hitDelayMs).Coroutine();

            EventSystem.Instance.Publish(attacker.Scene(), new BattleUnitSkillCast { Unit = attacker });
            return true;
        }

        private static async ETTask ResolveAttackInstancesAfterDelay(Scene scene, List<BattleAttackInstance> attackInstances, int delayMs)
        {
            if (scene == null || attackInstances == null || attackInstances.Count == 0)
            {
                return;
            }

            if (delayMs > 0)
            {
                await scene.Root().GetComponent<TimerComponent>().WaitAsync(delayMs);
            }

            foreach (BattleAttackInstance attackInstance in attackInstances)
            {
                if (attackInstance == null || attackInstance.IsDisposed)
                {
                    continue;
                }

                attackInstance.ResolveHit();
            }
        }

        public static bool IsAttackReady(this BattleAttackComponent self, BattleAttackRuntime attack, long nowMs)
        {
            long cooldownEndTime = self.EmitterCooldownEndTimeById.TryGetValue(attack.AttackRuntimeId, out long value) ? value : 0;
            return nowMs >= cooldownEndTime;
        }

        private static BattleAttackRuntime CopyRuntime(BattleAttackRuntime attack)
        {
            return new BattleAttackRuntime
            {
                AttackRuntimeId = attack.AttackRuntimeId,
                SourceConfigId = attack.SourceConfigId,
                Level = System.Math.Max(1, attack.Level),
                CooldownMs = attack.CooldownMs,
                AttackRange = attack.AttackRange,
                AttackHitRatio = attack.AttackHitRatio,
                BaseDamage = attack.BaseDamage,
                WhiteAttackRatio = attack.WhiteAttackRatio,
                WhiteDamageMultiplier = attack.WhiteDamageMultiplier > 0f ? attack.WhiteDamageMultiplier : 1.0f,
                CanMoveCast = attack.CanMoveCast,
                DeliveryType = attack.DeliveryType,
                PayloadType = attack.PayloadType,
                EffectPackIds = attack.EffectPackIds != null ? new List<int>(attack.EffectPackIds) : new List<int>(),
                BuffGroupIds = new List<int>(attack.BuffGroupIds),
            };
        }

        private static BattleAttackRuntime ToBattleAttackRuntime(EmitterRuntime emitterRuntime)
        {
            if (emitterRuntime == null)
            {
                return null;
            }

            return new BattleAttackRuntime
            {
                AttackRuntimeId = emitterRuntime.RuntimeId,
                SourceConfigId = emitterRuntime.EmitterConfigId,
                Level = System.Math.Max(1, emitterRuntime.Level),
                BuffSlotCount = System.Math.Max(0, emitterRuntime.BuffSlotCount),
                CooldownMs = emitterRuntime.CooldownMs,
                AttackRange = emitterRuntime.AttackRange,
                AttackHitRatio = NormalizeAttackHitRatio(emitterRuntime.AttackHitRatio),
                BaseDamage = emitterRuntime.BaseDamage,
                WhiteAttackRatio = emitterRuntime.WhiteAttackRatio,
                WhiteDamageMultiplier = emitterRuntime.WhiteDamageMultiplier > 0f ? emitterRuntime.WhiteDamageMultiplier : 1.0f,
                CanMoveCast = emitterRuntime.CanMoveCast,
                DeliveryType = emitterRuntime.DeliveryType,
                PayloadType = emitterRuntime.PayloadType,
                EffectPackIds = emitterRuntime.EffectPackIds != null ? new List<int>(emitterRuntime.EffectPackIds) : new List<int>(),
                BuffGroupIds = emitterRuntime.BuffGroupIds != null ? new List<int>(emitterRuntime.BuffGroupIds) : new List<int>(),
            };
        }

        private static float NormalizeAttackHitRatio(float ratio)
        {
            if (ratio <= 0f || ratio > 1f)
            {
                return 0.5f;
            }

            return ratio;
        }

        private static void TrimStaleTriggerRecords(this BattleAttackComponent self)
        {
            if (self.EmitterCooldownEndTimeById.Count == 0)
            {
                return;
            }

            HashSet<long> activeIds = new();
            foreach (BattleAttackRuntime attack in self.Attacks)
            {
                if (attack != null)
                {
                    activeIds.Add(attack.AttackRuntimeId);
                }
            }

            List<long> staleIds = null;
            foreach (long attackId in self.EmitterCooldownEndTimeById.Keys)
            {
                if (activeIds.Contains(attackId))
                {
                    continue;
                }

                staleIds ??= new List<long>();
                staleIds.Add(attackId);
            }

            if (staleIds == null)
            {
                return;
            }

            foreach (long staleId in staleIds)
            {
                self.EmitterCooldownEndTimeById.Remove(staleId);
            }
        }
    }
}
