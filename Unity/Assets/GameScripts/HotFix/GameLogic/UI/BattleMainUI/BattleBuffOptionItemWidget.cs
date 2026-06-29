using System;
using ET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed partial class BattleBuffOptionItemWidget : UIWidget
    {
        private Image _background;
        private TMP_FontAsset _font;
        private Material _fontMaterial;
        private UIButton _button;
        private EmitterEffectPackConfig _config;
        private Action<EmitterEffectPackConfig> _clickHandler;

        protected override void OnCreate()
        {
            ValidateBindings();
            EnsureButton();
        }

        private void ValidateBindings()
        {
            if (m_tmpName == null || m_tmpMeta == null)
            {
                throw new InvalidOperationException("BattleBuffOptionItemWidget prefab bindings are incomplete.");
            }

            _background = gameObject.GetComponent<Image>();
            if (_background == null)
            {
                throw new InvalidOperationException("BattleBuffOptionItemWidget Image is missing.");
            }

            _button = gameObject.GetComponent<UIButton>();
            if (_button == null)
            {
                throw new InvalidOperationException("BattleBuffOptionItemWidget UIButton is missing.");
            }
        }

        public void SetFontFrom(TextMeshProUGUI source)
        {
            if (source == null || source.font == null)
            {
                return;
            }

            _font = source.font;
            _fontMaterial = source.fontSharedMaterial;
            ApplyFont(m_tmpName);
            ApplyFont(m_tmpMeta);
        }

        public void SetClickHandler(Action<EmitterEffectPackConfig> handler)
        {
            _clickHandler = handler;
        }

        public void RefreshOption(EmitterEffectPackConfig config, bool canAdd)
        {
            _config = config;
            Visible = config != null;
            if (!Visible)
            {
                return;
            }

            m_tmpName.SetText(GetName(config));
            m_tmpMeta.SetText(GetMeta(config));
            _button.SetInteractable(canAdd);
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null)
            {
                throw new InvalidOperationException("BattleBuffOptionItemWidget text binding is missing.");
            }

            if (_font == null)
            {
                return;
            }

            text.font = _font;
            if (_fontMaterial != null)
            {
                text.fontSharedMaterial = _fontMaterial;
            }
        }

        private void EnsureButton()
        {
            if (_button.targetGraphic == null)
            {
                _button.targetGraphic = _background;
            }

            _button.targetGraphic.raycastTarget = true;
            _button.SetClick(OnClicked);
        }

        private void OnClicked()
        {
            if (_config == null)
            {
                return;
            }

            _clickHandler?.Invoke(_config);
        }

        private static string GetName(EmitterEffectPackConfig config)
        {
            if (!string.IsNullOrEmpty(config.Name))
            {
                return config.Name;
            }

            if (!string.IsNullOrEmpty(config.Desc))
            {
                return config.Desc;
            }

            return $"效果包 {config.Id}";
        }

        private static string GetMeta(EmitterEffectPackConfig config)
        {
            int count = config.EffectIds?.Length ?? 0;
            if (!string.IsNullOrEmpty(config.Desc))
            {
                return $"{count} 个效果  {config.Desc}";
            }

            return count > 0 ? $"{count} 个效果" : "无效果";
        }
    }
}
