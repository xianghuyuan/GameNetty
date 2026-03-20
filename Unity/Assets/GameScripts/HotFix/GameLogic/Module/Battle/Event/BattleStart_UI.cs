namespace ET
{
    [Event(SceneType.Main)]
    public class BattleStart_UI : AEvent<Scene, BattleStart>
    {
        protected override async ETTask Run(Scene scene, BattleStart args)
        {
            // 1. 打开战斗 HUD 窗口
            await GameModule.UI.ShowUIAsyncAwait<GameLogic.BattleHUDWindow>();
            
            // 2. 如果此时战斗中已经存在单位（例如由于消息乱序或预加载逻辑），补刷 UI
            var units = args.Battle.GetAllBattleUnits();
            foreach (var unit in units)
            {
                BattleUIHelper.CreateUnitUI(unit);
            }

            await ETTask.CompletedTask;
        }
    }
}
