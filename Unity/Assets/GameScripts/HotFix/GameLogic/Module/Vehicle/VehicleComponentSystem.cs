using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(VehicleComponent))]
    [FriendOf(typeof(VehicleComponent))]
    public static partial class VehicleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this VehicleComponent self)
        {
            self.EquippedVehicleId = 0;
            self.EquippedVehicle = null;
            self.OwnedVehicles.Clear();
            self.OwnedShards.Clear();
        }

        [EntitySystem]
        private static void Destroy(this VehicleComponent self)
        {
            self.EquippedVehicleId = 0;
            self.EquippedVehicle = null;
            self.OwnedVehicles.Clear();
            self.OwnedShards.Clear();
        }

        public static void EquipVehicle(this VehicleComponent self, long vehicleId)
        {
            VehicleData data = self.OwnedVehicles.Find(v => v.VehicleId == vehicleId);
            if (data == null) return;

            data.State = VehicleState.Equipped;
            self.EquippedVehicleId = vehicleId;
            self.EquippedVehicle = data;
        }

        public static void UnequipVehicle(this VehicleComponent self)
        {
            foreach (VehicleData vehicle in self.OwnedVehicles)
            {
                vehicle.State = VehicleState.Stored;
            }

            self.EquippedVehicleId = 0;
            self.EquippedVehicle = null;
        }

        public static VehicleData AddNewVehicle(this VehicleComponent self, int vehicleConfigId)
        {
            long vehicleId = IdGenerater.Instance.GenerateInstanceId();
            VehicleData data = new VehicleData
            {
                VehicleId = vehicleId,
                VehicleConfigId = vehicleConfigId,
                Level = 1,
                State = VehicleState.Stored,
            };

            // 根据 VehicleConfig 初始化槽位（用0表示空）
            self.OwnedVehicles.Add(data);
            return data;
        }

        public static VehicleData AddOrUpgradeVehicle(this VehicleComponent self, int vehicleConfigId, int maxLevel, out bool upgraded)
        {
            upgraded = false;
            if (vehicleConfigId > 0)
            {
                VehicleData existing = self.OwnedVehicles.Find(v => v != null && v.VehicleConfigId == vehicleConfigId);
                if (existing != null)
                {
                    int currentLevel = System.Math.Max(1, existing.Level);
                    int levelCap = maxLevel > 0 ? maxLevel : int.MaxValue;
                    existing.Level = System.Math.Min(levelCap, currentLevel + 1);
                    upgraded = true;
                    return existing;
                }
            }

            return self.AddNewVehicle(vehicleConfigId);
        }

        public static List<VehicleData> GetEquippedVehicles(this VehicleComponent self)
        {
            List<VehicleData> equipped = new();
            foreach (VehicleData vehicle in self.OwnedVehicles)
            {
                if (vehicle != null && vehicle.State == VehicleState.Equipped)
                {
                    equipped.Add(vehicle);
                }
            }

            if (equipped.Count == 0 && self.EquippedVehicle != null)
            {
                equipped.Add(self.EquippedVehicle);
            }

            return equipped;
        }

        public static void SlotBuff(this VehicleComponent self, int slotIndex, int buffGroupId)
        {
            if (self.EquippedVehicle == null || slotIndex < 0) return;

            List<int> slots = self.EquippedVehicle.SlottedEffectPackIds;
            if (slots == null) return;
            while (slots.Count <= slotIndex)
            {
                slots.Add(0);
            }

            // 如果槽位已有碎片，先归还
            if (slots[slotIndex] != 0)
            {
                self.AddShard(slots[slotIndex], 1);
            }

            // 扣除碎片
            if (buffGroupId != 0)
            {
                self.RemoveShard(buffGroupId, 1);
            }

            slots[slotIndex] = buffGroupId;
        }

        public static void UnslotBuff(this VehicleComponent self, int slotIndex)
        {
            if (self.EquippedVehicle == null || slotIndex < 0) return;

            List<int> slots = self.EquippedVehicle.SlottedEffectPackIds;
            if (slots == null) return;
            if (slotIndex >= slots.Count) return;

            if (slots[slotIndex] != 0)
            {
                self.AddShard(slots[slotIndex], 1);
                slots[slotIndex] = 0;
            }
        }

        public static void AddShard(this VehicleComponent self, int buffGroupId, int count)
        {
            if (!self.OwnedShards.ContainsKey(buffGroupId))
            {
                self.OwnedShards[buffGroupId] = 0;
            }
            self.OwnedShards[buffGroupId] += count;
        }

        public static bool RemoveShard(this VehicleComponent self, int buffGroupId, int count)
        {
            if (!self.OwnedShards.TryGetValue(buffGroupId, out int current) || current < count)
            {
                return false;
            }
            self.OwnedShards[buffGroupId] -= count;
            if (self.OwnedShards[buffGroupId] <= 0)
            {
                self.OwnedShards.Remove(buffGroupId);
            }
            return true;
        }
    }
}
