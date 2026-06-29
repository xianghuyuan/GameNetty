using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public sealed class UIImage : Image
    {
        public void SetSprite(string location, bool nativeSize = false)
        {
            if (string.IsNullOrEmpty(location))
            {
                Clear();
                return;
            }

            SetSpriteExtensions.SetSprite(this, location, nativeSize);
        }

        public void SetSubSprite(string location, string spriteName, bool nativeSize = false)
        {
            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(spriteName))
            {
                Clear();
                return;
            }

            SetSpriteExtensions.SetSubSprite(this, location, spriteName, nativeSize);
        }

        public void SetFill(float value)
        {
            fillAmount = Mathf.Clamp01(value);
        }

        public void SetColor(Color color)
        {
            this.color = color;
        }

        public void SetAlpha(float alpha)
        {
            Color color = this.color;
            color.a = Mathf.Clamp01(alpha);
            this.color = color;
        }

        public void SetRaycast(bool enabled)
        {
            raycastTarget = enabled;
        }

        public void Clear()
        {
            sprite = null;
        }
    }
}
