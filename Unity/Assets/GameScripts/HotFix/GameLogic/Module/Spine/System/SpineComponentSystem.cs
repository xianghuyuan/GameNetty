using System;
using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;
using GameLogic;
namespace ET
{
    [EntitySystemOf(typeof(SpineComponent))]
    [FriendOf(typeof(SpineComponent))]
    public static partial class SpineComponentSystem
    {
        [EntitySystem]
        private static async void Awake(this SpineComponent self, string resourcePath)
        {
            try
            {
                // 加载Spine预制体
                GameObject go = await UIModule.Resource.LoadGameObjectAsync(resourcePath,
                    UnityEngine.GameObject.Find("Global/Units")?.transform);

                self.GameObject = go;
                self.SkeletonAnimation = go.GetComponent<SkeletonAnimation>();

                // 同步Unit的位置到GameObject
                Unit unit = self.GetParent<Unit>();
                go.transform.position = unit.Position;
                go.transform.rotation = unit.Rotation;
            }
            catch (Exception e)
            {
                throw; // TODO 处理异常
            }
        }

        [EntitySystem]
        private static void Destroy(this SpineComponent self)
        {
        }

        // 播放动画
        public static void PlayAnimation(this SpineComponent self, string animationName, bool loop = true)
        {
            if (self.SkeletonAnimation == null) return;

            self.CurrentAnimation = animationName;
            self.SkeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
        }

        // 设置皮肤
        public static void SetSkin(this SpineComponent self, string skinName)
        {
            if (self.SkeletonAnimation == null) return;

            self.SkeletonAnimation.Skeleton.SetSkin(skinName);
            self.SkeletonAnimation.Skeleton.SetSlotsToSetupPose();
        }

        // 更新位置
        public static void UpdatePosition(this SpineComponent self, float3 position)
        {
            if (self.GameObject != null)
            {
                self.GameObject.transform.position = position;
            }
        }

        // 更新旋转
        public static void UpdateRotation(this SpineComponent self, quaternion rotation)
        {
            if (self.GameObject != null)
            {
                self.GameObject.transform.rotation = rotation;
            }
        }
    }
}