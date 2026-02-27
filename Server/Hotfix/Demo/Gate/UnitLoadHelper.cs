namespace ET.Server
{
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
            }
            return (isNewUnit, unit);
        }
    }
}