namespace ET.Server
{
    /// <summary>
    /// 效果应用组件 - 负责将技能效果应用到目标
    /// 订阅 SkillHitEvent，发布细分效果事件
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class EffectApplyComponent : Entity, IAwake, IDestroy
    {
    }
}
