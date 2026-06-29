namespace ET.Server
{
    /// <summary>
    /// 技能命中事件 - 当技能命中目标时触发
    /// </summary>
    public struct SkillHitEvent
    {
        public BattleUnit Attacker;
        public BattleUnit Target;
        public int SkillId;
        public BuffGroupConfig BuffGroup;
    }

    /// <summary>
    /// Buff执行事件 - 当buff需要执行效果时发布，由各效果类型的事件处理器响应。
    /// BuffComponentSystem 通过此事件解耦，避免静态类环形依赖。
    /// </summary>
    public struct BuffExecuteEvent
    {
        public BattleUnit Target;
        public BuffEntity BuffEntity;
    }

    /// <summary>
    /// 伤害事件 - 当单位受到伤害时触发
    /// </summary>
    public struct DamageEvent
    {
        public BattleUnit Attacker;
        public BattleUnit Target;
        public int Damage;
        public int DamageType;
        public int SkillId;
        public long CasterId;
    }

    /// <summary>
    /// 单位死亡事件 - 当单位因伤害死亡时触发
    /// </summary>
    public struct UnitDeadEvent
    {
        public BattleUnit Target;
        public long KillerId;
    }
    
    /// <summary>
    /// 冻结事件 - 当单位被冻结时触发
    /// </summary>
    public struct FreezeEvent
    {
        public BattleUnit Target;
        public long SourceId;
        public int DurationMs;
    }
    
    /// <summary>
    /// 冻结结束事件 - 当单位冻结结束时触发
    /// </summary>
    public struct FreezeEndEvent
    {
        public BattleUnit Target;
    }
    
    /// <summary>
    /// 冻结开始事件 - 当单位被冻结时触发（用于通知移动组件中断）
    /// </summary>
    public struct FreezeStartEvent
    {
        public BattleUnit Target;
        public int DurationMs;
    }
    
    /// <summary>
    /// 击退事件 - 当单位被击退时触发
    /// </summary>
    public struct KnockbackEvent
    {
        public BattleUnit Target;
        public BattleUnit Attacker;
        public float Distance;
        public float Direction;
        public long CasterId;
    }

    /// <summary>
    /// 到达目标位置事件 - 当移动组件到达目标位置时触发
    /// </summary>
    public struct ReachTargetEvent
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 请求移动事件 - 决策组件发出，移动组件响应
    /// </summary>
    public struct RequestMoveEvent
    {
        public BattleUnit Unit;
        public System.Numerics.Vector3 TargetPosition;
        public long ChaseTargetId;
        public float ChaseAttackRange;
    }

    /// <summary>
    /// 请求停止移动事件
    /// </summary>
    public struct RequestStopMoveEvent
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 请求施法事件 - 决策组件发出，战斗组件响应
    /// </summary>
    public struct RequestCastEvent
    {
        public BattleUnit Unit;
        public int SkillId;
        public long TargetId;
    }

    /// <summary>
    /// 目标变化事件 - 当决策组件切换目标时发布
    /// </summary>
    public struct TargetChangedEvent
    {
        public long UnitId;
        public long OldTargetId;
        public long NewTargetId;
    }

    /// <summary>
    /// 施法结束事件 - 当施法锁定结束时触发，通知决策组件重新决策
    /// </summary>
    public struct CastingEndEvent
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 投射物命中事件 - 当投射物命中目标时触发
    /// </summary>
    public struct ProjectileHitEvent
    {
        public BattleUnit Projectile;
        public BattleUnit Target;
        public long CasterId;
        public int SkillId;
    }

    /// <summary>
    /// 投射物销毁事件 - 当投射物被销毁时触发（到达最大距离、命中非穿透目标等）
    /// </summary>
    public struct ProjectileDestroyEvent
    {
        public BattleUnit Projectile;
        public long CasterId;
        public int SkillId;
    }

    /// <summary>
    /// 投射物发射事件 - 当投射物被创建并发射时触发
    /// </summary>
    public struct ProjectileLaunchEvent
    {
        public BattleUnit Projectile;
        public long CasterId;
        public int SkillId;
        public float Direction;
    }

    /// <summary>
    /// 请求生成投射物事件 - 由 BattleSkillHelper 发布，UnitFactory 响应
    /// </summary>
    public struct SpawnProjectileEvent
    {
        public BattleUnit Caster;
        public EmitterConfig EmitterConfig;
        public BattleUnit Target;
    }

    /// <summary>
    /// 注册判定框事件 - 客户端发起攻击时，将判定框注册到时间轴队列
    /// </summary>
    public struct RegisterHitBoxEvent
    {
        public BattleUnit Caster;
        public int SkillId;
        public long TargetId;
        public float HitStartTick;
        public float HitEndTick;
        public float HitBoxMinX;
        public float HitBoxMaxX;
    }

    /// <summary>
    /// Boss创建事件 - 当Boss怪物被创建时触发，用于解耦 UnitFactory 与 BossSyncComponentSystem 的环形依赖
    /// </summary>
    public struct BossCreatedEvent
    {
        public BattleRoom BattleRoom;
        public long BossUnitId;
    }

    /// <summary>
    /// 位置校正事件 - 当服务端检测到位置误差过大时，下发强制校正
    /// 客户端收到后平滑滑过去，而非瞬移
    /// </summary>
    public struct ForceCorrectPosEvent
    {
        public BattleUnit Target;
        public System.Numerics.Vector3 CorrectPosition;
        public float CorrectionThreshold;
    }

    /// <summary>
    /// Buff移除事件 - 当buff过期或被主动移除时触发。
    /// 用于在buff结束时还原属性（如AttackBuff/DefenseBuff的临时加成）。
    /// </summary>
    public struct BuffRemoveEvent
    {
        public BattleUnit Target;
        public BuffEntity BuffEntity;
    }
}
