using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleMoveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleMoveComponent self)
        {
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
        }

        [EntitySystem]
        private static void Destroy(this BattleMoveComponent self)
        {
        }

        public static void StartMove(this BattleMoveComponent self, Vector3 targetPosition)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null)
            {
                return;
            }
            NumericComponent numeric = owner.GetComponent<NumericComponent>();
            float moveSpeed = numeric?.GetAsFloat(NumericType.Speed) ?? self.MoveSpeed;
            if (moveSpeed <= 0f)
            {
                moveSpeed = self.MoveSpeed;
            }
            self.MoveSpeed = moveSpeed;
        }

        public static void StopMove(this BattleMoveComponent self)
        {
            self.MoveSpeed = 0f;
        }

        [EntitySystem]
        private static void Update(this BattleMoveComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                self.StopMove();
                return;
            }

            float distance = BattleDistanceHelper.GetDistance(owner.Position, self.TargetPosition);
            if (distance < 0.01f)
            {
                self.StopMove();
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            long deltaTime = currentTime - self.LastUpdateTime;
            self.LastUpdateTime = currentTime;

            NumericComponent numeric = owner.GetComponent<NumericComponent>();
            float moveSpeed = numeric?.GetAsFloat(NumericType.Speed) ?? self.MoveSpeed;
            if (moveSpeed <= 0f)
            {
                moveSpeed = self.MoveSpeed;
            }
            self.MoveSpeed = moveSpeed;

            float moveDistance = moveSpeed * deltaTime / 1000f;
            Vector3 newPosition = BattleDistanceHelper.MoveTowardsX(owner.Position, self.TargetPosition, moveDistance);
            if (BattleDistanceHelper.GetDistance(owner.Position, newPosition) <= 0.0001f)
            {
                return;
            }

            owner.Position = newPosition;

            if (BattleDistanceHelper.GetDistance(owner.Position, self.TargetPosition) < 0.01f)
            {
                self.StopMove();
                EventSystem.Instance.Publish(self.Scene<BattleRoom>()!, new ReachTargetEvent { Unit = owner });
            }
        }
    }
}
