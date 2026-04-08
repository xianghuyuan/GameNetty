using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleSpatialGrid))]
    [FriendOf(typeof(BattleSpatialGrid))]
    [FriendOf(typeof(BattleRoom))]
    public static partial class BattleSpatialGridSystem
    {
        /// <summary>
        /// 默认网格单元大小
        /// </summary>
        private const float DefaultCellSize = 5f;

        [EntitySystem]
        private static void Awake(this BattleSpatialGrid self, float cellSize)
        {
            self.CellSize = cellSize > 0 ? cellSize : DefaultCellSize;
        }

        [EntitySystem]
        private static void Destroy(this BattleSpatialGrid self)
        {
            self.Cells.Clear();
            self.UnitCellMap.Clear();
        }

        /// <summary>
        /// 注册单位到网格。在单位创建或移动后调用。
        /// </summary>
        public static void Insert(this BattleSpatialGrid self, long unitId, float x)
        {
            int cellIndex = GetCellIndex(self, x);

            // 如果已在某个格子，先移除
            if (self.UnitCellMap.TryGetValue(unitId, out int oldCellIndex))
            {
                if (oldCellIndex == cellIndex)
                {
                    return;
                }
                self.RemoveFromCell(unitId, oldCellIndex);
            }

            // 添加到新格子
            self.UnitCellMap[unitId] = cellIndex;
            if (!self.Cells.TryGetValue(cellIndex, out List<long> unitList))
            {
                unitList = new List<long>();
                self.Cells[cellIndex] = unitList;
            }
            unitList.Add(unitId);
        }

        /// <summary>
        /// 从网格中移除单位。在单位销毁时调用。
        /// </summary>
        public static void Remove(this BattleSpatialGrid self, long unitId)
        {
            if (!self.UnitCellMap.TryGetValue(unitId, out int cellIndex))
            {
                return;
            }

            self.RemoveFromCell(unitId, cellIndex);
            self.UnitCellMap.Remove(unitId);
        }

        /// <summary>
        /// 更新单位所在的网格（单位移动后调用）。
        /// </summary>
        public static void UpdatePosition(this BattleSpatialGrid self, long unitId, float newX)
        {
            int newCellIndex = GetCellIndex(self, newX);

            if (self.UnitCellMap.TryGetValue(unitId, out int oldCellIndex))
            {
                if (oldCellIndex == newCellIndex)
                {
                    return;
                }
                self.RemoveFromCell(unitId, oldCellIndex);
            }

            self.UnitCellMap[unitId] = newCellIndex;
            if (!self.Cells.TryGetValue(newCellIndex, out List<long> unitList))
            {
                unitList = new List<long>();
                self.Cells[newCellIndex] = unitList;
            }
            unitList.Add(unitId);
        }

        /// <summary>
        /// 查询指定矩形范围内的所有单位ID。
        /// 仅考虑X轴（横版战斗），Y轴范围暂不使用。
        /// </summary>
        public static void QueryRange(this BattleSpatialGrid self, float minX, float maxX, List<long> results)
        {
            results.Clear();

            int minCellIndex = GetCellIndex(self, minX);
            int maxCellIndex = GetCellIndex(self, maxX);

            for (int cellIndex = minCellIndex; cellIndex <= maxCellIndex; cellIndex++)
            {
                if (self.Cells.TryGetValue(cellIndex, out List<long> unitList))
                {
                    for (int i = 0; i < unitList.Count; i++)
                    {
                        results.Add(unitList[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 查询指定单位周围N个格子范围内的所有单位ID。
        /// 用于技能范围查询等场景。
        /// </summary>
        public static void QueryNearby(this BattleSpatialGrid self, long unitId, int cellRange, List<long> results)
        {
            results.Clear();

            if (!self.UnitCellMap.TryGetValue(unitId, out int centerCell))
            {
                return;
            }

            for (int cellIndex = centerCell - cellRange; cellIndex <= centerCell + cellRange; cellIndex++)
            {
                if (self.Cells.TryGetValue(cellIndex, out List<long> unitList))
                {
                    for (int i = 0; i < unitList.Count; i++)
                    {
                        if (unitList[i] != unitId)
                        {
                            results.Add(unitList[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前网格中注册的单位总数
        /// </summary>
        public static int Count(this BattleSpatialGrid self)
        {
            return self.UnitCellMap.Count;
        }

        private static void RemoveFromCell(this BattleSpatialGrid self, long unitId, int cellIndex)
        {
            if (self.Cells.TryGetValue(cellIndex, out List<long> unitList))
            {
                unitList.Remove(unitId);
                if (unitList.Count == 0)
                {
                    self.Cells.Remove(cellIndex);
                }
            }
        }

        private static int GetCellIndex(BattleSpatialGrid self, float x)
        {
            if (x >= 0)
            {
                return (int)(x / self.CellSize);
            }
            return (int)(x / self.CellSize) - 1;
        }
    }
}
