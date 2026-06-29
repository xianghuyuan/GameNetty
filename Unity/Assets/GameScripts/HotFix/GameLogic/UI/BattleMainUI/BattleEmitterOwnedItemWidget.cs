using System;
using System.Collections.Generic;
using ET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed partial class BattleEmitterOwnedItemWidget : UIWidget
    {
        private Image _background;
        private Image _cooldownFill;
        private TMP_FontAsset _font;
        private Material _fontMaterial;
        private UIButton _button;
        private Action<long> _clickHandler;
        private long _attackRuntimeId;

        protected override void OnCreate()
        {
            ValidateBindings();
            PrepareCooldownFill(_cooldownFill);
            EnsureButton();
        }

        private void ValidateBindings()
        {
            if (m_imgCooldownFill == null || m_tmpName == null || m_tmpMeta == null || m_tmpBuff == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedItemWidget prefab bindings are incomplete.");
            }

            _background = gameObject.GetComponent<Image>();
            _cooldownFill = m_imgCooldownFill;
            if (_background == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedItemWidget Image is missing.");
            }

            _button = gameObject.GetComponent<UIButton>();
            if (_button == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedItemWidget UIButton is missing.");
            }
        }

        public void SetClickHandler(Action<long> clickHandler)
        {
            _clickHandler = clickHandler;
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

        public void RefreshOwned(int index, BattleAttackComponent attackComponent, BattleAttackRuntime attack, long nowMs, bool selected)
        {
            Visible = attackComponent != null && attack != null;
            if (!Visible)
            {
                return;
            }

            _attackRuntimeId = attack.AttackRuntimeId;
            m_tmpName.SetText(GetEmitterName(attack));
            m_tmpMeta.SetText($"CD {FormatCooldown(attackComponent, attack, nowMs)}  R {attack.AttackRange:0.0}  DMG {FormatDamage(attack)}");
            m_tmpBuff.SetText(GetBuffText(attack));
            _background.color = selected ? new Color(1f, 0.93f, 0.68f, 1f) : Color.white;

            _cooldownFill.fillAmount = GetCooldownFill(attackComponent, attack, nowMs);
            _cooldownFill.color = GetSlotColor(index);
        }

        public void RefreshPlaceholder(int index)
        {
            Visible = true;
            _attackRuntimeId = 0;
            m_tmpName.SetText("发射器槽");
            m_tmpMeta.SetText("待装载");
            m_tmpBuff.SetText("BuffStack");
            _background.color = new Color(1f, 1f, 1f, 0.74f);
            _cooldownFill.fillAmount = 0.18f;
            _cooldownFill.color = GetSlotColor(index);
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedItemWidget text binding is missing.");
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
            if (_attackRuntimeId == 0)
            {
                return;
            }

            _clickHandler?.Invoke(_attackRuntimeId);
        }

        private static void PrepareCooldownFill(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Bottom;
            image.fillAmount = 1f;
            image.raycastTarget = false;
        }

        private static string GetEmitterName(BattleAttackRuntime attack)
        {
            EmitterConfig config = ConfigHelper.EmitterConfig?.GetOrDefault(attack.SourceConfigId);
            if (config != null && !string.IsNullOrEmpty(config.Name))
            {
                return attack.Level > 1 ? $"{config.Name} Lv.{attack.Level}" : config.Name;
            }

            string fallback = attack.SourceConfigId > 0 ? $"发射器 {attack.SourceConfigId}" : $"发射器 {System.Math.Abs(attack.AttackRuntimeId % 1000)}";
            return attack.Level > 1 ? $"{fallback} Lv.{attack.Level}" : fallback;
        }

        private static string FormatCooldown(BattleAttackComponent attackComponent, BattleAttackRuntime attack, long nowMs)
        {
            if (attackComponent.EmitterCooldownEndTimeById.TryGetValue(attack.AttackRuntimeId, out long endMs))
            {
                long remainMs = System.Math.Max(0, endMs - nowMs);
                if (remainMs > 0)
                {
                    return $"{remainMs / 1000f:0.0}s";
                }
            }

            return "就绪";
        }

        private static string FormatDamage(BattleAttackRuntime attack)
        {
            return $"{attack.BaseDamage:0.#}+ATK*{attack.WhiteAttackRatio:0.##} x{attack.WhiteDamageMultiplier:0.##}";
        }

        private static float GetCooldownFill(BattleAttackComponent attackComponent, BattleAttackRuntime attack, long nowMs)
        {
            if (!attackComponent.EmitterCooldownEndTimeById.TryGetValue(attack.AttackRuntimeId, out long endMs) || attack.CooldownMs <= 0)
            {
                return 1f;
            }

            float remain = System.Math.Max(0, endMs - nowMs);
            return 1f - Mathf.Clamp01(remain / attack.CooldownMs);
        }

        private static string GetBuffText(BattleAttackRuntime attack)
        {
            List<int> effectPackIds = attack?.EffectPackIds;
            int slotCount = System.Math.Max(0, attack?.BuffSlotCount ?? 0);
            if (effectPackIds != null && effectPackIds.Count > 0)
            {
                EmitterEffectPackConfig pack = ConfigHelper.EmitterEffectPackConfig?.GetOrDefault(effectPackIds[0]);
                string name = pack != null && !string.IsNullOrEmpty(pack.Name) ? pack.Name : $"效果包 {effectPackIds[0]}";
                return slotCount > 0 ? $"效果 {effectPackIds.Count}/{slotCount}  {name}" : name;
            }

            List<int> buffGroupIds = attack?.BuffGroupIds;
            if (buffGroupIds == null || buffGroupIds.Count == 0)
            {
                return slotCount > 0 ? $"效果 0/{slotCount}" : "无效果槽";
            }

            BuffGroupConfig group = ConfigHelper.BuffGroupConfig?.GetOrDefault(buffGroupIds[0]);
            if (group != null && !string.IsNullOrEmpty(group.Desc))
            {
                return slotCount > 0 ? $"Buff {buffGroupIds.Count}/{slotCount}  {group.Desc}" : group.Desc;
            }

            return slotCount > 0 ? $"Buff {buffGroupIds.Count}/{slotCount}  Buff组 {buffGroupIds[0]}" : $"Buff组 {buffGroupIds[0]}";
        }

        private static Color GetSlotColor(int index)
        {
            return index switch
            {
                0 => new Color(0.16f, 0.50f, 0.95f, 0.62f),
                1 => new Color(0.35f, 0.70f, 0.25f, 0.62f),
                2 => new Color(0.82f, 0.48f, 0.16f, 0.62f),
                _ => new Color(0.64f, 0.34f, 0.88f, 0.62f),
            };
        }
    }
}
