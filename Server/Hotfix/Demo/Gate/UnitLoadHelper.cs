namespace ET.Server
{
    /// <summary>
    /// 进入Game场景
    /// </summary>
    public static class UnitLoadHelper
    {
        public static async ETTask<(bool, Unit)> LoadUnit(Player player)
        {
            GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
            gateMapComponent.Scene = await GateMapFactory.Create(gateMapComponent, player.Id, IdGenerater.Instance.GenerateInstanceId(), "GateMap");

            Unit unit = await UnitCacheHelper.GetUnitCache(player.Root(),gateMapComponent.Scene,player.UnitId);

            bool isNewUnit = unit == null;
            if (isNewUnit)
            {
                unit = await UnitFactory.Create(gateMapComponent.Scene, player.UnitId, UnitType.Player);
                unit.AddComponent<UnitDBSaveComponent>();
                UnitCacheHelper.AddOrUpdateUnitAllCache(unit);
            }
            else
            {
                if (unit.GetComponent<UnitDBSaveComponent>() == null)
                {
                    unit.AddComponent<UnitDBSaveComponent>();
                }

                // Transfer前，将缓存的IUnitCache组件从Bytes反序列化挂载到Unit
                // 否则Transfer序列化时组件不在unit.Components中，会导致Map场景拿不到
                UnitDBSaveComponent saveComp = unit.GetComponent<UnitDBSaveComponent>();
                if (saveComp != null)
                {
                    foreach (var kv in saveComp.Bytes)
                    {
                        if (!unit.Components.ContainsKey(kv.Key.TypeHandle.Value.ToInt64()))
                        {
                            Entity t = MongoHelper.Deserialize(kv.Key, kv.Value) as Entity;
                            unit.AddComponent(t);
                        }
                    }
                }
            }
            return (isNewUnit, unit);
        }
    }
}