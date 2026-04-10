using System.Collections.Generic;
using ET;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic
{
    public partial class UIDamageWindow
    {
        public Transform ContentTransform => m_tfContent;

        private const int MaxPoolSize = 50;
        private readonly Queue<UIDamageWidget> _pool = new();
        private readonly List<UIDamageWidget> _activeWidgets = new();
        private Camera _uiCamera;
        private RectTransform _canvasRect;
        private static readonly int EventId_Damaged = typeof(ET.BattleUnitDamaged).GetHashCode();

        protected override void OnCreate()
        {
            ScriptGenerator();
            _uiCamera = UIModule.Instance.UICamera;
            var canvas = UIModule.UIRoot.GetComponentInParent<Canvas>();
            _canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            AddUIEvent<ET.BattleUnitDamaged>(EventId_Damaged, OnBattleUnitDamaged);
        }

        protected override void OnDestroy()
        {
            RemoveAllUIEvent();
            ClearPool();
        }

        protected override void OnUpdate()
        {
            for (int i = _activeWidgets.Count - 1; i >= 0; i--)
            {
                var widget = _activeWidgets[i];
                if (!widget.Tick())
                {
                    _activeWidgets.RemoveAt(i);
                    ReturnWidget(widget);
                }
            }

            _hasOverrideUpdate = _activeWidgets.Count > 0;
        }

        private void ClearPool()
        {
            _activeWidgets.Clear();
            _pool.Clear();
        }

        private UIDamageWidget RentWidget()
        {
            UIDamageWidget widget;
            if (_pool.Count > 0)
            {
                widget = _pool.Dequeue();
                widget.Visible = true;
            }
            else
            {
                widget = CreateWidgetByType<UIDamageWidget>(m_tfContent);
            }

            return widget;
        }

        private void ReturnWidget(UIDamageWidget widget)
        {
            widget.Visible = false;

            if (_pool.Count >= MaxPoolSize)
            {
                return;
            }

            _pool.Enqueue(widget);
        }

        private void OnBattleUnitDamaged(ET.BattleUnitDamaged args)
        {
            var view = args.Unit.GetComponent<ET.BattleUnitView>();
            Vector3 worldPos = view != null && view.GameObject != null
                ? view.GameObject.transform.position + new Vector3(0f, 1f, 0f)
                : ET.BattleAreaConfig.GetWorldPosition(args.Unit.Camp, args.Unit.Position + new float3(0f, 1f, 0f));

            ShowDamage(worldPos, args.Damage, args.IsCrit);
        }

        public void ShowDamage(Vector3 worldPos, int damage, bool isCrit)
        {
            var widget = RentWidget();
            if (widget == null) return;

            // 使用主相机（而非 UI 相机）将世界坐标转为屏幕坐标，
            // 因为虚拟相机会移动主相机，世界空间由主相机渲染
            Camera mainCam = Camera.main;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, _uiCamera, out Vector2 localPos);
            localPos.y += 80f;

            widget.rectTransform.SetParent(m_tfContent, false);
            widget.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            widget.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            widget.rectTransform.pivot = new Vector2(0.5f, 0f);
            widget.rectTransform.anchoredPosition = localPos;
            widget.rectTransform.localScale = Vector3.one;

            widget.Show(damage, isCrit);
            _activeWidgets.Add(widget);
            _hasOverrideUpdate = true;
        }
    }
}
