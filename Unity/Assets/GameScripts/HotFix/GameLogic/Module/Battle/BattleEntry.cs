using Cysharp.Threading.Tasks;
using GameLogic;
using Unity.Mathematics;

namespace ET
{
    public enum BattleStartMode
    {
        Offline = 0,
        Online = 1,
        Debug = 2,
    }

    /// <summary>
    /// 战斗启动请求。Map/UI/Editor 只构造请求，不直接关心具体战斗运行域。
    /// </summary>
    public sealed class BattleStartRequest
    {
        public BattleStartMode Mode = BattleStartMode.Offline;
        public long PlayerUnitId;
        public int StageId;
        public int BattleType = (int)ET.BattleType.WaveBattle;
        public bool AutoReady;
    }

    public sealed class BattleStartResult
    {
        public bool IsSuccess;
        public BattleStartMode Mode;
        public long BattleId;
        public Battle Battle;
        public string ErrorMessage;

        public static BattleStartResult Success(BattleStartMode mode, Battle battle, long battleId = 0)
        {
            long resolvedBattleId = battle != null ? battle.BattleId : battleId;
            return new BattleStartResult
            {
                IsSuccess = resolvedBattleId > 0,
                Mode = mode,
                Battle = battle,
                BattleId = resolvedBattleId,
            };
        }

        public static BattleStartResult Fail(BattleStartMode mode, string errorMessage)
        {
            return new BattleStartResult
            {
                IsSuccess = false,
                Mode = mode,
                ErrorMessage = errorMessage,
            };
        }
    }

    /// <summary>
    /// 统一战斗入口。现阶段只做门面封装，底层仍复用 OfflineBattleHelper / BattleHelper。
    /// </summary>
    public static class BattleEntry
    {
        public static async ETTask<BattleStartResult> StartBattle(Scene scene, BattleStartRequest request)
        {
            if (scene == null)
            {
                return BattleStartResult.Fail(request?.Mode ?? BattleStartMode.Offline, "Scene is null");
            }

            if (request == null)
            {
                request = new BattleStartRequest();
            }

            switch (request.Mode)
            {
                case BattleStartMode.Online:
                    return await StartOnlineBattle(scene, request);
                case BattleStartMode.Debug:
                case BattleStartMode.Offline:
                    return await StartOfflineBattle(scene, request);
                default:
                    return BattleStartResult.Fail(request.Mode, $"Unsupported battle mode: {request.Mode}");
            }
        }

        public static async ETTask<bool> ExitCurrentBattle(Scene scene)
        {
            BattleComponent battleComponent = scene?.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            if (battle == null)
            {
                return false;
            }

            if (battle.GetComponent<OfflineBattleComponent>() != null)
            {
                OfflineBattleHelper.ExitOfflineBattle(scene);
                BattleUIHelper.ClearAll();
                return true;
            }

            bool success = await BattleHelper.ExitBattle(scene, battle.BattleId);
            if (!success)
            {
                return false;
            }

            battle.End(false);
            battleComponent.RemoveBattle(battle.BattleId);
            BattleUIHelper.ClearAll();
            return true;
        }

        private static async ETTask<BattleStartResult> StartOfflineBattle(Scene scene, BattleStartRequest request)
        {
            await EnsureOfflineMapScene(scene, request);

            Battle battle = await OfflineBattleHelper.StartOfflineBattle(scene, request.PlayerUnitId);
            if (battle == null)
            {
                return BattleStartResult.Fail(request.Mode, "Offline battle create failed");
            }

            return BattleStartResult.Success(request.Mode, battle);
        }

        private static async ETTask<BattleStartResult> StartOnlineBattle(Scene scene, BattleStartRequest request)
        {
            long battleId = await BattleHelper.StartBattle(scene, request.StageId, request.BattleType);
            if (battleId <= 0)
            {
                return BattleStartResult.Fail(request.Mode, "Online battle start failed");
            }

            if (request.AutoReady)
            {
                BattleHelper.BattleReady(scene).Coroutine();
            }

            BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetBattle(battleId);
            return BattleStartResult.Success(request.Mode, battle, battleId);
        }

        private static async ETTask EnsureOfflineMapScene(Scene root, BattleStartRequest request)
        {
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            if (currentScenesComponent == null)
            {
                Log.Error("BattleEntry: CurrentScenesComponent not found");
                return;
            }

            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            long playerUnitId = request.PlayerUnitId;
            if (playerUnitId <= 0 && playerComponent != null)
            {
                playerUnitId = playerComponent.MyId;
            }

            if (playerUnitId <= 0)
            {
                playerUnitId = IdGenerater.Instance.GenerateInstanceId();
            }

            Scene currentScene = currentScenesComponent.Scene;
            if (currentScene == null || currentScene.Name != "Map")
            {
                currentScene?.Dispose();
                currentScene = CurrentSceneFactory.Create(IdGenerater.Instance.GenerateInstanceId(), "Map", currentScenesComponent);
                currentScene.AddComponent<UnitComponent>();
                await GameModule.Scene.LoadSceneAsync("Map");
                EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());
                root.GetComponent<ObjectWait>()?.Notify(new Wait_SceneChangeFinish());
            }

            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                unitComponent = currentScene.AddComponent<UnitComponent>();
            }

            if (unitComponent.Get(playerUnitId) == null)
            {
                Unit unit = UnitFactory.Create(currentScene, CreateOfflinePlayerUnitInfo(playerUnitId));
                unitComponent.Add(unit);
            }

            if (playerComponent != null)
            {
                playerComponent.MyId = playerUnitId;
            }

            request.PlayerUnitId = playerUnitId;
            EventSystem.Instance.Publish(root, new EnterMapFinish());
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
        }

        private static UnitInfo CreateOfflinePlayerUnitInfo(long playerUnitId)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = playerUnitId;
            unitInfo.ConfigId = 1001;
            unitInfo.Type = (int)UnitType.Player;
            unitInfo.Position = new float3(-5f, BattleAreaConfig.BattleUnitSpawnY, 0f);
            unitInfo.Forward = float3.zero;
            unitInfo.KV[NumericType.Hp] = 1000;
            unitInfo.KV[NumericType.MaxHp] = 1000;
            unitInfo.KV[NumericType.Speed] = 3 * 10000;

            UnitCombatConfig combatConfig = ConfigHelper.UnitCombatConfig?.GetOrDefault(unitInfo.ConfigId);
            if (combatConfig != null)
            {
                unitInfo.KV[NumericType.Speed] = (long)(combatConfig.MoveSpeed * 10000);
            }

            return unitInfo;
        }
    }
}
