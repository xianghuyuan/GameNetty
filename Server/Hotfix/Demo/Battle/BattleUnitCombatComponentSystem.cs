namespace ET.Server
{
    [EntitySystemOf(typeof(BattleUnitCombatComponent))]
    [FriendOf(typeof(BattleUnitCombatComponent))]
    public static partial class BattleUnitCombatComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitCombatComponent self)
        {
            self.AttackCooldown = 1000;
            self.LastAttackTime = 0;
            self.AttackRange = 2.0f;
            self.CanAttack = true;
        }
        
        [EntitySystem]
        private static void Destroy(this BattleUnitCombatComponent self)
        {
            self.LastAttackTime = 0;
            self.CanAttack = false;
        }
        
        public static bool IsAttackReady(this BattleUnitCombatComponent self)
        {
            if (!self.CanAttack)
            {
                return false;
            }
            
            long currentTime = TimeInfo.Instance.ClientFrameTime();
            return currentTime >= self.LastAttackTime + self.AttackCooldown;
        }
        
        public static void StartAttackCooldown(this BattleUnitCombatComponent self)
        {
            self.LastAttackTime = TimeInfo.Instance.ClientFrameTime();
        }
        
        public static void SetAttackCooldown(this BattleUnitCombatComponent self, int cooldownMs)
        {
            self.AttackCooldown = cooldownMs;
        }
        
        public static void SetAttackRange(this BattleUnitCombatComponent self, float range)
        {
            self.AttackRange = range;
        }
    }
}
