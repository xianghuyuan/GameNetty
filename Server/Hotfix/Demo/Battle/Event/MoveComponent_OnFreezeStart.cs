namespace ET.Server
{
    /// <summary>
    /// 冻结开始事件处理器 - 停止移动
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnFreezeStart : AEvent<Scene, FreezeStartEvent>
    {
        protected override async ETTask Run(Scene scene, FreezeStartEvent args)
        {
            BattleUnit unit = args.Target;
            if (unit == null)
            {
                return;
            }
            
            BattleMoveComponent moveComp = unit.GetComponent<BattleMoveComponent>();
            moveComp?.StopMove();
            
            await ETTask.CompletedTask;
        }
    }
}
