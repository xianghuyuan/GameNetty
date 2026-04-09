using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_ReconnectBattleHandler : MessageHandler<Scene, M2C_ReconnectBattle>
    {
        protected override async ETTask Run(Scene root, M2C_ReconnectBattle message)
        {
            Log.Info($"收到战斗恢复数据: BattleId={message.battleId}, State={message.state}, Wave={message.currentWave}/{message.totalWaves}, Units={message.units.Count}");

            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_ReconnectBattle: BattleComponent not found");
                return;
            }

            // 如果已有战斗实体，先清理
            if (battleComponent.GetCurrentBattle() != null)
            {
                battleComponent.RemoveBattle(battleComponent.GetCurrentBattle().BattleId);
            }

            // 创建 Battle 实体但不启动（先创建单位，再Start确保BattleStart_UI的补刷循环能找到单位）
            Battle battle = battleComponent.CreateBattleWithoutStart(message.battleId, message.battleType);
            battle.TotalWaves = message.totalWaves;
            battle.CurrentWave = message.completedWaveNumbers.Count > 0 ? message.completedWaveNumbers.Count : 0;

            // 先创建所有存活单位（在Start之前，这样BattleStart_UI打开HUD后的补刷循环能找到它们）
            foreach (var unitInfo in message.units)
            {
                CreateBattleUnitFromInfo(battle, unitInfo);
            }

            // 启动战斗：发布BattleStart → BattleStart_UI打开HUD → 补刷已存在的单位UI → SetBattleActive(true)
            battle.Start();

            // 后续M2C_SpawnWave会由M2C_SpawnWaveHandler处理，自动创建杂兵+ClientMinionAI

            Log.Info($"战斗恢复完成: BattleId={message.battleId}, 单位数={message.units.Count}");

            await ETTask.CompletedTask;
        }

        private static void CreateBattleUnitFromInfo(Battle battle, BattleUnitInfo unitInfo)
        {
            BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitInfo.unitId, unitInfo.configId);
            unit.Camp = (UnitCamp)unitInfo.camp;
            unit.Position = unitInfo.position;
            unit.FaceDirection = 1f;
            unit.IsBoss = unitInfo.isBoss;
            unit.OwnerId = unitInfo.ownerId;

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
        }
    }
}
