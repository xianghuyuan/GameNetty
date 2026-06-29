namespace ET.Server
{
    [Invoke(TimerInvokeType.SlowDownEnd)]
    public class SlowDownEndTimer : ATimer<SlowDownComponent>
    {
        protected override void Run(SlowDownComponent self)
        {
            self.RemoveOneSlowLayer();
        }
    }

    [EntitySystemOf(typeof(SlowDownComponent))]
    [FriendOf(typeof(SlowDownComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class SlowDownComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SlowDownComponent self)
        {
            self.BaseSpeed = 0;
            self.TotalSlowPercent = 0;
            self.SlowCount = 0;
            self.SlowTimerId = 0;
        }

        [EntitySystem]
        private static void Destroy(this SlowDownComponent self)
        {
            self.RemoveAllSlow();
        }

        /// <summary>
        /// 应用一层减速效果。slowPercent 表示减速百分比，如 0.3 = 减速30%。
        /// 支持叠加：每次增加引用计数，累加减速百分比，速度 = BaseSpeed * (1 - TotalSlowPercent)。
        /// </summary>
        public static void ApplySlow(this SlowDownComponent self, float slowPercent, int durationMs)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            NumericComponent numeric = unit.GetComponent<NumericComponent>();
            BattleStatsComponent stats = unit.GetOrCreateBattleStats();
            if (numeric == null || stats == null) return;

            // 第一次施加时记录基础速度
            if (self.SlowCount == 0)
            {
                self.BaseSpeed = stats.Speed;
            }

            self.SlowCount++;
            self.TotalSlowPercent += slowPercent;

            // 上限：最高减速90%
            if (self.TotalSlowPercent > 0.9f)
            {
                self.TotalSlowPercent = 0.9f;
            }

            // 应用减速后的速度
            float newSpeed = self.BaseSpeed * (1f - self.TotalSlowPercent);
            stats.Speed = newSpeed;
            numeric.Set(NumericType.Speed, newSpeed);

            // 注册定时器，单层到期减少一层
            long endTime = TimeInfo.Instance.ServerFrameTime() + durationMs;
            unit.Root().GetComponent<TimerComponent>()
                ?.NewOnceTimer(endTime, TimerInvokeType.SlowDownEnd, self);
        }

        /// <summary>
        /// 移除一层减速（单个减速buff到期时调用）。
        /// 每次移除需要重算速度，但由于无法知道到期的是哪一层的百分比，
        /// 简化处理：每次到期减少固定比例（1/SlowCount * TotalSlowPercent）。
        /// </summary>
        public static void RemoveOneSlowLayer(this SlowDownComponent self)
        {
            if (self.SlowCount <= 0) return;

            BattleUnit unit = self.GetParent<BattleUnit>();
            NumericComponent numeric = unit?.GetComponent<NumericComponent>();
            BattleStatsComponent stats = unit?.GetOrCreateBattleStats();
            if (unit == null || unit.IsDisposed || numeric == null || stats == null) return;

            self.SlowCount--;

            if (self.SlowCount <= 0)
            {
                // 所有减速层已移除，恢复基础速度
                self.RemoveAllSlow();
            }
            else
            {
                // 简化处理：每层平均分配减速比例
                // 由于无法追踪单个层的具体百分比，按剩余层数均摊
                // 实际效果：每次到期减速效果略微减弱
                float avgSlowPercent = self.TotalSlowPercent / (self.SlowCount + 1);
                self.TotalSlowPercent -= avgSlowPercent;
                if (self.TotalSlowPercent < 0) self.TotalSlowPercent = 0;

                float newSpeed = self.BaseSpeed * (1f - self.TotalSlowPercent);
                stats.Speed = newSpeed;
                numeric.Set(NumericType.Speed, newSpeed);
            }
        }

        /// <summary>
        /// 移除所有减速层，恢复基础速度（单位死亡时调用）
        /// </summary>
        public static void RemoveAllSlow(this SlowDownComponent self)
        {
            if (self.SlowCount <= 0) return;

            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit != null && !unit.IsDisposed)
            {
                NumericComponent numeric = unit.GetComponent<NumericComponent>();
                BattleStatsComponent stats = unit.GetOrCreateBattleStats();
                if (numeric != null && stats != null && self.BaseSpeed > 0)
                {
                    stats.Speed = self.BaseSpeed;
                    numeric.Set(NumericType.Speed, self.BaseSpeed);
                }
            }

            self.BaseSpeed = 0;
            self.TotalSlowPercent = 0;
            self.SlowCount = 0;
        }
    }
}
