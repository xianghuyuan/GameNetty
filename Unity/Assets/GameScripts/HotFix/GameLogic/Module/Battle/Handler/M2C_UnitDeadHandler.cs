namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_UnitDeadHandler : MessageHandler<Scene, M2C_UnitDead>
    {
        protected override async ETTask Run(Scene root, M2C_UnitDead message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            Log.Info(BattleMessageLogHelper.FormatUnitDead(battle, message));
            BattleMoveDebugLog.Write(BattleMessageLogHelper.FormatUnitDead(battle, message));

            BattleUnit battleUnit = battle?.GetChild<BattleUnit>(message.unitId);
            if (battleUnit == null || battleUnit.IsDead)
            {
                await ETTask.CompletedTask;
                return;
            }

            battleUnit.IsDead = true;
            EventSystem.Instance.Publish(root, new BattleUnitDead { BattleUnit = battleUnit });
            battleUnit.Dispose();
            await ETTask.CompletedTask;
        }
    }
}
