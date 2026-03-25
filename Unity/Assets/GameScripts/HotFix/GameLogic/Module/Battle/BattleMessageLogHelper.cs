using Unity.Mathematics;
using System.Linq;

namespace ET
{
    public static class BattleMessageLogHelper
    {
        public static string FormatMoveCommand(Battle battle, M2C_BattleUnitMoveCommand message)
        {
            string unitLabel = GetUnitLabel(battle, message.unitId);

            if (message.isMoving)
            {
                return $"[收到消息注释] M2C_BattleUnitMoveCommand -> {unitLabel} 移动到 {FormatFloat3(message.targetPosition)}，速度 {message.moveSpeed:F2}，时间 {message.duration:F2}秒，系数 {message.moveCoefficient:F2}";
            }
            
            return $"[收到消息注释] M2C_BattleUnitMoveCommand -> {unitLabel} 停止移动，停在 {FormatFloat3(message.targetPosition)}";
        }

        public static string FormatSkillCast(Battle battle, M2C_SkillCast message)
        {
            string casterLabel = GetUnitLabel(battle, message.casterId);
            string targetLabel = GetUnitLabel(battle, message.targetId);
            return $"[收到消息注释] M2C_SkillCast -> {casterLabel} 释放技能 {message.skillId}，目标 {targetLabel}，目标位置 {FormatFloat3(message.targetPos)}";
        }

        public static string FormatDamage(Battle battle, M2C_Damage message)
        {
            string attackerLabel = GetUnitLabel(battle, message.attackerId);
            string targetLabel = GetUnitLabel(battle, message.targetId);
            return $"[收到消息注释] M2C_Damage -> {attackerLabel} 对 {targetLabel} 造成 {message.damage} 点{FormatDamageType(message.damageType)}伤害，目标血量 {message.targetCurrentHp}/{message.targetMaxHp}，死亡={message.targetDead}";
        }

        public static string FormatUnitDead(Battle battle, M2C_UnitDead message)
        {
            string deadUnitLabel = GetUnitLabel(battle, message.unitId);
            string killerLabel = GetUnitLabel(battle, message.killerId);
            return $"[收到消息注释] M2C_UnitDead -> {deadUnitLabel} 已死亡，击杀者 {killerLabel}";
        }

        private static string GetUnitLabel(Battle battle, long unitId)
        {
            if (unitId == 0)
            {
                return "无目标";
            }

            BattleUnit unit = battle?.GetChild<BattleUnit>(unitId);
            if (unit == null)
            {
                return $"单位{unitId}";
            }

            if (unit.Camp == UnitCamp.Friend)
            {
                return "玩家";
            }

            if (unit.Camp == UnitCamp.Enemy)
            {
                int enemyIndex = battle.GetBattleUnitsByCamp(UnitCamp.Enemy)
                    .OrderBy(x => x.Id)
                    .ToList()
                    .FindIndex(x => x.Id == unitId);
                return enemyIndex >= 0 ? $"敌人{enemyIndex + 1}" : $"敌人({unitId})";
            }

            return $"单位{unitId}";
        }

        private static string FormatFloat3(float3 value)
        {
            return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
        }

        private static string FormatDirection(float direction)
        {
            return direction switch
            {
                < 0f => "目标左侧",
                > 0f => "目标右侧",
                _ => "居中",
            };
        }

        private static string FormatDamageType(int damageType)
        {
            return damageType switch
            {
                0 => "普攻",
                1 => "技能",
                2 => "持续",
                3 => "反伤",
                _ => $"未知({damageType})",
            };
        }
    }
}
