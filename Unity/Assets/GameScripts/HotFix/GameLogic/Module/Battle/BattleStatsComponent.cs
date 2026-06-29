namespace ET
{
    /// <summary>
    /// 单场战斗内使用的数值快照。
    /// 与 Unit 上的 NumericComponent 隔离，战斗内扣血、临时属性变化优先读写这里。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleStatsComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前生命。由伤害、治疗和服务端同步直接改写。
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        /// 当前战斗中的最大生命。
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 当前战斗中的攻击力快照。
        /// </summary>
        public int Attack { get; set; }

        /// <summary>
        /// 当前战斗中的防御力快照。
        /// </summary>
        public int Defense { get; set; }

        /// <summary>
        /// 当前战斗中的移动速度，表现层和移动逻辑只读这个值。
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// 暴击率快照，当前预留给后续伤害公式接入。
        /// </summary>
        public int CritRate { get; set; }

        /// <summary>
        /// 暴击伤害快照，当前预留给后续伤害公式接入。
        /// </summary>
        public int CritDamage { get; set; }

        /// <summary>
        /// 攻击速度快照，后续可用于修正发射器冷却。
        /// </summary>
        public int AttackSpeed { get; set; }

        /// <summary>
        /// 吸血属性快照，当前预留给命中后回复逻辑。
        /// </summary>
        public int LifeSteal { get; set; }

        /// <summary>
        /// 伤害减免快照，当前预留给最终伤害结算。
        /// </summary>
        public int DamageReduction { get; set; }

        /// <summary>
        /// 穿透属性快照，当前预留给防御修正公式。
        /// </summary>
        public int Penetration { get; set; }

        /// <summary>
        /// 生命回复快照，当前预留给周期回复逻辑。
        /// </summary>
        public int HpRegen { get; set; }

        /// <summary>
        /// 魔法回复快照，当前预留给周期回复逻辑。
        /// </summary>
        public int MpRegen { get; set; }

        /// <summary>
        /// 局内护盾值。服务端已有独立护盾组件，客户端先保留快照字段用于展示和预测扩展。
        /// </summary>
        public int Shield { get; set; }
    }
}
