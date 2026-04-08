using Cysharp.Threading.Tasks;
using Unity.Mathematics;

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

            foreach (var unitInfo in message.units)
            {
                BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitInfo.unitId, unitInfo.configId);
                unit.Camp = (UnitCamp)unitInfo.camp;
                unit.Position = unitInfo.position;
                unit.FaceDirection = 1f; // 玩家默认面朝右

                NumericComponent numeric = unit.AddComponent<NumericComponent>();
                numeric.Set(NumericType.Hp, unitInfo.hp);
                numeric.Set(NumericType.MaxHp, unitInfo.maxHp);
                numeric.Set(NumericType.Attack, unitInfo.attack);
                numeric.Set(NumericType.Defense, unitInfo.defense);
                if (unitInfo.speed > 0)
                {
                    numeric.Set(NumericType.Speed, unitInfo.speed);
                }

                unit.AddComponent<BattleUnitCombatComponent, float>(unitInfo.attackRange);

                if (unit.Camp == UnitCamp.Friend)
                {
                    BattleUnitCombatComponent combat = unit.GetComponent<BattleUnitCombatComponent>();
                    if (combat != null)
                    {
                        combat.AutoSkillIds = new[] { 11001 };
                    }

                    unit.AddComponent<ClientPlayerAIComponent>();
                }

                BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
                view.InitViewAsync().Forget();

                BattleUIHelper.CreateUnitUI(unit);
            }

            await ETTask.CompletedTask;
        }
    }
}
