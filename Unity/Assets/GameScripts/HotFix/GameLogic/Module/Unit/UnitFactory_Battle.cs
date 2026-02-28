using Unity.Mathematics;

namespace ET
{
    public static partial class UnitFactory
    {
        /// <summary>
        /// 创建玩家英雄（从主世界 Unit）
        /// </summary>
        /// <param name="battle">战斗实例</param>
        /// <param name="unit">主世界 Unit</param>
        /// <param name="position">战斗位置</param>
        /// <returns>创建的 BattleUnit</returns>
        public static BattleUnit CreateHero(Battle battle, Unit unit, float3 position)
        {
            // 使用 BattleUnitHelper 从 Unit 创建 BattleUnit
            return BattleUnitHelper.CreateFromUnit(battle, unit, position);
        }
    
        /// <summary>
        /// 创建怪物（从配置表）
        /// </summary>
        /// <param name="battle">战斗实例</param>
        /// <param name="configId">配置表 ID</param>
        /// <param name="position">位置</param>
        /// <returns>创建的 BattleUnit</returns>
        public static BattleUnit CreateMonster(Battle battle, int configId, float3 position)
        {
            // 使用 BattleUnitHelper 从配置表创建怪物
            return BattleUnitHelper.CreateMonsterFromConfig(battle, configId, position);
        }
    }
}
