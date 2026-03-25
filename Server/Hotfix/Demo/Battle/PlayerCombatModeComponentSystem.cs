namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerCombatModeComponent))]
    [FriendOf(typeof(PlayerCombatModeComponent))]
    public static partial class PlayerCombatModeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerCombatModeComponent self)
        {
            self.Mode = BattleMode.Auto;
        }
        
        [EntitySystem]
        private static void Destroy(this PlayerCombatModeComponent self)
        {
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

            if (mode == BattleMode.Auto)
            {
            }
            else
            {
                owner.GetComponent<BattleMoveComponent>()?.StopMove();
                owner.GetComponent<BattleActionDecisionComponent>()?.Reset();
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
