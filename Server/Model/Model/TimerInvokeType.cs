namespace ET
{
    [UniqueId(100, 10000)]
    public static class TimerInvokeType
    {
        // 框架层100-200，
        public const int WaitTimer = 100;
        public const int SessionIdleChecker = 101;
        public const int MessageLocationSenderChecker = 102;
        public const int MessageSenderChecker = 103;
        // 逻辑层的timer type从200起
        public const int MoveTimer = 201;
        public const int AITimer = 202;
        public const int SessionAcceptTimeout = 203;
        
        public const int AccountSessionCheckOutTime = 204;

        public const int PlayerOfflineOutTime = 205;

        public const int SaveChangeDBData = 206;
        public const int NumericSync = 207;
        public const int MakeQueueOver = 208;
        public const int RoomUpdate = 301;
        public const int BuildingOver = 302;
        
        // 敌人测试相关定时器类型
        public const int EnemyTestStart = 303;
        
        // Buff系统定时器
        public const int BuffExpire = 310;  // Buff过期
        public const int BuffTick = 311;    // Buff周期触发
        
        public const int MonsterAITick = 320; // 怪物AI心跳
        public const int PlayerAITick = 321; // 玩家AI心跳
        
        public const int StateMachineUpdate = 322; // 状态机更新
        public const int FreezeEnd = 323;          // 冻结结束
        
        // 移动和决策定时器
        public const int BattleMoveTick = 324;     // 战斗移动心跳 100ms
        public const int BattleDecisionTick = 325; // 战斗决策心跳 200ms
        public const int CastingEnd = 326;         // 施法锁定结束
        public const int ProjectileTick = 327;     // 投射物飞行心跳 50ms
        public const int SkillTimelineTick = 328;  // 技能时间轴心跳 20ms
        public const int BossSyncTick = 329;       // Boss高频同步心跳 50ms (20Hz)
        public const int BatchDamageSend = 330;    // 批量伤害下发心跳 100ms
        public const int NetworkStateCheck = 331;  // 网络状态机检查心跳 500ms
    }
}