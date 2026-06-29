namespace ET
{
    /// <summary>
    /// 客户端玩家AI Tick驱动组件
    /// 挂载到 Battle 实体上，每 100ms 驱动所有玩家 AI Tick
    /// </summary>
    [ComponentOf(typeof(Battle))]
    public class ClientPlayerAITickComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public long LastTickTime { get; set; }

        /// <summary>
        /// AI Tick 间隔（毫秒）
        /// </summary>
        public const long TICK_INTERVAL = 100;
    }
}
