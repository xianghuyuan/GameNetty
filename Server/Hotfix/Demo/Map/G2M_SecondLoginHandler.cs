namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class G2M_SecondLoginHandler : MessageLocationHandler<Unit, G2M_SecondLogin, M2G_SecondLogin>
    {
        protected override async ETTask Run(Unit unit, G2M_SecondLogin request, M2G_SecondLogin response)
        {
            Scene scene = unit.Root();

            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneInstanceId = scene.InstanceId;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.SendToClient(unit, m2CStartSceneChange);

            M2C_CreateMyUnit m2CCreateMyUnit = M2C_CreateMyUnit.Create();
            m2CCreateMyUnit.Unit = UnitHelper.CreateUnitInfo(unit);
            MapMessageHelper.SendToClient(unit, m2CCreateMyUnit);

            // 检查玩家是否仍在战斗中，如果是则推送战斗恢复数据
            BattleRoomManagerComponent roomManager = scene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager != null && roomManager.IsUnitInBattle(unit.Id))
            {
                BattleRoom battleRoom = roomManager.GetBattleRoomByUnitId(unit.Id);
                if (battleRoom != null && battleRoom.State != BattleState.End)
                {
                    SendReconnectBattleInfo(unit, battleRoom);
                }
                else
                {
                    // 战斗已结束但映射残留，清理
                    roomManager.RemoveUnitFromBattleRoom(unit.Id);
                }
            }

            await ETTask.CompletedTask;
        }

        private static void SendReconnectBattleInfo(Unit player, BattleRoom battleRoom)
        {
            M2C_ReconnectBattle reconnectMsg = M2C_ReconnectBattle.Create();
            reconnectMsg.battleId = battleRoom.Id;
            reconnectMsg.battleType = battleRoom.ConfigId;
            reconnectMsg.state = (int)battleRoom.State;
            reconnectMsg.currentWave = 0;
            reconnectMsg.totalWaves = 0;

            WaveManagerComponent waveManager = battleRoom.GetComponent<WaveManagerComponent>();
            if (waveManager != null)
            {
                reconnectMsg.currentWave = waveManager.CurrentWaveIndex;
                reconnectMsg.totalWaves = waveManager.TotalWaves;

                // 收集已完成的波次编号
                for (int i = 0; i < waveManager.CurrentWaveIndex; i++)
                {
                    reconnectMsg.completedWaveNumbers.Add(i + 1);
                }
            }

            // 收集所有存活的战斗单位（英雄+Boss）
            var allUnits = battleRoom.GetAllUnits();
            foreach (BattleUnit battleUnit in allUnits)
            {
                reconnectMsg.units.Add(BattleUnitHelper.CreateBattleUnitInfo(battleUnit));
            }

            MapMessageHelper.SendToClient(player, reconnectMsg);

            // 重新下发当前波次的杂兵 M2C_SpawnWave（仅发送消息，不创建新服务端实体）
            if (waveManager != null && waveManager.State == WaveState.Fighting && waveManager.CurrentWaveIndex >= 0)
            {
                ResendCurrentWaveMinionSpawns(player, battleRoom, waveManager);
            }

            Log.Info($"推送战斗恢复数据: PlayerId={player.Id}, BattleId={battleRoom.Id}, Units={allUnits.Count}");
        }

        /// <summary>
        /// 重新下发当前波次的杂兵刷怪消息给重连玩家
        /// 杂兵是客户端权威的，服务端只存储轻量验证实体，无法恢复精确位置
        /// 因此从配置表重新读取刷怪参数，让客户端重新刷怪
        /// </summary>
        private static void ResendCurrentWaveMinionSpawns(Unit player, BattleRoom battleRoom, WaveManagerComponent waveManager)
        {
            int waveConfigId = waveManager.WaveConfigIds[waveManager.CurrentWaveIndex];
            WaveConfig waveConfig = WaveConfigCategory.Instance.GetOrDefault(waveConfigId);
            if (waveConfig == null) return;

            foreach (var batch in waveConfig.Batches)
            {
                SpawnConfig spawnConfig = SpawnConfigCategory.Instance.GetOrDefault(batch.SpawnId);
                if (spawnConfig == null) continue;

                float centerX = spawnConfig.PositionX;
                float spreadRange = spawnConfig.SpreadRange;

                foreach (var monsterInfo in spawnConfig.Monsters)
                {
                    MonsterUnitConfig monsterConfig = MonsterUnitConfigCategory.Instance.GetOrDefault(monsterInfo.MonsterId);
                    bool isBoss = monsterConfig != null && monsterConfig.Type == 3;

                    // Boss 已经在 M2C_ReconnectBattle 的 units 列表中发送，跳过
                    if (isBoss) continue;

                    // 杂兵：重发 M2C_SpawnWave 让客户端本地创建
                    long startUnitId = IdGenerater.Instance.GenerateInstanceId();

                    var spawnWaveMsg = M2C_SpawnWave.Create();
                    spawnWaveMsg.battleId = battleRoom.Id;
                    spawnWaveMsg.waveId = waveManager.CurrentWaveIndex;
                    spawnWaveMsg.centerX = centerX;
                    spawnWaveMsg.centerY = 0f;
                    spawnWaveMsg.count = monsterInfo.Count;
                    spawnWaveMsg.monsterConfigId = monsterInfo.MonsterId;
                    spawnWaveMsg.moveDirX = -1f;
                    spawnWaveMsg.moveDirY = 0f;
                    spawnWaveMsg.spreadRange = spreadRange;
                    spawnWaveMsg.startUnitId = startUnitId;

                    MapMessageHelper.SendToClient(player, spawnWaveMsg);
                }
            }

            Log.Info($"重连下发当前波次杂兵: PlayerId={player.Id}, WaveIndex={waveManager.CurrentWaveIndex}");
        }
    }
}

