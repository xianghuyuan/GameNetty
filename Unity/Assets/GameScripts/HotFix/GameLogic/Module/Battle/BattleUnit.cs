using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 战斗单位实体
    /// 战斗中的角色副本，与主世界 Unit 隔离
    /// </summary>
    [ChildOf(typeof(Battle))]
    public partial class BattleUnit : Entity, IAwake<int>
    {
        /// <summary>
        /// 配置表 ID
        /// </summary>
        public int ConfigId { get; set; }
        
        /// <summary>
        /// 关联的主世界 Unit ID（重要：用于战斗结束后同步数据）
        /// 怪物的 OwnerId 为 0
        /// </summary>
        public long OwnerId { get; set; }
        
        /// <summary>
        /// 阵营
        /// </summary>
        public UnitCamp Camp { get; set; }
        
        /// <summary>
        /// 位置
        /// </summary>
        public float3 Position { get; set; }
        
        /// <summary>
        /// 朝向
        /// </summary>
        public float3 Forward { get; set; }
        
        /// <summary>
        /// 是否已死亡
        /// </summary>
        public bool IsDead { get; set; }
    }
}
