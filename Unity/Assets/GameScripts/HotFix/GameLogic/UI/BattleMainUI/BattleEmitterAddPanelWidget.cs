using System;
using System.Collections.Generic;
using System.Linq;
using ET;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed partial class BattleEmitterAddPanelWidget : UIWidget
    {
        private readonly List<BattleEmitterOptionItemWidget> _optionItems = new List<BattleEmitterOptionItemWidget>();
        private readonly List<EmitterConfig> _optionConfigs = new List<EmitterConfig>();

        private GameObject _optionPrefab;
        private Action<EmitterConfig> _addEmitterHandler;
        private int _lastPlayerEmitterCount = -1;

        protected override void OnCreate()
        {
            CacheViews();
            SetPanelVisible(false);
        }

        private partial void OnClickBackBtn()
        {
            Visible = false;
        }

        public void SetAddEmitterHandler(Action<EmitterConfig> addEmitterHandler)
        {
            _addEmitterHandler = addEmitterHandler;
            foreach (BattleEmitterOptionItemWidget item in _optionItems)
            {
                item.SetClickHandler(OnOptionClicked);
            }
        }

        public void SetPanelVisible(bool visible)
        {
            Visible = visible;
        }

        public void Refresh(Battle battle, long playerUnitId)
        {
            BattleUnit player = ResolvePlayer(battle, playerUnitId);
            BattleAttackComponent attackComponent = player?.GetComponent<BattleAttackComponent>();
            int playerEmitterCount = attackComponent?.Attacks.Count ?? 0;

            bool rebuilt = false;
            if (_optionConfigs.Count == 0 || playerEmitterCount != _lastPlayerEmitterCount)
            {
                RebuildOptions();
                _lastPlayerEmitterCount = playerEmitterCount;
                rebuilt = true;
            }

            bool canAdd = player != null && !player.IsDisposed && _optionConfigs.Count > 0;
            RefreshOptionItems(canAdd);
            if (rebuilt)
            {
                ResetScrollPosition();
            }
        }

        private void CacheViews()
        {
            _optionItems.Clear();
            AddOptionItem(CreateWidget<BattleEmitterOptionItemWidget>(m_itemEmitterSlot));

            _optionPrefab = m_itemEmitterSlot;
        }

        private void RebuildOptions()
        {
            _optionConfigs.Clear();
            List<EmitterConfig> dataList = ConfigHelper.EmitterConfig?.DataList;
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            _optionConfigs.AddRange(dataList
                .Where(config => config != null && config.IsEnabled)
                .OrderByDescending(config => config.Priority)
                .ThenBy(config => config.Id));
        }

        private void RefreshOptionItems(bool canAdd)
        {
            EnsureOptionItemCount(_optionConfigs.Count);

            for (int i = 0; i < _optionItems.Count; i++)
            {
                EmitterConfig config = i < _optionConfigs.Count ? _optionConfigs[i] : null;
                _optionItems[i].RefreshOption(i, config, canAdd);
            }
        }

        private void EnsureOptionItemCount(int count)
        {
            int currentCount = _optionItems.Count;
            for (int i = currentCount; i < count; i++)
            {
                BattleEmitterOptionItemWidget item = CreateWidgetByPrefab<BattleEmitterOptionItemWidget>(_optionPrefab, m_tfEmitterSlots);
                AddOptionItem(item);
            }

            for (int i = count; i < _optionItems.Count; i++)
            {
                _optionItems[i].Visible = false;
            }
        }

        private void AddOptionItem(BattleEmitterOptionItemWidget item)
        {
            item.SetClickHandler(OnOptionClicked);
            _optionItems.Add(item);
        }

        private void OnOptionClicked(EmitterConfig config)
        {
            _addEmitterHandler?.Invoke(config);
        }

        private void ResetScrollPosition()
        {
            m_scrollEmitterOptions.verticalNormalizedPosition = 1f;
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

    }
}
