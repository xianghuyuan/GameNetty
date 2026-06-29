namespace ET
{
    [Event(SceneType.Main)]
    public class BattleStart_UI : AEvent<Scene, BattleStart>
    {
        protected override async ETTask Run(Scene scene, BattleStart args)
        {
            // 1. 打开战斗主窗口
            await GameModule.UI.ShowUIAsyncAwait<GameLogic.BattleMainWindow>();
            // 2. 打开伤害飘字窗口
            await GameModule.UI.ShowUIAsyncAwait<GameLogic.UIDamageWindow>();
            BattleUIHelper.OnBattleStarted(args.Battle);

            // 3. 桥接到 TE 侧，供纯 UI 层订阅（如背景特效切换、BGM 播放等）
            EventBridge.PublishToTE(args);

            // 4. 修改 BattleUI 按钮状态为战斗中
            GameModule.UI.GetUIAsync<GameLogic.BattleUI>(ui =>
            {
                if (ui != null) ui.SetBattleActive(true);
            });

            await ETTask.CompletedTask;
        }
    }
}
