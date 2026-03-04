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
                    numericComponent.Set(NumericType.Speed, 6f); // 速度是6米每秒
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
        
        // 创建玩家英雄
        public static BattleUnit CreateHero(BattleRoom battleRoom, long playerId, int configId, Vector3 position)
        {
            BattleUnit unit = battleRoom.AddChild<BattleUnit, int>(configId);
            unit.Camp = UnitCamp.Friend;
            unit.OwnerId = playerId;
            unit.Position = position;
        
            // 添加组件
            unit.AddComponent<NumericComponent>();
            unit.AddComponent<NumericNoticeComponent>();
            
            // 初始化属性
            InitUnitNumeric(unit, configId);
        
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
        
            // 初始化属性
            InitUnitNumeric(unit, configId);
        
            return unit;
        }
        // 初始化数值
        private static void InitUnitNumeric(BattleUnit unit, int configId)
        {
            // 从配置表读取初始属性
            var config = UnitConfigCategory.Instance.Get(configId);
            var numeric = unit.GetComponent<NumericComponent>();
        
            numeric.Set(NumericType.MaxHp, 1000);
            numeric.Set(NumericType.Hp, 1000);
            numeric.Set(NumericType.Attack, 10);
            numeric.Set(NumericType.Defense, 1);
            numeric.Set(NumericType.Speed, 2);
        }
    }
}