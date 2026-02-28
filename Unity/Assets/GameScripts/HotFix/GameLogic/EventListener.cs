using GameLogic;

namespace ET
{
    [Event(SceneType.Main)]
    public class OnSceneChangeStart : AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            // 在这里处理场景切换开始的逻辑
            // 例如：显示加载界面
            Log.Info("场景切换开始");
            GameModule.Scene.LoadScene("Map");
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class OnLoginFinish : AEvent<Scene, LoginFinish>
    {
        protected override async ETTask Run(Scene scene, LoginFinish a)
        {
            GameModule.UI.CloseAll();
            await ETTask.CompletedTask;
        }
    }
}