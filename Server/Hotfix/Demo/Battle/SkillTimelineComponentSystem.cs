using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 技能时间轴心跳定时器 - 每20ms驱动时间轴判定框检测
    /// </summary>
    [Invoke(TimerInvokeType.SkillTimelineTick)]
    public class SkillTimelineTimer : ATimer<SkillTimelineComponent>
    {
        protected override void Run(SkillTimelineComponent self)
        {
            SkillTimelineComponentSystem.OnTimelineTick(self);
        }
    }

    /// <summary>
    /// 批量伤害下发心跳定时器 - 每100ms打包下发累积的伤害结果
    /// </summary>
    [Invoke(TimerInvokeType.BatchDamageSend)]
    public class BatchDamageSendTimer : ATimer<SkillTimelineComponent>
    {
        protected override void Run(SkillTimelineComponent self)
        {
            SkillTimelineComponentSystem.FlushBatchResults(self);
        }
    }

    [EntitySystemOf(typeof(SkillTimelineComponent))]
    [FriendOf(typeof(SkillTimelineComponent))]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class SkillTimelineComponentSystem
    {
        /// <summary>时间轴检测频率（毫秒），20ms</summary>
        private const long TimelineTickInterval = 20;

        [EntitySystem]
        private static void Awake(this SkillTimelineComponent self)
        {
            self.LastBatchSendTime = TimeInfo.Instance.ServerFrameTime();
            self.PendingEntries.Clear();
            self.AccumulatedResults.Clear();
            self.AccumulatedBossResults.Clear();

            // 启动时间轴心跳
            self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(TimelineTickInterval, TimerInvokeType.SkillTimelineTick, self);

            // 启动批量下发心跳
            self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(SkillTimelineComponent.BatchInterval, TimerInvokeType.BatchDamageSend, self);
        }

        [EntitySystem]
        private static void Destroy(this SkillTimelineComponent self)
        {
            // 时间轴和批量下发的定时器会随Entity销毁自动清理
            self.PendingEntries.Clear();
            self.AccumulatedResults.Clear();
            self.AccumulatedBossResults.Clear();
        }

        /// <summary>
        /// 注册判定框到时间轴队列。
        /// 由技能系统在施法时调用，不立刻计算伤害。
        /// </summary>
        public static void RegisterHitBox(this SkillTimelineComponent self, long casterId, int skillId,
            long targetId, float hitStartTick, float hitEndTick, float hitBoxMinX, float hitBoxMaxX)
        {
            var entry = new HitBoxEntry
            {
                CasterId = casterId,
                SkillId = skillId,
                TargetId = targetId,
                StartTick = hitStartTick,
                EndTick = hitEndTick,
                HitBoxMinX = hitBoxMinX,
                HitBoxMaxX = hitBoxMaxX,
                IsProcessed = false,
            };

            self.PendingEntries.Add(entry);
        }

        /// <summary>
        /// 时间轴心跳回调 - 每20ms执行一次。
        /// 检查所有待结算的判定框，当逻辑时间到达有效窗口时执行碰撞检测。
        /// </summary>
        internal static void OnTimelineTick(SkillTimelineComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            if (battleRoom == null || battleRoom.IsDisposed)
            {
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            float currentTick = currentTime;

            BattleSpatialGrid spatialGrid = battleRoom.GetComponent<BattleSpatialGrid>();

            for (int i = self.PendingEntries.Count - 1; i >= 0; i--)
            {
                HitBoxEntry entry = self.PendingEntries[i];

                // 尚未到达判定开始时间
                if (currentTick < entry.StartTick)
                {
                    continue;
                }

                // 已经过了判定结束时间且未处理，标记处理
                if (currentTick > entry.EndTick && !entry.IsProcessed)
                {
                    ProcessHitBoxEntry(self, battleRoom, spatialGrid, entry, currentTime);
                    entry.IsProcessed = true;
                    self.PendingEntries.RemoveAt(i);
                    continue;
                }

                // 在判定窗口内且未处理
                if (!entry.IsProcessed && currentTick >= entry.StartTick && currentTick <= entry.EndTick)
                {
                    ProcessHitBoxEntry(self, battleRoom, spatialGrid, entry, currentTime);
                    entry.IsProcessed = true;
                    self.PendingEntries.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 处理单个判定框条目 - 执行碰撞检测和伤害结算
        /// </summary>
        private static void ProcessHitBoxEntry(SkillTimelineComponent self, BattleRoom battleRoom,
            BattleSpatialGrid spatialGrid, HitBoxEntry entry, long currentTime)
        {
            BattleUnit caster = battleRoom.GetUnit(entry.CasterId);
            if (caster == null || caster.IsDead)
            {
                return;
            }

            SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(entry.SkillId);
            SkillTargetingConfig targetingConfig = skillConfig?.TargetingConfigIdConfig;
            BuffGroupConfig effectGroupConfig = skillConfig?.BuffGroupIdConfig;
            if (skillConfig == null || effectGroupConfig == null)
            {
                return;
            }

            // 使用空间网格查询判定框范围内的单位
            List<long> candidateIds = new List<long>();
            if (spatialGrid != null)
            {
                spatialGrid.QueryRange(entry.HitBoxMinX, entry.HitBoxMaxX, candidateIds);
            }
            else
            {
                // 降级为全遍历
                foreach (var kv in battleRoom.Units)
                {
                    BattleUnit unit = kv.Value;
                    if (unit != null && !unit.IsDead && unit.Id != entry.CasterId && unit.Camp != caster.Camp)
                    {
                        candidateIds.Add(unit.Id);
                    }
                }
            }

            foreach (long unitId in candidateIds)
            {
                BattleUnit target = battleRoom.GetUnit(unitId);
                if (target == null || target.IsDead)
                {
                    continue;
                }

                // 阵营检查
                if (target.Camp == caster.Camp)
                {
                    continue;
                }

                // 只对Boss做碰撞检测（Boss位置服务端权威）
                // 杂兵走客户端权威路径，由 C2M_ClientBatchHitHandler 直接处理
                if (!IsBossUnit(target))
                {
                    continue;
                }

                // 精确X轴碰撞检查
                float targetX = target.Position.X;
                if (targetX < entry.HitBoxMinX || targetX > entry.HitBoxMaxX)
                {
                    continue;
                }

                int damage = BattleSkillHelper.ApplyEffects(caster, target, effectGroupConfig, skillConfig);

                // Boss伤害：记录，等待批量下发
                var bossResult = new BossDamageResult
                {
                    AttackerId = entry.CasterId,
                    SkillId = entry.SkillId,
                    Damage = damage,
                    DamageType = skillConfig.SkillKind == 1 ? 0 : 1,
                };

                NumericComponent targetNumeric = target.GetComponent<NumericComponent>();
                bossResult.BossCurrentHp = targetNumeric?.GetAsInt(NumericType.Hp) ?? 0;
                bossResult.BossMaxHp = targetNumeric?.GetAsInt(NumericType.MaxHp) ?? 0;

                self.AccumulatedBossResults.Add(bossResult);
            }

            candidateIds.Clear();
        }

        /// <summary>
        /// 批量下发心跳 - 每100ms将累积的伤害结果打包发送给客户端
        /// </summary>
        internal static void FlushBatchResults(SkillTimelineComponent self)
        {
            if (self.AccumulatedResults.Count == 0 && self.AccumulatedBossResults.Count == 0)
            {
                return;
            }

            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            if (battleRoom == null || battleRoom.IsDisposed)
            {
                return;
            }

            // 批量下发杂兵伤害
            if (self.AccumulatedResults.Count > 0)
            {
                var batchMsg = M2C_BatchDamage.Create();
                batchMsg.battleId = battleRoom.Id;

                var damageList = new List<M2C_BatchDamage_DamageInfo>();
                var deadList = new List<long>();

                foreach (BatchDamageResult result in self.AccumulatedResults)
                {
                    var info = M2C_BatchDamage_DamageInfo.Create();
                    info.attackerId = result.AttackerId;
                    info.targetId = result.TargetId;
                    info.damage = result.Damage;
                    info.damageType = result.DamageType;
                    info.targetCurrentHp = result.TargetCurrentHp;
                    info.targetMaxHp = result.TargetMaxHp;
                    damageList.Add(info);

                    if (result.TargetDead)
                    {
                        deadList.Add(result.TargetId);
                    }
                }

                batchMsg.damages = damageList;
                batchMsg.deadUnitIds = deadList;

                battleRoom.BroadcastToPlayers(batchMsg);
                self.AccumulatedResults.Clear();
            }

            // Boss伤害：每100ms也打包下发
            if (self.AccumulatedBossResults.Count > 0)
            {
                // 合并同一Boss的所有伤害
                var bossDamageMap = new Dictionary<long, int>();

                foreach (BossDamageResult result in self.AccumulatedBossResults)
                {
                    if (!bossDamageMap.ContainsKey(result.AttackerId))
                    {
                        bossDamageMap[result.AttackerId] = 0;
                    }
                    bossDamageMap[result.AttackerId] += result.Damage;
                }

                // 取最后一个BossDamageResult来获取Hp信息（因为Boss不会死）
                BossDamageResult lastResult = self.AccumulatedBossResults[self.AccumulatedBossResults.Count - 1];

                var bossDmgMsg = M2C_BossDamage.Create();
                bossDmgMsg.battleId = battleRoom.Id;
                bossDmgMsg.totalDamage = lastResult.Damage;
                bossDmgMsg.bossCurrentHp = lastResult.BossCurrentHp;
                bossDmgMsg.bossMaxHp = lastResult.BossMaxHp;
                bossDmgMsg.damageType = lastResult.DamageType;

                battleRoom.BroadcastToPlayers(bossDmgMsg);
                self.AccumulatedBossResults.Clear();
            }

            self.LastBatchSendTime = TimeInfo.Instance.ServerFrameTime();
        }

        /// <summary>
        /// 判断目标是否为Boss单位
        /// </summary>
        private static bool IsBossUnit(BattleUnit target)
        {
            return target != null && target.IsBoss;
        }
    }
}
