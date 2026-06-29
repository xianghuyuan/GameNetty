using Cysharp.Threading.Tasks;
using Spine.Unity;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(BattleUnitView))]
    [FriendOf(typeof(BattleUnitView))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleUnitViewSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitView self, UnitCamp camp, float3 position)
        {
            self.Camp = camp;
        }

        [EntitySystem]
        private static void Destroy(this BattleUnitView self)
        {
            self.Initialized = false;
            self.HasPendingPosition = false;
            if (self.GameObject != null)
            {
                UnityEngine.Object.Destroy(self.GameObject);
                self.GameObject = null;
            }
        }

        public static async UniTask InitViewAsync(this BattleUnitView self)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();

            string prefabPath = self.Camp == UnitCamp.Friend
                ? BattleAreaConfig.HeroUnitViewPrefabPath
                : BattleAreaConfig.MonsterUnitViewPrefabPath;

            self.GameObject = await GameModule.Resource.LoadGameObjectAsync(prefabPath);

            if (self.GameObject == null)
            {
                Log.Error($"加载战斗单位 Prefab 失败: {prefabPath}");
                return;
            }

            self.GameObject.name = $"Unit_{unit.Id}";
            self.GameObject.transform.localScale = new Vector3(unit.FaceDirection > 0f ? 1f : -1f, 1f, 1f);
            self.SkeletonAnimation = self.GameObject.GetComponent<SkeletonAnimation>();
            self.InitPresentation();

            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, unit.Position);
            self.GameObject.transform.position = worldPos;

            if (self.HasPendingPosition)
            {
                worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, self.PendingPosition);
                self.GameObject.transform.position = worldPos;
                self.HasPendingPosition = false;
            }

            self.Initialized = true;
        }

        public static void InitPresentation(this BattleUnitView self)
        {
            if (self.GameObject == null) return;
            self.DefaultScale = self.GameObject.transform.localScale;
        }

        /// <summary>
        /// 每帧驱动：基于 Forward 增量移动，FaceDirection 控制视觉翻转，攻击命中检测
        /// </summary>
        [EntitySystem]
        private static void Update(this BattleUnitView self)
        {
            if (!self.Initialized || self.GameObject == null) return;

            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit == null) return;

            // 攻击命中检测
            if (self.CurrentAnimName == AnimAttack && !self.AttackHitTriggered && self.OnAttackHit != null)
            {
                float elapsed = Time.time - self.AttackStartTime;
                if (elapsed >= self.AttackHitTime)
                {
                    self.AttackHitTriggered = true;
                    self.OnAttackHit?.Invoke();
                    self.OnAttackHit = null;
                }
            }

            // 死亡后只做命中检测，不移动不切换动画
            if (unit.IsDead) return;

            // 视觉翻转
            float faceDir = unit.FaceDirection;
            if (faceDir != 0f)
            {
                self.GameObject.transform.localScale = new Vector3(faceDir > 0f ? 1f : -1f, 1f, 1f);
            }

            // Forward != zero 时增量移动
            float speed = unit.GetOrCreateBattleStats()?.Speed ?? 0f;
            bool shouldMove = unit.Forward.x != 0f && speed > 0f;

            self.PlayLocomotionAnimation(shouldMove);
            self.TryFlushPendingHitReact();

            if (!shouldMove) return;

            float dt = Time.deltaTime;
            float moveDelta = speed * dt;

            // 更新逻辑位置
            unit.Position = new float3(unit.Position.x + unit.Forward.x * moveDelta, unit.Position.y, unit.Position.z);

            // 同步视觉位置
            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, unit.Position);
            self.GameObject.transform.position = worldPos;
        }

        /// <summary>
        /// 直接 snap 到指定逻辑位置（服务端同步用）
        /// </summary>
        public static void SetPosition(this BattleUnitView self, float3 position)
        {
            if (!self.Initialized)
            {
                self.PendingPosition = position;
                self.HasPendingPosition = true;
                return;
            }

            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, position);
            self.GameObject.transform.position = worldPos;
        }

        public static void SetColor(this BattleUnitView self, Color color)
        {
            if (self.SkeletonAnimation != null)
            {
                self.SkeletonAnimation.Skeleton.SetColor(color);
            }
            else if (self.GameObject != null)
            {
                SpriteRenderer sr = self.GameObject.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
        }

        public static void SetVisible(this BattleUnitView self, bool visible)
        {
            if (self.GameObject != null) self.GameObject.SetActive(visible);
        }

        private const string AnimIdle = "idle";
        private const string AnimRun = "run";
        private const string AnimAttack = "atk1";
        private const string AnimHit = "hit";
        private const string AnimDeath = "die";
        private const float HitReactMinIntervalSeconds = 0.15f;
        private const float AttackUninterruptibleBufferSeconds = 0.05f;

        /// <summary>
        /// 切换移动/待机动画
        /// </summary>
        public static void PlayLocomotionAnimation(this BattleUnitView self, bool moving)
        {
            if (self.SkeletonAnimation == null) return;

            string targetAnim = moving ? AnimRun : AnimIdle;
            if (self.CurrentAnimName == AnimAttack || self.CurrentAnimName == AnimHit || self.CurrentAnimName == AnimDeath)
                return;

            if (self.CurrentAnimName == targetAnim) return;

            self.CurrentAnimName = targetAnim;
            self.IsMoving = moving;
            self.SkeletonAnimation.AnimationState.SetAnimation(0, targetAnim, true);
        }

        /// <summary>
        /// 播放攻击动画，结束后自动回到当前移动/待机动画。
        /// 在动画进行到 AttackHitTime 时触发 OnAttackHit 回调（伤害结算）。
        /// </summary>
        public static void PlayAttackAnimation(this BattleUnitView self, float hitTimeRatio = 0.5f)
        {
            if (self.SkeletonAnimation == null) return;

            self.CurrentAnimName = AnimAttack;
            self.AttackHitTriggered = false;

            // 计算命中时间：动画时长的 hitTimeRatio 比例处
            float animDuration = self.SkeletonAnimation.AnimationState.SetAnimation(0, AnimAttack, false).Animation.Duration;
            self.AttackHitTime = animDuration * hitTimeRatio;
            self.AttackStartTime = Time.time;
            self.AttackUninterruptibleEndTime = self.AttackStartTime + self.AttackHitTime + AttackUninterruptibleBufferSeconds;

            // 如果没有设置回调，则帧末直接标记命中已触发
            if (self.OnAttackHit == null)
            {
                self.AttackHitTriggered = true;
            }

            self.SkeletonAnimation.AnimationState.GetCurrent(0).Complete += _ =>
            {
                if (self.SkeletonAnimation == null) return;
                if (self.TryFlushPendingHitReact())
                {
                    return;
                }
                self.CurrentAnimName = self.IsMoving ? AnimRun : AnimIdle;
                self.SkeletonAnimation.AnimationState.SetAnimation(0, self.CurrentAnimName, true);
            };
        }

        /// <summary>
        /// 播放受击动画，结束后自动回到当前移动/待机动画
        /// </summary>
        public static void PlayHitAnimation(this BattleUnitView self)
        {
            if (self.SkeletonAnimation == null) return;
            if (self.CurrentAnimName == AnimDeath) return;

            self.CurrentAnimName = AnimHit;
            self.LastHitReactTime = Time.time;
            self.PendingHitReact = false;

            var entry = self.SkeletonAnimation.AnimationState.SetAnimation(0, AnimHit, false);
            entry.Complete += _ =>
            {
                if (self.SkeletonAnimation == null) return;
                if (self.CurrentAnimName == AnimDeath) return;
                BattleUnit unit = self.GetParent<BattleUnit>();
                if (unit != null && unit.IsDead) return;
                if (self.TryFlushPendingHitReact())
                {
                    return;
                }
                self.CurrentAnimName = self.IsMoving ? AnimRun : AnimIdle;
                self.SkeletonAnimation.AnimationState.SetAnimation(0, self.CurrentAnimName, true);
            };
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        public static void PlayDeathAnimation(this BattleUnitView self)
        {
            if (self.SkeletonAnimation == null) return;

            self.CurrentAnimName = AnimDeath;
            self.SkeletonAnimation.AnimationState.SetAnimation(0, AnimDeath, false);
        }

        /// <summary>
        /// 播放攻击动画并注册命中回调（伤害在动画命中点触发）
        /// </summary>
        public static void PlayAttackFeedback(this BattleUnitView self, System.Action onHit, float hitTimeRatio = 0.5f)
        {
            self.OnAttackHit = onHit;
            self.PlayAttackAnimation(hitTimeRatio);
        }

        public static void PlayHitFeedback(this BattleUnitView self, int damage)
        {
            // 死亡状态下不播放受击动画，死亡动画优先
            BattleUnit unit = self.GetParent<BattleUnit>();
            if (unit != null && unit.IsDead) return;

            if (self.ShouldDeferHitReact())
            {
                self.PendingHitReact = true;
                return;
            }

            if (Time.time - self.LastHitReactTime < HitReactMinIntervalSeconds)
            {
                self.PendingHitReact = true;
                return;
            }

            self.PlayHitAnimation();
        }

        private static bool ShouldDeferHitReact(this BattleUnitView self)
        {
            return self.CurrentAnimName == AnimAttack && Time.time <= self.AttackUninterruptibleEndTime;
        }

        private static bool TryFlushPendingHitReact(this BattleUnitView self)
        {
            if (!self.PendingHitReact)
            {
                return false;
            }

            if (self.ShouldDeferHitReact())
            {
                return false;
            }

            if (Time.time - self.LastHitReactTime < HitReactMinIntervalSeconds)
            {
                return false;
            }

            self.PlayHitAnimation();
            return true;
        }
    }
}
