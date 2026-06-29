using System;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleUnitRegistryComponent))]
    [FriendOf(typeof(BattleUnitRegistryComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleUnitRegistryComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitRegistryComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BattleUnitRegistryComponent self)
        {
            self.Units.Clear();
        }

        public static void Register(this BattleUnitRegistryComponent self, BattleUnit unit)
        {
            if (unit != null && !unit.IsDisposed)
            {
                self.Units[unit.Id] = unit;
            }
        }

        public static void Unregister(this BattleUnitRegistryComponent self, long unitId)
        {
            self.Units.Remove(unitId);
        }

        public static BattleUnit GetUnit(this BattleUnitRegistryComponent self, long unitId)
        {
            return self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef) ? unitRef : null;
        }

        public static void ForEachUnit(this BattleUnitRegistryComponent self, Action<BattleUnit> action)
        {
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead)
                {
                    action(unit);
                }
            }
        }
    }
}
