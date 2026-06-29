using ET;
using UnityEngine;

namespace GameLogic
{
    public class UIDamageWidget : UIWidget
    {
        #region 脚本工具生成的代码

        private UIBindComponent m_bindComponent;
        private UIText m_tmpDamage = null!;

        protected override void ScriptGenerator()
        {
            m_bindComponent = gameObject.GetComponent<UIBindComponent>();
            if(m_bindComponent == null)
            {
                Log.Error($"根物体: {gameObject.name} 缺少组件 UIBindComponent, 请检查！！！");
                return;
            }
            m_tmpDamage = m_bindComponent.GetComponent<UIText>(0);
        }

        #endregion

        public const float FloatDuration = 1.0f;
        public const float FloatHeight = 80f;
        public const float HorizontalRange = 30f;
        public static readonly Color NormalColor = new Color(1f, 1f, 1f, 1f);
        public static readonly Color CritColor = new Color(1f, 0.8f, 0f, 1f);
        public const float CritFontSize = 36f;
        public const float NormalFontSize = 24f;

        private RectTransform _cachedRect;
        private float _elapsed;
        private float _offsetX;
        private bool _active;

        protected override void OnCreate()
        {
            _cachedRect = m_tmpDamage.RectTransform;
            gameObject.SetActive(false);
        }

        public void Show(int damage, bool isCrit)
        {
            m_tmpDamage.SetNumber(damage);
            m_tmpDamage.SetColor(isCrit ? CritColor : NormalColor);
            m_tmpDamage.SetFontSize(isCrit ? CritFontSize : NormalFontSize);

            _offsetX = Random.Range(-HorizontalRange, HorizontalRange);
            _cachedRect.anchoredPosition = new Vector2(_offsetX, 0f);

            _elapsed = 0f;
            _active = true;
            gameObject.SetActive(true);
            m_tmpDamage.SetAlpha(1f);
            _cachedRect.localScale = Vector3.one;
        }

        public bool Tick()
        {
            if (!_active) return false;

            _elapsed += Time.deltaTime;
            float t = _elapsed / FloatDuration;

            if (t >= 1f)
            {
                m_tmpDamage.SetAlpha(0f);
                gameObject.SetActive(false);
                _active = false;
                return false;
            }

            Vector2 startPos = new Vector2(_offsetX, 0f);
            Vector2 endPos = new Vector2(_offsetX, FloatHeight);
            _cachedRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            float scale;
            if (t < 0.1f)
                scale = Mathf.Lerp(0.85f, 1.15f, t / 0.1f);
            else if (t < 0.22f)
                scale = Mathf.Lerp(1.15f, 1f, (t - 0.1f) / 0.12f);
            else
                scale = Mathf.Lerp(1f, 0.8f, (t - 0.22f) / 0.78f);
            _cachedRect.localScale = Vector3.one * scale;

            if (t > 0.6f)
            {
                m_tmpDamage.SetAlpha(1f - ((t - 0.6f) / 0.4f));
            }

            return true;
        }
    }
}
