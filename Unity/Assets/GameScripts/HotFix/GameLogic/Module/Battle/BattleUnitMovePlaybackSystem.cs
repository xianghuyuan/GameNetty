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
            self.CommandVersion++;
        }

        public static void ApplyMoveCommand(this BattleMoveComponent self, Scene root, float3 targetPosition, float moveSpeed)
        {
            self.CommandVersion++;
            self.IsMoving = true;
            self.TargetPosition = targetPosition;
            self.MoveSpeed = moveSpeed;
            self.MoveAsync(root, self.CommandVersion).Coroutine();
        }

        public static void StopMove(this BattleMoveComponent self, float3 finalPosition)
        {
            self.CommandVersion++;
            self.IsMoving = false;
            self.TargetPosition = finalPosition;

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

        private static async ETTask MoveAsync(this BattleMoveComponent self, Scene root, int version)
        {
            TimerComponent timerComponent = root.GetComponent<TimerComponent>();
            if (timerComponent == null)
            {
                return;
            }

            while (!self.IsDisposed && self.IsMoving && version == self.CommandVersion)
            {
                await timerComponent.WaitFrameAsync();

                if (self.IsDisposed || !self.IsMoving || version != self.CommandVersion)
                {
                    break;
                }

                BattleUnit unit = self.GetParent<BattleUnit>();
                if (unit == null || unit.IsDisposed || unit.IsDead)
                {
                    break;
                }

                float deltaX = self.TargetPosition.x - unit.Position.x;
                float distance = math.abs(deltaX);
                if (distance <= 0.0001f)
                {
                    unit.Position = self.TargetPosition;
                    self.IsMoving = false;
                }
                else
                {
                    float moveDistance = self.MoveSpeed * Time.deltaTime;
                    if (moveDistance <= 0f || moveDistance >= distance)
                    {
                        unit.Position = self.TargetPosition;
                        self.IsMoving = false;
                    }
                    else
                    {
                        unit.Position = new float3(unit.Position.x + math.sign(deltaX) * moveDistance, unit.Position.y, unit.Position.z);
                    }
                }

                Battle battle = unit.GetParent<Battle>();
                BattleUnitViewComponent viewComponent = battle?.GetComponent<BattleUnitViewComponent>();
                viewComponent?.UpdateViewPosition(unit.Id, unit.Position);
            }
        }
    }
}
