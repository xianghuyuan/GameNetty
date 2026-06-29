using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 战斗构筑运行时。进入战斗时由玩家载具、发射器配置和效果包配置解析得到。
    /// </summary>
    public sealed class BuildRuntime
    {
        public List<EmitterRuntime> Emitters { get; } = new();
    }

    /// <summary>
    /// 单个发射器在战斗中的最终可执行参数。
    /// </summary>
    public sealed class EmitterRuntime
    {
        public long RuntimeId;
        public int EmitterConfigId;
        public int Level = 1;
        public int BuffSlotCount;
        public int CooldownMs = 1000;
        public float AttackRange = 1.5f;
        public float AttackHitRatio = 0.5f;
        public float BaseDamage;
        public float WhiteAttackRatio;
        public float WhiteDamageMultiplier = 1.0f;
        public bool CanMoveCast;
        public BattleAttackDeliveryType DeliveryType = BattleAttackDeliveryType.Instant;
        public BattleAttackPayloadType PayloadType = BattleAttackPayloadType.VehicleBuff;
        public List<int> EffectPackIds = new();
        public List<int> BuffGroupIds = new();
    }
}
