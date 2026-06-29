using System;
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

        /// <summary>
        /// 攻击命中点时间（秒），从攻击动画开始到触发伤害的时间
        /// </summary>
        public float AttackHitTime { get; set; }

        /// <summary>
        /// 攻击命中回调，在攻击动画达到命中点时触发
        /// </summary>
        public Action OnAttackHit { get; set; }

        /// <summary>
        /// 攻击命中是否已触发（防止重复触发）
        /// </summary>
        public bool AttackHitTriggered { get; set; }

        /// <summary>
        /// 攻击动画开始时间（Time.time）
        /// </summary>
        public float AttackStartTime { get; set; }

        /// <summary>
        /// 死亡取消标记：服务端纠错复活时置 true，阻止延迟销毁
        /// </summary>
        public bool DeathCancelled { get; set; }

        /// <summary>
        /// 受击动画待播放标记（攻击关键窗口内延后播放）
        /// </summary>
        public bool PendingHitReact { get; set; }

        /// <summary>
        /// 上次播放受击动画的时间（Time.time）
        /// </summary>
        public float LastHitReactTime { get; set; }

        /// <summary>
        /// 攻击不可被受击打断的结束时间（Time.time）
        /// </summary>
        public float AttackUninterruptibleEndTime { get; set; }
    }
}
