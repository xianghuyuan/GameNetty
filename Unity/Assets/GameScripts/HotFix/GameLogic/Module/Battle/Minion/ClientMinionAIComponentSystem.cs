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
            self.IsHoldingAttackRange = false;
        }

        [EntitySystem]
        private static void Destroy(this ClientMinionAIComponent self)
        {
            self.TargetUnitId = 0;
            self.IsHoldingAttackRange = false;
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
                self.IsHoldingAttackRange = false;
                minion.Forward = float3.zero;
                return;
            }

            self.TargetUnitId = target.Id;

            float distance = math.abs(minion.Position.x - target.Position.x);
            BattleAttackComponent battleAttack = minion.GetComponent<BattleAttackComponent>();
            if (battleAttack == null || !battleAttack.HasAttacks())
            {
                self.IsHoldingAttackRange = false;
                minion.Forward = float3.zero;
                return;
            }

            // 面朝目标
            float faceDir = target.Position.x >= minion.Position.x ? 1f : -1f;
            minion.FaceDirection = faceDir;

            if (battleAttack.IsCastMoveLocked(nowMs))
            {
                minion.Forward = float3.zero;
                return;
            }

            if (battleAttack.HasReadyAttackInRange(distance, nowMs))
            {
                self.IsHoldingAttackRange = true;
                minion.Forward = float3.zero;
                battleAttack.TryTriggerReadyAttacks(battle, target, nowMs);
                return;
            }

            if (battleAttack.HasReadyAttackOutOfRange(distance, nowMs))
            {
                self.IsHoldingAttackRange = false;
                minion.Forward = new float3(faceDir, 0, 0);
                return;
            }

            self.IsHoldingAttackRange = true;
            minion.Forward = float3.zero;
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
