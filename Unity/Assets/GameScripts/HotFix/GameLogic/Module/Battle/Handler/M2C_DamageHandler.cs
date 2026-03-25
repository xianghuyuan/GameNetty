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

            BattleUnit target = battle?.GetChild<BattleUnit>(message.targetId);
            if (target == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            target.SetNumeric(NumericType.MaxHp, message.targetMaxHp);
            target.SetNumeric(NumericType.Hp, message.targetCurrentHp);

            BattleUnitViewComponent viewComponent = battle.GetComponent<BattleUnitViewComponent>();
            viewComponent?.PlayHitFeedback(root, message.targetId, message.damage);
            await ETTask.CompletedTask;
        }
    }
}
