namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_DamageHandler : MessageHandler<Scene, M2C_Damage>
    {
        protected override async ETTask Run(Scene root, M2C_Damage message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            Log.Info(BattleMessageLogHelper.FormatDamage(battle, message));
            BattleMoveDebugLog.Write(BattleMessageLogHelper.FormatDamage(battle, message));

            BattleUnit target = battle?.GetChild<BattleUnit>(message.targetId);
            if (target == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            target.GetOrCreateBattleStats().SetHpMax(message.targetCurrentHp, message.targetMaxHp, true);

            EventSystem.Instance.Publish(root, new BattleUnitDamaged
            {
                Unit = target,
                AttackerId = message.attackerId,
                Damage = message.damage,
                IsCrit = message.isCrit,
            });
            await ETTask.CompletedTask;
        }
    }
}
