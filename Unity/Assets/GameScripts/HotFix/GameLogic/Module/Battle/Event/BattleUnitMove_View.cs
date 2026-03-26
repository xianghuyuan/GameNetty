namespace ET
{
    [Event(SceneType.Main)]
    public class BattleUnitMoveStarted_View : AEvent<Scene, BattleUnitMoveStarted>
    {
        protected override async ETTask Run(Scene scene, BattleUnitMoveStarted args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            view?.UpdatePosition(args.To, args.Duration);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitMoveStopped_View : AEvent<Scene, BattleUnitMoveStopped>
    {
        protected override async ETTask Run(Scene scene, BattleUnitMoveStopped args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            view?.UpdatePosition(args.FinalPosition, 0);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitPositionSynced_View : AEvent<Scene, BattleUnitPositionSynced>
    {
        protected override async ETTask Run(Scene scene, BattleUnitPositionSynced args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            view?.UpdatePosition(args.Position, 0);
            await ETTask.CompletedTask;
        }
    }
}
