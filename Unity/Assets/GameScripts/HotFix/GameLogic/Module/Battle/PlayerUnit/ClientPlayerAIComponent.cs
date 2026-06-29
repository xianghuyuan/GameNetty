namespace ET
{
    /// <summary>
    /// 客户端玩家AI组件 - 挂载在玩家的BattleUnit上
    /// 负责自动选目标和增量移动，攻击冷却由 BattleAttackComponent 管理。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class ClientPlayerAIComponent : Entity, IAwake, IDestroy
    {
        public long CurrentTargetId { get; set; }
    }
}
