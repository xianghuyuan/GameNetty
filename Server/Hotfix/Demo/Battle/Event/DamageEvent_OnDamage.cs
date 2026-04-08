namespace ET.Server
{
    /// <summary>
    /// 伤害事件处理器 - 订阅 DamageEvent，负责扣血、广播伤害和死亡。
    /// 杂兵死亡通过 M2C_BatchDamage 的 deadUnitIds 下发，此处只广播 Boss 死亡。
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
                await ETTask.CompletedTask;
                return;
            }

            // 兼容新旧调用方：如果 Attacker 为空但 CasterId 有效，尝试查找施法者
            BattleUnit attacker = args.Attacker;
            if (attacker == null && args.CasterId > 0)
            {
                BattleRoom battleRoom = target.GetParent<BattleRoom>();
                attacker = battleRoom?.GetUnit(args.CasterId);
            }

            // 应用伤害
            target.TakeDamage(args.Damage);

            // 广播伤害给客户端
            BattleUnitHelper.BroadcastDamage(attacker, target, args.Damage, args.DamageType);

            // Boss 死亡广播 M2C_UnitDead（杂兵死亡已通过 M2C_BatchDamage.deadUnitIds 下发）
            if (target.IsDead)
            {
                bool isBoss = target.Camp == UnitCamp.Enemy && IsBossUnit(target);
                if (isBoss)
                {
                    BattleUnitHelper.BroadcastUnitDead(target, attacker?.Id ?? args.CasterId);
                }
            }

            await ETTask.CompletedTask;
        }

        private static bool IsBossUnit(BattleUnit target)
        {
            return target != null && target.IsBoss;
        }
    }
}
