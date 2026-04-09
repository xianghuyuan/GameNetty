using UnityEngine;

namespace ET
{
    [Event(SceneType.Main)]
    public class BattleUnitSkillCast_View : AEvent<Scene, BattleUnitSkillCast>
    {
        protected override async ETTask Run(Scene scene, BattleUnitSkillCast args)
        {
            var view = args.Unit.GetComponent<BattleUnitView>();
            view?.PlayAttackFeedback();

            // 桥接到 TE 侧，供纯 UI 层（如技能栏特效）订阅
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

            // 桥接到 TE 侧，UIDamageWindow 等 UIWindow 直接监听
            EventBridge.PublishToTE(args);

            await ETTask.CompletedTask;
        }
    }
}
