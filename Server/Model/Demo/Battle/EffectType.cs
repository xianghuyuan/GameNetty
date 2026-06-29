namespace ET.Server
{
    /// <summary>
    /// 效果类型枚举 - 定义 BuffConfig 的效果行为类型。
    /// 每种类型对应 BuffExecuteEvent_Handler / BattleSkillHelper.ApplyEffects 中的一个 case 分支。
    /// 即时效果 (Duration=0) 在 ApplyEffects 中直接执行，持续效果 (Duration>0) 注册到 BuffComponent。
    /// </summary>
    public enum EffectType
    {
        Damage = 1,      // 伤害 - 即时，扣减目标HP
        Freeze = 2,      // 冻结 - 持续，停止目标移动和攻击
        Knockback = 3,   // 击退 - 即时，强制位移目标
        Heal = 4,        // 治疗 - 即时，恢复目标HP
        Stun = 5,        // 眩晕 - 持续，停止目标行动
        SlowDown = 6,    // 减速 - 持续，降低目标移动速度
        LifeSteal = 7,   // 吸血 - 即时，造成伤害并按比例回复自身HP
        Shield = 8,      // 护盾 - 持续，增加临时HP吸收伤害
        AttackBuff = 9,  // 增攻 - 持续，临时增加攻击力
        DefenseBuff = 10,// 增防 - 持续，临时增加防御力
        DOT = 11,        // 持续伤害 - 持续，每tick造成一次伤害
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
