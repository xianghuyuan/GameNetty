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
    [FriendOf(typeof(BattleUnitRegistryComponent))]
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

            BattleUnit caster = battleRoom.GetUnit(args.CasterId);
            if (caster == null)
            {
                return;
            }

            EmitterConfig skillConfig = EmitterConfigCategory.Instance.GetOrDefault(args.SkillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return;
            }

            BattleSkillHelper.ApplyEmitterDamage(caster, args.Target, skillConfig);
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
