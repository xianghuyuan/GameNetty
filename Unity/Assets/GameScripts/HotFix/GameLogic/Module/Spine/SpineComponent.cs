//管理Spine动画的组件
using UnityEngine;
using Spine.Unity;

namespace ET
{
    public class SpineComponent : Entity,IAwake<string>,IDestroy
    {
        public GameObject GameObject;
        public SkeletonAnimation SkeletonAnimation;
        // 当前播放的动画
        public string CurrentAnimation { get; set; }
    }
}