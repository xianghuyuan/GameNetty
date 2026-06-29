using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 在线模式下的客户端验证请求辅助类。
    /// 统一行为模拟由 BattleSkillSimulationHelper 负责，这里只负责把本地命中结果发送给服务端验证。
    /// </summary>
    public static class ClientBattleDamageHelper
    {
        public static void SendValidationRequest(Battle battle, BattleUnit caster, BattleSkillSimulationHelper.SkillSimulation simulation)
        {
            if (battle == null || caster == null || simulation == null)
            {
                return;
            }

            List<long> hitUnitIds = new List<long>();
            foreach (BattleSkillSimulationHelper.PredictedTargetImpact impact in simulation.Impacts)
            {
                if (impact.IsBoss || impact.TotalDamage <= 0)
                {
                    continue;
                }

                hitUnitIds.Add(impact.TargetId);
            }

            if (hitUnitIds.Count == 0)
            {
                return;
            }

            C2M_ClientBatchHit message = C2M_ClientBatchHit.Create();
            message.battleId = battle.BattleId;
            message.skillId = simulation.SkillId;
            message.casterId = caster.Id;
            message.hitUnitIds = hitUnitIds;

            float faceDir = caster.FaceDirection;
            float casterX = caster.Position.x;
            message.hitBoxMinX = faceDir >= 0f ? casterX : casterX - simulation.CastRange;
            message.hitBoxMaxX = faceDir >= 0f ? casterX + simulation.CastRange : casterX;

            long nowMs = TimeInfo.Instance.ClientNow();
            message.hitStartTick = nowMs;
            message.hitEndTick = nowMs;

            Log.Debug($"[ClientBattleDamage] SendClientBatchHit: skillId={simulation.SkillId} hitCount={hitUnitIds.Count} hitUnitIds=[{string.Join(",", hitUnitIds)}]");
            battle.Root().GetComponent<ClientSenderComponent>()?.Send(message);
        }
    }
}
