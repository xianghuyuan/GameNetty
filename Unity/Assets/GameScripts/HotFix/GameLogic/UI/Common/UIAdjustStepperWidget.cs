using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed partial class UIAdjustStepperWidget : UIWidget
    {
        private float _min;
        private float _max = 1f;
        private float _step = 1f;
        private float _value;
        private Func<float, string> _formatter;
        private Action<float> _changedHandler;
        private RectTransform _trackRect;
        private RectTransform _fillRect;
        private RectTransform _handleRect;

        protected override void OnCreate()
        {
            CacheBindings();
            BindDragHandler(m_imgTrack);
            BindDragHandler(m_imgHandle);
            RefreshView();
        }

        public void SetRange(float min, float max, float step = 1f)
        {
            _min = min;
            _max = Mathf.Max(min, max);
            _step = Mathf.Max(0.0001f, step);
            SetValue(_value, false);
        }

        public void SetValue(float value, bool notify = false)
        {
            float normalized = NormalizeValue(value);
            if (Mathf.Approximately(_value, normalized))
            {
                RefreshView();
                return;
            }

            _value = normalized;
            RefreshView();
            if (notify)
            {
                _changedHandler?.Invoke(_value);
            }
        }

        public void SetFormatter(Func<float, string> formatter)
        {
            _formatter = formatter;
            RefreshView();
        }

        public void SetChangedHandler(Action<float> handler)
        {
            _changedHandler = handler;
        }

        private partial void OnClickMinusBtn()
        {
            SetValue(_value - _step, true);
        }

        private partial void OnClickPlusBtn()
        {
            SetValue(_value + _step, true);
        }

        private void CacheBindings()
        {
            _trackRect = m_imgTrack.rectTransform;
            _fillRect = m_imgFill.rectTransform;
            _handleRect = m_imgHandle.rectTransform;

            if (m_btnMinus.targetGraphic == null)
            {
                m_btnMinus.targetGraphic = m_btnMinus.GetComponent<Image>();
            }

            if (m_btnPlus.targetGraphic == null)
            {
                m_btnPlus.targetGraphic = m_btnPlus.GetComponent<Image>();
            }

            m_imgTrack.raycastTarget = true;
            m_imgHandle.raycastTarget = true;
        }

        private void BindDragHandler(Image image)
        {
            StepperDragHandler handler = image.GetComponent<StepperDragHandler>();
            if (handler == null)
            {
                handler = image.gameObject.AddComponent<StepperDragHandler>();
            }

            handler.SetOwner(this);
        }

        private float NormalizeValue(float value)
        {
            float clamped = Mathf.Clamp(value, _min, _max);
            if (_step <= 0f)
            {
                return clamped;
            }

            float snapped = _min + Mathf.Round((clamped - _min) / _step) * _step;
            return Mathf.Clamp(snapped, _min, _max);
        }

        private float GetProgress()
        {
            return Mathf.Approximately(_max, _min) ? 1f : Mathf.Clamp01((_value - _min) / (_max - _min));
        }

        private void RefreshView()
        {
            float progress = GetProgress();
            m_tmpValue.SetText(_formatter != null ? _formatter(_value) : _value.ToString("0.##"));
            m_imgFill.fillAmount = progress;

            if (_fillRect != null && _trackRect != null && m_imgFill.type != Image.Type.Filled)
            {
                Vector2 size = _fillRect.sizeDelta;
                size.x = _trackRect.rect.width * progress;
                _fillRect.sizeDelta = size;
            }

            if (_handleRect != null && _trackRect != null)
            {
                float x = Mathf.Lerp(_trackRect.rect.xMin, _trackRect.rect.xMax, progress);
                Vector2 local = _handleRect.anchoredPosition;
                local.x = x;
                _handleRect.anchoredPosition = local;
            }

            m_btnMinus.SetInteractable(_value > _min && !Mathf.Approximately(_value, _min));
            m_btnPlus.SetInteractable(_value < _max && !Mathf.Approximately(_value, _max));
        }

        private void SetValueByScreenPosition(Vector2 screenPosition, Camera eventCamera)
        {
            if (_trackRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_trackRect, screenPosition, eventCamera, out Vector2 localPoint))
            {
                return;
            }

            float width = _trackRect.rect.width;
            if (width <= 0f)
            {
                return;
            }

            float progress = Mathf.InverseLerp(_trackRect.rect.xMin, _trackRect.rect.xMax, localPoint.x);
            SetValue(Mathf.Lerp(_min, _max, progress), true);
        }

        private sealed class StepperDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            private UIAdjustStepperWidget _owner;

            public void SetOwner(UIAdjustStepperWidget owner)
            {
                _owner = owner;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                _owner?.SetValueByScreenPosition(eventData.position, eventData.pressEventCamera);
            }

            public void OnDrag(PointerEventData eventData)
            {
                _owner?.SetValueByScreenPosition(eventData.position, eventData.pressEventCamera);
            }
        }
    }
}
