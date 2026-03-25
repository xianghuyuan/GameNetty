using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleUnitView : Entity, IAwake<UnitCamp, float3>, IDestroy
    {
        public UnitCamp Camp { get; set; }
        public UnityEngine.GameObject GameObject { get; set; }
        public Color DefaultColor { get; set; } = Color.white;
        public Vector3 DefaultScale { get; set; } = Vector3.one;
        public Sequence PresentationTweener { get; set; }
        public float3 Position { get; set; }
    }
}
