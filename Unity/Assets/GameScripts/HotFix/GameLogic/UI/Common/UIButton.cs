using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public sealed class UIButton : Button
    {
        [SerializeField] private float m_clickInterval = 0.3f;

        private Action _clickHandler;
        private float _lastClickTime = -999f;

        protected override void Awake()
        {
            base.Awake();
            onClick.AddListener(OnClicked);
        }

        protected override void OnDestroy()
        {
            onClick.RemoveListener(OnClicked);
            _clickHandler = null;
            base.OnDestroy();
        }

        public void SetClick(Action handler)
        {
            _clickHandler = handler;
        }

        public void SetInteractable(bool interactable)
        {
            this.interactable = interactable;
        }

        public void SetClickInterval(float seconds)
        {
            m_clickInterval = Mathf.Max(0f, seconds);
        }

        private void OnClicked()
        {
            if (m_clickInterval > 0f && Time.unscaledTime - _lastClickTime < m_clickInterval)
            {
                return;
            }

            _lastClickTime = Time.unscaledTime;
            _clickHandler?.Invoke();
        }
    }
}
