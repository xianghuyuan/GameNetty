using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(ClientPlayerAIComponent))]
    [FriendOf(typeof(ClientPlayerAIComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class ClientPlayerAIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientPlayerAIComponent self)
        {
            self.CurrentTargetId = 0;
            self.LastAttackTimeMs = 0;
            self.SkillCooldownEnd.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ClientPlayerAIComponent self)
        {
            self.CurrentTargetId = 0;
            self.LastAttackTimeMs = 0;
            self.SkillCooldownEnd.Clear();
        }

        public static void Tick(this ClientPlayerAIComponent self, Battle battle, long nowMs)
        {
            BattleUnit player = self.GetParent<BattleUnit>();
            if (player == null || player.IsDead) return;

            self.CleanupCooldowns(nowMs);

            BattleUnit target = self.FindNearestEnemy(battle, player);
            if (target == null)
            {
                self.CurrentTargetId = 0;
                player.Forward = float3.zero;
                return;
            }

            self.CurrentTargetId = target.Id;

            BattleUnitCombatComponent combatComp = player.GetComponent<BattleUnitCombatComponent>();
            if (combatComp?.AutoSkillIds == null || combatComp.AutoSkillIds.Length == 0) return;

            float distance = math.abs(player.Position.x - target.Position.x);

            // 遍历自动技能，找第一个在射程内且CD就绪的
            float bestCastRange = 0f;
            int bestSkillId = 0;
            float shortestReadyRange = float.MaxValue;   // CD就绪技能中最短射程
            float shortestAllRange = float.MaxValue;     // 所有技能中最短射程

            foreach (int skillId in combatComp.AutoSkillIds)
            {
                SkillConfig skillConfig = ConfigHelper.SkillConfig?.GetOrDefault(skillId);
                if (skillConfig == null || !skillConfig.IsEnabled) continue;

                SkillTargetingConfig targetingConfig = skillConfig.TargetingConfigId_Ref;
                if (targetingConfig == null) continue;

                float castRange = targetingConfig.CastRange + targetingConfig.EdgeDistance;

                // 统计所有技能最短射程（不受CD限制）
                if (castRange < shortestAllRange)
                {
                    shortestAllRange = castRange;
                }

                if (self.IsSkillOnCooldown(skillId, nowMs)) continue;

                // 统计CD就绪技能最短射程
                if (castRange < shortestReadyRange)
                {
                    shortestReadyRange = castRange;
                }

                if (distance <= castRange)
                {
                    bestSkillId = skillId;
                    bestCastRange = castRange;
                    break;
                }

                if (castRange > bestCastRange)
                {
                    bestCastRange = castRange;
                    bestSkillId = skillId;
                }
            }

            // 面朝目标
            float faceDir = target.Position.x >= player.Position.x ? 1f : -1f;
            player.FaceDirection = faceDir;

            // 停移判定：优先用CD就绪技能的最短射程，全部CD中则用所有技能的最短射程
            float stopRange = shortestReadyRange < float.MaxValue ? shortestReadyRange : shortestAllRange;
            if (stopRange > 0f && distance <= stopRange)
            {
                player.Forward = float3.zero;

                // CD就绪 → 释放
                if (bestSkillId > 0 && distance <= bestCastRange)
                {
                    SkillConfig skillConfig = ConfigHelper.SkillConfig?.GetOrDefault(bestSkillId);
                    self.LastAttackTimeMs = nowMs;
                    self.SetSkillCooldown(bestSkillId, nowMs + skillConfig.CooldownMs);

                    EventSystem.Instance.Publish(player.Scene(), new BattleUnitSkillCast { Unit = player });

                    if (battle.GetComponent<OfflineBattleComponent>() != null)
                    {
                        // 离线：通过攻击动画命中点延迟伤害，视觉上攻击动作与伤害同步
                        var view = player.GetComponent<BattleUnitView>();
                        long targetId = target.Id;
                        int skillId = bestSkillId;
                        long attackerId = player.Id;

                        void OnHit()
                        {
                            // 命中时目标可能已被其他攻击杀死
                            BattleUnit hitTarget = battle.GetChild<BattleUnit>(targetId);
                            if (hitTarget == null || hitTarget.IsDisposed || hitTarget.IsDead) return;

                            var result = OfflineBattleDamageHelper.ApplySkillEffects(player, hitTarget, skillId);
                            // 配置不全时 fallback：直接用 ATK - DEF 结算
                            if (result.TotalDamage <= 0 && !hitTarget.IsDead)
                            {
                                int atk = player.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Attack) ?? 0;
                                int def = hitTarget.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Defense) ?? 0;
                                int dmg = System.Math.Max(1, atk - def);
                                hitTarget.GetComponent<BattleUnitCombatComponent>()?.TakeDamage(dmg);
                                EventSystem.Instance.Publish(hitTarget.Scene(), new BattleUnitDamaged
                                {
                                    Unit = hitTarget,
                                    AttackerId = attackerId,
                                    Damage = dmg,
                                    IsCrit = false,
                                });
                            }
                        }

                        view?.PlayAttackFeedback(OnHit);
                    }
                    else
                    {
                        BattleHelper.CastSkill(player.Scene(), bestSkillId, target.Id).Coroutine();

                        // 客户端权威：本地碰撞检测 + 立刻扣血 + 发送 C2M_ClientBatchHit 给服务端验证
                        ClientBattleDamageHelper.ApplySkillOnMinions(battle, player, bestSkillId);
                    }
                }
                return;
            }

            // 不在射程内 → 设定移动方向，同步位置给服务端
            player.Forward = new float3(faceDir, 0, 0);
            BattleHelper.SyncPlayerPosition(player.Scene(), battle.BattleId, player.Position);
        }

        private static BattleUnit FindNearestEnemy(this ClientPlayerAIComponent self, Battle battle, BattleUnit player)
        {
            BattleUnit nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsDead) continue;
                if (unit.Camp == player.Camp) continue;

                float dist = math.abs(player.Position.x - unit.Position.x);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit;
                }
            }

            return nearest;
        }

        private static bool IsSkillOnCooldown(this ClientPlayerAIComponent self, int skillId, long nowMs)
        {
            return self.SkillCooldownEnd.TryGetValue(skillId, out long cooldownEnd) && nowMs < cooldownEnd;
        }

        private static void SetSkillCooldown(this ClientPlayerAIComponent self, int skillId, long endTime)
        {
            self.SkillCooldownEnd[skillId] = endTime;
        }

        private static void CleanupCooldowns(this ClientPlayerAIComponent self, long nowMs)
        {
            List<int> expired = null;
            foreach (var kv in self.SkillCooldownEnd)
            {
                if (nowMs >= kv.Value)
                {
                    expired ??= new List<int>();
                    expired.Add(kv.Key);
                }
            }

            if (expired != null)
            {
                foreach (int skillId in expired)
                {
                    self.SkillCooldownEnd.Remove(skillId);
                }
            }
        }
    }
}
