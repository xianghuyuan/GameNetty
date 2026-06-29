using System;
using System.Collections.Generic;
using ET;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public enum BattleEmitterTuneParam
    {
        CooldownMs,
        AttackRange,
        AttackHitRatio,
        WhiteDamageMultiplier,
    }

    public sealed partial class BattleEmitterOwnedBarWidget : UIWidget
    {
        private const int MaxOwnedItems = 8;

        private readonly List<BattleEmitterOwnedItemWidget> _items = new List<BattleEmitterOwnedItemWidget>(MaxOwnedItems);
        private GameObject _itemPrefab;
        private TMP_FontAsset _font;
        private Material _fontMaterial;
        private Action<long> _selectionChangedHandler;
        private Action<BattleEmitterTuneParam, int> _tuneHandler;
        private long _selectedEmitterId;

        protected override void OnCreate()
        {
            CacheViews();
        }

        public void SetFontFrom(TextMeshProUGUI source)
        {
            if (source == null || source.font == null)
            {
                return;
            }

            _font = source.font;
            _fontMaterial = source.fontSharedMaterial;
            ApplyFont(m_tmpTitle);
            ApplyFont(m_tmpEmpty);
            ApplyFont(m_tmpTuneTitle);
            ApplyFont(m_tmpTuneCooldown);
            ApplyFont(m_tmpTuneRange);
            ApplyFont(m_tmpTuneHit);
            ApplyFont(m_tmpTuneDamage);

            foreach (BattleEmitterOwnedItemWidget item in _items)
            {
                item.SetFontFrom(source);
            }
        }

        public void SetSelectionChangedHandler(Action<long> handler)
        {
            _selectionChangedHandler = handler;
        }

        public void SetTuneHandler(Action<BattleEmitterTuneParam, int> handler)
        {
            _tuneHandler = handler;
        }

        public void Refresh(Battle battle, long playerUnitId)
        {
            BattleUnit player = ResolvePlayer(battle, playerUnitId);
            BattleAttackComponent attackComponent = player?.GetComponent<BattleAttackComponent>();
            int count = attackComponent?.Attacks.Count ?? 0;
            int visibleCount = Math.Max(3, count);
            long nowMs = TimeInfo.Instance.ClientNow();

            SetActive(m_tmpEmpty.gameObject, false);
            ClearInvalidSelection(attackComponent);
            EnsureItemCount(visibleCount);

            for (int i = 0; i < _items.Count; i++)
            {
                BattleAttackRuntime attack = attackComponent != null && i < count ? attackComponent.Attacks[i] : null;
                bool selected = attack != null && attack.AttackRuntimeId == _selectedEmitterId;
                if (attack != null)
                {
                    _items[i].RefreshOwned(i, attackComponent, attack, nowMs, selected);
                }
                else
                {
                    _items[i].RefreshPlaceholder(i);
                }
            }

            RefreshTunePanel(attackComponent);
        }

        private void CacheViews()
        {
            if (m_tmpTitle == null || m_tmpEmpty == null || m_tfOwnedEmitterItems == null || m_itemOwnedEmitter == null ||
                m_tfTunePanel == null || m_tmpTuneTitle == null || m_tmpTuneCooldown == null || m_btnCooldownMinus == null ||
                m_btnCooldownPlus == null || m_tmpTuneRange == null || m_btnRangeMinus == null || m_btnRangePlus == null ||
                m_tmpTuneHit == null || m_btnHitMinus == null || m_btnHitPlus == null || m_tmpTuneDamage == null ||
                m_btnDamageMinus == null || m_btnDamagePlus == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedBarWidget prefab bindings are incomplete.");
            }

            m_tmpTitle.SetText("当前发射器");
            m_tmpEmpty.SetText("暂无发射器");
            m_tmpTuneTitle.SetText("发射器参数");
            BindTuneButton(m_btnCooldownMinus, BattleEmitterTuneParam.CooldownMs, -1);
            BindTuneButton(m_btnCooldownPlus, BattleEmitterTuneParam.CooldownMs, 1);
            BindTuneButton(m_btnRangeMinus, BattleEmitterTuneParam.AttackRange, -1);
            BindTuneButton(m_btnRangePlus, BattleEmitterTuneParam.AttackRange, 1);
            BindTuneButton(m_btnHitMinus, BattleEmitterTuneParam.AttackHitRatio, -1);
            BindTuneButton(m_btnHitPlus, BattleEmitterTuneParam.AttackHitRatio, 1);
            BindTuneButton(m_btnDamageMinus, BattleEmitterTuneParam.WhiteDamageMultiplier, -1);
            BindTuneButton(m_btnDamagePlus, BattleEmitterTuneParam.WhiteDamageMultiplier, 1);
            SetActive(m_tfTunePanel.gameObject, false);
            _items.Clear();
            AddItem(m_itemOwnedEmitter);
            _itemPrefab = m_itemOwnedEmitter.gameObject;
        }

        private void RefreshTunePanel(BattleAttackComponent attackComponent)
        {
            BattleAttackRuntime selected = FindSelectedAttack(attackComponent);
            bool hasSelected = selected != null;
            SetActive(m_tfTunePanel.gameObject, hasSelected);
            if (!hasSelected)
            {
                return;
            }

            m_tmpTuneCooldown.SetText($"CD {selected.CooldownMs}ms");
            m_tmpTuneRange.SetText($"射程 {selected.AttackRange:0.0}");
            m_tmpTuneHit.SetText($"命中 {selected.AttackHitRatio:0.00}");
            m_tmpTuneDamage.SetText($"倍率 x{selected.WhiteDamageMultiplier:0.0}");
        }

        private BattleAttackRuntime FindSelectedAttack(BattleAttackComponent attackComponent)
        {
            if (attackComponent == null || _selectedEmitterId == 0)
            {
                return null;
            }

            foreach (BattleAttackRuntime attack in attackComponent.Attacks)
            {
                if (attack != null && attack.AttackRuntimeId == _selectedEmitterId)
                {
                    return attack;
                }
            }

            return null;
        }

        private void BindTuneButton(UIButton button, BattleEmitterTuneParam param, int direction)
        {
            if (button.targetGraphic == null)
            {
                button.targetGraphic = button.GetComponent<UnityEngine.UI.Image>();
            }

            button.GetComponentInChildren<UIText>(true)?.SetText(direction > 0 ? "+" : "-");
            button.SetClick(() => _tuneHandler?.Invoke(param, direction));
        }

        private void EnsureItemCount(int count)
        {
            while (_items.Count < count)
            {
                BattleEmitterOwnedItemWidget item = CreateWidgetByPrefab<BattleEmitterOwnedItemWidget>(_itemPrefab, m_tfOwnedEmitterItems);
                AddItem(item);
            }

            for (int i = count; i < _items.Count; i++)
            {
                _items[i].Visible = false;
            }
        }

        private void AddItem(BattleEmitterOwnedItemWidget item)
        {
            if (item == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedBarWidget item is missing.");
            }

            item.SetFontFrom(m_tmpTitle);
            item.SetClickHandler(OnItemClicked);
            _items.Add(item);
        }

        private void OnItemClicked(long attackRuntimeId)
        {
            if (attackRuntimeId == 0)
            {
                return;
            }

            _selectedEmitterId = _selectedEmitterId == attackRuntimeId ? 0 : attackRuntimeId;
            _selectionChangedHandler?.Invoke(_selectedEmitterId);
        }

        private void ClearInvalidSelection(BattleAttackComponent attackComponent)
        {
            if (_selectedEmitterId == 0)
            {
                return;
            }

            if (attackComponent != null)
            {
                foreach (BattleAttackRuntime attack in attackComponent.Attacks)
                {
                    if (attack != null && attack.AttackRuntimeId == _selectedEmitterId)
                    {
                        return;
                    }
                }
            }

            _selectedEmitterId = 0;
            _selectionChangedHandler?.Invoke(0);
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null)
            {
                throw new InvalidOperationException("BattleEmitterOwnedBarWidget text binding is missing.");
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

        private static BattleUnit ResolvePlayer(Battle battle, long playerUnitId)
        {
            if (battle == null || battle.IsDisposed)
            {
                return null;
            }

            if (playerUnitId != 0)
            {
                BattleUnit unit = battle.GetChild<BattleUnit>(playerUnitId);
                if (unit != null && !unit.IsDisposed)
                {
                    return unit;
                }
            }

            foreach (BattleUnit unit in battle.GetAllBattleUnits())
            {
                if (unit != null && !unit.IsDisposed && unit.Camp == UnitCamp.Friend)
                {
                    return unit;
                }
            }

            return null;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
