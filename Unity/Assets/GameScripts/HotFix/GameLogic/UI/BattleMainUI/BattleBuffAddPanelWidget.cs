using System;
using System.Collections.Generic;
using System.Linq;
using ET;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed partial class BattleBuffAddPanelWidget : UIWidget
    {
        private const int MaxBuffOptions = 12;

        private readonly List<BattleBuffOptionItemWidget> _items = new List<BattleBuffOptionItemWidget>(MaxBuffOptions);
        private readonly List<EmitterEffectPackConfig> _configs = new List<EmitterEffectPackConfig>(MaxBuffOptions);
        private GameObject _itemPrefab;
        private TMP_FontAsset _font;
        private Material _fontMaterial;
        private Action<EmitterEffectPackConfig> _addBuffHandler;
        private long _selectedEmitterId;
        private bool _rewardMode;

        protected override void OnCreate()
        {
            CacheViews();
            SetPanelVisible(false);
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

            foreach (BattleBuffOptionItemWidget item in _items)
            {
                item.SetFontFrom(source);
            }
        }

        public void SetAddBuffHandler(Action<EmitterEffectPackConfig> handler)
        {
            _addBuffHandler = handler;
        }

        public void SetSelectedEmitter(long emitterId)
        {
            _selectedEmitterId = emitterId;
            RefreshTitle();
        }

        public void TogglePanel()
        {
            if (!Visible)
            {
                _rewardMode = false;
                _configs.Clear();
            }

            SetPanelVisible(!Visible);
        }

        public void SetPanelVisible(bool visible)
        {
            Visible = visible;
        }

        public void Refresh()
        {
            if (!_rewardMode && _configs.Count == 0)
            {
                RebuildOptions();
            }

            bool canAdd = _selectedEmitterId != 0 && _configs.Count > 0;
            SetActive(m_tmpEmpty.gameObject, _configs.Count == 0);
            EnsureItemCount(_configs.Count);

            for (int i = 0; i < _items.Count; i++)
            {
                EmitterEffectPackConfig config = i < _configs.Count ? _configs[i] : null;
                _items[i].RefreshOption(config, canAdd);
            }
        }

        public void ShowRewardOptions(IReadOnlyList<EmitterEffectPackConfig> configs, long emitterId)
        {
            _rewardMode = true;
            _selectedEmitterId = emitterId;
            _configs.Clear();

            if (configs != null)
            {
                int count = Math.Min(3, configs.Count);
                for (int i = 0; i < count; i++)
                {
                    if (configs[i] != null)
                    {
                        _configs.Add(configs[i]);
                    }
                }
            }

            RefreshTitle();
            SetPanelVisible(true);
            Refresh();
        }

        private void CacheViews()
        {
            if (m_tmpTitle == null || m_tmpEmpty == null || m_tfBuffOptions == null || m_itemBuffOption == null)
            {
                throw new InvalidOperationException("BattleBuffAddPanelWidget prefab bindings are incomplete.");
            }

            m_tmpEmpty.SetText("暂无可添加效果包");
            _items.Clear();
            AddItem(m_itemBuffOption);
            _itemPrefab = m_itemBuffOption.gameObject;
            RefreshTitle();
        }

        private void EnsureItemCount(int count)
        {
            while (_items.Count < count)
            {
                BattleBuffOptionItemWidget item = CreateWidgetByPrefab<BattleBuffOptionItemWidget>(_itemPrefab, m_tfBuffOptions);
                AddItem(item);
            }

            for (int i = count; i < _items.Count; i++)
            {
                _items[i].Visible = false;
            }
        }

        private void RebuildOptions()
        {
            _configs.Clear();
            List<EmitterEffectPackConfig> dataList = ConfigHelper.EmitterEffectPackConfig?.DataList;
            if (dataList == null)
            {
                return;
            }

            _configs.AddRange(dataList
                .Where(config => config != null && config.EffectIds != null && config.EffectIds.Length > 0)
                .OrderBy(config => config.Id)
                .Take(MaxBuffOptions));
        }

        private void AddItem(BattleBuffOptionItemWidget item)
        {
            if (item == null)
            {
                throw new InvalidOperationException("BattleBuffAddPanelWidget option item is missing.");
            }

            item.SetFontFrom(m_tmpTitle);
            item.SetClickHandler(OnOptionClicked);
            _items.Add(item);
        }

        private void RefreshTitle()
        {
            string targetText = _selectedEmitterId != 0 ? $"目标 {System.Math.Abs(_selectedEmitterId % 1000)}" : "未选择目标";
            string prefix = _rewardMode ? "击杀奖励 - 三选一 Buff" : "添加效果包";
            m_tmpTitle.SetText($"{prefix} - {targetText}");
        }

        private void OnOptionClicked(EmitterEffectPackConfig config)
        {
            if (config == null || _selectedEmitterId == 0)
            {
                return;
            }

            _addBuffHandler?.Invoke(config);
            if (_rewardMode)
            {
                _rewardMode = false;
                _configs.Clear();
                SetPanelVisible(false);
            }
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null)
            {
                throw new InvalidOperationException("BattleBuffAddPanelWidget text binding is missing.");
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

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
