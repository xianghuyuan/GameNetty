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
        }

        [EntitySystem]
        private static void Destroy(this ClientPlayerAIComponent self)
        {
            self.CurrentTargetId = 0;
        }

        public static void Tick(this ClientPlayerAIComponent self, Battle battle, long nowMs)
        {
            BattleUnit player = self.GetParent<BattleUnit>();
            if (player == null || player.IsDead) return;

            // 通用攻击组件统一驱动玩家发射器/载具攻击。
            BattleAttackComponent battleAttack = player.GetComponent<BattleAttackComponent>();
            VehicleBuffComponent vehicleBuff = player.GetComponent<VehicleBuffComponent>();
            vehicleBuff?.OnTick(nowMs);
            if (battleAttack == null)
            {
                player.Forward = float3.zero;
                return;
            }

            VehicleComponent vehicleComponent = player.GetComponent<VehicleComponent>();
            battleAttack.SyncFromVehicleComponent(vehicleComponent);
            if (!battleAttack.HasAttacks())
            {
                self.CurrentTargetId = 0;
                player.Forward = float3.zero;
                return;
            }

            BattleUnit target = FindNearestEnemy(battle, player);
            if (target == null)
            {
                self.CurrentTargetId = 0;
                player.Forward = float3.zero;
                return;
            }

            self.CurrentTargetId = target.Id;
            battleAttack.CurrentTargetId = target.Id;

            float distance = math.abs(player.Position.x - target.Position.x);
            float faceDir = target.Position.x >= player.Position.x ? 1f : -1f;
            player.FaceDirection = faceDir;

            if (battleAttack.IsCastMoveLocked(nowMs))
            {
                player.Forward = float3.zero;
                return;
            }

            if (battleAttack.HasReadyAttackInRange(distance, nowMs))
            {
                player.Forward = float3.zero;
                battleAttack.TryTriggerReadyAttacks(battle, target, nowMs);
                return;
            }

            if (battleAttack.HasReadyAttackOutOfRange(distance, nowMs))
            {
                player.Forward = new float3(faceDir, 0, 0);
                BattleHelper.SyncPlayerPosition(player.Scene(), battle.BattleId, player.Position);
                return;
            }

            player.Forward = float3.zero;
        }

        private static BattleUnit FindNearestEnemy(Battle battle, BattleUnit player)
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
    }
}
