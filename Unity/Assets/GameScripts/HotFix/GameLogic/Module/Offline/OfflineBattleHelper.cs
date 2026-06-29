namespace ET
{
    /// <summary>
    /// 离线战斗辅助类 - 入口 API
    /// 不连接服务器，完全在客户端本地运行战斗。
    /// 空战斗模式：不启动波次管理，通过编辑器测试工具手动添加怪物。
    /// </summary>
    public static class OfflineBattleHelper
    {
        /// <summary>
        /// 启动离线战斗（空战斗模式，通过编辑器测试工具手动添加怪物）
        /// </summary>
        /// <param name="scene">Main scene</param>
        /// <param name="playerUnitId">主世界 Unit ID（0 = 自动查找或创建默认）</param>
        public static async ETTask<Battle> StartOfflineBattle(Scene scene, long playerUnitId = 0)
        {
            if (playerUnitId <= 0)
            {
                PlayerComponent playerComponent = scene.Root().GetComponent<PlayerComponent>();
                if (playerComponent != null)
                {
                    playerUnitId = playerComponent.MyId;
                }
            }

            BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("OfflineBattle: BattleComponent not found");
                return null;
            }

            long battleId = IdGenerater.Instance.GenerateInstanceId();
            Battle battle = battleComponent.CreateBattleWithoutStart(battleId, (int)BattleType.WaveBattle);

            OfflineBattleComponent offlineComp = battle.AddComponent<OfflineBattleComponent>();
            offlineComp.PlayerUnitId = playerUnitId;

            await offlineComp.CreatePlayerBattleUnitAsync();

            battle.Start();

            Log.Info($"OfflineBattle started: BattleId={battleId}");
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
