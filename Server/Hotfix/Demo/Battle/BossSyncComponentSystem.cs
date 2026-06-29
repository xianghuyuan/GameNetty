namespace ET.Server
{
    /// <summary>
    /// Boss高频同步心跳定时器 - 每50ms驱动Boss位置/状态同步（20Hz）
    /// </summary>
    [Invoke(TimerInvokeType.BossSyncTick)]
    public class BossSyncTimer : ATimer<BossSyncComponent>
    {
        protected override void Run(BossSyncComponent self)
        {
            BossSyncComponentSystem.OnBossSyncTick(self);
        }
    }

    [EntitySystemOf(typeof(BossSyncComponent))]
    [FriendOf(typeof(BossSyncComponent))]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    [FriendOf(typeof(BattleUnitRegistryComponent))]
    public static partial class BossSyncComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BossSyncComponent self)
        {
            self.BossUnitIds.Clear();
            self.LastSyncTime = TimeInfo.Instance.ServerFrameTime();
            self.SyncTimerId = self.Root().GetComponent<TimerComponent>()
                .NewRepeatedTimer(BossSyncComponent.SyncInterval, TimerInvokeType.BossSyncTick, self);
        }

        [EntitySystem]
        private static void Destroy(this BossSyncComponent self)
        {
            long timerId = self.SyncTimerId;
            self.Root().GetComponent<TimerComponent>()?.Remove(ref timerId);
            self.SyncTimerId = 0;
            self.BossUnitIds.Clear();
        }

        /// <summary>
        /// 注册Boss单位到同步列表。在Boss创建时调用。
        /// </summary>
        public static void RegisterBoss(this BossSyncComponent self, long bossUnitId)
        {
            if (!self.BossUnitIds.Contains(bossUnitId))
            {
                self.BossUnitIds.Add(bossUnitId);
            }
        }

        /// <summary>
        /// 从同步列表中移除Boss。在Boss死亡时调用。
        /// </summary>
        public static void UnregisterBoss(this BossSyncComponent self, long bossUnitId)
        {
            self.BossUnitIds.Remove(bossUnitId);
        }

        /// <summary>
        /// Boss同步心跳回调 - 每50ms（20Hz）执行一次。
        /// 向所有玩家广播Boss的位置和状态。
        /// </summary>
        internal static void OnBossSyncTick(BossSyncComponent self)
        {
            BattleRoom battleRoom = self.GetParent<BattleRoom>();
            if (battleRoom == null || battleRoom.IsDisposed || self.BossUnitIds.Count == 0)
            {
                return;
            }

            self.LastSyncTime = TimeInfo.Instance.ServerFrameTime();

            foreach (long bossId in self.BossUnitIds)
            {
                // 内联 GetUnit：直接访问注册表字典，避免 BossSyncComponentSystem → BattleRoomSystem 的静态类引用
                BattleUnitRegistryComponent registry = battleRoom.GetComponent<BattleUnitRegistryComponent>();
                BattleUnit boss = registry?.GetUnit(bossId);
                if (boss == null || boss.IsDead)
                {
                    continue;
                }

                // 构建Boss状态同步消息
                var syncMsg = M2C_SyncBoss.Create();
                syncMsg.bossId = boss.Id;
                syncMsg.position = new Unity.Mathematics.float3(boss.Position.X, boss.Position.Y, boss.Position.Z);
                syncMsg.rotation = boss.Rotation;

                // Boss状态机状态
                syncMsg.state = GetBossStateString(boss);

                // 当前正在施放的技能
                CastingComponent casting = boss.GetComponent<CastingComponent>();
                syncMsg.currentSkillId = (casting != null && casting.IsCasting) ? casting.SkillId : 0;

                // HP信息
                BattleStatsComponent stats = boss.GetOrCreateBattleStats();
                syncMsg.currentHp = stats?.Hp ?? 0;
                syncMsg.maxHp = stats?.MaxHp ?? 0;

                // 内联 BroadcastToPlayers：直接遍历玩家发送，避免 BossSyncComponentSystem → BattleRoomSystem 的静态类引用
                Scene mapScene = battleRoom.Root();
                UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
                foreach (long playerId in battleRoom.PlayerIds)
                {
                    Unit player = unitComponent.Get(playerId);
                    if (player != null)
                    {
                        player.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Send(player.Id, syncMsg);
                    }
                }
            }
        }

        /// <summary>
        /// 获取Boss的当前状态字符串
        /// </summary>
        private static string GetBossStateString(BattleUnit boss)
        {
            FreezeComponent freeze = boss.GetComponent<FreezeComponent>();
            if (freeze != null && freeze.IsFrozen)
            {
                return "Frozen";
            }

            CastingComponent casting = boss.GetComponent<CastingComponent>();
            if (casting != null && casting.IsCasting)
            {
                return "CastSkill";
            }

            BattleMoveComponent move = boss.GetComponent<BattleMoveComponent>();
            if (move != null && move.MoveSpeed > 0)
            {
                return "Moving";
            }

            if (boss.IsDead)
            {
                return "Dead";
            }

            return "Idle";
        }
    }

    /// <summary>
    /// Boss创建事件处理器 - 将Boss注册到同步组件。
    /// 使用事件解耦 UnitFactory → BossSyncComponentSystem 的静态类依赖。
    /// </summary>
    [Event(SceneType.Battle)]
    public class BossCreatedEvent_Handler : AEvent<Scene, BossCreatedEvent>
    {
        protected override async ETTask Run(Scene scene, BossCreatedEvent args)
        {
            BattleRoom battleRoom = args.BattleRoom;
            if (battleRoom != null && !battleRoom.IsDisposed)
            {
                BossSyncComponent bossSync = battleRoom.GetComponent<BossSyncComponent>();
                bossSync?.RegisterBoss(args.BossUnitId);
            }

            await ETTask.CompletedTask;
        }
    }
}
