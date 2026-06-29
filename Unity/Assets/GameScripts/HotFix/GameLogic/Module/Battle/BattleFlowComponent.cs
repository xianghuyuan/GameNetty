namespace ET
{
    /// <summary>
    /// 战斗会话流程控制器。
    /// 技术形态上它是一个挂在 Battle 上的 Component，
    /// 设计职责上它负责推进整场战斗的会话流程：
    /// Start / Pause / Resume / End。
    /// </summary>
    [ComponentOf(typeof(Battle))]
    public class BattleFlowComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 上一次状态切换前的状态，用于调试和理解流程。
        /// </summary>
        public BattleState PreviousState { get; set; }

        /// <summary>
        /// 最近一次流程切换发生的时间戳（客户端毫秒）。
        /// </summary>
        public long LastTransitionTime { get; set; }

        /// <summary>
        /// 最近一次结束结果。
        /// null 表示当前会话尚未结束。
        /// </summary>
        public bool? LastEndSuccess { get; set; }
    }
}
