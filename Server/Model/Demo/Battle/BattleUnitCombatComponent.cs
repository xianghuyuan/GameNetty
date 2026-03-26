using System.Collections.Generic;
namespace ET.Server
{
    /// <summary>
    /// 战斗单位战斗组件
    /// 攻击冷却
    /// 攻击范围
    /// 技能CD
    /// 是否可攻击
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleUnitCombatComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 攻击冷却时间（毫秒）
        /// </summary>
        public int AttackCooldown { get; set; } = 1000;
        
        /// <summary>
        /// 上次攻击时间
        /// </summary>
        public long LastAttackTime { get; set; }
        
        /// <summary>
        /// 攻击范围
        /// </summary>
        public float AttackRange { get; set; } = 2.0f;
        
        /// <summary>
        /// 是否可以攻击
        /// </summary>
        public bool CanAttack { get; set; } = true;

        public Dictionary<int, long> SkillCooldownEnds { get; } = new();
    }
}
