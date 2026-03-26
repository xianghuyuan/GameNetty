namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_SkillCastHandler : MessageHandler<Scene, M2C_SkillCast>
    {
        protected override async ETTask Run(Scene root, M2C_SkillCast message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            Log.Info(BattleMessageLogHelper.FormatSkillCast(battle, message));

            BattleUnit caster = battle?.GetChild<BattleUnit>(message.casterId);
            if (caster == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            EventSystem.Instance.Publish(root, new BattleUnitSkillCast { Unit = caster });
            await ETTask.CompletedTask;
        }
    }
}
