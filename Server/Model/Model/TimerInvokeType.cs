namespace ET
{
    [UniqueId(100, 10000)]
    public static class TimerInvokeType
    {
        // 框架层100-200，逻辑层的timer type从200起
        public const int WaitTimer = 100;
        public const int SessionIdleChecker = 101;
        public const int MessageLocationSenderChecker = 102;
        public const int MessageSenderChecker = 103;
        
        // 框架层100-200，逻辑层的timer type 200-300
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
    }
}