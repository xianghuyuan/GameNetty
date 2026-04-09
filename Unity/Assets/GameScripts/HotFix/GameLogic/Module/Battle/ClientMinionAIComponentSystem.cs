using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(ClientMinionAIComponent))]
    [FriendOf(typeof(ClientMinionAIComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class ClientMinionAIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientMinionAIComponent self)
        {
            self.TargetUnitId = 0;
            self.LastAttackTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this ClientMinionAIComponent self)
        {
            self.TargetUnitId = 0;
            self.LastAttackTime = 0;
        }

        /// <summary>
        /// AI Tick，由 ClientMinionAITickComponent 每 100ms 调用
        /// 杂兵 AI：找到最近的友方单位，朝其移动，进入攻击范围后攻击
        /// </summary>
        public static void Tick(this ClientMinionAIComponent self, Battle battle, long nowMs)
        {
            BattleUnit minion = self.GetParent<BattleUnit>();
            if (minion == null || minion.IsDead) return;

            // 寻找最近的友方目标
            BattleUnit target = self.FindNearestEnemy(battle, minion);
            if (target == null)
            {
                self.TargetUnitId = 0;
                minion.Forward = float3.zero;
                return;
            }

            self.TargetUnitId = target.Id;

            float distance = math.abs(minion.Position.x - target.Position.x);

            // 面朝目标
            float faceDir = target.Position.x >= minion.Position.x ? 1f : -1f;
            minion.FaceDirection = faceDir;

            // 获取攻击范围（使用 BattleUnitCombatComponent 的 AttackRange）
            BattleUnitCombatComponent combatComp = minion.GetComponent<BattleUnitCombatComponent>();
            float attackRange = combatComp?.AttackRange ?? 1.5f;

            if (distance <= attackRange)
            {
                // 在攻击范围内 → 停止移动
                minion.Forward = float3.zero;

                // 攻击冷却检查
                long attackCooldownMs = combatComp?.AttackCooldown ?? 1000;
                if (nowMs - self.LastAttackTime < attackCooldownMs)
                {
                    return;
                }

                self.LastAttackTime = nowMs;

                // 客户端权威：直接对目标造成伤害
                int damage = minion.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Attack) ?? 0;
                if (damage <= 0) return;

                int defense = target.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Defense) ?? 0;
                int finalDamage = System.Math.Max(1, damage - defense);

                target.GetComponent<BattleUnitCombatComponent>()?.TakeDamage(finalDamage);

                EventSystem.Instance.Publish(minion.Scene(), new BattleUnitSkillCast { Unit = minion });
                EventSystem.Instance.Publish(target.Scene(), new BattleUnitDamaged
                {
                    Unit = target,
                    AttackerId = minion.Id,
                    Damage = finalDamage,
                    IsCrit = false,
                });
            }
            // 不在攻击范围内 → 设置移动方向，由 BattleUnitViewSystem.Update 驱动增量移动
            minion.Forward = new float3(faceDir, 0, 0);
        }

        private static BattleUnit FindNearestEnemy(this ClientMinionAIComponent self, Battle battle, BattleUnit minion)
        {
            BattleUnit nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsDead) continue;
                if (unit.Camp == minion.Camp) continue;

                float dist = math.abs(minion.Position.x - unit.Position.x);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit;
                }
            }

            return nearest;
        }
    }
}
