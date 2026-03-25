namespace ET.Server
{
    /// <summary>
    /// 伤害事件处理器 - 订阅 DamageEvent
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    [FriendOf(typeof(NumericComponent))]
    public class DamageEvent_OnDamage : AEvent<Scene, DamageEvent>
    {
        protected override async ETTask Run(Scene scene, DamageEvent args)
        {
            BattleUnit target = args.Target;
            if (target == null || target.IsDead)
            {
                return;
            }
            
            // 应用伤害
            target.TakeDamage(args.Damage);
            
            // 广播伤害给客户端
            BattleUnitHelper.BroadcastDamage(args.Attacker, target, args.Damage, args.DamageType);
            
            // 如果死亡，广播死亡
            if (target.IsDead)
            {
                BattleUnitHelper.BroadcastUnitDead(target, args.Attacker?.Id ?? 0);
            }
            
            await ETTask.CompletedTask;
        }
    }
}
