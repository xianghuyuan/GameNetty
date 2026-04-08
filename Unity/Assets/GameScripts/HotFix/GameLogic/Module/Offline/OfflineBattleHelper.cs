namespace ET
{
    /// <summary>
    /// 离线战斗辅助类 - 入口 API
    /// 不连接服务器，完全在客户端本地运行战斗
    /// </summary>
    public static class OfflineBattleHelper
    {
        /// <summary>
        /// 启动离线波次战斗
        /// </summary>
        /// <param name="scene">Main scene</param>
        /// <param name="stageId">关卡配置 ID（0 = 使用第一个关卡）</param>
        /// <param name="playerUnitId">主世界 Unit ID（0 = 自动查找或创建默认）</param>
        public static async ETTask<Battle> StartOfflineBattle(Scene scene, int stageId = 0, long playerUnitId = 0)
        {
            // 1. 确定关卡配置
            if (stageId <= 0)
            {
                var stageConfigs = ConfigHelper.StageConfig?.DataList;
                if (stageConfigs != null && stageConfigs.Count > 0)
                {
                    stageId = stageConfigs[0].Id;
                }
                else
                {
                    Log.Error("OfflineBattle: No stage configs available");
                    return null;
                }
            }

            StageConfig stageConfig = ConfigHelper.StageConfig?.GetOrDefault(stageId);
            if (stageConfig == null)
            {
                Log.Error($"OfflineBattle: StageConfig not found: id={stageId}");
                return null;
            }

            // 2. 查找玩家 Unit ID
            if (playerUnitId <= 0)
            {
                PlayerComponent playerComponent = scene.Root().GetComponent<PlayerComponent>();
                if (playerComponent != null)
                {
                    playerUnitId = playerComponent.MyId;
                }
            }

            // 3. 创建 Battle 实体（会自动调用 Battle.Start() 启动 AI tick）
            BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("OfflineBattle: BattleComponent not found");
                return null;
            }

            long battleId = IdGenerater.Instance.GenerateInstanceId();
            Battle battle = battleComponent.CreateBattle(battleId, (int)BattleType.WaveBattle);

            // 4. 添加离线战斗组件
            OfflineBattleComponent offlineComp = battle.AddComponent<OfflineBattleComponent>();
            offlineComp.StageConfigId = stageId;
            offlineComp.PlayerUnitId = playerUnitId;

            // 5. 创建玩家单位
            offlineComp.CreatePlayerBattleUnit();

            // 6. 等一帧让视图初始化
            await scene.Root().GetComponent<TimerComponent>().WaitFrameAsync();

            // 7. 启动波次管理
            OfflineWaveManagerComponent waveManager = battle.AddComponent<OfflineWaveManagerComponent, int>(stageId);
            await waveManager.StartFirstWave();

            Log.Info($"OfflineBattle started: BattleId={battleId}, StageId={stageId}, Waves={stageConfig.TotalWaves}");
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
