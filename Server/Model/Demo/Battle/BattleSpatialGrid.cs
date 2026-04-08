using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 战斗空间网格 - 用于将战斗区域划分为固定大小的网格，
    /// 加速碰撞检测和范围查询。将 O(N^2) 降为 O(N)。
    /// 仅用于 X 轴（横版战斗）。
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class BattleSpatialGrid : Entity, IAwake<float>, IDestroy
    {
        /// <summary>网格单元大小（世界单位）</summary>
        public float CellSize { get; set; }

        /// <summary>当前网格（key = 网格索引，value = 该格内所有单位ID列表）</summary>
        public Dictionary<int, List<long>> Cells { get; } = new();

        /// <summary>单位ID到网格索引的缓存，用于快速移除</summary>
        public Dictionary<long, int> UnitCellMap { get; } = new();
    }
}
