namespace ET.Server
{
    [Invoke(TimerInvokeType.FreezeEnd)]
    public class FreezeEndTimer : ATimer<FreezeComponent>
    {
        protected override void Run(FreezeComponent self)
        {
            self.EndFreeze();
        }
    }

    [EntitySystemOf(typeof(FreezeComponent))]
    [FriendOf(typeof(FreezeComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class FreezeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this FreezeComponent self)
        {
            self.IsFrozen = false;
            self.FreezeEndTime = 0;
        }
        
        [EntitySystem]
        private static void Destroy(this FreezeComponent self)
        {
            self.IsFrozen = false;
            self.FreezeEndTime = 0;
        }
        
        /// <summary>
        /// 应用冻结效果（支持刷新时长：已冻结时延长冻结时间）
        /// </summary>
        public static void ApplyFreeze(this FreezeComponent self, int durationMs)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            long currentTime = TimeInfo.Instance.ServerFrameTime();

            if (self.IsFrozen)
            {
                // 已冻结时刷新时长：移除旧定时器，注册新定时器
                long oldTimerId = self.FreezeTimerId;
                if (oldTimerId != 0)
                {
                    unit.Root().GetComponent<TimerComponent>()?.Remove(ref oldTimerId);
                }
                self.FreezeEndTime = currentTime + durationMs;
                self.FreezeTimerId = unit.Root().GetComponent<TimerComponent>()
                    ?.NewOnceTimer(self.FreezeEndTime, TimerInvokeType.FreezeEnd, self) ?? 0;

                BattleUnitHelper.BroadcastUnitFrozen(unit, durationMs);
                return;
            }

            // 发布冻结开始事件，让 MoveComponent 自己处理移动中断
            EventSystem.Instance.Publish(unit.Root(), new FreezeStartEvent { Target = unit, DurationMs = durationMs });

            self.IsFrozen = true;
            self.FreezeEndTime = currentTime + durationMs;
            self.FreezeTimerId = unit.Root().GetComponent<TimerComponent>()
                ?.NewOnceTimer(self.FreezeEndTime, TimerInvokeType.FreezeEnd, self) ?? 0;

            // 广播冻结状态
            BattleUnitHelper.BroadcastUnitFrozen(unit, durationMs);
        }
        
        /// <summary>
        /// 结束冻结（只发布事件，不直接操作移动）
        /// </summary>
        public static void EndFreeze(this FreezeComponent self)
        {
            if (!self.IsFrozen)
            {
                return;
            }
            
            self.IsFrozen = false;
            self.FreezeEndTime = 0;
            self.FreezeTimerId = 0;
            
            BattleUnit unit = self.GetParent<BattleUnit>();
            
            // 发布冻结结束事件，让 MoveComponent 自己决定是否恢复移动
            EventSystem.Instance.Publish(unit.Root(), new FreezeEndEvent { Target = unit });
        }
    }
}
