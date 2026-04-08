namespace ET.Server
{
    [MessageSessionHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_CastSkillHandler : MessageSessionHandler<C2M_CastSkill, M2C_CastSkill>
    {
        protected override async ETTask Run(Session session, C2M_CastSkill request, M2C_CastSkill response)
        {
            // 从 Session 获取玩家
            SessionPlayerComponent sessionPlayer = session.GetComponent<SessionPlayerComponent>();
            if (sessionPlayer?.Player == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }

            Player player = sessionPlayer.Player;

            // 获取玩家所在的 Map 场景
            Scene mapScene = player.GetParent<Scene>();
            if (mapScene == null || mapScene.SceneType != SceneType.Map)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in map scene";
                return;
            }

            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }

            // 获取玩家所在的战斗房间
            BattleRoom battleRoom = roomManager.GetBattleRoomByUnitId(player.Id);
            if (battleRoom == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }

            // 获取玩家的战斗单位
            BattleUnit caster = battleRoom.GetUnit(player.Id);
            if (caster == null || caster.IsDead)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Caster not found or dead";
                return;
            }

            if (!BattleSkillHelper.TryExecuteSkill(caster, request.skillId, request.targetId, out BattleSkillHelper.SkillExecutionResult executionResult))
            {
                response.Error = executionResult.Error;
                response.Message = executionResult.Message;
                return;
            }

            response.cooldownEnd = executionResult.CooldownEnd;

            await ETTask.CompletedTask;
        }
    }
}
