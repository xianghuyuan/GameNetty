using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleRoom))]
    public static partial class BattleRoomSystem
    {
        private const float PlayerSpawnX = -5f;
        private const float EnemySpawnX = 5f;
        private const float TeamMemberSpacingX = 1.5f;

        [EntitySystem]
        private static void Awake(this BattleRoom self, int configId)
        {
            self.State = BattleState.Prepare;
            self.SceneType = SceneType.Battle;
            self.ConfigId = configId;
        }
        
        [EntitySystem]
        private static void Update(this BattleRoom self)
        {
            if (self.State != BattleState.Fighting)
            {
                return;
            }
            
            WaveManagerComponent waveManager = self.GetComponent<WaveManagerComponent>();
            if (waveManager == null || waveManager.State != WaveState.Fighting)
            {
                return;
            }
            
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit == null || unit.IsDead)
                {
                    continue;
                }
                
                BattleAIComponent ai = unit.GetComponent<BattleAIComponent>();
                ai?.Update();

                BattleMoveComponent moveComponent = unit.GetComponent<BattleMoveComponent>();
                moveComponent?.Update();
            }
        }
        
        [EntitySystem]
        private static void Destroy(this BattleRoom self)
        {
            self.PlayerIds.Clear();
            self.Units.Clear();
            self.State = BattleState.End;
        }
        
        public static void AddPlayer(this BattleRoom self, long playerId)
        {
            if (!self.PlayerIds.Contains(playerId))
            {
                self.PlayerIds.Add(playerId);
            }
        }
        
        public static void RemovePlayer(this BattleRoom self, long playerId)
        {
            self.PlayerIds.Remove(playerId);
        }
        
        public static BattleUnit GetUnit(this BattleRoom self, long unitId)
        {
            if (self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                return unitRef;
            }
            return null;
        }
        
        public static void RemoveUnit(this BattleRoom self, long unitId)
        {
            if (self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
            {
                BattleUnit unit = unitRef;
                unit?.Dispose();
                self.Units.Remove(unitId);
            }
        }
        
        public static List<BattleUnit> GetAllUnits(this BattleRoom self)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead)
                {
                    units.Add(unit);
                }
            }
            return units;
        }
        
        public static List<BattleUnit> GetUnitsByCamp(this BattleRoom self, UnitCamp camp)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead && unit.Camp == camp)
                {
                    units.Add(unit);
                }
            }
            return units;
        }
        
        /// <summary>
        /// 初始化战斗房间（创建玩家Unit、WaveManager等）
        /// </summary>
        public static void InitBattle(this BattleRoom self, Unit playerUnit, int stageId, int battleType)
        {
            // 创建玩家战斗单位
            BattleUnit heroUnit = UnitFactory.CreateHero(self, playerUnit.Id, playerUnit.ConfigId, new System.Numerics.Vector3(PlayerSpawnX, 0, 0));
            self.Units[heroUnit.Id] = heroUnit;
            
            // 获取关卡配置
            StageConfigInfo stageInfo = GetStageConfig(stageId > 0 ? stageId : 1);
            
            // 创建波次管理器
            self.AddComponent<WaveManagerComponent, int, List<int>>(
                stageId > 0 ? stageId : 1,
                stageInfo.WaveConfigIds
            );
            
            Log.Info($"初始化战斗: BattleRoomId={self.Id}, StageId={stageId}, TotalWaves={stageInfo.TotalWaves}");
        }
        
        /// <summary>
        /// 初始化组队战斗
        /// </summary>
        public static void InitTeamBattle(this BattleRoom self, Scene mapScene, List<long> memberIds, int battleType)
        {
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            
            int index = 0;
            float centerOffset = (memberIds.Count - 1) * 0.5f;
            foreach (long memberId in memberIds)
            {
                Unit unit = unitComponent.Get(memberId);
                if (unit == null)
                {
                    Log.Warning($"找不到队员 Unit: {memberId}");
                    continue;
                }
                
                float xOffset = (index - centerOffset) * TeamMemberSpacingX;
                var position = new System.Numerics.Vector3(PlayerSpawnX + xOffset, 0, 0);
                BattleUnit battleUnit = UnitFactory.CreateHero(self, unit.Id, unit.ConfigId, position);
                self.Units[battleUnit.Id] = battleUnit;
                index++;
            }
            
            // 根据战斗类型初始化
            if (battleType == 2) // Boss
            {
                BattleUnit bossUnit = UnitFactory.CreateMonster(self, self.ConfigId, new System.Numerics.Vector3(EnemySpawnX, 0, 0));
                self.Units[bossUnit.Id] = bossUnit;
            }
            
            // 初始化波次管理器
            StageConfigInfo stageInfo = GetStageConfig(1);
            self.AddComponent<WaveManagerComponent, int, List<int>>(1, stageInfo.WaveConfigIds);
            
            Log.Info($"初始化组队战斗: BattleRoomId={self.Id}, BattleType={battleType}, MemberCount={memberIds.Count}");
        }
        
        /// <summary>
        /// 开始第一波（延迟启动，确保客户端已收到响应）
        /// </summary>
        public static async ETTask StartFirstWaveDelayed(this BattleRoom self)
        {
            // 等待一帧，确保响应已发送到客户端
            await self.Root().GetComponent<TimerComponent>().WaitFrameAsync();
            
            if (self.IsDisposed)
            {
                return;
            }
            
            // 发送玩家英雄创建消息
            self.SendHeroUnits();
            
            // 再等待一帧，确保客户端处理完英雄创建
            await self.Root().GetComponent<TimerComponent>().WaitFrameAsync();
            
            // 开始波次
            WaveManagerComponent waveManager = self.GetComponent<WaveManagerComponent>();
            if (waveManager != null && !waveManager.IsDisposed)
            {
                await waveManager.StartFirstWave();
            }
        }
        
        /// <summary>
        /// 发送玩家英雄创建消息
        /// </summary>
        public static void SendHeroUnits(this BattleRoom self)
        {
            List<BattleUnit> heroes = self.GetUnitsByCamp(UnitCamp.Friend);
            if (heroes.Count == 0)
            {
                return;
            }
            
            List<BattleUnitInfo> battleUnitInfos = new List<BattleUnitInfo>();
            foreach (BattleUnit hero in heroes)
            {
                BattleUnitInfo unitInfo = BattleUnitHelper.CreateBattleUnitInfo(hero);
                battleUnitInfos.Add(unitInfo);
            }
            
            M2C_CreateBattleUnits createMsg = M2C_CreateBattleUnits.Create();
            createMsg.battleId = self.Id;
            createMsg.units = battleUnitInfos;
            
            self.BroadcastToPlayers(createMsg);
            
            Log.Info($"发送玩家英雄创建消息: BattleRoomId={self.Id}, Count={battleUnitInfos.Count}");
        }
        
        /// <summary>
        /// 创建战斗单位信息（包含数值）
        /// </summary>
        [System.Obsolete("Use BattleUnitHelper.CreateBattleUnitInfo instead")]
        public static BattleUnitInfo CreateBattleUnitInfo(BattleUnit unit)
        {
            return BattleUnitHelper.CreateBattleUnitInfo(unit);
        }
        
        /// <summary>
        /// 广播消息给所有玩家
        /// </summary>
        public static void BroadcastToPlayers(this BattleRoom self, IMessage message)
        {
            Scene mapScene = self.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            
            foreach (long playerId in self.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, message);
                }
            }
        }

        private static StageConfigInfo GetStageConfig(int stageId)
        {
            List<int> waveConfigIds = new List<int>();
            
            foreach (var config in WaveConfigCategory.Instance.DataList)
            {
                waveConfigIds.Add(config.Id);
            }
            
            waveConfigIds.Sort((a, b) =>
            {
                var configA = WaveConfigCategory.Instance.Get(a);
                var configB = WaveConfigCategory.Instance.Get(b);
                return (configA?.WaveNumber ?? 0).CompareTo(configB?.WaveNumber ?? 0);
            });
            
            return new StageConfigInfo
            {
                TotalWaves = waveConfigIds.Count,
                WaveConfigIds = waveConfigIds
            };
        }
        
        private struct StageConfigInfo
        {
            public int TotalWaves;
            public List<int> WaveConfigIds;
        }
    }
}
