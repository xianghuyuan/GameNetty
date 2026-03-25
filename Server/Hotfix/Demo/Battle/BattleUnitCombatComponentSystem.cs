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
            self.SkillCooldownEnds.Clear();
        }
        
        [EntitySystem]
        private static void Destroy(this BattleUnitCombatComponent self)
        {
            self.LastAttackTime = 0;
            self.CanAttack = false;
            self.SkillCooldownEnds.Clear();
        }
        
        public static bool IsAttackReady(this BattleUnitCombatComponent self)
        {
            if (!self.CanAttack)
            {
                return false;
            }
            
            long currentTime = TimeInfo.Instance.ServerFrameTime();
            return currentTime >= self.LastAttackTime + self.AttackCooldown;
        }
        
        public static void StartAttackCooldown(this BattleUnitCombatComponent self)
        {
            self.LastAttackTime = TimeInfo.Instance.ServerFrameTime();
        }
        
        public static void SetAttackCooldown(this BattleUnitCombatComponent self, int cooldownMs)
        {
            self.AttackCooldown = cooldownMs;
        }
        
        public static void SetAttackRange(this BattleUnitCombatComponent self, float range)
        {
            self.AttackRange = range;
        }

        public static bool IsSkillReady(this BattleUnitCombatComponent self, SkillConfig skillConfig)
        {
            if (skillConfig == null || !self.CanAttack)
            {
                return false;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            int cooldownKey = skillConfig.CooldownGroupId != 0 ? skillConfig.CooldownGroupId : skillConfig.Id;
            if (!self.SkillCooldownEnds.TryGetValue(cooldownKey, out long cooldownEnd))
            {
                return true;
            }

            return currentTime >= cooldownEnd;
        }

        public static long StartSkillCooldown(this BattleUnitCombatComponent self, SkillConfig skillConfig)
        {
            long cooldownEnd = TimeInfo.Instance.ServerFrameTime() + (skillConfig?.CooldownMs ?? self.AttackCooldown);
            if (skillConfig != null)
            {
                int cooldownKey = skillConfig.CooldownGroupId != 0 ? skillConfig.CooldownGroupId : skillConfig.Id;
                self.SkillCooldownEnds[cooldownKey] = cooldownEnd;
                self.AttackCooldown = skillConfig.CooldownMs;
            }

            self.LastAttackTime = TimeInfo.Instance.ServerFrameTime();
            return cooldownEnd;
        }
    }
}
