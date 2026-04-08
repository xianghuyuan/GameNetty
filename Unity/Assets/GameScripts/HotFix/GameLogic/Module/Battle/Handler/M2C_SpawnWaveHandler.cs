using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_SpawnWaveHandler : MessageHandler<Scene, M2C_SpawnWave>
    {
        protected override async ETTask Run(Scene root, M2C_SpawnWave message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("M2C_SpawnWave: BattleComponent not found");
                return;
            }

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null)
            {
                Log.Error("M2C_SpawnWave: 当前没有进行中的战斗");
                return;
            }

            MonsterUnitConfig monsterConfig = ConfigHelper.MonsterUnitConfig.Get(message.monsterConfigId);
            if (monsterConfig == null)
            {
                Log.Error($"M2C_SpawnWave: 找不到怪物配置 monsterConfigId={message.monsterConfigId}");
                return;
            }

            for (int i = 0; i < message.count; i++)
            {
                long unitId = message.startUnitId + i;
                float offsetX = (UnityEngine.Random.Range(0f, 1f) * 2f - 1f) * message.spreadRange;
                float posX = message.centerX + offsetX;

                BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, message.monsterConfigId);
                unit.Camp = UnitCamp.Enemy;
                unit.Position = new float3(posX, 0, 0);
                unit.Forward = new float3(message.moveDirX, message.moveDirY, 0);
                unit.FaceDirection = message.moveDirX >= 0f ? 1f : -1f;

                NumericComponent numeric = unit.AddComponent<NumericComponent>();
                numeric.Set(NumericType.Hp, monsterConfig.MaxHp);
                numeric.Set(NumericType.MaxHp, monsterConfig.MaxHp);
                numeric.Set(NumericType.Attack, monsterConfig.Attack);
                numeric.Set(NumericType.Defense, monsterConfig.Defense);
                numeric.Set(NumericType.Speed, monsterConfig.Speed);

                unit.AddComponent<BattleUnitCombatComponent, float>(1.5f);

                // 从 UnitCombatConfig 读取技能射程作为真实攻击距离
                UnitCombatConfig combatConfig = ConfigHelper.UnitCombatConfig?.GetOrDefault(message.monsterConfigId);
                if (combatConfig != null)
                {
                    // 优先从 AutoSkillIds 推算最短射程
                    float shortestRange = float.MaxValue;
                    int[] skillIds = combatConfig.AutoSkillIds;
                    if (skillIds == null || skillIds.Length == 0)
                    {
                        skillIds = combatConfig.NormalAttackSkillId > 0 ? new[] { combatConfig.NormalAttackSkillId } : null;
                    }

                    if (skillIds != null)
                    {
                        foreach (int skillId in skillIds)
                        {
                            SkillConfig sc = ConfigHelper.SkillConfig?.GetOrDefault(skillId);
                            if (sc == null) continue;
                            SkillTargetingConfig tc = sc.TargetingConfigId_Ref;
                            if (tc == null) continue;
                            float range = tc.CastRange + tc.EdgeDistance;
                            if (range < shortestRange) shortestRange = range;
                        }
                    }

                    if (shortestRange < float.MaxValue)
                    {
                        var cc = unit.GetComponent<BattleUnitCombatComponent>();
                        if (cc != null) cc.AttackRange = shortestRange;
                    }
                }
                unit.AddComponent<ClientMinionAIComponent>();

                BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
                view.InitViewAsync().Forget();

                BattleUIHelper.CreateUnitUI(unit);
            }

            // 确保 Battle 上挂载 AI Tick 组件
            if (battle.GetComponent<ClientMinionAITickComponent>() == null)
            {
                battle.AddComponent<ClientMinionAITickComponent>();
            }

            Log.Info($"M2C_SpawnWave: 创建 {message.count} 个杂兵, configId={message.monsterConfigId}, waveId={message.waveId}");
            await ETTask.CompletedTask;
        }
    }
}
