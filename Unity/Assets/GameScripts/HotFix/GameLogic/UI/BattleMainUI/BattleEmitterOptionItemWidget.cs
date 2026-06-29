using System;
using ET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed partial class BattleEmitterOptionItemWidget : UIWidget
    {
        private TMP_FontAsset _font;
        private Material _fontMaterial;
        private EmitterConfig _config;
        private Action<EmitterConfig> _clickHandler;
        private UIButton _button;
        private Image _background;

        protected override void OnCreate()
        {
            ValidateBindings();
            PrepareCooldownFill(m_imgCooldownFill);
            ApplyFont(m_tmpName);
            ApplyFont(m_tmpMeta);
            ApplyFont(m_tmpBuff);
            EnsureButton();
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
            ApplyFont(m_tmpBuff);
        }

        public void SetClickHandler(Action<EmitterConfig> clickHandler)
        {
            _clickHandler = clickHandler;
        }

        public void RefreshOption(int index, EmitterConfig config, bool canAdd)
        {
            _config = config;
            Visible = config != null;
            if (!Visible)
            {
                return;
            }

            SkillTargetingConfig targetingConfig = ConfigHelper.SkillTargetingConfig?.GetOrDefault(config.TargetingConfigId);
            float range = targetingConfig != null ? targetingConfig.CastRange + targetingConfig.EdgeDistance : 1.5f;

            m_tmpName.SetText(GetEmitterName(config));
            m_tmpMeta.SetText(GetEmitterMeta(config));
            m_tmpBuff.SetText(GetEmitterDebugInfo(config, range));

            m_imgCooldownFill.fillAmount = 1f;
            m_imgCooldownFill.color = GetSlotColor(index);
            _button.SetInteractable(canAdd);
        }

        private static string GetEmitterName(EmitterConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.Name))
            {
                return $"[{config.Id}] {config.Name}";
            }

            return config != null ? $"发射器 {config.Id}" : "发射器";
        }

        private static string GetEmitterMeta(EmitterConfig config)
        {
            return $"目标配置 {config.TargetingConfigId}  优先级 {config.Priority}\n类型 {config.EmitterKind}  释放 {config.CastType}  目标 {config.TargetType}";
        }

        private static string FormatCooldown(int cooldownMs)
        {
            cooldownMs = System.Math.Max(0, cooldownMs);
            return cooldownMs > 0 ? $"{cooldownMs / 1000f:0.0}秒" : "无";
        }

        private static string FormatDamage(EmitterConfig config)
        {
            return $"{config.BaseDamage:0.#}+攻击*{config.WhiteAttackRatio:0.##}";
        }

        private static string GetEmitterDebugInfo(EmitterConfig config, float range)
        {
            if (config == null)
            {
                return string.Empty;
            }

            string moveCast = config.CanMoveCast ? "移动施法" : "站定施法";
            string enabled = config.IsEnabled ? "启用" : "禁用";
            return $"冷却 {FormatCooldown(config.CooldownMs)}  冷却组 {config.CooldownGroupId}  射程 {range:0.0}  命中点 {config.AttackHitRatio:0.##}\n" +
                   $"伤害 {FormatDamage(config)}  效果槽 {config.BuffSlotCount}  升级组 {config.UpgradeConfigId}\n" +
                   $"{moveCast}  指定目标 {FormatBool(config.NeedExplicitTarget)}  {enabled}\n" +
                   $"{FormatDesc(config.Desc)}";
        }

        private static string FormatBool(bool value)
        {
            return value ? "是" : "否";
        }

        private static string FormatDesc(string desc)
        {
            return string.IsNullOrEmpty(desc) ? "描述 -" : desc;
        }

        private static Color GetSlotColor(int index)
        {
            return index switch
            {
                0 => new Color(0.16f, 0.50f, 0.95f, 0.72f),
                1 => new Color(0.35f, 0.70f, 0.25f, 0.72f),
                2 => new Color(0.82f, 0.48f, 0.16f, 0.72f),
                _ => new Color(0.64f, 0.34f, 0.88f, 0.72f),
            };
        }

        private void ValidateBindings()
        {
            if (m_imgCooldownFill == null || m_tmpName == null || m_tmpMeta == null || m_tmpBuff == null)
            {
                throw new InvalidOperationException("BattleEmitterOptionItemWidget prefab bindings are incomplete.");
            }

            _button = gameObject.GetComponent<UIButton>();
            if (_button == null)
            {
                throw new InvalidOperationException("BattleEmitterOptionItemWidget UIButton is missing.");
            }

            _background = gameObject.GetComponent<Image>();
            if (_background == null)
            {
                throw new InvalidOperationException("BattleEmitterOptionItemWidget Image is missing.");
            }
        }

        private static void PrepareCooldownFill(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Bottom;
            image.fillAmount = 0f;
            image.raycastTarget = false;
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null)
            {
                throw new InvalidOperationException("BattleEmitterOptionItemWidget text binding is missing.");
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
    }
}
