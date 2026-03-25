using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(SlotManagerComponent))]
    [FriendOf(typeof(SlotManagerComponent))]
    public static partial class SlotManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SlotManagerComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SlotManagerComponent self)
        {
            self.TargetSlots.Clear();
            self.UnitSlotMap.Clear();
        }

        /// <summary>
        /// 为单位分配站位插槽
        /// 左右交替分配：index 0=左1, 1=右1, 2=左2, 3=右2 ...
        /// </summary>
        /// <param name="self">插槽管理器</param>
        /// <param name="unitId">请求站位的单位ID</param>
        /// <param name="targetUnitId">目标单位ID</param>
        /// <param name="targetPosition">目标当前位置（用于计算插槽世界坐标）</param>
        /// <returns>分配的插槽世界坐标，分配失败返回 null</returns>
        public static Vector3? AllocateSlot(this SlotManagerComponent self, long unitId, long targetUnitId, Vector3 targetPosition)
        {
            float spacing = BattleGlobalConfigCategory.Instance.SlotSpacing;
            // 如果该单位已经占用了某个插槽，先释放
            if (self.UnitSlotMap.TryGetValue(unitId, out var existing))
            {
                if (existing.TargetUnitId == targetUnitId)
                {
                    // 同一个目标，直接返回当前插槽位置
                    return GetSlotWorldPosition(targetPosition, existing.SlotIndex, spacing);
                }

                // 不同目标，释放旧插槽
                self.ReleaseSlot(unitId);
            }

            if (!self.TargetSlots.ContainsKey(targetUnitId))
            {
                self.TargetSlots[targetUnitId] = new Dictionary<int, long>();
            }

            Dictionary<int, long> slots = self.TargetSlots[targetUnitId];

            // 找到最小的空闲 slotIndex
            int slotIndex = 0;
            while (slots.ContainsKey(slotIndex))
            {
                slotIndex++;
            }

            slots[slotIndex] = unitId;
            self.UnitSlotMap[unitId] = (targetUnitId, slotIndex);
            return GetSlotWorldPosition(targetPosition, slotIndex, spacing);
        }

        /// <summary>
        /// 释放单位占用的插槽
        /// </summary>
        /// <param name="self">插槽管理器</param>
        /// <param name="unitId">要释放的单位ID</param>
        public static void ReleaseSlot(this SlotManagerComponent self, long unitId)
        {
            if (!self.UnitSlotMap.TryGetValue(unitId, out var mapping))
            {
                return;
            }

            if (self.TargetSlots.TryGetValue(mapping.TargetUnitId, out var slots))
            {
                slots.Remove(mapping.SlotIndex);
            }

            self.UnitSlotMap.Remove(unitId);
        }

        /// <summary>
        /// 释放目标单位的所有插槽（目标死亡时调用）
        /// </summary>
        /// <param name="self">插槽管理器</param>
        /// <param name="targetUnitId">目标单位ID</param>
        public static void ReleaseAllSlotsForTarget(this SlotManagerComponent self, long targetUnitId)
        {
            if (!self.TargetSlots.TryGetValue(targetUnitId, out var slots))
            {
                return;
            }

            foreach (var unitId in slots.Values)
            {
                self.UnitSlotMap.Remove(unitId);
            }

            slots.Clear();
            self.TargetSlots.Remove(targetUnitId);
        }

        /// <summary>
        /// 获取单位当前插槽的世界坐标（目标移动时更新位置用）
        /// </summary>
        /// <param name="self">插槽管理器</param>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetPosition">目标当前位置</param>
        /// <returns>插槽世界坐标，未分配返回 null</returns>
        public static Vector3? GetSlotPosition(this SlotManagerComponent self, long unitId, Vector3 targetPosition)
        {
            if (!self.UnitSlotMap.TryGetValue(unitId, out var mapping))
            {
                return null;
            }

            float spacing = BattleGlobalConfigCategory.Instance.SlotSpacing;
            return GetSlotWorldPosition(targetPosition, mapping.SlotIndex, spacing);
        }

        /// <summary>
        /// 根据 slotIndex 计算世界坐标
        /// 偶数索引在目标左侧，奇数索引在目标右侧
        /// </summary>
        private static Vector3 GetSlotWorldPosition(Vector3 targetPosition, int slotIndex, float spacing)
        {
            int layer = slotIndex / 2 + 1;
            float directionSign = (slotIndex % 2 == 0) ? -1f : 1f;
            float offsetX = directionSign * layer * spacing;
            return new Vector3(targetPosition.X + offsetX, targetPosition.Y, targetPosition.Z);
        }
    }
}
