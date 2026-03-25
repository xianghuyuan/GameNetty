namespace ET.Server
{
    /// <summary>
    /// 冻结结束事件处理器 - 重新决策
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class MoveComponent_OnFreezeEnd : AEvent<Scene, FreezeEndEvent>
    {
        protected override async ETTask Run(Scene scene, FreezeEndEvent args)
        {
            BattleUnit unit = args.Target;
            if (unit == null || unit.IsDead)
            {
                return;
            }
            
            BattleActionDecisionComponent decisionComponent = unit.GetComponent<BattleActionDecisionComponent>();
            if (decisionComponent != null)
            {
                decisionComponent.MakeDecision();
            }
            
            await ETTask.CompletedTask;
        }
    }
}
