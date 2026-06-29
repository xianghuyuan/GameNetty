using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 载具管理组件 - 挂载在玩家 BattleUnit 上
    /// 管理玩家拥有的载具和 Buff 碎片
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class VehicleComponent : Entity, IAwake, IDestroy
    {
        /// <summary>当前装备的载具实例ID</summary>
        public long EquippedVehicleId { get; set; }

        /// <summary>拥有的所有载具</summary>
        public List<VehicleData> OwnedVehicles { get; } = new();

        /// <summary>拥有的 Buff 碎片 BuffConfigId -> 数量</summary>
        public Dictionary<int, int> OwnedShards { get; } = new();

        /// <summary>当前主载具数据（兼容旧逻辑的缓存）</summary>
        public VehicleData EquippedVehicle { get; set; }

        public bool HasVehicleEquipped => EquippedVehicle != null || OwnedVehicles.Exists(v => v.State == VehicleState.Equipped);
    }
}
