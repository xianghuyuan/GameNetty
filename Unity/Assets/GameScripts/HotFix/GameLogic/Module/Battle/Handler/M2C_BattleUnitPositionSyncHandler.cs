namespace ET
{
    /// <summary>
    /// 位置校准
    /// </summary>
    [MessageHandler(SceneType.Main)]
    public class M2C_BattleUnitPositionSyncHandler : MessageHandler<Scene, M2C_BattleUnitPositionSync>
    {
        protected override async ETTask Run(Scene root, M2C_BattleUnitPositionSync message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            if (battle == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (battle.BattleId != message.battleId)
            {
                await ETTask.CompletedTask;
                return;
            }

            BattleUnit unit = battle.GetChild<BattleUnit>(message.unitId);
            if (unit == null)
            {
                Log.Warning($"M2C_BattleUnitPositionSync: 未找到战斗单位, BattleId={message.battleId}, UnitId={message.unitId}");
                await ETTask.CompletedTask;
                return;
            }

            unit.Position = message.position;

            BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
            moveComponent?.StopMove(message.position);

            BattleUnitViewComponent viewComponent = battle.GetComponent<BattleUnitViewComponent>();
            viewComponent?.UpdateViewPosition(unit.Id, message.position);

            await ETTask.CompletedTask;
        }
    }
}
