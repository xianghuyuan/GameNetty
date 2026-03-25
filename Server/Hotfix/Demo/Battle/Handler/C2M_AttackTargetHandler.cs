namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_AttackTargetHandler : MessageHandler<Scene, C2M_AttackTarget, M2C_AttackTarget>
    {
        protected override async ETTask Run(Scene scene, C2M_AttackTarget request, M2C_AttackTarget response)
        {
            BattleRoomManagerComponent roomManager = scene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }
            
            // 找到玩家所在的战斗房间
            BattleRoom battleRoom = null;
            BattleUnit attacker = null;
            
            foreach (var roomRef in roomManager.BattleRoomIdToBattleRoom.Values)
            {
                BattleRoom room = roomRef;
                if (room == null) continue;
                
                foreach (var kv in room.Units)
                {
                    BattleUnit unit = kv.Value;
                    if (unit != null && !unit.IsDead && unit.Camp == UnitCamp.Friend)
                    {
                        attacker = unit;
                        battleRoom = room;
                        break;
                    }
                }
                if (attacker != null) break;
            }
            
            if (battleRoom == null || attacker == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }
            
            if (!BattleSkillHelper.TryExecuteNormalAttack(attacker, request.targetId, out BattleSkillHelper.SkillExecutionResult executionResult))
            {
                response.Error = executionResult.Error;
                response.Message = executionResult.Message;
                return;
            }

            BattleUnit mainTarget = executionResult.MainTarget;
            NumericComponent attackerNumeric = attacker.GetComponent<NumericComponent>();
            NumericComponent mainTargetNumeric = mainTarget.GetComponent<NumericComponent>();
            
            response.result = CombatResultProto.Create();
            response.result.attackerId = attacker.Id;
            response.result.targetId = mainTarget.Id;
            response.result.damage = executionResult.TotalDamage;
            response.result.attackerCurrentHp = attackerNumeric?.GetAsInt(NumericType.Hp) ?? 0;
            response.result.targetCurrentHp = mainTargetNumeric?.GetAsInt(NumericType.Hp) ?? 0;
            response.result.targetDead = mainTarget.IsDead;
            response.result.attackerDead = attacker.IsDead;
            
            await ETTask.CompletedTask;
        }
    }
}
