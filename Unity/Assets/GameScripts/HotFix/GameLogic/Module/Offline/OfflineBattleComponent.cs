namespace ET
{
    /// <summary>
    /// 离线战斗组件 - 挂载在 Battle 上
    /// 标记这是一场离线战斗，作为本地"服务器代理"管理战斗流程
    /// </summary>
    [ComponentOf(typeof(Battle))]
    public class OfflineBattleComponent : Entity, IAwake, IDestroy, IUpdate
    {
        /// <summary>
        /// 玩家在主世界的 Unit ID，用于拷贝属性
        /// </summary>
        public long PlayerUnitId { get; set; }
    }
}
