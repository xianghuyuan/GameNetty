namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_BattleUnitMoveCommandHandler : MessageHandler<Scene, M2C_BattleUnitMoveCommand>
    {
        protected override async ETTask Run(Scene root, M2C_BattleUnitMoveCommand message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            if (battle == null || battle.BattleId != message.battleId)
            {
                await ETTask.CompletedTask;
                return;
            }

            BattleUnit unit = battle.GetChild<BattleUnit>(message.unitId);
            if (unit == null)
            {
                Log.Warning($"M2C_BattleUnitMoveCommand: 未找到战斗单位, BattleId={message.battleId}, UnitId={message.unitId}");
                await ETTask.CompletedTask;
                return;
            }

            BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
            if (moveComponent == null)
            {
                moveComponent = unit.AddComponent<BattleMoveComponent>();
            }

            if (message.isMoving)
            {
                moveComponent.ApplyMoveCommand(root, message.targetPosition, message.moveSpeed);
            }
            else
            {
                moveComponent.StopMove(message.targetPosition);
            }

            await ETTask.CompletedTask;
        }
    }
}
