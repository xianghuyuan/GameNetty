using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 站位插槽管理器
    /// 挂在 BattleRoom 上，管理所有目标单位的站位插槽分配
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class SlotManagerComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// targetUnitId -> (slotIndex -> unitId)
        /// 每个目标单位维护独立的插槽映射
        /// </summary>
        public Dictionary<long, Dictionary<int, long>> TargetSlots { get; } = new();

        /// <summary>
        /// unitId -> (targetUnitId, slotIndex)
        /// 反向映射，快速查找单位占用的插槽
        /// </summary>
        public Dictionary<long, (long TargetUnitId, int SlotIndex)> UnitSlotMap { get; } = new();
    }
}
