namespace ET
{
    /// <summary>
    /// 配置访问辅助类 - 提供便捷的配置访问方法
    /// 替代直接访问 Generate 目录中的单例模式
    /// </summary>
    public static class ConfigHelper
    {
        private static Tables tables;
        
        /// <summary>
        /// Tables 实例
        /// </summary>
        public static Tables Tables 
        { 
            get => tables;
            set => tables = value;
        }
        
        /// <summary>
        /// Unit 配置表快捷访问
        /// 使用：ConfigHelper.UnitConfig.Get(id)
        /// </summary>
        public static UnitConfigCategory UnitConfig => tables?.UnitConfigCategory;
        
        /// <summary>
        /// AI 配置表快捷访问
        /// 使用：ConfigHelper.AIConfig.Get(id)
        /// </summary>
        public static AIConfigCategory AIConfig => tables?.AIConfigCategory;
        
        /// <summary>
        /// 怪物配置表快捷访问
        /// 使用：ConfigHelper.MonsterUnitConfig.Get(id)
        /// </summary>
        public static MonsterUnitConfigCategory MonsterUnitConfig => tables?.MonsterUnitConfigCategory;

        /// <summary>
        /// 资源配置表快捷访问
        /// 使用：ConfigHelper.ResourceConfig.Get(id)
        /// </summary>
        public static ResourceConfigCategory ResourceConfig => tables?.ResourceConfigCategory;

        public static UnitCombatConfigCategory UnitCombatConfig => tables?.UnitCombatConfigCategory;

        public static EmitterConfigCategory EmitterConfig => tables?.EmitterConfigCategory;

        public static EmitterUpgradeConfigCategory EmitterUpgradeConfig => tables?.EmitterUpgradeConfigCategory;

        public static EmitterEffectConfigCategory EmitterEffectConfig => tables?.EmitterEffectConfigCategory;

        public static EmitterEffectPackConfigCategory EmitterEffectPackConfig => tables?.EmitterEffectPackConfigCategory;

        public static SkillTargetingConfigCategory SkillTargetingConfig => tables?.SkillTargetingConfigCategory;

        public static BuffGroupConfigCategory BuffGroupConfig => tables?.BuffGroupConfigCategory;

        public static BuffConfigCategory BuffConfigCategory => tables?.BuffConfigCategory;

        public static SpawnConfigCategory SpawnConfig => tables?.SpawnConfigCategory;

        public static StageConfigCategory StageConfig => tables?.StageConfigCategory;

        public static WaveConfigCategory WaveConfig => tables?.WaveConfigCategory;
        
        /// <summary>
        /// 清理配置引用
        /// </summary>
        public static void Clear()
        {
            tables = null;
        }
    }
}
