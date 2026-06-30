using System;
using System.Collections.Generic;
using ET;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed partial class BattleEmitterAdjustPanelWidget : UIWidget
    {
        private readonly List<BuffSlot> _buffSlots = new List<BuffSlot>();
        private readonly List<BuffOption> _buffOptions = new List<BuffOption>();

        private Action<int> _levelChangedHandler;
        private Action<int> _addBuffHandler;
        private Action<int> _removeBuffHandler;
        private Action _deleteEmitterHandler;
        private UIAdjustStepperWidget _levelAdjustWidget;
        private GameObject _buffSlotPrefab;
        private GameObject _buffOptionPrefab;
        private int _currentLevel = 1;
        private int _maxLevel = 1;

        protected override void OnCreate()
        {
            _levelAdjustWidget = CreateWidget<UIAdjustStepperWidget>(m_itemLevelAdjust);
            _buffSlotPrefab = m_itemBuffSlot;
            _buffOptionPrefab = m_itemBuffOption;
            m_tmpTitle.SetText("发射器调节");
            _levelAdjustWidget.SetRange(1f, 1f, 1f);
            _levelAdjustWidget.SetFormatter(FormatLevel);
            _levelAdjustWidget.SetChangedHandler(OnLevelChanged);
            SetPanelVisible(false);
        }

        private partial void OnClickCloseBtn()
        {
            SetPanelVisible(false);
        }

        private partial void OnClickDeleteEmitterBtn()
        {
            _deleteEmitterHandler?.Invoke();
        }

        public void SetPanelVisible(bool visible)
        {
            Visible = visible;
        }

        public void SetLevelChangedHandler(Action<int> handler)
        {
            _levelChangedHandler = handler;
        }

        public void SetAddBuffHandler(Action<int> handler)
        {
            _addBuffHandler = handler;
            foreach (BuffOption option in _buffOptions)
            {
                option.SetAddHandler(OnAddBuff);
            }
        }

        public void SetRemoveBuffHandler(Action<int> handler)
        {
            _removeBuffHandler = handler;
            foreach (BuffSlot slot in _buffSlots)
            {
                slot.SetRemoveHandler(OnRemoveBuff);
            }
        }

        public void SetDeleteEmitterHandler(Action handler)
        {
            _deleteEmitterHandler = handler;
        }

        public void Refresh(VehicleData vehicle, BattleAttackRuntime attackRuntime = null)
        {
            if (vehicle == null)
            {
                SetPanelVisible(false);
                return;
            }

            EmitterConfig config = ConfigHelper.EmitterConfig?.GetOrDefault(vehicle.VehicleConfigId);
            EmitterUpgradeConfig levelConfig = config != null
                ? EmitterUpgradeRuntimeHelper.ResolveLevelConfig(config, vehicle.Level)
                : null;
            _maxLevel = config != null ? EmitterUpgradeRuntimeHelper.ResolveMaxLevel(config) : 1;
            _maxLevel = Mathf.Max(1, _maxLevel);
            _currentLevel = Mathf.Clamp(vehicle.Level, 1, _maxLevel);

            m_tmpEmitterName.SetText(GetEmitterDisplayName(vehicle, config));
            m_tmpStats.SetText(BuildStatsText(vehicle, attackRuntime, levelConfig, _maxLevel));
            _levelAdjustWidget.SetRange(1f, _maxLevel, 1f);
            _levelAdjustWidget.SetValue(_currentLevel, false);
            RefreshBuffSlots(vehicle);
            RefreshBuffOptions(vehicle);
        }

        private string FormatLevel(float value)
        {
            return $"等级 {Mathf.RoundToInt(value)}/{_maxLevel}";
        }

        private void OnLevelChanged(float value)
        {
            int level = Mathf.Clamp(Mathf.RoundToInt(value), 1, _maxLevel);
            if (level == _currentLevel)
            {
                return;
            }

            _currentLevel = level;
            _levelChangedHandler?.Invoke(level);
        }

        private void RefreshBuffSlots(VehicleData vehicle)
        {
            int slotCount = Math.Max(0, vehicle.BuffSlotCount);
            EnsureBuffSlotCount(slotCount);

            for (int i = 0; i < _buffSlots.Count; i++)
            {
                BuffSlot slot = _buffSlots[i];
                if (i >= slotCount)
                {
                    slot.Visible = false;
                    continue;
                }

                EmitterEffectPackConfig config = null;
                if (vehicle.SlottedEffectPackIds != null && i < vehicle.SlottedEffectPackIds.Count)
                {
                    config = ConfigHelper.EmitterEffectPackConfig?.GetOrDefault(vehicle.SlottedEffectPackIds[i]);
                }

                slot.Refresh(i, config);
            }
        }

        private void RefreshBuffOptions(VehicleData vehicle)
        {
            List<EmitterEffectPackConfig> dataList = ConfigHelper.EmitterEffectPackConfig?.DataList;
            int optionCount = dataList?.Count ?? 0;
            EnsureBuffOptionCount(optionCount);

            bool hasEmptySlot = (vehicle.SlottedEffectPackIds?.Count ?? 0) < Math.Max(0, vehicle.BuffSlotCount);

            for (int i = 0; i < _buffOptions.Count; i++)
            {
                EmitterEffectPackConfig config = dataList != null && i < dataList.Count ? dataList[i] : null;
                bool canAdd = config != null && hasEmptySlot;
                _buffOptions[i].Refresh(config, canAdd);
            }
        }

        private void EnsureBuffSlotCount(int count)
        {
            int currentCount = _buffSlots.Count;
            for (int i = currentCount; i < count; i++)
            {
                AddBuffSlot(CreateWidgetByPrefab<BuffSlot>(_buffSlotPrefab, m_tfBuffSlots));
            }
        }

        private void EnsureBuffOptionCount(int count)
        {
            int currentCount = _buffOptions.Count;
            for (int i = currentCount; i < count; i++)
            {
                AddBuffOption(CreateWidgetByPrefab<BuffOption>(_buffOptionPrefab, m_tfBuffOptions));
            }
        }

        private void AddBuffSlot(BuffSlot slot)
        {
            slot.SetRemoveHandler(OnRemoveBuff);
            _buffSlots.Add(slot);
        }

        private void AddBuffOption(BuffOption option)
        {
            option.SetAddHandler(OnAddBuff);
            _buffOptions.Add(option);
        }

        private void OnAddBuff(int effectPackId)
        {
            _addBuffHandler?.Invoke(effectPackId);
        }

        private void OnRemoveBuff(int slotIndex)
        {
            _removeBuffHandler?.Invoke(slotIndex);
        }

        private static string GetEmitterDisplayName(VehicleData vehicle, EmitterConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.Name))
            {
                return config.Name;
            }

            return vehicle.VehicleConfigId > 0 ? $"发射器 {vehicle.VehicleConfigId}" : "调试发射器";
        }

        private static string BuildStatsText(
            VehicleData vehicle,
            BattleAttackRuntime attackRuntime,
            EmitterUpgradeConfig levelConfig,
            int maxLevel)
        {
            int cooldownMs = attackRuntime?.CooldownMs ?? vehicle.AttackCooldownMs;
            float range = attackRuntime?.AttackRange ?? vehicle.AttackRange;
            float baseDamage = attackRuntime?.BaseDamage ?? vehicle.BaseDamage;
            float attackRatio = attackRuntime?.WhiteAttackRatio ?? vehicle.WhiteAttackRatio;
            int slotCount = Math.Max(0, vehicle.BuffSlotCount);
            int usedCount = vehicle.SlottedEffectPackIds?.Count ?? 0;
            string levelLine = $"等级 {vehicle.Level}/{maxLevel}";

            if (levelConfig != null)
            {
                levelLine = $"等级 {levelConfig.Level}/{maxLevel}";
            }

            return $"{levelLine}\n伤害 {baseDamage:0.#}+攻击 x{attackRatio:0.##}\nCD {cooldownMs}ms  射程 {range:0.0}\nBuff {usedCount}/{slotCount}";
        }
    }
}
