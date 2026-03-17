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
            
            var numeric = self.GetComponent<NumericComponent>();
            if (numeric == null)
            {
                return false;
            }
            
            long hp = numeric.GetAsInt(NumericType.Hp);
            return hp <= 0;
        }
        
        public static void TakeDamage(this BattleUnit self, int damage)
        {
            if (self.IsDead)
            {
                return;
            }
            
            var numeric = self.GetComponent<NumericComponent>();
            if (numeric == null)
            {
                return;
            }
            
            int currentHp = numeric.GetAsInt(NumericType.Hp);
            int newHp = currentHp - damage;
            
            if (newHp < 0)
            {
                newHp = 0;
            }
            
            numeric.Set(NumericType.Hp, newHp);
            
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
            
            var numeric = self.GetComponent<NumericComponent>();
            if (numeric == null)
            {
                return;
            }
            
            int currentHp = numeric.GetAsInt(NumericType.Hp);
            int maxHp = numeric.GetAsInt(NumericType.MaxHp);
            int newHp = currentHp + healAmount;
            
            if (newHp > maxHp)
            {
                newHp = maxHp;
            }
            
            numeric.Set(NumericType.Hp, newHp);
        }
    }
    
    public struct BattleUnitDead
    {
        public BattleUnit BattleUnit;
    }
}
