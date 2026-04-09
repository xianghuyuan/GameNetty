namespace ET
{
    [Event(SceneType.Main)]
    public class BattleStart_UI : AEvent<Scene, BattleStart>
    {
        protected override async ETTask Run(Scene scene, BattleStart args)
        {
            // 1. 打开战斗 HUD 窗口
            await GameModule.UI.ShowUIAsyncAwait<GameLogic.BattleHUDWindow>();
            // 2. 打开伤害飘字窗口
            await GameModule.UI.ShowUIAsyncAwait<GameLogic.UIDamageWindow>();
            
            // 3. 如果此时战斗中已经存在单位（例如由于消息乱序或预加载逻辑），补刷 UI
            var units = args.Battle.GetAllBattleUnits();
            foreach (var unit in units)
            {
                BattleUIHelper.CreateUnitUI(unit);
            }

            // 4. 桥接到 TE 侧，供纯 UI 层订阅（如背景特效切换、BGM 播放等）
            EventBridge.PublishToTE(args);

            // 5. 修改 BattleUI 按钮状态为战斗中
            GameModule.UI.GetUIAsync<GameLogic.BattleUI>(ui =>
            {
                if (ui != null) ui.SetBattleActive(true);
            });

            await ETTask.CompletedTask;
        }
    }
}
