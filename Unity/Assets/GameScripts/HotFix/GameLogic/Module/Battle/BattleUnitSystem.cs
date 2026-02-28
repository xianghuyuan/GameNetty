namespace ET
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
        
        /// <summary>
        /// 获取配置
        /// </summary>
        public static UnitConfig GetConfig(this BattleUnit self)
        {
            return ConfigHelper.UnitConfig.Get(self.ConfigId);
        }
        
        /// <summary>
        /// 判断是否死亡
        /// </summary>
        public static bool CheckIsDead(this BattleUnit self)
        {
            var numeric = self.GetComponent<NumericComponent>();
            if (numeric == null)
            {
                return false;
            }
            
            long hp = numeric.GetByKey(NumericType.Hp);
            return hp <= 0;
        }
        
        /// <summary>
        /// 受到伤害
        /// </summary>
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
            
            long currentHp = numeric.GetByKey(NumericType.Hp);
            long newHp = currentHp - damage;
            
            if (newHp < 0)
            {
                newHp = 0;
            }
            
            numeric.Set(NumericType.Hp, (int)newHp);
            
            // 检查是否死亡
            if (newHp <= 0)
            {
                self.IsDead = true;
                
                // 触发死亡事件
                EventSystem.Instance.Publish(self.Scene(), new BattleUnitDead { BattleUnit = self });
            }
        }
        
        /// <summary>
        /// 治疗
        /// </summary>
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
            
            long currentHp = numeric.GetByKey(NumericType.Hp);
            long maxHp = numeric.GetByKey(NumericType.MaxHp);
            long newHp = currentHp + healAmount;
            
            if (newHp > maxHp)
            {
                newHp = maxHp;
            }
            
            numeric.Set(NumericType.Hp, (int)newHp);
        }
    }
}
