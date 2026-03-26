using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(BattleUnit))]
    public class BattleUnitView : Entity, IAwake<UnitCamp, float3>, IDestroy
    {
        public UnitCamp Camp { get; set; }
        public GameObject GameObject { get; set; }
        public Color DefaultColor { get; set; } = Color.white;
        public Vector3 DefaultScale { get; set; } = Vector3.one;
        public Sequence PresentationTweener { get; set; }
        public Tweener MoveTweener { get; set; }

        /// <summary>
        /// 是否已完成初始化（Prefab 已加载）
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// 初始化完成前缓存的待应用位置
        /// </summary>
        public float3 PendingPosition { get; set; }

        /// <summary>
        /// 初始化完成前缓存的时间
        /// </summary>
        public float PendingDuration { get; set; }
        /// <summary>
        /// 是否有待应用的位置更新
        /// </summary>
        public bool HasPendingPosition { get; set; }
    }
}
