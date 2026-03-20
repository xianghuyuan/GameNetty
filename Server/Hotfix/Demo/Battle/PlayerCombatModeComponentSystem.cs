namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerCombatModeComponent))]
    [FriendOf(typeof(PlayerCombatModeComponent))]
    [FriendOf(typeof(BattleAIComponent))]
    public static partial class PlayerCombatModeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerCombatModeComponent self)
        {
            self.Mode = BattleMode.Manual;
        }
        
        [EntitySystem]
        private static void Destroy(this PlayerCombatModeComponent self)
        {
            self.AutoAI = null;
        }
        
        /// <summary>
        /// 设置战斗模式
        /// </summary>
        public static void SetMode(this PlayerCombatModeComponent self, BattleMode mode)
        {
            if (self.Mode == mode)
            {
                return;
            }
            
            self.Mode = mode;
            
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                return;
            }

            BattleAIComponent ai = owner.GetComponent<BattleAIComponent>();
            if (ai == null)
            {
                ai = owner.AddComponent<BattleAIComponent>();
            }

            self.AutoAI = ai;
            
            if (mode == BattleMode.Auto)
            {
                Log.Info($"玩家 {owner.Id} 切换到自动战斗模式");
            }
            else
            {
                owner.GetComponent<BattleMoveComponent>()?.StopMove();
                owner.GetComponent<BattleActionDecisionComponent>()?.Reset();
                Log.Info($"玩家 {owner.Id} 切换到手动战斗模式");
            }
        }
        
        /// <summary>
        /// 切换战斗模式
        /// </summary>
        public static void ToggleMode(this PlayerCombatModeComponent self)
        {
            self.SetMode(self.Mode == BattleMode.Manual ? BattleMode.Auto : BattleMode.Manual);
        }
    }
}
