using UnityEngine;

namespace ET
{
    [Event(SceneType.Main)]
    public class BattleUnitSkillCast_View : AEvent<Scene, BattleUnitSkillCast>
    {
        protected override async ETTask Run(Scene scene, BattleUnitSkillCast args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            // 动画由 AI 通过 PlayAttackFeedback(onHit) 播放
            // 如果 AI 未播放（如在线路径），则在此处兜底播放（无命中回调）
            if (view != null && view.CurrentAnimName != "atk1")
            {
                view.PlayAttackAnimation();
            }

            EventBridge.PublishToTE(args);

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

            EventBridge.PublishToTE(args);

            await ETTask.CompletedTask;
        }
    }
}
