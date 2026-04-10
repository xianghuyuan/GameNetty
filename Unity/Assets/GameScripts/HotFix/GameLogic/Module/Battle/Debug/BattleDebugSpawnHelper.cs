using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 战斗调试生成辅助类
    /// 在任意战斗中生成纯客户端怪物，属性由面板手动指定，不依赖配置表
    /// </summary>
    public static class BattleDebugSpawnHelper
    {
        /// <summary>
        /// 在战斗中生成怪物（纯客户端，属性手动指定）
        /// </summary>
        public static void SpawnMonster(Battle battle, int hp, int atk, int def, float speed, int count, float offsetFromCamera, float spreadRange)
        {
            if (battle == null || battle.State != BattleState.Fighting) return;

            // 基于相机右边界偏移生成，怪物始终出现在屏幕右侧外
            float cameraRight = BattleCameraHelper.GetCameraRightBound();
            float centerX = cameraRight + offsetFromCamera;

            for (int i = 0; i < count; i++)
            {
                long unitId = IdGenerater.Instance.GenerateInstanceId();
                float offsetX = (UnityEngine.Random.Range(0f, 1f) * 2f - 1f) * spreadRange;
                float posX = centerX + offsetX;

                BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, 0);
                unit.Camp = UnitCamp.Enemy;
                unit.Position = new float3(posX, 0, 0);
                unit.Forward = new float3(-1f, 0, 0);
                unit.FaceDirection = -1f;

                NumericComponent numeric = unit.AddComponent<NumericComponent>();
                numeric.Set(NumericType.Hp, hp);
                numeric.Set(NumericType.MaxHp, hp);
                numeric.Set(NumericType.Attack, atk);
                numeric.Set(NumericType.Defense, def);
                numeric.Set(NumericType.Speed, speed);

                unit.AddComponent<BattleUnitCombatComponent, float>(1.5f);
                unit.AddComponent<ClientMinionAIComponent>();

                BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, float3>(unit.Camp, unit.Position);
                view.InitViewAsync().Forget();

                BattleUIHelper.CreateUnitUI(unit);
            }

            if (battle.GetComponent<ClientMinionAITickComponent>() == null)
            {
                battle.AddComponent<ClientMinionAITickComponent>();
            }

            Log.Info($"BattleDebugSpawn: {count}x monsters (HP:{hp} ATK:{atk} DEF:{def} SPD:{speed})");
        }
    }
}
