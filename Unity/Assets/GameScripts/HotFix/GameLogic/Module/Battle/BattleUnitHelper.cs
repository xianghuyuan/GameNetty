using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 战斗单位辅助类
    /// 负责 Unit 和 BattleUnit 之间的数据复制和同步
    /// </summary>
    public static class BattleUnitHelper
    {
        /// <summary>
        /// 从主世界 Unit 创建 BattleUnit
        /// </summary>
        /// <param name="battle">战斗实例</param>
        /// <param name="unit">主世界 Unit</param>
        /// <param name="position">战斗位置</param>
        /// <returns>创建的 BattleUnit</returns>
        public static BattleUnit CreateFromUnit(Battle battle, Unit unit, float3 position)
        {
            // 创建 BattleUnit
            BattleUnit battleUnit = battle.AddChild<BattleUnit, int>(unit.ConfigId);
            
            // 设置关键属性
            battleUnit.OwnerId = unit.Id; // ⭐ 重要：记录对应的 Unit ID
            battleUnit.Camp = UnitCamp.Friend; // 玩家阵营
            battleUnit.Position = position;
            battleUnit.Forward = unit.Forward;
            
            // 添加战斗组件
            battleUnit.AddComponent<NumericComponent>();
            battleUnit.AddComponent<NumericNoticeComponent>();
            
            // 复制数值
            CopyNumeric(unit, battleUnit);
            
            Log.Info($"从 Unit 创建 BattleUnit: UnitId={unit.Id}, BattleUnitId={battleUnit.Id}, ConfigId={unit.ConfigId}");
            
            return battleUnit;
        }
        
        /// <summary>
        /// 复制数值组件
        /// </summary>
        /// <param name="unit">源 Unit</param>
        /// <param name="battleUnit">目标 BattleUnit</param>
        public static void CopyNumeric(Unit unit, BattleUnit battleUnit)
        {
            var unitNumeric = unit.GetComponent<NumericComponent>();
            var battleNumeric = battleUnit.GetComponent<NumericComponent>();
            
            if (unitNumeric == null || battleNumeric == null)
            {
                Log.Error("NumericComponent 不存在，无法复制数值");
                return;
            }
            
            // 复制所有数值
            foreach (var kv in unitNumeric.NumericDic)
            {
                battleNumeric[kv.Key] = kv.Value;
            }
            
            // TODO: 这里可以添加战斗加成
            // - 装备加成
            // - Buff 加成
            // - 技能加成
            
            Log.Debug($"复制数值完成: 共 {unitNumeric.NumericDic.Count} 个属性");
        }
        
        /// <summary>
        /// 战斗结算同步回主世界 Unit
        /// </summary>
        /// <param name="unit">主世界 Unit</param>
        /// <param name="result">战斗结果</param>
        public static void SyncBattleResultToUnit(Unit unit, BattleResult result)
        {
            if (unit == null)
            {
                Log.Error("Unit 为空，无法同步战斗结果");
                return;
            }
            
            if (!result.Success)
            {
                Log.Info($"战斗失败，不同步数据: UnitId={unit.Id}");
                return;
            }
            
            Log.Info($"同步战斗结果到 Unit: UnitId={unit.Id}, Exp={result.Exp}, Drops={result.Drops.Count}");
            
            // 1. 添加经验值
            if (result.Exp > 0)
            {
                var numeric = unit.GetComponent<NumericComponent>();
                if (numeric != null)
                {
                    long currentExp = numeric.GetByKey(NumericType.Exp);
                    numeric.Set(NumericType.Exp, (int)(currentExp + result.Exp));
                    Log.Info($"添加经验: {result.Exp}, 当前经验: {currentExp + result.Exp}");
                }
            }
            
            // 2. 添加掉落物品到背包
            if (result.Drops != null && result.Drops.Count > 0)
            {
                var bagComponent = unit.GetComponent<BagComponent>();
                if (bagComponent != null)
                {
                    foreach (var drop in result.Drops)
                    {
                        // TODO: 实现 BagComponent.AddItem 方法
                        // bagComponent.AddItem(drop.ConfigId, drop.Count);
                        Log.Info($"添加物品: ConfigId={drop.ConfigId}, Count={drop.Count}");
                    }
                }
                else
                {
                    Log.Warning("背包组件不存在，无法添加掉落物品");
                }
            }
            
            // 3. 更新任务进度
            // TODO: 实现任务系统后添加
            
            // 4. 更新成就统计
            // TODO: 实现成就系统后添加
        }
        
        /// <summary>
        /// 查找 BattleUnit 对应的主世界 Unit
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="battleUnit">战斗单位</param>
        /// <returns>主世界 Unit，如果找不到返回 null</returns>
        public static Unit FindOwnerUnit(Scene scene, BattleUnit battleUnit)
        {
            if (battleUnit.OwnerId == 0)
            {
                // 怪物没有对应的 Unit
                return null;
            }
            
            var unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Error("UnitComponent 不存在");
                return null;
            }
            
            Unit unit = unitComponent.Get(battleUnit.OwnerId);
            if (unit == null)
            {
                Log.Warning($"找不到对应的 Unit: OwnerId={battleUnit.OwnerId}");
            }
            
            return unit;
        }
        
        /// <summary>
        /// 创建怪物 BattleUnit（从配置表）
        /// </summary>
        /// <param name="battle">战斗实例</param>
        /// <param name="configId">配置表 ID</param>
        /// <param name="position">位置</param>
        /// <returns>创建的 BattleUnit</returns>
        public static BattleUnit CreateMonsterFromConfig(Battle battle, int configId, float3 position)
        {
            // 创建 BattleUnit
            BattleUnit battleUnit = battle.AddChild<BattleUnit, int>(configId);
            
            // 设置属性
            battleUnit.OwnerId = 0; // 怪物没有主人
            battleUnit.Camp = UnitCamp.Enemy; // 敌方阵营
            battleUnit.Position = position;
            
            // 添加组件
            battleUnit.AddComponent<NumericComponent>();
            battleUnit.AddComponent<NumericNoticeComponent>();
            
            // 从配置表初始化数值
            InitMonsterNumeric(battleUnit, configId);
            
            Log.Info($"创建怪物 BattleUnit: ConfigId={configId}, BattleUnitId={battleUnit.Id}");
            
            return battleUnit;
        }
        
        /// <summary>
        /// 从配置表初始化怪物数值
        /// </summary>
        private static void InitMonsterNumeric(BattleUnit battleUnit, int configId)
        {
            var config = ConfigHelper.UnitConfig.Get(configId);
            var numeric = battleUnit.GetComponent<NumericComponent>();
            
            if (numeric == null)
            {
                Log.Error("NumericComponent 不存在");
                return;
            }
            
            // TODO: 从配置表读取真实数值
            // 目前使用临时值
            numeric.Set(NumericType.MaxHp, 1000);
            numeric.Set(NumericType.Hp, 1000);
            numeric.Set(NumericType.Attack, 10);
            numeric.Set(NumericType.Defense, 1);
            numeric.Set(NumericType.Speed, 2);
            
            Log.Debug($"初始化怪物数值: ConfigId={configId}");
        }
    }
}
