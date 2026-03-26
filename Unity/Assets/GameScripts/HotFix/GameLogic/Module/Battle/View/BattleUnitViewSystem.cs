using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(BattleUnitView))]
    [FriendOf(typeof(BattleUnitView))]
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
            self.KillMoveTween();
            self.KillTweens();
            if (self.GameObject != null)
            {
                UnityEngine.Object.Destroy(self.GameObject);
                self.GameObject = null;
            }
        }

        public static async UniTask InitViewAsync(this BattleUnitView self)
        {
            BattleUnit unit = self.GetParent<BattleUnit>();

            self.GameObject = await GameModule.Resource.LoadGameObjectAsync(BattleAreaConfig.BattleUnitViewPrefabPath);

            if (self.GameObject == null)
            {
                Log.Error($"加载战斗单位 Prefab 失败: {BattleAreaConfig.BattleUnitViewPrefabPath}");
                return;
            }

            self.GameObject.name = $"Unit_{unit.Id}";

            self.GameObject.transform.localScale = new Vector3(self.Camp == UnitCamp.Friend ? 1f : -1f, 1f, 1f);
            self.InitPresentation();

            
            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, unit.Position);
            self.GameObject.transform.position = worldPos;
            if (self.HasPendingPosition)//存在推送消息
            {
                self.UpdatePosition(self.PendingPosition,self.PendingDuration);
                self.HasPendingPosition = false;
            }

            self.Initialized = true;

            Log.Info($"创建单位表现 UnitId={unit.Id}, Camp={self.Camp}, Pos={self.GameObject.transform.position}");
        }

        public static void InitPresentation(this BattleUnitView self)
        {
            if (self.GameObject == null) return;
            
            SpriteRenderer sr = self.GameObject.GetComponentInChildren<SpriteRenderer>();
            self.DefaultColor = sr != null ? sr.color : Color.white;
            self.DefaultScale = self.GameObject.transform.localScale;
        }

        public static void UpdatePosition(this BattleUnitView self, float3 position, float timer)
        {
            if (!self.Initialized)
            {
                self.PendingPosition = position;
                self.PendingDuration = timer;
                self.HasPendingPosition = true;
                BattleUnit unit = self.GetParent<BattleUnit>();
                BattleMoveDebugLog.Write($"ViewPending unit={unit?.Id ?? 0} target={position} timer={timer:F3}");
                return;
            }

            BattleUnit owner = self.GetParent<BattleUnit>();
            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, position);
            if (timer == 0)
            {
                self.KillMoveTween();
                BattleMoveDebugLog.Write(
                    $"ViewSnap unit={owner?.Id ?? 0} currentWorld={self.GameObject.transform.position} targetWorld={worldPos}");
                self.GameObject.transform.position = worldPos;
            }
            else
            {
                self.KillMoveTween();
                BattleMoveDebugLog.Write(
                    $"ViewTweenStart unit={owner?.Id ?? 0} currentWorld={self.GameObject.transform.position} targetWorld={worldPos} timer={timer:F3}");
                self.MoveTweener = self.GameObject.transform.DOMove(worldPos, timer)
                    .SetEase(Ease.Linear)
                    .SetRecyclable(true)
                    .OnComplete(() =>
                    {
                        BattleMoveDebugLog.Write(
                            $"ViewTweenComplete unit={owner?.Id ?? 0} worldPos={self.GameObject.transform.position} targetWorld={worldPos}");
                        self.MoveTweener = null;
                    });
            }
        }

        public static void SetColor(this BattleUnitView self, Color color)
        {
            if (self.GameObject == null) return;
            
            SpriteRenderer sr = self.GameObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }

        public static void SetVisible(this BattleUnitView self, bool visible)
        {
            if (self.GameObject != null) self.GameObject.SetActive(visible);
        }

        public static void PlayAttackFeedback(this BattleUnitView self)
        {
            self.KillTweens();
            self.PlayPulse(new Color(1f, 0.95f, 0.55f, 1f), 1.1f, 0.10f);
        }

        public static void PlayHitFeedback(this BattleUnitView self, int damage)
        {
            self.KillTweens();
            self.PlayPulse(new Color(1f, 0.45f, 0.45f, 1f), 1.06f, 0.12f);
            self.PlayDamagePopup(damage);
        }

        private static void KillTweens(this BattleUnitView self)
        {
            self.PresentationTweener?.Kill();
            self.PresentationTweener = null;
        }

        private static void KillMoveTween(this BattleUnitView self)
        {
            self.MoveTweener?.Kill();
            self.MoveTweener = null;
        }

        private static void PlayPulse(this BattleUnitView self, Color flashColor, float scaleMultiplier, float duration)
        {
            if (self.GameObject == null) return;
            
            Transform transform = self.GameObject.transform;
            SpriteRenderer sr = self.GameObject.GetComponentInChildren<SpriteRenderer>();
            
            Sequence scaleSeq = DOTween.Sequence();
            scaleSeq.Append(transform.DOScale(self.DefaultScale * scaleMultiplier, duration * 0.5f).SetEase(Ease.OutQuad));
            scaleSeq.Append(transform.DOScale(self.DefaultScale, duration * 0.5f).SetEase(Ease.InQuad));
            scaleSeq.Play();
            self.PresentationTweener = scaleSeq;
            
            if (sr != null)
            {
                sr.DOColor(flashColor, duration * 0.5f).SetEase(Ease.OutQuad)
                    .OnComplete(() => sr.DOColor(self.DefaultColor, duration * 0.5f).SetEase(Ease.InQuad));
            }
        }

        private static void PlayDamagePopup(this BattleUnitView self, int damage)
        {
            if (self.GameObject == null) return;
        }
    }
}
