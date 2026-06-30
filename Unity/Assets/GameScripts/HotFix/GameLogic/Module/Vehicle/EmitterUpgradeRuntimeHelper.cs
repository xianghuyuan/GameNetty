namespace ET
{
    public static class EmitterUpgradeRuntimeHelper
    {
        public static EmitterUpgradeConfig ResolveLevelConfig(EmitterConfig config, int level)
        {
            if (config == null || ConfigHelper.EmitterUpgradeConfig?.DataList == null)
            {
                return null;
            }

            int targetLevel = System.Math.Max(1, level);
            EmitterUpgradeConfig levelOne = null;
            foreach (EmitterUpgradeConfig levelConfig in ConfigHelper.EmitterUpgradeConfig.DataList)
            {
                if (levelConfig == null || levelConfig.UpgradeConfigId != config.UpgradeConfigId)
                {
                    continue;
                }

                if (levelConfig.Level == 1)
                {
                    levelOne = levelConfig;
                }

                if (levelConfig.Level == targetLevel)
                {
                    return levelConfig;
                }
            }

            return levelOne;
        }

        public static int ResolveMaxLevel(EmitterConfig config)
        {
            if (config == null || ConfigHelper.EmitterUpgradeConfig?.DataList == null)
            {
                return 1;
            }

            int maxLevel = 1;
            foreach (EmitterUpgradeConfig levelConfig in ConfigHelper.EmitterUpgradeConfig.DataList)
            {
                if (levelConfig != null && levelConfig.UpgradeConfigId == config.UpgradeConfigId && levelConfig.Level > maxLevel)
                {
                    maxLevel = levelConfig.Level;
                }
            }

            return maxLevel;
        }

        public static int ResolveCooldownMs(EmitterUpgradeConfig levelConfig)
        {
            return System.Math.Max(100, levelConfig?.CooldownMs ?? 1000);
        }

        public static float ResolveRange(EmitterConfig config, EmitterUpgradeConfig levelConfig)
        {
            if (levelConfig != null && levelConfig.Range > 0f)
            {
                return levelConfig.Range;
            }

            SkillTargetingConfig targetingConfig = ConfigHelper.SkillTargetingConfig?.GetOrDefault(config?.TargetingConfigId ?? 0);
            return targetingConfig != null ? targetingConfig.CastRange + targetingConfig.EdgeDistance : 1.5f;
        }

        public static float ResolveBaseDamage(EmitterUpgradeConfig levelConfig)
        {
            return System.Math.Max(0f, levelConfig?.BaseDamage ?? 0f);
        }

        public static float ResolveAttackRatio(EmitterUpgradeConfig levelConfig)
        {
            return System.Math.Max(0f, levelConfig?.AttackRatio ?? 0f);
        }
    }
}
