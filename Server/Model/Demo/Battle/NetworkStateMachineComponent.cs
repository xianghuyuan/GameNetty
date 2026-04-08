namespace ET.Server
{
    /// <summary>
    /// 网络状态机组件 - 挂载在玩家英雄BattleUnit上，
    /// 追踪待确认的技能请求数量，用于兜底检测网络延迟。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class NetworkStateMachineComponent : Entity, IAwake, IDestroy
    {
        /// <summary>已发送但未收到批量结算包的技能请求计数</summary>
        public int PendingSkillCount { get; set; }

        /// <summary>连续未确认的阈值，超过则提示网络延迟</summary>
        public const int MaxPendingSkills = 3;

        /// <summary>是否处于网络延迟警告状态</summary>
        public bool IsNetworkWarning { get; set; }

        /// <summary>上次收到服务端批量结算的时间</summary>
        public long LastConfirmTime { get; set; }
    }
}
