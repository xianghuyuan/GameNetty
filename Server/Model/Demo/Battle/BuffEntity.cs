namespace ET.Server
{
    /// <summary>
    /// Buff实体 - 运行时的单个buff，挂载在 BuffComponent 下。
    /// 对应一个 BuffConfig 配置，所有战斗效果（伤害、冻结、击退、治疗、DoT等）统一为buff。
    /// duration=0 的buff立即执行一次后销毁（即时效果）。
    /// duration>0 的buff会按 TickInterval 周期触发，到期自动销毁。
    /// </summary>
    [ChildOf(typeof(BuffComponent))]
    public class BuffEntity : Entity, IAwake, IDestroy
    {
        public int BuffId { get; set; }
        public long CasterId { get; set; }
        public int SkillId { get; set; }
        public BuffConfig Config { get; set; }
        public int Duration { get; set; }
        public int TickInterval { get; set; }
        public long NextTickTime { get; set; }
        public long ExpireTime { get; set; }
        public int TickCount { get; set; }
        public int MaxStack { get; set; }
        public int StackCount { get; set; }

        public bool IsInstant => Duration <= 0;
        public bool IsExpired => ExpireTime > 0 && TimeInfo.Instance.ServerFrameTime() >= ExpireTime;
    }
}
