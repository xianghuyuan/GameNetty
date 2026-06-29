namespace ET.Server
{
    /// <summary>
    /// 服务端战斗单位的数值快照。
    /// 战斗结算、技能公式、移动和同步优先读取这里，避免直接污染持久 Unit 数值。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleStatsComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前生命。服务端权威伤害和治疗直接改写。
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
        /// 当前战斗中的移动速度。
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
        /// 局内护盾值。当前服务端实际吸收逻辑仍由 ShieldComponent 负责。
        /// </summary>
        public int Shield { get; set; }
    }
}
