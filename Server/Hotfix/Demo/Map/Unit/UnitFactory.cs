using System;
using System.Numerics;
using Unity.Mathematics;

namespace ET.Server
{
    public static partial class UnitFactory
    {
        /// <summary>
        /// 只有新创建的角色才会走这里，否则从缓存服拿
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="id"></param>
        /// <param name="unitType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async ETTask<Unit> Create(Scene scene, long id, UnitType unitType)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            switch (unitType)
            {
                case UnitType.Player:
                {
                    //UnitConfig的配置
                    Unit unit = unitComponent.AddChildWithId<Unit, int>(id, 1001);
                    unit.Position = new float3(-10, 0, -10);
			
                    NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
                    numericComponent.Set(NumericType.Speed, 1f);
                    numericComponent.Set(NumericType.AOI, 15000); // 视野15米
                    numericComponent.Set(NumericType.MaxHpBase, 100);
                    numericComponent.Set(NumericType.HpBase, 100);
                    numericComponent.Set(NumericType.MaxHp, 100);
                    numericComponent.Set(NumericType.Hp, 100);
                    numericComponent.Set(NumericType.AttackBase, 10);
                    numericComponent.Set(NumericType.Attack, 10);
                    numericComponent.Set(NumericType.DefenseBase, 5);
                    numericComponent.Set(NumericType.Defense, 5);
                    numericComponent.Set(NumericType.Level, 1);
                    
                    unit.AddComponent<KnapsackComponent>();//添加背包组件
                    DBComponent dbComponent = unit.Root().GetComponent<DBManagerComponent>().GetZoneDB(unit.Zone());
                    var roleInfos = await dbComponent.Query<RoleInfo>(d => d.Id == id);
                    unit.AddChild(roleInfos[0]);
                    unitComponent.Add(unit);
                    // 加入aoi
                    // unit.AddComponent<AOIEntity, int, float3>(9 * 1000, unit.Position);
                    return unit;
                }
                case UnitType.Enemy:
                {
                    // 创建敌人
                    Unit unit = unitComponent.AddChildWithId<Unit, int>(id, 2001);
                    unit.Position = new float3(10, 0, 10); // 敌人初始位置
                    
                    // 设置敌人属性
                    NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
                    numericComponent.Set(NumericType.MaxHp, 10); // 最大血量10
                    numericComponent.Set(NumericType.Hp, 10); // 当前血量10
                    numericComponent.Set(NumericType.Speed, 3f); // 速度3米每秒
                    numericComponent.Set(NumericType.AOI, 15000); // 视野15米
                    numericComponent.Set(NumericType.Attack, 5); // 攻击力
                    unitComponent.Add(unit);
                    // 加入aoi
                    // unit.AddComponent<AOIEntity, int, float3>(9 * 1000, unit.Position);
                    return unit;
                }
                default:
                    throw new Exception($"not such unit type: {unitType}");
            }
        }
        
        // 创建玩家英雄（从持久化 Unit 复制属性到战斗单位）
        public static BattleUnit CreateHero(BattleRoom battleRoom, Unit playerUnit, Vector3 position)
        {
            BattleUnit unit = battleRoom.AddChild<BattleUnit, int>(playerUnit.ConfigId);
            unit.Camp = UnitCamp.Friend;
            unit.OwnerId = playerUnit.Id;
            unit.Position = position;

            // 添加组件
            unit.AddComponent<NumericComponent>();
            unit.AddComponent<NumericNoticeComponent>();
            unit.AddComponent<BuffComponent>();

            // 添加战斗组件
            BattleUnitCombatComponent combatComp = unit.AddComponent<BattleUnitCombatComponent>();

            // 从持久化单位复制战斗属性
            CopyNumericFromPlayer(unit, playerUnit);
            unit.GetOrCreateBattleStats().LoadFromNumeric(unit.GetComponent<NumericComponent>());

            Log.Debug($"[Hero] Speed={unit.GetComponent<NumericComponent>()?.GetAsFloat(NumericType.Speed)} MaxHp={unit.GetComponent<NumericComponent>()?.GetAsInt(NumericType.MaxHp)} Attack={unit.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Attack)} Defense={unit.GetComponent<NumericComponent>()?.GetAsInt(NumericType.Defense)}");

            // 从 UnitCombatConfig 读取技能配置初始化战斗参数（冷却、射程）
            ApplyNormalAttackConfigFromCombatConfig(unit, combatComp);

            unit.AddComponent<PlayerCombatModeComponent>();

            return unit;
        }

        // 创建怪物
        public static BattleUnit CreateMonster(BattleRoom battleRoom, int configId, Vector3 position)
        {
            BattleUnit unit = battleRoom.AddChild<BattleUnit, int>(configId);
            unit.Camp = UnitCamp.Enemy;
            unit.OwnerId = 0;
            unit.Position = position;

            // 添加组件
            unit.AddComponent<NumericComponent>();
            unit.AddComponent<NumericNoticeComponent>();
            unit.AddComponent<BattleMoveComponent>();
            unit.AddComponent<BuffComponent>();

            // 添加战斗组件
            BattleUnitCombatComponent combatComp = unit.AddComponent<BattleUnitCombatComponent>();
            combatComp.AttackCooldown = 1500;
            combatComp.AttackRange = 2.0f;
            unit.AddComponent<BattleActionDecisionComponent>();

            // 初始化属性
            InitMonsterNumeric(unit, configId);
            unit.GetOrCreateBattleStats().LoadFromNumeric(unit.GetComponent<NumericComponent>());

            // 从 UnitCombatConfig 读取技能配置初始化战斗参数（冷却、射程）
            ApplyNormalAttackConfigFromCombatConfig(unit, combatComp);

            // 注册到空间网格
            BattleSpatialGrid spatialGrid = battleRoom.GetComponent<BattleSpatialGrid>();
            spatialGrid?.Insert(unit.Id, unit.Position.X);

            // 标记Boss并注册到Boss同步组件
            MonsterUnitConfig monsterConfig = MonsterUnitConfigCategory.Instance.GetOrDefault(configId);
            if (monsterConfig != null)
            {
                unit.IsBoss = monsterConfig.Type == 3;
            }
            if (unit.IsBoss)
            {
                EventSystem.Instance.Publish(battleRoom.Root(), new BossCreatedEvent
                {
                    BattleRoom = battleRoom,
                    BossUnitId = unit.Id,
                });
            }

            return unit;
        }

        /// <summary>
        /// 创建杂兵（轻量级实体）- 用于客户端本地刷怪模式下，
        /// 服务端仅保留碰撞检测所需的最小数据。
        /// 与 CreateMonster 不同：不挂载决策组件，减少服务端开销。
        /// </summary>
        public static BattleUnit CreateMinion(BattleRoom battleRoom, int configId, Vector3 position, long localUnitId)
        {
            BattleUnit unit = battleRoom.AddChildWithId<BattleUnit, int>(localUnitId, configId);
            unit.Camp = UnitCamp.Enemy;
            unit.OwnerId = 0;
            unit.Position = position;

            unit.AddComponent<NumericComponent>();
            unit.AddComponent<BuffComponent>();

            InitMonsterNumeric(unit, configId);
            unit.GetOrCreateBattleStats().LoadFromNumeric(unit.GetComponent<NumericComponent>());

            // 注册到空间网格
            BattleSpatialGrid spatialGrid = battleRoom.GetComponent<BattleSpatialGrid>();
            spatialGrid?.Insert(unit.Id, unit.Position.X);

            return unit;
        }

        /// <summary>
        /// 从持久化玩家单位复制战斗属性到战斗单位
        /// </summary>
        private static void CopyNumericFromPlayer(BattleUnit battleUnit, Unit playerUnit)
        {
            NumericComponent srcNumeric = playerUnit.GetComponent<NumericComponent>();
            NumericComponent dstNumeric = battleUnit.GetComponent<NumericComponent>();
            if (srcNumeric == null || dstNumeric == null)
            {
                return;
            }

            dstNumeric.Set(NumericType.MaxHp, srcNumeric.GetAsInt(NumericType.MaxHp));
            dstNumeric.Set(NumericType.Hp, srcNumeric.GetAsInt(NumericType.Hp));
            dstNumeric.Set(NumericType.Attack, srcNumeric.GetAsInt(NumericType.Attack));
            dstNumeric.Set(NumericType.Defense, srcNumeric.GetAsInt(NumericType.Defense));
            dstNumeric.Set(NumericType.Speed, srcNumeric.GetAsFloat(NumericType.Speed));
        }

        // 初始化怪物数值（从 MonsterUnitConfig 读取）
        private static void InitMonsterNumeric(BattleUnit unit, int configId)
        {
            var config = MonsterUnitConfigCategory.Instance.Get(configId);
            var numeric = unit.GetComponent<NumericComponent>();

            numeric.Set(NumericType.MaxHp, config.MaxHp);
            numeric.Set(NumericType.Hp, config.MaxHp);
            numeric.Set(NumericType.Attack, config.Attack);
            numeric.Set(NumericType.Defense, config.Defense);
            numeric.Set(NumericType.Speed, config.Speed);
        }

        /// <summary>
        /// 从 UnitCombatConfig 读取普攻技能配置，初始化战斗参数（攻击冷却、攻击范围）。
        /// </summary>
        private static void ApplyNormalAttackConfigFromCombatConfig(BattleUnit unit, BattleUnitCombatComponent combat)
        {
            if (unit == null || combat == null)
            {
                return;
            }

            UnitCombatConfig unitCombatConfig = UnitCombatConfigCategory.Instance.GetOrDefault(unit.ConfigId);
            if (unitCombatConfig == null)
            {
                return;
            }

            EmitterConfig skillConfig = unitCombatConfig != null ? EmitterConfigCategory.Instance.GetOrDefault(unitCombatConfig.NormalAttackEmitterId) : null;
            SkillTargetingConfig targetingConfig = skillConfig != null ? SkillTargetingConfigCategory.Instance.GetOrDefault(skillConfig.TargetingConfigId) : null;

            if (skillConfig != null)
            {
                combat.AttackCooldown = skillConfig.CooldownMs;
            }

            if (targetingConfig != null)
            {
                combat.AttackRange = targetingConfig.CastRange;
            }
        }
    }
}
