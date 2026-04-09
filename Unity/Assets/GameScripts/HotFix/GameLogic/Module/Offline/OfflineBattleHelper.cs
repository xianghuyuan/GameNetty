namespace ET
{
    /// <summary>
    /// 离线战斗辅助类 - 入口 API
    /// 不连接服务器，完全在客户端本地运行战斗
    /// </summary>
    public static class OfflineBattleHelper
    {
#if UNITY_EDITOR
        private static GameLogic.BattleDebugPanel _debugPanel;
#endif

        /// <summary>
        /// 确保调试面板存在（仅 Editor 下自动挂载）
        /// </summary>
        private static void EnsureDebugPanel()
        {
#if UNITY_EDITOR
            if (_debugPanel != null) return;
            var go = new UnityEngine.GameObject("~BattleDebugPanel");
            _debugPanel = go.AddComponent<GameLogic.BattleDebugPanel>();
#endif
        }
        /// <summary>
        /// 启动离线战斗（空战斗模式，不启动波次管理，通过调试面板手动添加怪物）
        /// </summary>
        /// <param name="scene">Main scene</param>
        /// <param name="playerUnitId">主世界 Unit ID（0 = 自动查找或创建默认）</param>
        public static async ETTask<Battle> StartOfflineBattle(Scene scene, long playerUnitId = 0)
        {
            // 1. 查找玩家 Unit ID
            if (playerUnitId <= 0)
            {
                PlayerComponent playerComponent = scene.Root().GetComponent<PlayerComponent>();
                if (playerComponent != null)
                {
                    playerUnitId = playerComponent.MyId;
                }
            }

            // 2. 创建 Battle 实体（会自动调用 Battle.Start() 启动 AI tick）
            BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("OfflineBattle: BattleComponent not found");
                return null;
            }

            long battleId = IdGenerater.Instance.GenerateInstanceId();
            Battle battle = battleComponent.CreateBattle(battleId, (int)BattleType.WaveBattle);

            // 3. 添加离线战斗组件
            OfflineBattleComponent offlineComp = battle.AddComponent<OfflineBattleComponent>();
            offlineComp.PlayerUnitId = playerUnitId;

            // 4. 创建玩家单位
            offlineComp.CreatePlayerBattleUnit();

            // 5. 挂载调试面板（仅 Editor）
            EnsureDebugPanel();

            // 6. 等一帧让视图初始化
            await scene.Root().GetComponent<TimerComponent>().WaitFrameAsync();

            Log.Info($"OfflineBattle started (empty mode): BattleId={battleId}");
            return battle;
        }

        /// <summary>
        /// 退出离线战斗
        /// </summary>
        public static void ExitOfflineBattle(Scene scene)
        {
            BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
            if (battleComponent == null) return;

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null) return;

            if (battle.GetComponent<OfflineBattleComponent>() == null)
            {
                Log.Warning("ExitOfflineBattle: Current battle is not offline, use BattleHelper.ExitBattle");
                return;
            }

            battle.End(false);
            battleComponent.RemoveBattle(battle.BattleId);
        }
    }
}
