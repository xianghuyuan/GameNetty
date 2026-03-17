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
            
            BattleRoom battleRoom = roomManager.GetBattleRoomById(request.targetId);
            if (battleRoom == null)
            {
                foreach (var roomRef in scene.GetComponent<BattleRoomManagerComponent>().BattleRoomIdToBattleRoom.Values)
                {
                    BattleRoom room = roomRef;
                    if (room != null)
                    {
                        battleRoom = room;
                        break;
                    }
                }
            }
            
            if (battleRoom == null)
            {
                response.Error = ErrorCode.ERR_BattleNotInBattle;
                response.Message = "Not in battle";
                return;
            }
            
            BattleUnit attacker = null;
            BattleUnit target = null;
            
            foreach (var kv in battleRoom.Units)
            {
                BattleUnit battleUnit = kv.Value;
                if (battleUnit != null && !battleUnit.IsDead)
                {
                    if (battleUnit.Camp == UnitCamp.Friend && attacker == null)
                    {
                        attacker = battleUnit;
                    }
                    else if (battleUnit.Id == request.targetId)
                    {
                        target = battleUnit;
                    }
                }
            }
            
            if (attacker == null)
            {
                response.Error = ErrorCode.ERR_BattleUnitNotFound;
                response.Message = "Attacker not found";
                return;
            }
            
            if (target == null || target.IsDead)
            {
                response.Error = ErrorCode.ERR_BattleTargetNotFound;
                response.Message = "Target not found or dead";
                return;
            }
            
            if (target.Camp == attacker.Camp)
            {
                response.Error = ErrorCode.ERR_BattleCannotAttackAlly;
                response.Message = "Cannot attack ally";
                return;
            }
            
            BattleUnitCombatComponent combat = attacker.GetComponent<BattleUnitCombatComponent>();
            if (combat == null || !combat.IsAttackReady())
            {
                response.Error = ErrorCode.ERR_BattleAttackNotReady;
                response.Message = "Attack not ready";
                return;
            }
            
            int damage = CalculateDamage(attacker, target);
            target.TakeDamage(damage);
            
            combat.StartAttackCooldown();
            
            response.result = CombatResultProto.Create();
            response.result.attackerId = attacker.Id;
            response.result.targetId = target.Id;
            response.result.damage = damage;
            
            NumericComponent attackerNumeric = attacker.GetComponent<NumericComponent>();
            NumericComponent targetNumeric = target.GetComponent<NumericComponent>();
            
            response.result.attackerCurrentHp = attackerNumeric?.GetAsInt(NumericType.Hp) ?? 0;
            response.result.targetCurrentHp = targetNumeric?.GetAsInt(NumericType.Hp) ?? 0;
            response.result.targetDead = target.IsDead;
            response.result.attackerDead = attacker.IsDead;
            
            Log.Debug($"玩家攻击: Attacker={attacker.Id}, Target={target.Id}, Damage={damage}");
            
            await ETTask.CompletedTask;
        }
        
        private static int CalculateDamage(BattleUnit attacker, BattleUnit target)
        {
            NumericComponent attackerNumeric = attacker.GetComponent<NumericComponent>();
            NumericComponent targetNumeric = target.GetComponent<NumericComponent>();
            
            if (attackerNumeric == null || targetNumeric == null)
            {
                return 1;
            }
            
            int attack = attackerNumeric.GetAsInt(NumericType.Attack);
            int defense = targetNumeric.GetAsInt(NumericType.Defense);
            
            int damage = attack - defense;
            if (damage < 1)
            {
                damage = 1;
            }
            
            return damage;
        }
    }
}
