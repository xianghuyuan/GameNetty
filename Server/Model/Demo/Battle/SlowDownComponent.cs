namespace ET.Server
{
    /// <summary>
    /// 减速状态组件 - 管理单位减速状态，支持多层减速叠加（引用计数）。
    /// BaseSpeed 记录未减速时的基础速度，保证多次施加/移除不会丢失原始值。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class SlowDownComponent : Entity, IAwake, IDestroy
    {
        /// <summary>未减速时的基础速度（第一次施加时记录）</summary>
        public float BaseSpeed { get; set; }
        /// <summary>当前生效的总减速百分比累加值（如 0.3+0.2=0.5）</summary>
        public float TotalSlowPercent { get; set; }
        /// <summary>减速层数（引用计数）</summary>
        public int SlowCount { get; set; }
        /// <summary>减速到期定时器ID</summary>
        public long SlowTimerId { get; set; }
        public bool IsSlowed => SlowCount > 0;
    }
}
