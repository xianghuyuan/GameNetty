using System.Numerics;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 战斗单位帮助类 - 用于创建战斗单位信息，避免静态类循环依赖
    /// </summary>
    public static partial class BattleUnitHelper
    {
        /// <summary>
        /// 创建战斗单位信息（包含数值）
        /// </summary>
        public static BattleUnitInfo CreateBattleUnitInfo(BattleUnit unit)
        {
            BattleUnitInfo unitInfo = BattleUnitInfo.Create();
            unitInfo.unitId = unit.Id;
            unitInfo.configId = unit.ConfigId;
            unitInfo.camp = (int)unit.Camp;
            unitInfo.position = new float3(unit.Position.X, unit.Position.Y, unit.Position.Z);
            unitInfo.forward = new float3(1, 0, 0);
            
            NumericComponent numeric = unit.GetComponent<NumericComponent>();
            if (numeric != null)
            {
                unitInfo.hp = numeric.GetAsInt(NumericType.Hp);
                unitInfo.maxHp = numeric.GetAsInt(NumericType.MaxHp);
                unitInfo.attack = numeric.GetAsInt(NumericType.Attack);
            }
            
            BattleUnitCombatComponent combat = unit.GetComponent<BattleUnitCombatComponent>();
            if (combat != null)
            {
                unitInfo.attackRange = combat.AttackRange;
            }
            
            return unitInfo;
        }

        public static void BroadcastMoveCommand(BattleUnit unit, Vector3 targetPosition, float moveSpeed, bool isMoving, float duration, float moveCoefficient)
        {
            if (unit == null)
            {
                return;
            }

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            M2C_BattleUnitMoveCommand message = M2C_BattleUnitMoveCommand.Create();
            message.battleId = battleRoom.Id;
            message.unitId = unit.Id;
            message.targetPosition = new float3(targetPosition.X, targetPosition.Y, targetPosition.Z);
            message.moveSpeed = moveSpeed;
            message.isMoving = isMoving;
            message.duration = duration;
            message.moveCoefficient = moveCoefficient;

            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, message);
                }
            }
        }

        public static void BroadcastSkillCast(BattleUnit caster, int skillId, long targetId, Vector3 targetPosition)
        {
            if (caster == null)
            {
                return;
            }

            BattleRoom battleRoom = caster.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }
            
            M2C_SkillCast message = M2C_SkillCast.Create();
            message.casterId = caster.Id;
            message.skillId = skillId;
            message.targetId = targetId;
            message.targetPos = new float3(targetPosition.X, targetPosition.Y, targetPosition.Z);

            BroadcastToPlayers(battleRoom, message);
        }

        public static void BroadcastDamage(BattleUnit attacker, BattleUnit target, int damage, int damageType)
        {
            if (attacker == null || target == null)
            {
                return;
            }

            BattleRoom battleRoom = attacker.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            NumericComponent numeric = target.GetComponent<NumericComponent>();

            M2C_Damage message = M2C_Damage.Create();
            message.attackerId = attacker.Id;
            message.targetId = target.Id;
            message.damage = damage;
            message.isCrit = false;
            message.targetCurrentHp = numeric?.GetAsInt(NumericType.Hp) ?? 0;
            message.targetMaxHp = numeric?.GetAsInt(NumericType.MaxHp) ?? 0;
            message.targetDead = target.IsDead;
            message.damageType = damageType;

            BroadcastToPlayers(battleRoom, message);
        }

        public static void BroadcastUnitDead(BattleUnit unit, long killerId)
        {
            if (unit == null)
            {
                return;
            }

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            M2C_UnitDead message = M2C_UnitDead.Create();
            message.unitId = unit.Id;
            message.killerId = killerId;

            BroadcastToPlayers(battleRoom, message);
        }
        
        /// <summary>
        /// 广播单位冻结状态
        /// </summary>
        public static void BroadcastUnitFrozen(BattleUnit unit, int durationMs)
        {
            if (unit == null)
            {
                return;
            }

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            M2C_UnitFrozen message = M2C_UnitFrozen.Create();
            message.unitId = unit.Id;
            message.durationMs = durationMs;

            BroadcastToPlayers(battleRoom, message);
        }
        
        /// <summary>
        /// 广播单位击退
        /// </summary>
        public static void BroadcastKnockback(BattleUnit unit, float distance, float direction)
        {
            if (unit == null)
            {
                return;
            }

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            M2C_UnitKnockback message = M2C_UnitKnockback.Create();
            message.unitId = unit.Id;
            message.distance = distance;
            message.direction = direction;
            message.newPosition = new float3(unit.Position.X, unit.Position.Y, unit.Position.Z);

            BroadcastToPlayers(battleRoom, message);
        }

        private static void BroadcastToPlayers(BattleRoom battleRoom, IMessage message)
        {
            if (battleRoom == null)
            {
                return;
            }

            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, message);
                }
            }
        }
    }
}
