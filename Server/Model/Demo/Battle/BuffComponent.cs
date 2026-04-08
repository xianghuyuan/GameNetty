namespace ET.Server
{
    /// <summary>
    /// Buff管理组件 - 挂在 BattleUnit 上，管理该单位身上所有 buff 的生命周期。
    /// buff 是战斗效果的最小执行单元，技能 = 一组 buff 的组合。
    /// 每个 buff 以 BuffEntity 子实体形式存在。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BuffComponent : Entity, IAwake, IDestroy
    {
        public long TimerId { get; set; }
    }
}
