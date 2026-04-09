using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;

namespace ET.Server
{
    public static partial class BattleUnitHelper
    {
        public static BattleUnitInfo CreateBattleUnitInfo(BattleUnit unit)
        {
            BattleUnitInfo unitInfo = BattleUnitInfo.Create();
            unitInfo.unitId = unit.Id;
            unitInfo.configId = unit.ConfigId;
            unitInfo.camp = (int)unit.Camp;
            unitInfo.position = new float3(unit.Position.X, unit.Position.Y, unit.Position.Z);
            unitInfo.forward = new float3(1, 0, 0);
            unitInfo.isBoss = unit.IsBoss;
            unitInfo.ownerId = unit.OwnerId;

            NumericComponent numeric = unit.GetComponent<NumericComponent>();
            if (numeric != null)
            {
                unitInfo.hp = numeric.GetAsInt(NumericType.Hp);
                unitInfo.maxHp = numeric.GetAsInt(NumericType.MaxHp);
                unitInfo.attack = numeric.GetAsInt(NumericType.Attack);
                unitInfo.defense = numeric.GetAsInt(NumericType.Defense);
                unitInfo.speed = numeric.GetAsFloat(NumericType.Speed);
            }

            BattleUnitCombatComponent combat = unit.GetComponent<BattleUnitCombatComponent>();
            if (combat != null)
            {
                unitInfo.attackRange = combat.AttackRange;
            }

            return unitInfo;
        }

        public static void BroadcastToRoom(BattleRoom battleRoom, IMessage message)
        {
            if (battleRoom == null)
            {
                return;
            }

            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    // 内联 MapMessageHelper.SendToClient，避免 BattleUnitHelper → MapMessageHelper 的静态类引用
                    player.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Send(player.Id, message);
                }
            }
        }

        public static void BroadcastMoveCommand(BattleUnit unit, Vector3 targetPosition, float moveSpeed, bool isMoving = true, float duration = 0.1f, float moveCoefficient = 1)
        {
            if (unit == null) return;

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_BattleUnitMoveCommand message = M2C_BattleUnitMoveCommand.Create();
            message.battleId = battleRoom.Id;
            message.unitId = unit.Id;
            message.targetPosition = new float3(targetPosition.X, targetPosition.Y, targetPosition.Z);
            message.moveSpeed = moveSpeed;
            message.isMoving = isMoving;
            message.duration = duration;
            message.moveCoefficient = moveCoefficient;

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastSkillCast(BattleUnit caster, int skillId, long targetId, Vector3 targetPosition)
        {
            if (caster == null) return;

            BattleRoom battleRoom = caster.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_SkillCast message = M2C_SkillCast.Create();
            message.casterId = caster.Id;
            message.skillId = skillId;
            message.targetId = targetId;
            message.targetPos = new float3(targetPosition.X, targetPosition.Y, targetPosition.Z);

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastDamage(BattleUnit attacker, BattleUnit target, int damage, int damageType)
        {
            if (attacker == null || target == null) return;

            BattleRoom battleRoom = attacker.GetParent<BattleRoom>();
            if (battleRoom == null) return;

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

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastUnitDead(BattleUnit unit, long killerId)
        {
            if (unit == null) return;

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_UnitDead message = M2C_UnitDead.Create();
            message.unitId = unit.Id;
            message.killerId = killerId;

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastUnitFrozen(BattleUnit unit, int durationMs)
        {
            if (unit == null) return;

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_UnitFrozen message = M2C_UnitFrozen.Create();
            message.unitId = unit.Id;
            message.durationMs = durationMs;

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastKnockback(BattleUnit unit, float distance, float direction)
        {
            if (unit == null) return;

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_UnitKnockback message = M2C_UnitKnockback.Create();
            message.unitId = unit.Id;
            message.distance = distance;
            message.direction = direction;
            message.newPosition = new float3(unit.Position.X, unit.Position.Y, unit.Position.Z);

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastProjectileLaunch(BattleUnit projectileUnit, long casterId, int skillId, float direction)
        {
            if (projectileUnit == null) return;

            BattleRoom battleRoom = projectileUnit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_ProjectileLaunch message = M2C_ProjectileLaunch.Create();
            message.projectileId = projectileUnit.Id;
            message.casterId = casterId;
            message.skillId = skillId;
            message.position = new float3(projectileUnit.Position.X, projectileUnit.Position.Y, projectileUnit.Position.Z);
            message.direction = direction;

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastProjectileHit(BattleUnit projectileUnit, long targetId)
        {
            if (projectileUnit == null) return;

            BattleRoom battleRoom = projectileUnit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_ProjectileHit message = M2C_ProjectileHit.Create();
            message.projectileId = projectileUnit.Id;
            message.targetId = targetId;
            message.position = new float3(projectileUnit.Position.X, projectileUnit.Position.Y, projectileUnit.Position.Z);

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastProjectileDestroy(BattleUnit projectileUnit)
        {
            if (projectileUnit == null) return;

            BattleRoom battleRoom = projectileUnit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_ProjectileDestroy message = M2C_ProjectileDestroy.Create();
            message.projectileId = projectileUnit.Id;
            message.position = new float3(projectileUnit.Position.X, projectileUnit.Position.Y, projectileUnit.Position.Z);

            BroadcastToRoom(battleRoom, message);
        }

        public static void BroadcastPositionSync(BattleUnit unit)
        {
            if (unit == null) return;

            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null) return;

            M2C_BattleUnitPositionSync message = M2C_BattleUnitPositionSync.Create();
            message.battleId = battleRoom.Id;
            message.unitId = unit.Id;
            message.position = new float3(unit.Position.X, unit.Position.Y, unit.Position.Z);

            BroadcastToRoom(battleRoom, message);
        }
    }
}
