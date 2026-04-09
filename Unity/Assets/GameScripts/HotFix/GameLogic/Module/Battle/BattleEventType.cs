using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 战斗事件定义
    /// </summary>

    /// <summary>
    /// 战斗开始事件
    /// </summary>
    [BridgeToTE]
    public struct BattleStart
    {
        public Battle Battle;
    }

    /// <summary>
    /// 战斗结束事件
    /// </summary>
    [BridgeToTE]
    public struct BattleEnd
    {
        public Battle Battle;
        public BattleResult Result;
    }

    /// <summary>
    /// 波次开始事件
    /// </summary>
    public struct WaveStart
    {
        public Battle Battle;
        public int WaveNumber;
    }

    /// <summary>
    /// 波次完成事件
    /// </summary>
    public struct WaveComplete
    {
        public Battle Battle;
        public int WaveNumber;
    }

    /// <summary>
    /// 战斗单位死亡事件
    /// </summary>
    [BridgeToTE]
    public struct BattleUnitDead
    {
        public BattleUnit BattleUnit;
    }

    /// <summary>
    /// 战斗单位移动开始事件
    /// </summary>
    public struct BattleUnitMoveStarted
    {
        public BattleUnit Unit;
        public float3 From;
        public float3 To;
        public float Duration;
    }

    /// <summary>
    /// 战斗单位移动停止事件
    /// </summary>
    public struct BattleUnitMoveStopped
    {
        public BattleUnit Unit;
        public float3 FinalPosition;
    }

    /// <summary>
    /// 战斗单位位置同步事件
    /// </summary>
    public struct BattleUnitPositionSynced
    {
        public BattleUnit Unit;
        public float3 Position;
    }

    /// <summary>
    /// 战斗单位释放技能事件
    /// </summary>
    [BridgeToTE]
    public struct BattleUnitSkillCast
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 战斗单位受到伤害事件
    /// </summary>
    [BridgeToTE]
    public struct BattleUnitDamaged
    {
        public BattleUnit Unit;
        public long AttackerId;
        public int Damage;
        public bool IsCrit;
    }

    /// <summary>
    /// 战斗单位复活事件（服务端纠错：客户端乐观击杀后服务端验证不通过）
    /// </summary>
    public struct BattleUnitRevived
    {
        public BattleUnit BattleUnit;
    }

    /// <summary>
    /// 杂兵波次生成完成事件（客户端本地）
    /// </summary>
    public struct MinionWaveSpawned
    {
        public Battle Battle;
        public int WaveId;
    }

    /// <summary>
    /// 杂兵波次全部死亡事件（客户端本地）
    /// </summary>
    public struct MinionWaveAllDead
    {
        public Battle Battle;
        public int WaveId;
    }
}
