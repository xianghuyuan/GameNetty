namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_ControlModeChangedHandler : MessageHandler<Scene, M2C_ControlModeChanged>
    {
        protected override async ETTask Run(Scene root, M2C_ControlModeChanged message)
        {
            BattleUIHelper.OnControlModeChanged(message.UnitId, message.NewMode);
            await ETTask.CompletedTask;
        }
    }
}
