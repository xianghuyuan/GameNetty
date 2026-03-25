namespace ET.Server
{
    /// <summary>
    /// 移动组件响应请求停止移动事件
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnRequestStopMove : AEvent<BattleRoom, RequestStopMoveEvent>
    {
        protected override async ETTask Run(BattleRoom scene, RequestStopMoveEvent args)
        {
            BattleUnit unit = args.Unit;
            if (unit == null)
            {
                return;
            }

            BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
            moveComponent?.StopMove();

            await ETTask.CompletedTask;
        }
    }
}
