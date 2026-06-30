using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 载具运行时数据 - 可序列化，持久化到玩家数据
    /// </summary>
    public class VehicleData
    {
        public long VehicleId;
        public int VehicleConfigId;
        public int Level = 1;
        public int BuffSlotCount;
        /// <summary>
        /// 槽位里存放 EmitterEffectPackConfig.Id。效果包可展开成发射器属性修饰和命中 BuffGroup。
        /// </summary>
        public List<int> SlottedEffectPackIds = new();

        /// <summary>
        /// 展开后的命中 BuffGroupConfig.Id。保留给既有命中执行器和调试怪物路径使用。
        /// </summary>
        public List<int> SlottedBuffIds = new();
        public int AttackCooldownMs = 1000;
        public float AttackRange = 2.0f;
        public float BaseDamage;
        public float WhiteAttackRatio;
        /// <summary>
        /// 当前载具是否允许边移动边释放。
        /// 后续可改为从 VehicleConfig 同步到运行时数据。
        /// </summary>
        public bool CanMoveCast;
        public VehicleState State = VehicleState.Stored;
    }

    public enum VehicleState
    {
        None = 0,
        //已装备
        Equipped = 1,
        //已拥有
        Stored = 2,
    }
}
