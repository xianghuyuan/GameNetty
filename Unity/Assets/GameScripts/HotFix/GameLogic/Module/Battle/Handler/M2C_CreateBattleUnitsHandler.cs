namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_CreateBattleUnitsHandler : MessageHandler<Scene, M2C_CreateBattleUnits>
    {
        protected override async ETTask Run(Scene root, M2C_CreateBattleUnits message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_CreateBattleUnits: BattleComponent not found");
                return;
            }

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null)
            {
                Log.Error($"M2C_CreateBattleUnits: 当前没有进行中的战斗");
                return;
            }

            Log.Info($"收到怪物创建消息: BattleId={message.battleId}, Count={message.units.Count}");

            foreach (var unitInfo in message.units)
            {
                // 使用 AddChildWithId 创建战斗单位，ET 框架会自动管理子实体
                BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitInfo.unitId, unitInfo.configId);
                unit.Camp = (UnitCamp)unitInfo.camp;
                unit.Position = unitInfo.position;

                Log.Debug($"创建战斗单位: UnitId={unit.Id}, ConfigId={unit.ConfigId}, Camp={unit.Camp}, Position={unitInfo.position}");

                // 发布战斗单位创建事件，供表现层订阅
                EventSystem.Instance.Publish(root, new BattleUnitCreated
                {
                    Battle = battle,
                    Unit = unit
                });
            }

            await ETTask.CompletedTask;
        }
    }
}
