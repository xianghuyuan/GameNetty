namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_WaveCompleteHandler : MessageHandler<Scene, M2C_WaveComplete>
    {
        protected override async ETTask Run(Scene root, M2C_WaveComplete message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_WaveComplete: BattleComponent not found");
                return;
            }

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null)
            {
                Log.Error($"M2C_WaveComplete: 当前没有进行中的战斗");
                return;
            }

            Log.Info($"波次完成: 第 {message.waveNumber}/{message.totalWaves} 波, 耗时: {message.duration}秒");

            EventSystem.Instance.Publish(root, new WaveComplete
            {
                Battle = battle,
                WaveNumber = message.waveNumber
            });

            await ETTask.CompletedTask;
        }
    }
}
