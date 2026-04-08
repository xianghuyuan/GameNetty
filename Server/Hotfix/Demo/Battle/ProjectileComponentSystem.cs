using System;
using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 投射物移动心跳定时器
    /// </summary>
    [Invoke(TimerInvokeType.ProjectileTick)]
    public class ProjectileTimer : ATimer<ProjectileComponent>
    {
        protected override void Run(ProjectileComponent self)
        {
            ProjectileComponentSystem.OnProjectileTick(self);
        }
    }

    [EntitySystemOf(typeof(ProjectileComponent))]
    [FriendOf(typeof(ProjectileComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class ProjectileComponentSystem
    {
        private const long ProjectileTickInterval = 50; // 50ms 投射物心跳（比普通移动更频繁，减少穿透）

        [EntitySystem]
        private static void Awake(this ProjectileComponent self, int skillId, long casterId, long targetUnitId)
        {
            self.SkillId = skillId;
            self.CasterId = casterId;
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
            self.StartPosition = self.GetParent<BattleUnit>().Position;
            self.PreviousPosition = self.StartPosition;
        }

        [EntitySystem]
        private static void Destroy(this ProjectileComponent self)
        {
            long timerId = self.TimerId;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref timerId);
            self.TimerId = 0;
            self.HitSet.Clear();
        }

        /// <summary>
        /// 初始化并启动投射物飞行
        /// </summary>
        public static void Launch(this ProjectileComponent self, SkillConfig skillConfig, BattleUnit caster, BattleUnit target)
        {
            BattleUnit projectileUnit = self.GetParent<BattleUnit>();
            if (projectileUnit == null || skillConfig == null || caster == null)
            {
                return;
            }

            SkillTargetingConfig targetingConfig = skillConfig.TargetingConfigIdConfig;

            self.Camp = caster.Camp;
            self.Speed = skillConfig.ProjectileSpeed > 0 ? skillConfig.ProjectileSpeed : 8f;
            self.MaxDistance = skillConfig.ProjectileMaxDistance > 0 ? skillConfig.ProjectileMaxDistance : 20f;
            self.CollisionRadius = skillConfig.ProjectileCollisionRadius;
            self.IsPiercing = skillConfig.ProjectilePiercing;
            self.MaxHitCount = skillConfig.ProjectileMaxHitCount;
            self.BuffGroupId = skillConfig.BuffGroupId;
            self.StartPosition = projectileUnit.Position;
            self.PreviousPosition = projectileUnit.Position;

            // 计算飞行方向
            if (target != null)
            {
                self.Direction = projectileUnit.Position.X <= target.Position.X ? 1f : -1f;
            }
            else
            {
                self.Direction = caster.Position.X <= projectileUnit.Position.X ? 1f : -1f;
            }

            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();

            // 启动心跳定时器
            self.TimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(ProjectileTickInterval, TimerInvokeType.ProjectileTick, self);

            // 通过事件通知投射物发射（客户端同步）
            EventSystem.Instance.Publish(projectileUnit.Root(), new ProjectileLaunchEvent
            {
                Projectile = projectileUnit,
                CasterId = caster.Id,
                SkillId = skillConfig.Id,
                Direction = self.Direction,
            });
        }

        /// <summary>
        /// 投射物心跳回调 - 每50ms执行一次
        /// </summary>
        internal static void OnProjectileTick(ProjectileComponent self)
        {
            BattleUnit projectileUnit = self.GetParent<BattleUnit>();
            if (projectileUnit == null)
            {
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            long deltaTime = currentTime - self.LastUpdateTime;
            self.LastUpdateTime = currentTime;

            // 记录上一帧位置
            Vector3 prevPos = projectileUnit.Position;

            // 移动投射物
            float moveDistance = self.Speed * deltaTime / 1000f;
            float newX = projectileUnit.Position.X + self.Direction * moveDistance;
            projectileUnit.Position = new Vector3(newX, projectileUnit.Position.Y, projectileUnit.Position.Z);
            self.PreviousPosition = prevPos;

            // 检查是否超过最大飞行距离
            float traveledDistance = BattleDistanceHelper.GetDistance(self.StartPosition, projectileUnit.Position);
            if (traveledDistance >= self.MaxDistance)
            {
                self.DestroyProjectile();
                return;
            }

            // 碰撞检测
            self.CheckCollisions(projectileUnit);
        }

        /// <summary>
        /// 碰撞检测 - 使用线段相交检测（1D下退化为区间重叠）
        /// </summary>
        private static void CheckCollisions(this ProjectileComponent self, BattleUnit projectileUnit)
        {
            BattleRoom battleRoom = projectileUnit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(self.SkillId);
            SkillTargetingConfig targetingConfig = skillConfig?.TargetingConfigIdConfig;
            BuffGroupConfig effectGroupConfig = skillConfig?.BuffGroupIdConfig;
            if (targetingConfig == null || effectGroupConfig == null)
            {
                return;
            }

            BattleUnit caster = FindUnitById(battleRoom, self.CasterId);

            float prevX = self.PreviousPosition.X;
            float currX = projectileUnit.Position.X;
            float pRadius = self.CollisionRadius;

            // 投射物在上一帧和当前帧之间扫过的X轴区间
            float sweepMin = Math.Min(prevX, currX) - pRadius;
            float sweepMax = Math.Max(prevX, currX) + pRadius;

            foreach (EntityRef<BattleUnit> unitRef in battleRoom.Units.Values)
            {
                BattleUnit target = unitRef;
                if (target == null || target.Id == projectileUnit.Id || target.Id == self.CasterId)
                {
                    continue;
                }

                if (target.IsDead)
                {
                    continue;
                }

                // 阵营检查
                if (!IsEnemyCamp(self.Camp, target.Camp))
                {
                    continue;
                }

                // 避免重复命中
                if (self.HitSet.Contains(target.Id))
                {
                    continue;
                }

                // 获取目标碰撞半径
                float targetRadius = GetTargetCollisionRadius(target);

                // 1D 线段相交检测：投射物扫过区间 与 目标区间 是否重叠
                float targetMin = target.Position.X - targetRadius;
                float targetMax = target.Position.X + targetRadius;

                if (sweepMax < targetMin || sweepMin > targetMax)
                {
                    continue;
                }

                // 命中！
                self.HitSet.Add(target.Id);

                // 通过事件通知伤害结算，避免静态类循环依赖
                EventSystem.Instance.Publish(projectileUnit.Root(), new ProjectileHitEvent
                {
                    Projectile = projectileUnit,
                    Target = target,
                    CasterId = self.CasterId,
                    SkillId = self.SkillId,
                });

                // 检查是否继续飞行
                if (!self.IsPiercing)
                {
                    self.DestroyProjectile();
                    return;
                }

                if (self.MaxHitCount > 0 && self.HitSet.Count >= self.MaxHitCount)
                {
                    self.DestroyProjectile();
                    return;
                }
            }
        }

        /// <summary>
        /// 销毁投射物 - 通过事件通知外部处理，避免静态类循环依赖
        /// </summary>
        private static void DestroyProjectile(this ProjectileComponent self)
        {
            BattleUnit projectileUnit = self.GetParent<BattleUnit>();
            if (projectileUnit == null)
            {
                return;
            }

            EventSystem.Instance.Publish(projectileUnit.Root(), new ProjectileDestroyEvent
            {
                Projectile = projectileUnit,
                CasterId = self.CasterId,
                SkillId = self.SkillId,
            });
        }

        private static bool IsEnemyCamp(UnitCamp casterCamp, UnitCamp targetCamp)
        {
            return casterCamp != targetCamp;
        }

        private static float GetTargetCollisionRadius(BattleUnit target)
        {
            return 0.5f;
        }

        private static BattleUnit FindUnitById(BattleRoom battleRoom, long unitId)
        {
            if (battleRoom == null || !battleRoom.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                return null;
            }

            return unitRef;
        }

        private static int GetDamageType(SkillConfig skillConfig)
        {
            if (skillConfig == null)
            {
                return 1;
            }

            return skillConfig.SkillKind == 1 ? 0 : 1;
        }
    }
}
