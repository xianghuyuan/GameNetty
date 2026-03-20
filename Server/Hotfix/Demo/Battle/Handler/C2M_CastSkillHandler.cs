namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_CastSkillHandler : MessageHandler<Scene, C2M_CastSkill, M2C_CastSkill>
    {
        protected override async ETTask Run(Scene scene, C2M_CastSkill request, M2C_CastSkill response)
        {
            BattleRoomManagerComponent roomManager = scene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }

            BattleRoom battleRoom = null;
            BattleUnit caster = null;

            foreach (EntityRef<BattleRoom> roomRef in roomManager.BattleRoomIdToBattleRoom.Values)
            {
                BattleRoom room = roomRef;
                if (room == null)
                {
                    continue;
                }

                foreach (var kv in room.Units)
                {
                    BattleUnit battleUnit = kv.Value;
                    if (battleUnit != null && !battleUnit.IsDead && battleUnit.Camp == UnitCamp.Friend)
                    {
                        caster = battleUnit;
                        battleRoom = room;
                        break;
                    }
                }

                if (caster != null)
                {
                    break;
                }
            }

            if (battleRoom == null || caster == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }

            if (!BattleSkillHelper.TryExecuteSkill(caster, request.skillId, request.targetId, out BattleSkillHelper.SkillExecutionResult executionResult))
            {
                response.Error = executionResult.Error;
                response.Message = executionResult.Message;
                return;
            }

            response.cooldownEnd = executionResult.CooldownEnd;

            Log.Info($"玩家释放技能成功: UnitId={caster.Id}, SkillId={request.skillId}, TargetId={executionResult.MainTarget?.Id ?? 0}, Damage={executionResult.TotalDamage}");

            await ETTask.CompletedTask;
        }
    }
}
