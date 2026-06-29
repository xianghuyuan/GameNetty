namespace ET.Server
{
    /// <summary>
    /// Buff移除事件处理器 - 当buff过期或被主动移除时，还原对应的属性修改。
    /// AttackBuff/DefenseBuff：减去之前增加的属性值。
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    [FriendOf(typeof(BuffEntity))]
    public class BuffRemoveEvent_Handler : AEvent<Scene, BuffRemoveEvent>
    {
        protected override async ETTask Run(Scene scene, BuffRemoveEvent args)
        {
            BattleUnit target = args.Target;
            BuffEntity buffEntity = args.BuffEntity;

            if (target == null || buffEntity == null || buffEntity.Config == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            switch ((EffectType)buffEntity.Config.EffectType)
            {
                case EffectType.AttackBuff:
                {
                    RestoreNumericBuff(target, NumericType.Attack, buffEntity);
                    break;
                }
                case EffectType.DefenseBuff:
                {
                    RestoreNumericBuff(target, NumericType.Defense, buffEntity);
                    break;
                }
            }

            await ETTask.CompletedTask;
        }

        private static void RestoreNumericBuff(BattleUnit target, int numericType, BuffEntity buffEntity)
        {
            NumericComponent numeric = target.GetComponent<NumericComponent>();
            if (numeric == null) return;

            int buffAmount = (int)buffEntity.Config.BaseValue;
            int addKey = numericType * 10 + 2;
            long current = numeric.GetByKey(addKey);
            numeric.Set(addKey, current - buffAmount);
            target.GetOrCreateBattleStats()?.LoadFromNumeric(numeric);
        }
    }
}
