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
        private RectTransform _contentRect;
        private static readonly int EventId_Damaged = typeof(ET.BattleUnitDamaged).GetHashCode();

        protected override void OnCreate()
        {
            ScriptGenerator();
            _contentRect = m_tfContent as RectTransform;
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
            Vector3 worldPos;
            bool usedView = view != null && view.GameObject != null && view.Initialized;
            if (usedView)
            {
                worldPos = view.GameObject.transform.position + new Vector3(0f, 1f, 0f);
            }
            else
            {
                worldPos = ET.BattleAreaConfig.GetWorldPosition(args.Unit.Camp, args.Unit.Position + new float3(0f, 1f, 0f));
            }

            ShowDamage(worldPos, args.Damage, args.IsCrit);
        }

        public void ShowDamage(Vector3 worldPos, int damage, bool isCrit)
        {
            var widget = RentWidget();
            if (widget == null) return;

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Log.Warning("UIDamageWindow: Camera.main is null, skip damage display");
                ReturnWidget(widget);
                return;
            }

            // 正交相机：世界坐标 → 屏幕像素 → Canvas 局部坐标
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPos);
            Camera uiCamera = UIModule.Instance.UICamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_contentRect, screenPoint, uiCamera, out Vector2 localPos);
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
