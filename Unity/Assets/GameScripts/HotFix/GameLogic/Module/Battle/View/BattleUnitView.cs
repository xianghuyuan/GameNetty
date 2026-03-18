using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单个战斗单位表现
    /// 管理 2D 场景表现（SpriteRenderer）
    /// </summary>
    [ChildOf(typeof(BattleUnitViewComponent))]
    public class BattleUnitView : Entity, IAwake<UnitCamp, float3>, IDestroy
    {
        /// <summary>
        /// 关联的战斗单位 ID
        /// </summary>
        public long UnitId { get; set; }
        
        /// <summary>
        /// 阵营
        /// </summary>
        public UnitCamp Camp { get; set; }
        
        /// <summary>
        /// 2D 场景 GameObject
        /// </summary>
        public UnityEngine.GameObject GameObject { get; set; }
        
        /// <summary>
        /// SpriteRenderer 组件
        /// </summary>
        public UnityEngine.SpriteRenderer SpriteRenderer { get; set; }
        
        /// <summary>
        /// 当前位置
        /// </summary>
        public float3 Position { get; set; }
    }
}
