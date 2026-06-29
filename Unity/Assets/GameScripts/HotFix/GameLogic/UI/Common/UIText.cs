using TMPro;
using UnityEngine;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public sealed class UIText : TextMeshProUGUI
    {
        public RectTransform RectTransform => rectTransform;

        public void SetText(string value)
        {
            text = value ?? string.Empty;
        }

        public void SetNumber(long value)
        {
            text = value.ToString();
        }

        public void SetPercent(long current, long max)
        {
            text = max > 0 ? $"{current}/{max}" : $"{current}/0";
        }

        public void SetColor(Color color)
        {
            this.color = color;
        }

        public void SetFontSize(float fontSize)
        {
            this.fontSize = fontSize;
        }

        public void SetAlpha(float alpha)
        {
            Color color = this.color;
            color.a = Mathf.Clamp01(alpha);
            this.color = color;
        }
    }
}
