namespace ET.Server
{
    /// <summary>
    /// 移动组件响应请求移动事件
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnRequestMove : AEvent<BattleRoom, RequestMoveEvent>
    {
        protected override async ETTask Run(BattleRoom scene, RequestMoveEvent args)
        {
            BattleUnit unit = args.Unit;
            if (unit == null || unit.IsDead)
            {
                return;
            }

            BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
            moveComponent?.StartMove(args.TargetPosition);

            await ETTask.CompletedTask;
        }
    }
}
