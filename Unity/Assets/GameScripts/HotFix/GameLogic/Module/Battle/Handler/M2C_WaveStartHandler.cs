namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_WaveStartHandler : MessageHandler<Scene, M2C_WaveStart>
    {
        protected override async ETTask Run(Scene root, M2C_WaveStart message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_WaveStart: BattleComponent not found");
                return;
            }

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null)
            {
                Log.Error($"M2C_WaveStart: 当前没有进行中的战斗");
                return;
            }

            battle.CurrentWave = message.waveNumber;
            battle.TotalWaves = message.totalWaves;

            BattleMoveDebugLog.Write($"[WaveStart] 第{message.waveNumber}/{message.totalWaves}波, 怪物数量={message.monsterCount}");

            Log.Info($"波次开始: 第 {message.waveNumber}/{message.totalWaves} 波, 怪物数量: {message.monsterCount}");

            EventSystem.Instance.Publish(root, new WaveStart
            {
                Battle = battle,
                WaveNumber = message.waveNumber
            });

            await ETTask.CompletedTask;
        }
    }
}
