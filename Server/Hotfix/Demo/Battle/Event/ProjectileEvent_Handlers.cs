namespace ET.Server
{
    /// <summary>
    /// 投射物发射事件处理器 - 负责广播投射物发射消息给客户端
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    public class ProjectileLaunchEvent_Handler : AEvent<Scene, ProjectileLaunchEvent>
    {
        protected override async ETTask Run(Scene scene, ProjectileLaunchEvent args)
        {
            if (args.Projectile == null)
            {
                return;
            }

            BattleUnitHelper.BroadcastProjectileLaunch(args.Projectile, args.CasterId, args.SkillId, args.Direction);

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 投射物命中事件处理器 - 负责伤害结算和广播
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    public class ProjectileHitEvent_Handler : AEvent<Scene, ProjectileHitEvent>
    {
        protected override async ETTask Run(Scene scene, ProjectileHitEvent args)
        {
            if (args.Projectile == null || args.Target == null || args.Target.IsDead)
            {
                return;
            }

            BattleRoom battleRoom = args.Target.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            if (!battleRoom.Units.TryGetValue(args.CasterId, out EntityRef<BattleUnit> casterRef))
            {
                return;
            }

            BattleUnit caster = casterRef;
            if (caster == null)
            {
                return;
            }

            SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(args.SkillId);
            BuffGroupConfig effectGroupConfig = skillConfig?.BuffGroupIdConfig;
            if (effectGroupConfig == null)
            {
                return;
            }

            // 通过 BuffComponent 统一执行buff（内部已包含伤害广播和死亡广播）
            BattleSkillHelper.ApplyEffects(caster, args.Target, effectGroupConfig, skillConfig);
            BattleUnitHelper.BroadcastProjectileHit(args.Projectile, args.Target.Id);

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 投射物销毁事件处理器 - 负责从战场移除投射物和广播
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    public class ProjectileDestroyEvent_Handler : AEvent<Scene, ProjectileDestroyEvent>
    {
        protected override async ETTask Run(Scene scene, ProjectileDestroyEvent args)
        {
            if (args.Projectile == null)
            {
                return;
            }

            BattleUnitHelper.BroadcastProjectileDestroy(args.Projectile);

            BattleRoom battleRoom = args.Projectile.GetParent<BattleRoom>();
            if (battleRoom != null)
            {
                battleRoom.RemoveUnit(args.Projectile.Id);
            }

            await ETTask.CompletedTask;
        }
    }
}
