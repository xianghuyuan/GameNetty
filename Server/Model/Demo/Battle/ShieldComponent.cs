namespace ET.Server
{
    /// <summary>
    /// 护盾组件 - 管理单位护盾状态。
    /// 护盾通过独立字段 ShieldCurrentAmount 记录，不再修改 MaxHp。
    /// TakeDamage 时先扣护盾再扣HP，避免与其他 MaxHp 修改冲突。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class ShieldComponent : Entity, IAwake, IDestroy
    {
        /// <summary>当前护盾剩余量</summary>
        public int ShieldCurrentAmount { get; set; }
        /// <summary>护盾到期定时器ID</summary>
        public long ShieldTimerId { get; set; }
        /// <summary>护盾是否激活</summary>
        public bool IsActive => ShieldCurrentAmount > 0;
    }
}
