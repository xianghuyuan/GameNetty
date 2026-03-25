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
            self.TargetPosition = targetPosition;
            self.MoveSpeed = moveSpeed;
            self.Duration = duration;
            self.MoveCoefficient = moveCoefficient;
            self.StartTime = Time.time;
            self.IsMoving = true;
            self.MoveAsync().Coroutine();
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

            Battle battle = unit.GetParent<Battle>();
            BattleUnitViewComponent viewComponent = battle?.GetComponent<BattleUnitViewComponent>();
            viewComponent?.UpdateViewPosition(unit.Id, finalPosition);
        }

        private static async ETTask MoveAsync(this BattleMoveComponent self)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit == null)
            {
                return;
            }
            Debug.Log(string.Format($"创建移动任务，当前位置:{unit.Position.x}，目标位置{self.TargetPosition.x}"));
            Battle battle = unit.GetParent<Battle>();
            BattleUnitViewComponent viewComponent = battle?.GetComponent<BattleUnitViewComponent>();
            viewComponent?.UpdateViewPosition(unit.Id, unit.Position,self.Duration);
        }
    }
}
