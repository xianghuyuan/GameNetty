using UnityEngine;

namespace GameLogic
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UISafeArea : MonoBehaviour
    {
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;
        [SerializeField] private Vector2 paddingMin;
        [SerializeField] private Vector2 paddingMax;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _lastApplyLeft;
        private bool _lastApplyRight;
        private bool _lastApplyTop;
        private bool _lastApplyBottom;
        private Vector2 _lastPaddingMin;
        private Vector2 _lastPaddingMax;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (IsDirty())
            {
                Apply();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        public void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x = applyLeft ? anchorMin.x / Screen.width : 0f;
            anchorMin.y = applyBottom ? anchorMin.y / Screen.height : 0f;
            anchorMax.x = applyRight ? anchorMax.x / Screen.width : 1f;
            anchorMax.y = applyTop ? anchorMax.y / Screen.height : 1f;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = paddingMin;
            _rectTransform.offsetMax = -paddingMax;

            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastApplyLeft = applyLeft;
            _lastApplyRight = applyRight;
            _lastApplyTop = applyTop;
            _lastApplyBottom = applyBottom;
            _lastPaddingMin = paddingMin;
            _lastPaddingMax = paddingMax;
        }

        private bool IsDirty()
        {
            return _lastSafeArea != Screen.safeArea ||
                   _lastScreenSize.x != Screen.width ||
                   _lastScreenSize.y != Screen.height ||
                   _lastApplyLeft != applyLeft ||
                   _lastApplyRight != applyRight ||
                   _lastApplyTop != applyTop ||
                   _lastApplyBottom != applyBottom ||
                   _lastPaddingMin != paddingMin ||
                   _lastPaddingMax != paddingMax;
        }
    }
}
