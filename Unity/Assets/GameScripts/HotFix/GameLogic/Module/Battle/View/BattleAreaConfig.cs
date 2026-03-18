using UnityEngine;

namespace ET
{
    /// <summary>
    /// 战斗区域配置
    /// 2D 游戏，相机 orthographicSize = 5，可视范围 X(-8.89, 8.89), Y(-5, 5)
    /// 布局：玩家在左，怪物在右
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
        /// 玩家出生 X 坐标（屏幕左侧）
        /// </summary>
        public const float PlayerSpawnX = -5f;
        
        /// <summary>
        /// 怪物生成 X 坐标（屏幕右侧）
        /// </summary>
        public const float MonsterSpawnX = 5f;
        
        /// <summary>
        /// Y 范围（上下浮动）
        /// </summary>
        public const float SpawnYMin = -3f;
        public const float SpawnYMax = 3f;
        
        /// <summary>
        /// 战斗单位 Prefab 路径
        /// </summary>
        public const string BattleUnitViewPrefabPath = "BattleUnitView";
    }
}
