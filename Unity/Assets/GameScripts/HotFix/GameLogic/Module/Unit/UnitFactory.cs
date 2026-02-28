using System;
using System.Numerics;
using Unity.Mathematics;

namespace ET
{
    public static partial class UnitFactory
    {
        /// <summary>
        /// 从服务器消息创建Unit
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="unitInfo">服务器发送的Unit信息</param>
        /// <returns>创建的Unit实体</returns>
        public static Unit Create(Scene scene, UnitInfo unitInfo)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            
            // 使用UnitInfo中的数据创建Unit
            Unit unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.UnitId, unitInfo.ConfigId);
            unit.Position = unitInfo.Position;
            unit.Forward = unitInfo.Forward;
            unit.UnitType = (UnitType)unitInfo.Type;
            
            // 添加数值组件并从KV字典初始化
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            if (unitInfo.KV != null && unitInfo.KV.Count > 0)
            {
                foreach (var kv in unitInfo.KV)
                {
                    numericComponent[kv.Key] = kv.Value;
                }
            }
            
            // 如果是玩家类型，添加背包组件
            if (unit.UnitType == UnitType.Player)
            {
                unit.AddComponent<BagComponent>();
            }
            
            return unit;
        }
    }
}