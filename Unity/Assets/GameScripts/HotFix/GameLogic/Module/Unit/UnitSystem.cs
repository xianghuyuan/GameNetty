namespace ET
{
    [EntitySystemOf(typeof(Unit))]
    public static partial class UnitSystem
    {
        [EntitySystem]
        private static void Awake(this Unit self, int configId)
        {
            self.ConfigId = configId;
            // 尝试从配置获取类型，如果配置存在的话
            UnitConfig config = ConfigHelper.UnitConfig.Get(configId);
            if (config != null)
            {
                self.UnitType = (UnitType)config.Type;
            }
        }

        public static UnitConfig Config(this Unit self)
        {
            return ConfigHelper.UnitConfig.Get(self.ConfigId);
        }
        
        public static UnitType Type(this Unit self)
        {
            return (UnitType)self.Config().Type;
        }
    }
}