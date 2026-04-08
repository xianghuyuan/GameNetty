namespace ET.Server
{
    /// <summary>
    /// 网络状态机检查心跳定时器 - 每500ms检查待确认技能数
    /// </summary>
    [Invoke(TimerInvokeType.NetworkStateCheck)]
    public class NetworkStateCheckTimer : ATimer<NetworkStateMachineComponent>
    {
        protected override void Run(NetworkStateMachineComponent self)
        {
            NetworkStateMachineComponentSystem.OnNetworkStateCheck(self);
        }
    }

    [EntitySystemOf(typeof(NetworkStateMachineComponent))]
    [FriendOf(typeof(NetworkStateMachineComponent))]
    [FriendOf(typeof(BattleUnit))]
    [FriendOf(typeof(BattleRoom))]
    public static partial class NetworkStateMachineComponentSystem
    {
        /// <summary>网络状态检查间隔（毫秒）</summary>
        private const long CheckInterval = 500;

        [EntitySystem]
        private static void Awake(this NetworkStateMachineComponent self)
        {
            self.PendingSkillCount = 0;
            self.IsNetworkWarning = false;
            self.LastConfirmTime = TimeInfo.Instance.ServerFrameTime();

            self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(CheckInterval, TimerInvokeType.NetworkStateCheck, self);
        }

        [EntitySystem]
        private static void Destroy(this NetworkStateMachineComponent self)
        {
            // 定时器会随Entity销毁自动清理
            self.PendingSkillCount = 0;
            self.IsNetworkWarning = false;
        }

        /// <summary>
        /// 技能请求发送时调用，增加待确认计数
        /// </summary>
        public static void OnSkillRequestSent(this NetworkStateMachineComponent self)
        {
            self.PendingSkillCount++;
        }

        /// <summary>
        /// 收到服务端批量结算包时调用，重置待确认计数
        /// </summary>
        public static void OnBatchConfirmed(this NetworkStateMachineComponent self)
        {
            self.PendingSkillCount = 0;
            self.LastConfirmTime = TimeInfo.Instance.ServerFrameTime();

            if (self.IsNetworkWarning)
            {
                self.IsNetworkWarning = false;
                // 广播网络恢复消息
                BattleUnit owner = self.GetParent<BattleUnit>();
                if (owner != null)
                {
                    BattleRoom battleRoom = owner.GetParent<BattleRoom>();
                    if (battleRoom != null)
                    {
                        var msg = M2C_NetworkStateNotice.Create();
                        msg.state = 0; // 恢复正常
                        battleRoom.BroadcastToPlayers(msg);
                    }
                }
            }
        }

        /// <summary>
        /// 网络状态检查回调 - 每500ms执行一次
        /// </summary>
        internal static void OnNetworkStateCheck(NetworkStateMachineComponent self)
        {
            if (self.PendingSkillCount >= NetworkStateMachineComponent.MaxPendingSkills && !self.IsNetworkWarning)
            {
                self.IsNetworkWarning = true;

                // 广播网络延迟警告
                BattleUnit owner = self.GetParent<BattleUnit>();
                if (owner != null)
                {
                    BattleRoom battleRoom = owner.GetParent<BattleRoom>();
                    if (battleRoom != null)
                    {
                        var msg = M2C_NetworkStateNotice.Create();
                        msg.state = 1; // 网络延迟
                        battleRoom.BroadcastToPlayers(msg);
                    }
                }
            }
        }
    }
}
