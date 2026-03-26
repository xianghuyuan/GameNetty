namespace ET
{
    [Event(SceneType.Main)]
    public class BattleUnitSkillCast_View : AEvent<Scene, BattleUnitSkillCast>
    {
        protected override async ETTask Run(Scene scene, BattleUnitSkillCast args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            view?.PlayAttackFeedback();
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitDamaged_View : AEvent<Scene, BattleUnitDamaged>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDamaged args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            view?.PlayHitFeedback(args.Damage);
            await ETTask.CompletedTask;
        }
    }
}
