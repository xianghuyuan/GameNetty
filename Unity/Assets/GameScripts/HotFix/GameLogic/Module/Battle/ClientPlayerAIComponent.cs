using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 客户端玩家AI组件 - 挂载在玩家的BattleUnit上
    /// 负责自动选目标、自动释放技能、增量移动
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class ClientPlayerAIComponent : Entity, IAwake, IDestroy
    {
        public long CurrentTargetId { get; set; }

        public long LastAttackTimeMs { get; set; }

        /// <summary>
        /// 技能冷却结束时间映射 SkillId -> ClientNowMs
        /// </summary>
        public Dictionary<int, long> SkillCooldownEnd { get; } = new();
    }
}
