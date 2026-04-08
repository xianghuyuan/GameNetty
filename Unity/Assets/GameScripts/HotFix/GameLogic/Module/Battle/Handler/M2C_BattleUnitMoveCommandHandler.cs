using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 移动指令（服务端权威单位：Boss 等）
    /// 直接更新逻辑位置和视觉位置
    /// </summary>
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
                await ETTask.CompletedTask;
                return;
            }

            if (message.isMoving)
            {
                float oldX = unit.Position.x;
                unit.Position = message.targetPosition;

                float faceDir = message.targetPosition.x >= oldX ? 1f : -1f;
                unit.FaceDirection = faceDir;
                unit.Forward = new float3(faceDir, 0, 0);

                unit.GetComponent<BattleUnitView>()?.SetPosition(unit.Position);
            }
            else
            {
                unit.Position = message.targetPosition;
                unit.Forward = float3.zero;

                unit.GetComponent<BattleUnitView>()?.SetPosition(unit.Position);
            }

            await ETTask.CompletedTask;
        }
    }
}
