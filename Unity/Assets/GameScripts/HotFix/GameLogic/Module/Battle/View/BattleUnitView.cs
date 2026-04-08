using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleUnitView : Entity, IAwake<UnitCamp, float3>, IUpdate, IDestroy
    {
        public UnitCamp Camp { get; set; }
        public GameObject GameObject { get; set; }
        public SkeletonAnimation SkeletonAnimation { get; set; }
        public Vector3 DefaultScale { get; set; } = Vector3.one;
        public bool Initialized { get; set; }
        public float3 PendingPosition { get; set; }
        public bool HasPendingPosition { get; set; }
        public string CurrentAnimName { get; set; }
        public bool IsMoving { get; set; }
    }
}
