using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(OfflineBattleComponent))]
    [FriendOf(typeof(OfflineBattleComponent))]
    [FriendOf(typeof(Battle))]
    [FriendOf(typeof(OfflineWaveManagerComponent))]
    public static partial class OfflineBattleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OfflineBattleComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this OfflineBattleComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this OfflineBattleComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null)
            {
                return;
            }

            if (battle.State == BattleState.Ended)
            {
                // 战斗结束后延迟清理
                BattleComponent battleComponent = battle.Root().GetComponent<BattleComponent>();
                battleComponent?.RemoveBattle(battle.BattleId);
                return;
            }

            if (battle.State != BattleState.Fighting)
            {
                return;
            }

            OfflineWaveManagerComponent waveManager = battle.GetComponent<OfflineWaveManagerComponent>();
            if (waveManager != null)
            {
                waveManager.CheckWaveState();
                waveManager.CheckPlayerAlive();
            }
        }

        /// <summary>
        /// 创建玩家 BattleUnit，复用 M2C_CreateBattleUnitsHandler 中友方单位的创建逻辑
        /// </summary>
        public static void CreatePlayerBattleUnit(this OfflineBattleComponent self)
        {
            Battle battle = self.GetParent<Battle>();

            long unitId = self.PlayerUnitId > 0
                ? self.PlayerUnitId
                : IdGenerater.Instance.GenerateInstanceId();
            int configId = 1001; // 默认英雄配置 ID

            // 尝试从主世界 Unit 获取属性
            UnitComponent unitComponent = battle.Root().GetComponent<UnitComponent>();
            Unit playerUnit = unitComponent?.Get(self.PlayerUnitId);

            if (playerUnit != null)
            {
                unitId = playerUnit.Id;
                configId = playerUnit.ConfigId;
            }

            BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, configId);
            unit.Camp = UnitCamp.Friend;
            unit.OwnerId = self.PlayerUnitId;
            unit.Position = new float3(-5f, 0, 0);

            NumericComponent numeric = unit.AddComponent<NumericComponent>();

            if (playerUnit != null)
            {
                // 从主世界 Unit 拷贝属性
                NumericComponent unitNumeric = playerUnit.GetComponent<NumericComponent>();
                if (unitNumeric != null)
                {
                    foreach (var kv in unitNumeric.NumericDic)
                    {
                        numeric[kv.Key] = kv.Value;
                    }
                }
            }
            else
            {
                // 默认属性
                numeric.Set(NumericType.Hp, 1000);
                numeric.Set(NumericType.MaxHp, 1000);
                numeric.Set(NumericType.Attack, 50);
                numeric.Set(NumericType.Defense, 10);
                numeric.Set(NumericType.Speed, 3f);

                // 尝试从 UnitCombatConfig 覆盖速度
                UnitCombatConfig combatConfig = ConfigHelper.UnitCombatConfig?.GetOrDefault(configId);
                if (combatConfig != null)
                {
                    numeric.Set(NumericType.Speed, combatConfig.MoveSpeed);
                }
            }

            unit.AddComponent<BattleUnitCombatComponent, float>(3f);

            BattleUnitCombatComponent combatComp = unit.GetComponent<BattleUnitCombatComponent>();
            if (combatComp != null)
            {
                combatComp.AutoSkillIds = new[] { 11001 };
            }

            unit.AddComponent<ClientPlayerAIComponent>();

            BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
            view.InitViewAsync().Forget();

            BattleUIHelper.CreateUnitUI(unit);
        }
    }
}
