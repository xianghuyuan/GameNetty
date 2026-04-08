using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 位置校准（服务端驱动）
    /// 直接更新逻辑位置和视觉位置
    /// </summary>
    [MessageHandler(SceneType.Main)]
    public class M2C_BattleUnitPositionSyncHandler : MessageHandler<Scene, M2C_BattleUnitPositionSync>
    {
        protected override async ETTask Run(Scene root, M2C_BattleUnitPositionSync message)
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

            unit.Position = message.position;
            unit.Forward = float3.zero;
            unit.GetComponent<BattleUnitView>()?.SetPosition(unit.Position);

            await ETTask.CompletedTask;
        }
    }
}
