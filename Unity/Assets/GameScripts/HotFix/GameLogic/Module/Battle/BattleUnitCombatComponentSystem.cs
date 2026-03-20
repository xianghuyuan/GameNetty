namespace ET
{
    [EntitySystemOf(typeof(BattleUnitCombatComponent))]
    [FriendOf(typeof(BattleUnitCombatComponent))]
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
    }
}
