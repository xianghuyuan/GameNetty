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
        /// 设置数值并发布事件
        /// </summary>
        public static void SetNumeric(this BattleUnit self, int numericType, long value)
        {
            var numeric = self.GetComponent<NumericComponent>();
            if (numeric == null) return;

            long oldValue = numeric.GetByKey(numericType);
            if (oldValue == value) return;

            numeric.Set(numericType, (int)value);

            EventSystem.Instance.Publish(self.Scene(), new BattleUnitNumericChange
            {
                BattleUnit = self,
                NumericType = numericType,
                OldValue = oldValue,
                NewValue = value
            });
        }
    }
}
