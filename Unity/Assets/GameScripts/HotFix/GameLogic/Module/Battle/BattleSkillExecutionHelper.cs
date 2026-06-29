namespace ET
{
    /// <summary>
    /// 战斗技能执行器。
    /// AI 只负责做决策，执行器统一协调表现、技能提交与命中时的本地落账。
    /// </summary>
    public static class BattleSkillExecutionHelper
    {
        private static readonly IBattleSkillExecutionExtension OfflineExtension = new OfflineBattleSkillExecutionExtension();
        private static readonly IBattleSkillExecutionExtension OnlineExtension = new OnlineBattleSkillExecutionExtension();

        public static void ExecutePlayerSkill(Battle battle, BattleUnit caster, int skillId, long explicitTargetId)
        {
            if (battle == null || caster == null || caster.IsDead)
            {
                return;
            }

            EmitterConfig skillConfig = ConfigHelper.EmitterConfig?.GetOrDefault(skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return;
            }

            ApplyCastMovePolicy(caster, skillConfig);

            IBattleSkillExecutionExtension extension = ResolveExtension(battle);
            extension.OnSkillTriggered(battle, caster, skillId, explicitTargetId);

            void OnHit()
            {
                if (battle.IsDisposed || caster.IsDisposed || caster.IsDead)
                {
                    return;
                }

                if (!BattleSkillSimulationHelper.TrySimulate(battle, caster, skillId, explicitTargetId, out BattleSkillSimulationHelper.SkillSimulation simulation))
                {
                    return;
                }

                extension.OnHitFrame(battle, caster, explicitTargetId, simulation);
            }

            BattleUnitView view = caster.GetComponent<BattleUnitView>();
            if (view != null && !view.IsDisposed && view.SkeletonAnimation != null)
            {
                view.PlayAttackFeedback(OnHit);
                EventSystem.Instance.Publish(caster.Scene(), new BattleUnitSkillCast { Unit = caster });
                return;
            }

            EventSystem.Instance.Publish(caster.Scene(), new BattleUnitSkillCast { Unit = caster });
            OnHit();
        }

        public static void ApplyCastMovePolicy(BattleUnit caster, EmitterConfig skillConfig)
        {
            if (caster == null || skillConfig == null)
            {
                return;
            }

            if (!skillConfig.CanMoveCast)
            {
                caster.Forward = Unity.Mathematics.float3.zero;
            }
        }

        private static IBattleSkillExecutionExtension ResolveExtension(Battle battle)
        {
            return battle.GetComponent<OfflineBattleComponent>() != null ? OfflineExtension : OnlineExtension;
        }

        private interface IBattleSkillExecutionExtension
        {
            void OnSkillTriggered(Battle battle, BattleUnit caster, int skillId, long explicitTargetId);

            void OnHitFrame(Battle battle, BattleUnit caster, long explicitTargetId, BattleSkillSimulationHelper.SkillSimulation simulation);
        }

        private sealed class OfflineBattleSkillExecutionExtension : IBattleSkillExecutionExtension
        {
            public void OnSkillTriggered(Battle battle, BattleUnit caster, int skillId, long explicitTargetId)
            {
            }

            public void OnHitFrame(Battle battle, BattleUnit caster, long explicitTargetId, BattleSkillSimulationHelper.SkillSimulation simulation)
            {
                BattleSkillSimulationHelper.ApplyLocalImpacts(battle, caster, simulation, true);
            }
        }

        private sealed class OnlineBattleSkillExecutionExtension : IBattleSkillExecutionExtension
        {
            public void OnSkillTriggered(Battle battle, BattleUnit caster, int skillId, long explicitTargetId)
            {
                BattleHelper.CastSkill(caster.Scene(), skillId, explicitTargetId).Coroutine();
            }

            public void OnHitFrame(Battle battle, BattleUnit caster, long explicitTargetId, BattleSkillSimulationHelper.SkillSimulation simulation)
            {
                BattleSkillSimulationHelper.ApplyLocalImpacts(battle, caster, simulation, false);
                ClientBattleDamageHelper.SendValidationRequest(battle, caster, simulation);
            }
        }
    }
}
