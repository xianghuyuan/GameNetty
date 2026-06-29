using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleUnit))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleUnitSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnit self, int configId)
        {
            self.ConfigId = configId;
            self.IsDead = false;
        }
        
        [EntitySystem]
        private static void Destroy(this BattleUnit self)
        {
            self.ConfigId = 0;
            self.OwnerId = 0;
            self.IsDead = false;
            self.Position = Vector3.Zero;
        }
        
        public static MonsterUnitConfig GetConfig(this BattleUnit self)
        {
            return MonsterUnitConfigCategory.Instance.Get(self.ConfigId);
        }
        
        public static bool CheckIsDead(this BattleUnit self)
        {
            if (self.IsDead)
            {
                return true;
            }

            BattleStatsComponent stats = self.GetOrCreateBattleStats();
            return stats != null && stats.Hp <= 0;
        }
        
        public static void TakeDamage(this BattleUnit self, int damage)
        {
            if (self.IsDead)
            {
                return;
            }
            
            BattleStatsComponent stats = self.GetOrCreateBattleStats();
            if (stats == null)
            {
                return;
            }

            // 先扣护盾
            ShieldComponent shieldComp = self.GetComponent<ShieldComponent>();
            if (shieldComp != null && shieldComp.IsActive)
            {
                damage = shieldComp.AbsorbDamage(damage);
            }

            if (damage <= 0)
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
                self.IsDead = true;
                EventSystem.Instance.Publish(self.Root(), new BattleUnitDead { BattleUnit = self });
            }
        }
        
        public static void Heal(this BattleUnit self, int healAmount)
        {
            if (self.IsDead)
            {
                return;
            }
            
            BattleStatsComponent stats = self.GetOrCreateBattleStats();
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
    
    public struct BattleUnitDead
    {
        public BattleUnit BattleUnit;
    }
}
