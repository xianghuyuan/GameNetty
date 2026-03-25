namespace ET.Server
{
    /// <summary>
    /// 击退事件处理器 - 停止移动并瞬移到击退位置
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleMoveComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class KnockbackEvent_OnKnockback : AEvent<Scene, KnockbackEvent>
    {
        protected override async ETTask Run(Scene scene, KnockbackEvent args)
        {
            BattleUnit target = args.Target;
            if (target == null || target.IsDead)
            {
                return;
            }
            
            BattleMoveComponent moveComp = target.GetComponent<BattleMoveComponent>();
            if (moveComp == null)
            {
                return;
            }
            
            // 停止当前移动
            moveComp.StopMove();
            
            // 计算击退目标位置并直接设置
            System.Numerics.Vector3 targetPosition = new System.Numerics.Vector3(
                target.Position.X + args.Direction * args.Distance,
                target.Position.Y,
                target.Position.Z
            );
            target.Position = targetPosition;
            
            // 广播击退
            BattleUnitHelper.BroadcastKnockback(target, args.Distance, args.Direction);
            
            await ETTask.CompletedTask;
        }
    }
}
