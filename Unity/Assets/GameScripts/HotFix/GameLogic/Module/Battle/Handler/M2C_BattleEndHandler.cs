namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_BattleEndHandler : MessageHandler<Scene, M2C_BattleEnd>
    {
        protected override async ETTask Run(Scene root, M2C_BattleEnd message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_BattleEnd: BattleComponent not found");
                return;
            }

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null)
            {
                Log.Error($"M2C_BattleEnd: 当前没有进行中的战斗");
                return;
            }

            battle.State = BattleState.Ended;

            Log.Info($"战斗结束: BattleId={message.battleId}, 成功={message.success}, 耗时={message.duration}秒");

            EventSystem.Instance.Publish(root, new BattleEnd
            {
                Battle = battle,
            });

            battleComponent.RemoveBattle(message.battleId);

            await ETTask.CompletedTask;
        }
    }
}
