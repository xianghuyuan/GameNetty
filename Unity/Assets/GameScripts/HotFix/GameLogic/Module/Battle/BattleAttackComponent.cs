using System.Collections.Generic;

namespace ET
{
    public enum BattleAttackPayloadType
    {
        None = 0,
        VehicleBuff = 1,
    }

    public enum BattleAttackDeliveryType
    {
        Instant = 1,
        Projectile = 2,
        Area = 3,
        Beam = 4,
    }

    public class BattleAttackRuntime
    {
        public long AttackRuntimeId;
        public int SourceConfigId;
        public int Level = 1;
        public int BuffSlotCount;
        public int CooldownMs = 1000;
        public float AttackRange = 1.5f;
        public float BaseDamage;
        public float WhiteAttackRatio;
        public bool CanMoveCast;
        public BattleAttackDeliveryType DeliveryType = BattleAttackDeliveryType.Instant;
        public BattleAttackPayloadType PayloadType = BattleAttackPayloadType.VehicleBuff;
        /// <summary>
        /// 槽位配置：存放 EmitterEffectPackConfig.Id。
        /// </summary>
        public List<int> EffectPackIds = new();

        /// <summary>
        /// 命中执行配置：由效果包展开得到的 BuffGroupConfig.Id。
        /// </summary>
        public List<int> BuffGroupIds = new();
    }

    /// <summary>
    /// 通用攻击执行组件，挂载在 BattleUnit 上。
    /// 玩家载具攻击和小怪普通攻击都转成统一的运行时攻击定义。
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleAttackComponent : Entity, IAwake, IDestroy
    {
        public long CurrentTargetId { get; set; }

        public List<BattleAttackRuntime> Attacks { get; } = new();

        /// <summary>发射器冷却结束时间：AttackRuntimeId -> ClientNowMs</summary>
        public Dictionary<long, long> EmitterCooldownEndTimeById { get; } = new();

        /// <summary>不可移动施法锁结束时间。</summary>
        public long CastMoveLockEndTime { get; set; }
    }
}
