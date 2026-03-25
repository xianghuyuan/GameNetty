namespace ET.Server
{
    /// <summary>
    /// 效果类型枚举
    /// </summary>
    public enum EffectType
    {
        Damage = 1,      // 伤害
        Freeze = 2,      // 冻结
        Knockback = 3,   // 击退
        Heal = 4,        // 治疗
        Stun = 5,        // 眩晕
    }
    
    /// <summary>
    /// 效果结果
    /// </summary>
    public struct EffectResult
    {
        public int EffectType;
        public int Value;
        public float FloatValue;
        public bool Success;
    }
}
