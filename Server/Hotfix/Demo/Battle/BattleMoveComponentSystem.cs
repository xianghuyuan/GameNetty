using System.Numerics;

namespace ET.Server
{
    [Invoke(TimerInvokeType.BattleMoveTick)]
    public class BattleMoveTimer : ATimer<BattleMoveComponent>
    {
        protected override void Run(BattleMoveComponent self)
        {
            BattleMoveComponentSystem.OnMoveTick(self);
        }
    }

    [EntitySystemOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleMoveComponentSystem
    {
        private const long MoveTickInterval = 100; // 100ms 移动心跳

        [EntitySystem]
        private static void Awake(this BattleMoveComponent self)
        {
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
            self.MoveTimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(MoveTickInterval, TimerInvokeType.BattleMoveTick, self);
        }

        [EntitySystem]
        private static void Destroy(this BattleMoveComponent self)
        {
            long timerId = self.MoveTimerId;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref timerId);
            self.MoveTimerId = 0;
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
            self.TargetPosition = targetPosition;
            BattleUnitHelper.BroadcastMoveCommand(owner,targetPosition,moveSpeed);
        }

        public static void StopMove(this BattleMoveComponent self)
        {
            self.MoveSpeed = 0f;
        }

        /// <summary>
        /// 移动定时器回调 - 每100ms执行一次
        /// </summary>
        internal static void OnMoveTick(BattleMoveComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead || self.MoveSpeed <= 0f)
            {
                return;
            }

            float distance = BattleDistanceHelper.GetDistance(owner.Position, self.TargetPosition);
            if (distance < 0.01f)
            {
                self.StopMove();
                EventSystem.Instance.Publish(self.Scene<BattleRoom>()!, new ReachTargetEvent { Unit = owner });
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            long deltaTime = currentTime - self.LastUpdateTime;
            self.LastUpdateTime = currentTime;

            float moveDistance = self.MoveSpeed * deltaTime / 1000f;
            Vector3 newPosition = BattleDistanceHelper.MoveTowardsX(owner.Position, self.TargetPosition, moveDistance);

            if (BattleDistanceHelper.GetDistance(owner.Position, newPosition) <= 0.0001f)
            {
                return;
            }

            owner.Position = newPosition;
        }
    }
}
