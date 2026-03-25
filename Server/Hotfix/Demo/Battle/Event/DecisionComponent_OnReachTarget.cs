namespace ET.Server
{
    /// <summary>
    /// 决策组件响应到达目标位置事件 - 到达后重新决策
    /// </summary>
    [Event(SceneType.Battle)]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public class DecisionComponent_OnReachTarget : AEvent<BattleRoom, ReachTargetEvent>
    {
        protected override async ETTask Run(BattleRoom scene, ReachTargetEvent args)
        {
            BattleUnit unit = args.Unit;
            if (unit == null || unit.IsDead)
            {
                return;
            }

            BattleActionDecisionComponent decisionComponent = unit.GetComponent<BattleActionDecisionComponent>();
            decisionComponent?.MakeDecision();

            await ETTask.CompletedTask;
        }
    }
}
