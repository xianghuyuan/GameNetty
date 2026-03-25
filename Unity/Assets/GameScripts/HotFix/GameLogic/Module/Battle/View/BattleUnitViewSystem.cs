using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

namespace ET
{
    [EntitySystemOf(typeof(BattleUnitView))]
    [FriendOf(typeof(BattleUnitView))]
    public static partial class BattleUnitViewSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitView self, UnitCamp camp, float3 position)
        {
            self.UnitId = self.Id;
            self.Camp = camp;
            self.Position = position;
        }

        [EntitySystem]
        private static void Destroy(this BattleUnitView self)
        {
            self.KillTweens();
            if (self.GameObject != null)
            {
                UnityEngine.Object.Destroy(self.GameObject);
                self.GameObject = null;
            }
        }

        public static void InitPresentation(this BattleUnitView self)
        {
            if (self.GameObject == null) return;
            
            SpriteRenderer sr = self.GameObject.GetComponentInChildren<SpriteRenderer>();
            self.DefaultColor = sr != null ? sr.color : Color.white;
            self.DefaultScale = self.GameObject.transform.localScale;
        }

        public static void UpdatePosition(this BattleUnitView self, float3 position,float timer)
        {
            self.Position = position;
            if (self.GameObject == null) return;
            
            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, position);
            if (timer == 0)
            {
                self.GameObject.transform.position = worldPos;
            }
            else
            {
                self.GameObject.transform.DOMove(worldPos, timer).SetEase(Ease.Linear).OnComplete(() => self.GameObject.transform.position = worldPos);
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

        /// <summary>
        /// 攻击和收击表现
        /// </summary>
        /// <param name="self"></param>
        /// <param name="flashColor"></param>
        /// <param name="scaleMultiplier"></param>
        /// <param name="duration"></param>
        private static void PlayPulse(this BattleUnitView self, Color flashColor, float scaleMultiplier, float duration)
        {
            if (self.GameObject == null) return;    
            
            Transform transform = self.GameObject.transform;
            SpriteRenderer sr = self.GameObject.GetComponentInChildren<SpriteRenderer>();
            
            // 缩放脉冲
            Sequence scaleSeq = DOTween.Sequence();
            scaleSeq.Append(transform.DOScale(self.DefaultScale * scaleMultiplier, duration * 0.5f).SetEase(Ease.OutQuad));
            scaleSeq.Append(transform.DOScale(self.DefaultScale, duration * 0.5f).SetEase(Ease.InQuad));
            scaleSeq.Play();
            self.PresentationTweener = scaleSeq;
            // 颜色闪烁
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
