using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(WaveManagerComponent))]
    [FriendOf(typeof(WaveManagerComponent))]
    [FriendOf(typeof(BattleRoom))]
    public static partial class WaveManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WaveManagerComponent self, int totalWaves)
        {
            self.TotalWaves = totalWaves;
            self.CurrentWave = 0;
            self.State = WaveState.None;
            self.CurrentWaveMonsterIds = new List<long>();
            self.WaveInterval = 5000; // 5秒间隔
            self.AutoStartNextWave = true;
        }
        
        [EntitySystem]
        private static void Destroy(this WaveManagerComponent self)
        {
            self.CurrentWaveMonsterIds.Clear();
        }
        
        /// <summary>
        /// 开始第一波
        /// </summary>
        public static async ETTask StartFirstWave(this WaveManagerComponent self)
        {
            await self.StartNextWave();
        }
        
        /// <summary>
        /// 开始下一波
        /// </summary>
        public static async ETTask StartNextWave(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            // 检查是否还有下一波
            if (self.CurrentWave >= self.TotalWaves)
            {
                Log.Info($"所有波次已完成: BattleRoomId={battleRoom.Id}");
                await self.OnAllWavesCompleted();
                return;
            }
            
            // 进入下一波
            self.CurrentWave++;
            self.State = WaveState.Preparing;
            
            Log.Info($"准备开始第 {self.CurrentWave}/{self.TotalWaves} 波: BattleRoomId={battleRoom.Id}");
            
            // 广播波次开始消息
            M2C_WaveStart waveStartMsg = M2C_WaveStart.Create();
            waveStartMsg.battleId = battleRoom.Id;
            waveStartMsg.waveNumber = self.CurrentWave;
            waveStartMsg.totalWaves = self.TotalWaves;
            waveStartMsg.monsterCount = self.GetMonsterCountForWave(self.CurrentWave);
            
            self.BroadcastToBattleRoom(waveStartMsg);
            
            // 等待准备时间（如果不是第一波）
            if (self.CurrentWave > 1)
            {
                await self.Root().GetComponent<TimerComponent>().WaitAsync(self.WaveInterval);
            }
            
            // 生成怪物
            await self.SpawnWaveMonsters();
            
            // 开始战斗
            self.State = WaveState.Fighting;
            self.WaveStartTime = TimeInfo.Instance.ServerFrameTime();
            
            Log.Info($"第 {self.CurrentWave} 波开始战斗: BattleRoomId={battleRoom.Id}, MonsterCount={self.CurrentWaveMonsterIds.Count}");
        }
        
        /// <summary>
        /// 生成当前波次的怪物
        /// </summary>
        private static async ETTask SpawnWaveMonsters(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            // 清空上一波的怪物列表
            self.CurrentWaveMonsterIds.Clear();
            
            // 根据波次计算怪物数量和配置
            int monsterCount = self.GetMonsterCountForWave(self.CurrentWave);
            int monsterConfigId = self.GetMonsterConfigIdForWave(self.CurrentWave);
            
            // 生成怪物
            for (int i = 0; i < monsterCount; i++)
            {
                // 计算怪物生成位置（排成一排）
                Vector3 position = new Vector3(
                    (i - monsterCount / (float)2) * 2, // X轴排列
                    0,
                    10 // Z轴距离
                );
                
                // 创建怪物
                BattleUnit monster = UnitFactory.CreateMonster(battleRoom, monsterConfigId, position);
                
                // 添加到房间和波次管理器
                battleRoom.Units[monster.Id] = monster;
                self.CurrentWaveMonsterIds.Add(monster.Id);
                
                Log.Debug($"生成怪物: MonsterId={monster.Id}, ConfigId={monsterConfigId}, Position={position}");
            }
            
            Log.Info($"第 {self.CurrentWave} 波怪物生成完成: Count={monsterCount}");
            
            await ETTask.CompletedTask;
        }
        
        /// <summary>
        /// 怪物死亡回调
        /// </summary>
        public static async ETTask OnMonsterDead(this WaveManagerComponent self, long monsterId)
        {
            // 从当前波次列表移除
            self.CurrentWaveMonsterIds.Remove(monsterId);
            
            Log.Debug($"怪物死亡: MonsterId={monsterId}, 剩余怪物数: {self.CurrentWaveMonsterIds.Count}");
            
            // 检查当前波次是否完成
            if (self.CurrentWaveMonsterIds.Count == 0 && self.State == WaveState.Fighting)
            {
                await self.OnWaveCompleted();
            }
        }
        
        /// <summary>
        /// 当前波次完成
        /// </summary>
        private static async ETTask OnWaveCompleted(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            self.State = WaveState.Completed;
            long duration = (TimeInfo.Instance.ServerFrameTime() - self.WaveStartTime) / 1000;
            
            Log.Info($"第 {self.CurrentWave} 波完成: BattleRoomId={battleRoom.Id}, 耗时={duration}秒");
            
            // 广播波次完成消息
            M2C_WaveComplete waveCompleteMsg = M2C_WaveComplete.Create();
            waveCompleteMsg.battleId = battleRoom.Id;
            waveCompleteMsg.waveNumber = self.CurrentWave;
            waveCompleteMsg.totalWaves = self.TotalWaves;
            waveCompleteMsg.duration = (int)duration;
            
            self.BroadcastToBattleRoom(waveCompleteMsg);
            
            // 检查是否还有下一波
            if (self.CurrentWave < self.TotalWaves)
            {
                if (self.AutoStartNextWave)
                {
                    // 自动开始下一波
                    await self.StartNextWave();
                }
                else
                {
                    // 等待玩家手动开始
                    Log.Info($"等待玩家开始下一波: BattleRoomId={battleRoom.Id}");
                }
            }
            else
            {
                // 所有波次完成
                await self.OnAllWavesCompleted();
            }
        }
        
        /// <summary>
        /// 所有波次完成
        /// </summary>
        private static async ETTask OnAllWavesCompleted(this WaveManagerComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            
            Log.Info($"所有波次完成，战斗胜利: BattleRoomId={battleRoom.Id}");
            
            // 设置战斗状态为结束
            battleRoom.State = BattleState.End;
            
            // 广播战斗结束消息
            M2C_BattleEnd battleEndMsg = M2C_BattleEnd.Create();
            battleEndMsg.battleId = battleRoom.Id;
            battleEndMsg.success = true;
            battleEndMsg.duration = 0; // TODO: 计算总耗时
            
            self.BroadcastToBattleRoom(battleEndMsg);
            
            // 延迟清理房间
            await self.Root().GetComponent<TimerComponent>().WaitAsync(5000);
            
            // 清理房间
            Scene mapScene = battleRoom.Scene();
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager != null)
            {
                foreach (long playerId in battleRoom.PlayerIds)
                {
                    roomManager.RemoveUnitFromBattleRoom(playerId);
                }
                roomManager.RemoveBattleRoom(battleRoom.Id);
            }
            
            battleRoom.Dispose();
        }
        
        /// <summary>
        /// 获取指定波次的怪物数量
        /// </summary>
        private static int GetMonsterCountForWave(this WaveManagerComponent self, int wave)
        {
            // 简单的递增规则：每波增加1个怪物
            // 第1波: 3个，第2波: 4个，第3波: 5个...
            return 2 + wave;
        }
        
        /// <summary>
        /// 获取指定波次的怪物配置ID
        /// </summary>
        private static int GetMonsterConfigIdForWave(this WaveManagerComponent self, int wave)
        {
            // TODO: 从配置表读取
            // 目前使用固定值
            return 2001; // 普通怪物配置ID
        }
        
        /// <summary>
        /// 广播消息给房间内所有玩家
        /// </summary>
        private static void BroadcastToBattleRoom(this WaveManagerComponent self, IMessage message)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            
            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, message);
                }
            }
        }
        
        /// <summary>
        /// 强制结束当前波次（用于调试或特殊情况）
        /// </summary>
        public static async ETTask ForceCompleteCurrentWave(this WaveManagerComponent self)
        {
            if (self.State != WaveState.Fighting)
            {
                return;
            }
            
            // 清除所有怪物
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            foreach (long monsterId in self.CurrentWaveMonsterIds.ToArray())
            {
                if (battleRoom.Units.TryGetValue(monsterId, out EntityRef<BattleUnit> monsterRef))
                {
                    BattleUnit monster = monsterRef;
                    monster?.Dispose();
                }
            }
            
            self.CurrentWaveMonsterIds.Clear();
            
            await self.OnWaveCompleted();
        }
    }
}
