namespace ET
{
    [EntitySystemOf(typeof(BattleUnitCombatComponent))]
    [FriendOf(typeof(BattleUnitCombatComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleUnitCombatComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitCombatComponent self, float attackRange)
        {
            self.AttackRange = attackRange;
        }

        [EntitySystem]
        private static void Destroy(this BattleUnitCombatComponent self)
        {
        }

        public static void SetAttackRange(this BattleUnitCombatComponent self, float range)
        {
            self.AttackRange = range;
        }

        /// <summary>
        /// 判断是否死亡
        /// </summary>
        public static bool CheckIsDead(this BattleUnitCombatComponent self)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            BattleStatsComponent stats = unit?.GetOrCreateBattleStats();
            return stats != null && stats.Hp <= 0;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public static void TakeDamage(this BattleUnitCombatComponent self, int damage)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit == null || unit.IsDead)
            {
                return;
            }

            BattleStatsComponent stats = unit.GetOrCreateBattleStats();
            if (stats == null)
            {
                return;
            }

            int newHp = stats.Hp - damage;

            if (newHp < 0)
            {
                newHp = 0;
            }

            stats.SetHp(newHp, true);

            if (newHp <= 0)
            {
                unit.IsDead = true;
                EventSystem.Instance.Publish(self.Scene(), new BattleUnitDead { BattleUnit = unit });
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public static void Heal(this BattleUnitCombatComponent self, int healAmount)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit == null || unit.IsDead)
            {
                return;
            }

            BattleStatsComponent stats = unit.GetOrCreateBattleStats();
            if (stats == null)
            {
                return;
            }

            int newHp = stats.Hp + healAmount;

            if (newHp > stats.MaxHp)
            {
                newHp = stats.MaxHp;
            }

            stats.SetHp(newHp, true);
        }
    }
}
