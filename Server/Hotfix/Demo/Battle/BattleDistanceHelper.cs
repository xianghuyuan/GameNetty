using System;
using System.Numerics;

namespace ET.Server
{
    public static class BattleDistanceHelper
    {
        public static float GetDistance(Vector3 from, Vector3 to)
        {
            return MathF.Abs(to.X - from.X);
        }

        public static float GetDeltaX(Vector3 from, Vector3 to)
        {
            return to.X - from.X;
        }

        public static Vector3 MoveTowardsX(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            float deltaX = GetDeltaX(current, target);
            float distance = MathF.Abs(deltaX);
            if (distance <= 0.0001f)
            {
                return current;
            }

            float directionX = deltaX > 0 ? 1f : -1f;
            float moveDistance = MathF.Min(distance, maxDistanceDelta);
            return new Vector3(current.X + directionX * moveDistance, current.Y, current.Z);
        }
    }
}
