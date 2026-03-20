namespace ET.Server
{
    [Event(SceneType.All)]
    public class NumbericChange_NotifyNumericWatcher : AEvent<Scene, NumbericChange>
    {
        protected override async ETTask Run(Scene scene, NumbericChange args)
        {
            if (args.Unit == null || NumericWatcherComponent.Instance == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            NumericWatcherComponent.Instance.Run(args.Unit, args);
            await ETTask.CompletedTask;
        }
    }
}
