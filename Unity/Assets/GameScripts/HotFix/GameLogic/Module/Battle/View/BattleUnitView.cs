using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ChildOf(typeof(BattleUnitViewComponent))]
    public class BattleUnitView : Entity, IAwake<UnitCamp, float3>, IDestroy
    {
        public long UnitId { get; set; }
        public UnitCamp Camp { get; set; }
        public UnityEngine.GameObject GameObject { get; set; }
        public Color DefaultColor { get; set; } = Color.white;
        public Vector3 DefaultScale { get; set; } = Vector3.one;
        /// <summary>
        /// 动画handler
        /// </summary>
        public Sequence PresentationTweener  { get; set; }
        public float3 Position { get; set; }
    }
}
