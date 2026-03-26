namespace ET.Server
{
    /// <summary>
    /// 施法锁定组件 - 技能执行期间阻止决策系统发出新指令
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class CastingComponent : Entity, IAwake, IDestroy
    {
        public bool IsCasting { get; set; }
        public long CastEndTime { get; set; }
        public int SkillId { get; set; }
    }
}
