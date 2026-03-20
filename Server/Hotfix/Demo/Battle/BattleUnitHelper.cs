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

        public static void BroadcastMoveCommand(BattleUnit unit, Vector3 targetPosition, float moveSpeed, bool isMoving)
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
    }
}
