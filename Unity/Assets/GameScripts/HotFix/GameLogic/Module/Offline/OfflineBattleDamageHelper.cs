namespace ET
{
    /// <summary>
    /// 离线伤害计算器 - 复制服务端 BattleSkillHelper 的伤害公式
    /// 供离线模式下玩家技能伤害本地结算使用
    /// </summary>
    public static class OfflineBattleDamageHelper
    {
        public struct DamageResult
        {
            public int TotalDamage;
            public bool TargetDied;
        }

        /// <summary>
        /// 对目标施放技能效果，返回伤害结果
        /// 只处理 CastType=0（即时）技能
        /// </summary>
        public static DamageResult ApplySkillEffects(BattleUnit caster, BattleUnit target, int skillId)
        {
            if (caster == null || target == null || caster.IsDisposed || target.IsDisposed)
            {
                return default;
            }

            Battle battle = caster.GetParent<Battle>();
            if (battle == null)
            {
                return default;
            }

            if (!BattleSkillSimulationHelper.TrySimulate(battle, caster, skillId, target.Id, out BattleSkillSimulationHelper.SkillSimulation simulation))
            {
                return default;
            }

            foreach (BattleSkillSimulationHelper.PredictedTargetImpact impact in simulation.Impacts)
            {
                if (impact.TargetId != target.Id)
                {
                    continue;
                }

                bool wasAlive = !target.IsDead;
                BattleSkillSimulationHelper.ApplyLocalImpacts(battle, caster, simulation, true);
                return new DamageResult
                {
                    TotalDamage = impact.TotalDamage,
                    TargetDied = wasAlive && target.IsDead,
                };
            }

            return default;
        }
    }
}
