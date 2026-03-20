using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleMoveComponentSystem
    {
        private const float MoveCommandTargetThreshold = 0.25f;

        [EntitySystem]
        private static void Awake(this BattleMoveComponent self)
        {
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
        }

        [EntitySystem]
        private static void Destroy(this BattleMoveComponent self)
        {
            self.IsMoving = false;
            self.IsMoveCommandActive = false;
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

            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
            self.TargetPosition = targetPosition;
            self.MoveSpeed = moveSpeed;
            self.IsMoving = true;

            if (!self.IsMoveCommandActive || BattleDistanceHelper.GetDistance(self.LastMoveCommandTarget, targetPosition) >= MoveCommandTargetThreshold)
            {
                BattleUnitHelper.BroadcastMoveCommand(owner, targetPosition, moveSpeed, true);
                self.IsMoveCommandActive = true;
                self.LastMoveCommandTarget = targetPosition;
            }
        }

        public static void StopMove(this BattleMoveComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
            self.IsMoving = false;

            if (!self.IsMoveCommandActive || owner == null)
            {
                return;
            }

            self.IsMoveCommandActive = false;
            self.LastMoveCommandTarget = owner.Position;
            BattleUnitHelper.BroadcastMoveCommand(owner, owner.Position, 0f, false);
        }

        public static void Update(this BattleMoveComponent self)
        {
            if (!self.IsMoving)
            {
                return;
            }

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
            }
        }
    }
}
