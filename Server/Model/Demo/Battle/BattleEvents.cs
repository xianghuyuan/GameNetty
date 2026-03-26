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
        public SkillEffectGroupConfig EffectGroup;
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
}
