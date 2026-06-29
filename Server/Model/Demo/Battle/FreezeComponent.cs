namespace ET.Server
{
    /// <summary>
    /// 冻结状态组件 - 管理单位冻结状态
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class FreezeComponent : Entity, IAwake, IDestroy
    {
        public bool IsFrozen { get; set; }
        public long FreezeEndTime { get; set; }
        public long FreezeTimerId { get; set; }
    }
}
