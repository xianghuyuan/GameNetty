namespace ET.Server
{
    /// <summary>
    /// Buff系统心跳定时器 - 周期驱动所有活跃buff的tick检测和过期清理。
    /// </summary>
    [Invoke(TimerInvokeType.BuffTick)]
    public class BuffTickTimer : ATimer<BuffComponent>
    {
        protected override void Run(BuffComponent self)
        {
            BuffComponentSystem.OnBuffTick(self);
        }
    }

    [EntitySystemOf(typeof(BuffComponent))]
    [FriendOf(typeof(BuffComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BuffComponentSystem
    {
        private const long BuffTickInterval = 100; // 100ms buff系统心跳间隔

        [EntitySystem]
        private static void Awake(this BuffComponent self)
        {
            self.TimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(BuffTickInterval, TimerInvokeType.BuffTick, self);
        }

        [EntitySystem]
        private static void Destroy(this BuffComponent self)
        {
            long timerId = self.TimerId;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref timerId);
            self.TimerId = 0;
        }

        /// <summary>
        /// 添加一个持续buff到目标身上。
        /// 即时效果（duration=0）不在此管理，由 BattleSkillHelper.ApplyEffects 直接执行。
        /// 此方法仅管理 duration>0 的持续效果（冻结、DoT等）。
        /// </summary>
        /// <param name="self">目标的BuffComponent</param>
        /// <param name="buffId">buff配置ID</param>
        /// <param name="casterId">来源施法者ID</param>
        /// <param name="skillId">来源技能ID</param>
        /// <param name="config">buff配置引用</param>
        /// <param name="duration">持续时间（毫秒）</param>
        /// <param name="tickInterval">周期触发间隔（毫秒）</param>
        public static void AddBuff(this BuffComponent self, int buffId, long casterId, int skillId,
            BuffConfig config, int duration, int tickInterval, int maxStack = 0)
        {
            if (config == null || duration <= 0)
            {
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();

            // 同名buff检测：已存在则刷新时长，不创建新实体
            BuffEntity existingBuff = self.FindBuffById(buffId);
            if (existingBuff != null)
            {
                existingBuff.ExpireTime = currentTime + duration;
                existingBuff.Duration = duration;
                if (tickInterval > 0)
                {
                    existingBuff.TickInterval = tickInterval;
                    existingBuff.NextTickTime = currentTime + tickInterval;
                }
                return;
            }

            BuffEntity buffEntity = self.AddChild<BuffEntity>();
            buffEntity.BuffId = buffId;
            buffEntity.CasterId = casterId;
            buffEntity.SkillId = skillId;
            buffEntity.Config = config;
            buffEntity.Duration = duration;
            buffEntity.TickInterval = tickInterval;
            buffEntity.NextTickTime = tickInterval > 0 ? currentTime + tickInterval : 0;
            buffEntity.ExpireTime = currentTime + duration;
            buffEntity.TickCount = 0;
            buffEntity.MaxStack = maxStack;
            buffEntity.StackCount = 1;

            // 首次执行
            EventSystem.Instance.Publish(self.Root(), new BuffExecuteEvent
            {
                Target = self.GetParent<BattleUnit>(),
                BuffEntity = buffEntity,
            });
        }

        /// <summary>
        /// 根据buffId查找已存在的BuffEntity。
        /// </summary>
        public static BuffEntity FindBuffById(this BuffComponent self, int buffId)
        {
            foreach (Entity child in self.Children.Values)
            {
                BuffEntity buffEntity = child as BuffEntity;
                if (buffEntity != null && buffEntity.BuffId == buffId)
                {
                    return buffEntity;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据EffectType查找已存在的BuffEntity。
        /// </summary>
        public static BuffEntity FindBuffByEffectType(this BuffComponent self, EffectType effectType)
        {
            foreach (Entity child in self.Children.Values)
            {
                BuffEntity buffEntity = child as BuffEntity;
                if (buffEntity != null && buffEntity.Config != null && buffEntity.Config.EffectType == (int)effectType)
                {
                    return buffEntity;
                }
            }
            return null;
        }

        /// <summary>
        /// 移除指定buff并发布BuffRemoveEvent。
        /// </summary>
        public static void RemoveBuff(this BuffComponent self, BuffEntity buffEntity)
        {
            if (buffEntity == null) return;

            BattleUnit target = self.GetParent<BattleUnit>();
            EventSystem.Instance.Publish(self.Root(), new BuffRemoveEvent
            {
                Target = target,
                BuffEntity = buffEntity,
            });
            buffEntity.Dispose();
        }

        /// <summary>
        /// Buff系统心跳 - 遍历所有子BuffEntity，处理tick触发和过期清理。
        /// </summary>
        internal static void OnBuffTick(BuffComponent self)
        {
            BattleUnit target = self.GetParent<BattleUnit>();
            if (target == null || target.IsDead)
            {
                // 目标死亡，销毁所有buff（发布移除事件以还原属性）
                self.DestroyAllBuffEntities();
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();

            foreach (Entity child in self.Children.Values)
            {
                BuffEntity buffEntity = child as BuffEntity;
                if (buffEntity == null)
                {
                    continue;
                }

                // 过期检测
                if (buffEntity.IsExpired)
                {
                    // 发布移除事件以还原属性（如AttackBuff/DefenseBuff）
                    EventSystem.Instance.Publish(self.Root(), new BuffRemoveEvent
                    {
                        Target = target,
                        BuffEntity = buffEntity,
                    });
                    buffEntity.Dispose();
                    continue;
                }

                // tick触发检测（只对有 tickInterval 的 buff）
                if (buffEntity.TickInterval > 0 && currentTime >= buffEntity.NextTickTime)
                {
                    EventSystem.Instance.Publish(self.Root(), new BuffExecuteEvent
                    {
                        Target = target,
                        BuffEntity = buffEntity,
                    });
                    buffEntity.TickCount++;
                    buffEntity.NextTickTime = currentTime + buffEntity.TickInterval;
                }
            }
        }

        private static void DestroyAllBuffEntities(this BuffComponent self)
        {
            foreach (Entity child in self.Children.Values)
            {
                BuffEntity buffEntity = child as BuffEntity;
                if (buffEntity != null)
                {
                    buffEntity.Dispose();
                }
            }
        }
    }
}
