using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 战斗区域配置。
    /// 卷轴战斗当前只使用 X 坐标，客户端直接使用服务端传来的 X，Y 固定在战斗线。
    /// </summary>
    public static class BattleAreaConfig
    {
        /// <summary>
        /// 战斗区域中心坐标（相机位置）
        /// </summary>
        public static readonly Vector3 BattleAreaCenter = Vector3.zero;
        
        /// <summary>
        /// 战斗区域大小（基于相机可视范围）
        /// </summary>
        public static readonly Vector2 BattleAreaSize = new Vector2(16f, 9f);
        
        /// <summary>
        /// 玩家战斗单位 Prefab 路径
        /// </summary>
        public const string HeroUnitViewPrefabPath = "HeroUnitView";

        /// <summary>
        /// 怪物战斗单位 Prefab 路径
        /// </summary>
        public const string MonsterUnitViewPrefabPath = "MonsterUnitView";

        public static Vector3 GetWorldPosition(UnitCamp camp, float3 position)
        {
            return new Vector3(position.x + BattleAreaCenter.x, BattleAreaCenter.y, 0f);
        }
    }
}
