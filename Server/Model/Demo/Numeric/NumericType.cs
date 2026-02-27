namespace ET
{
	// 这个可弄个配置表生成
    public static class NumericType
    {
	    public const int Max = 10000;
	    
	    // ========== 基础属性 ==========
	    public const int Speed = 1000;
	    public const int SpeedBase = Speed * 10 + 1;
	    
	    public const int Hp = 1001;
	    public const int HpBase = Hp * 10 + 1;
	    
	    public const int MaxHp = 1002;
	    public const int MaxHpBase = MaxHp * 10 + 1;
	    
	    public const int AOI = 1003;
	    public const int Level = 1004;
	    
	    // ========== 战斗属性 ==========
        public const int Attack = 1005;
        public const int AttackBase = Attack * 10 + 1;
        
        public const int Defense = 1006;
        public const int DefenseBase = Defense * 10 + 1;
        
        public const int Mp = 1007;           // 魔法值
        public const int MpBase = Mp * 10 + 1;
        
        public const int MaxMp = 1008;
        public const int MaxMpBase = MaxMp * 10 + 1;
        
        public const int CritRate = 1009;     // 暴击率（万分比）
        public const int CritRateBase = CritRate * 10 + 1;
        
        public const int CritDamage = 1010;   // 暴击伤害（万分比，10000=100%）
        public const int CritDamageBase = CritDamage * 10 + 1;
        
        public const int AttackSpeed = 1011;  // 攻击速度（万分比）
        public const int AttackSpeedBase = AttackSpeed * 10 + 1;
        
        public const int MoveSpeed = 1012;    // 移动速度
        public const int MoveSpeedBase = MoveSpeed * 10 + 1;
        
        public const int LifeSteal = 1013;    // 吸血（万分比）
        public const int LifeStealBase = LifeSteal * 10 + 1;
        
        public const int DamageReduction = 1014; // 伤害减免（万分比）
        public const int DamageReductionBase = DamageReduction * 10 + 1;
        
        public const int Penetration = 1015;  // 穿透
        public const int PenetrationBase = Penetration * 10 + 1;
        
        public const int HpRegen = 1016;      // 生命回复/秒
        public const int HpRegenBase = HpRegen * 10 + 1;
        
        public const int MpRegen = 1017;      // 魔法回复/秒
        public const int MpRegenBase = MpRegen * 10 + 1;
    }
}
