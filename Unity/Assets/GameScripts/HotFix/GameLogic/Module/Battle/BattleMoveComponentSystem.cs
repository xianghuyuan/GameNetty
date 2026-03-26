using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleMoveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleMoveComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BattleMoveComponent self)
        {
            self.IsMoving = false;
            self.FollowTargetUnitId = 0;
            self.CommandVersion++;
        }

        public static void ApplyMoveCommand(this BattleMoveComponent self, Scene root, float3 targetPosition, float moveSpeed,
            float duration, float moveCoefficient)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit == null)
            {
                return;
            }

            BattleUnitView view = unit.GetComponent<BattleUnitView>();
            Vector3? viewPos = view?.GameObject != null ? view.GameObject.transform.position : null;
            float3 from = viewPos.HasValue
                ? new float3(viewPos.Value.x - BattleAreaConfig.BattleAreaCenter.x, unit.Position.y, unit.Position.z)
                : unit.Position;

            unit.Position = from;
            self.TargetPosition = targetPosition;
            self.MoveSpeed = moveSpeed;
            self.Duration = duration;
            self.MoveCoefficient = moveCoefficient;
            self.StartTime = Time.time;
            self.IsMoving = true;

            float timer = moveSpeed > 0f ? Mathf.Abs(from.x - targetPosition.x) / moveSpeed : 0f;
            BattleMoveDebugLog.Write(
                $"ApplyMove unit={unit.Id} fromLogical={from} viewPos={(viewPos.HasValue ? viewPos.Value.ToString() : "null")} target={targetPosition} speed={moveSpeed:F3} timer={timer:F3}");

            EventSystem.Instance.Publish(root, new BattleUnitMoveStarted
            {
                Unit = unit,
                From = from,
                To = targetPosition,
                Duration = timer,
            });
        }

        public static void StopMove(this BattleMoveComponent self, float3 finalPosition)
        {
            self.CommandVersion++;
            self.IsMoving = false;
            self.TargetPosition = finalPosition;
            self.FollowTargetUnitId = 0;

            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit == null)
            {
                return;
            }

            unit.Position = finalPosition;
            BattleMoveDebugLog.Write($"StopMove unit={unit.Id} finalPos={finalPosition}");

            EventSystem.Instance.Publish(self.Scene(), new BattleUnitMoveStopped
            {
                Unit = unit,
                FinalPosition = finalPosition,
            });
        }
    }
}
