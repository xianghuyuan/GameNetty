using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 战斗单位战斗组件 (客户端)
    /// 同步服务端的攻击属性，用于 UI 显示和本地预测
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleUnitCombatComponent : Entity, IAwake<float>, IDestroy
    {
        /// <summary>
        /// 攻击冷却时间（毫秒）
        /// </summary>
        public int AttackCooldown { get; set; } = 1000;

        /// <summary>
        /// 攻击范围
        /// </summary>
        public float AttackRange { get; set; } = 2.0f;

        /// <summary>
        /// 是否可以攻击
        /// </summary>
        public bool CanAttack { get; set; } = true;

        /// <summary>
        /// 自动技能列表 [TEST] 硬编码，验证后从服务端同步
        /// </summary>
        public int[] AutoSkillIds { get; set; } = System.Array.Empty<int>();
    }
}
