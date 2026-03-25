namespace ET.Server
{
    /// <summary>
    /// 冻结事件处理器 - 订阅 FreezeEvent
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleUnit))]
    public class FreezeEvent_OnFreeze : AEvent<Scene, FreezeEvent>
    {
        protected override async ETTask Run(Scene scene, FreezeEvent args)
        {
            BattleUnit target = args.Target;
            if (target == null || target.IsDead)
            {
                return;
            }
            
            // 获取或添加冻结组件
            FreezeComponent freezeComp = target.GetComponent<FreezeComponent>();
            if (freezeComp == null)
            {
                freezeComp = target.AddComponent<FreezeComponent>();
            }
            
            // 应用冻结
            freezeComp.ApplyFreeze(args.DurationMs);
            
            await ETTask.CompletedTask;
        }
    }
}
