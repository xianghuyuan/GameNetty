namespace ET
{
	// 这个可弄个配置表生成。
    // 数值分两层：
    // 1. 最终层：Speed/Attack/Defense 等，业务系统只读这些最终值。
    // 2. 修饰层：Base/Add/Pct/FinalAdd/FinalPct，Buff/装备只改这些中间值，再由 NumericComponent.Update 写回最终值。
    // 修饰层公式：
    // Final = (((Base + Add) * (100 + Pct) / 100) + FinalAdd) * (100 + FinalPct) / 100
    // 注意：Pct/FinalPct 是百分比修正，使用 Set(type, floatValue) 写入；例如 -30f 表示 -30%。
    public static class NumericType
    {
	    public const int Max = 10000;
	    
	    // ========== 基础属性 ==========
	    public const int Speed = 1000; // 最终移动速度，移动系统只读这个值。
	    public const int SpeedBase = Speed * 10 + 1; // 基础移动速度，来自配置/成长。
	    public const int SpeedAdd = Speed * 10 + 2; // 固定移动速度加成。
	    public const int SpeedPct = Speed * 10 + 3; // 百分比移动速度加成，-30f 表示减速 30%。
	    public const int SpeedFinalAdd = Speed * 10 + 4; // 最终固定移动速度加成，通常用于最后结算类效果。
	    public const int SpeedFinalPct = Speed * 10 + 5; // 最终百分比移动速度加成。
	    
	    public const int Hp = 1001; // 当前生命值，只由伤害/治疗/同步直接修改，不建议由 Buff 修饰。
	    public const int HpBase = Hp * 10 + 1; // 当前生命初始化基准，保留给旧初始化链路；战斗 Buff 通常不要改它。
	    
	    public const int MaxHp = 1002; // 最终最大生命值。
	    public const int MaxHpBase = MaxHp * 10 + 1; // 基础最大生命值。
	    public const int MaxHpAdd = MaxHp * 10 + 2; // 固定最大生命加成。
	    public const int MaxHpPct = MaxHp * 10 + 3; // 百分比最大生命加成。
	    public const int MaxHpFinalAdd = MaxHp * 10 + 4; // 最终固定最大生命加成。
	    public const int MaxHpFinalPct = MaxHp * 10 + 5; // 最终百分比最大生命加成。
	    
	    public const int AOI = 1003; // 视野范围，当前不走修饰层。
	    public const int Level = 1004; // 等级，当前不走修饰层。
	    
	    // ========== 战斗属性 ==========
        public const int Attack = 1005; // 最终攻击力。
        public const int AttackBase = Attack * 10 + 1; // 基础攻击力。
        public const int AttackAdd = Attack * 10 + 2; // 固定攻击力加成。
        public const int AttackPct = Attack * 10 + 3; // 百分比攻击力加成。
        public const int AttackFinalAdd = Attack * 10 + 4; // 最终固定攻击力加成。
        public const int AttackFinalPct = Attack * 10 + 5; // 最终百分比攻击力加成。
        
        public const int Defense = 1006; // 最终防御力。
        public const int DefenseBase = Defense * 10 + 1; // 基础防御力。
        public const int DefenseAdd = Defense * 10 + 2; // 固定防御力加成。
        public const int DefensePct = Defense * 10 + 3; // 百分比防御力加成。
        public const int DefenseFinalAdd = Defense * 10 + 4; // 最终固定防御力加成。
        public const int DefenseFinalPct = Defense * 10 + 5; // 最终百分比防御力加成。
        
        public const int Mp = 1007; // 当前魔法值，只由消耗/恢复/同步直接修改，不建议由 Buff 修饰。
        public const int MpBase = Mp * 10 + 1; // 当前魔法初始化基准，保留给旧初始化链路；战斗 Buff 通常不要改它。
        
        public const int MaxMp = 1008; // 最终最大魔法值。
        public const int MaxMpBase = MaxMp * 10 + 1; // 基础最大魔法值。
        public const int MaxMpAdd = MaxMp * 10 + 2; // 固定最大魔法加成。
        public const int MaxMpPct = MaxMp * 10 + 3; // 百分比最大魔法加成。
        public const int MaxMpFinalAdd = MaxMp * 10 + 4; // 最终固定最大魔法加成。
        public const int MaxMpFinalPct = MaxMp * 10 + 5; // 最终百分比最大魔法加成。
        
        public const int CritRate = 1009; // 最终暴击率。
        public const int CritRateBase = CritRate * 10 + 1; // 基础暴击率。
        public const int CritRateAdd = CritRate * 10 + 2; // 固定暴击率加成。
        public const int CritRatePct = CritRate * 10 + 3; // 百分比暴击率加成。
        public const int CritRateFinalAdd = CritRate * 10 + 4; // 最终固定暴击率加成。
        public const int CritRateFinalPct = CritRate * 10 + 5; // 最终百分比暴击率加成。
        
        public const int CritDamage = 1010; // 最终暴击伤害。
        public const int CritDamageBase = CritDamage * 10 + 1; // 基础暴击伤害。
        public const int CritDamageAdd = CritDamage * 10 + 2; // 固定暴击伤害加成。
        public const int CritDamagePct = CritDamage * 10 + 3; // 百分比暴击伤害加成。
        public const int CritDamageFinalAdd = CritDamage * 10 + 4; // 最终固定暴击伤害加成。
        public const int CritDamageFinalPct = CritDamage * 10 + 5; // 最终百分比暴击伤害加成。
        
        public const int AttackSpeed = 1011; // 最终攻击速度。
        public const int AttackSpeedBase = AttackSpeed * 10 + 1; // 基础攻击速度。
        public const int AttackSpeedAdd = AttackSpeed * 10 + 2; // 固定攻击速度加成。
        public const int AttackSpeedPct = AttackSpeed * 10 + 3; // 百分比攻击速度加成。
        public const int AttackSpeedFinalAdd = AttackSpeed * 10 + 4; // 最终固定攻击速度加成。
        public const int AttackSpeedFinalPct = AttackSpeed * 10 + 5; // 最终百分比攻击速度加成。
        
        public const int LifeSteal = 1013; // 最终吸血。
        public const int LifeStealBase = LifeSteal * 10 + 1; // 基础吸血。
        public const int LifeStealAdd = LifeSteal * 10 + 2; // 固定吸血加成。
        public const int LifeStealPct = LifeSteal * 10 + 3; // 百分比吸血加成。
        public const int LifeStealFinalAdd = LifeSteal * 10 + 4; // 最终固定吸血加成。
        public const int LifeStealFinalPct = LifeSteal * 10 + 5; // 最终百分比吸血加成。
        
        public const int DamageReduction = 1014; // 最终伤害减免。
        public const int DamageReductionBase = DamageReduction * 10 + 1; // 基础伤害减免。
        public const int DamageReductionAdd = DamageReduction * 10 + 2; // 固定伤害减免加成。
        public const int DamageReductionPct = DamageReduction * 10 + 3; // 百分比伤害减免加成。
        public const int DamageReductionFinalAdd = DamageReduction * 10 + 4; // 最终固定伤害减免加成。
        public const int DamageReductionFinalPct = DamageReduction * 10 + 5; // 最终百分比伤害减免加成。
        
        public const int Penetration = 1015; // 最终穿透。
        public const int PenetrationBase = Penetration * 10 + 1; // 基础穿透。
        public const int PenetrationAdd = Penetration * 10 + 2; // 固定穿透加成。
        public const int PenetrationPct = Penetration * 10 + 3; // 百分比穿透加成。
        public const int PenetrationFinalAdd = Penetration * 10 + 4; // 最终固定穿透加成。
        public const int PenetrationFinalPct = Penetration * 10 + 5; // 最终百分比穿透加成。
        
        public const int HpRegen = 1016; // 最终生命回复/秒。
        public const int HpRegenBase = HpRegen * 10 + 1; // 基础生命回复/秒。
        public const int HpRegenAdd = HpRegen * 10 + 2; // 固定生命回复加成。
        public const int HpRegenPct = HpRegen * 10 + 3; // 百分比生命回复加成。
        public const int HpRegenFinalAdd = HpRegen * 10 + 4; // 最终固定生命回复加成。
        public const int HpRegenFinalPct = HpRegen * 10 + 5; // 最终百分比生命回复加成。
        
        public const int MpRegen = 1017; // 最终魔法回复/秒。
        public const int MpRegenBase = MpRegen * 10 + 1; // 基础魔法回复/秒。
        public const int MpRegenAdd = MpRegen * 10 + 2; // 固定魔法回复加成。
        public const int MpRegenPct = MpRegen * 10 + 3; // 百分比魔法回复加成。
        public const int MpRegenFinalAdd = MpRegen * 10 + 4; // 最终固定魔法回复加成。
        public const int MpRegenFinalPct = MpRegen * 10 + 5; // 最终百分比魔法回复加成。
    }
}
