using System.Collections.Generic;
using ET;
using TMPro;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    public partial class BattleHUDWindow : UIWindow
    {
        private readonly Dictionary<long, BattlePlayerInfoUI> _playerWidgets = new();
        private readonly Dictionary<long, BattleMonsterInfoUI> _monsterWidgets = new();
        private int? _currentControlMode;

        protected override void OnCreate()
        {
            ScriptGenerator();
            EnsureControlModeLabel();
            BattleUIHelper.BindHUD(this);
        }

        protected override void OnDestroy()
        {
            BattleUIHelper.BindHUD(null);
            ClearAllWidgets();
        }

        public void AddUnitWidget(BattleUnit unit)
        {
            if (unit.Camp == UnitCamp.Friend)
            {
                if (!_currentControlMode.HasValue)
                {
                    SetControlMode(1);
                }

                if (_playerWidgets.ContainsKey(unit.Id)) return;
                var widget = CreateWidgetByType<BattlePlayerInfoUI>(transform);
                widget.SetBattleUnit(unit.Id);
                _playerWidgets[unit.Id] = widget;
            }
            else
            {
                if (_monsterWidgets.ContainsKey(unit.Id)) return;
                var widget = CreateWidgetByType<BattleMonsterInfoUI>(m_tfEnemy);
                widget.SetBattleUnit(unit.Id);
                _monsterWidgets[unit.Id] = widget;
            }
            UpdateUnitWidget(unit);
        }

        public void UpdateUnitWidget(BattleUnit unit)
        {
            var numeric = unit.GetComponent<NumericComponent>();
            if (numeric == null) return;

            long hp = numeric.GetByKey(NumericType.Hp);
            long maxHp = numeric.GetByKey(NumericType.MaxHp);
            long attack = numeric.GetByKey(NumericType.Attack);
            
            float range = 1.5f;
            var combat = unit.GetComponent<BattleUnitCombatComponent>();
            if (combat != null) range = combat.AttackRange;

            if (unit.Camp == UnitCamp.Friend)
            {
                if (_playerWidgets.TryGetValue(unit.Id, out var widget))
                {
                    widget.SetData(hp, maxHp, attack, range);
                }
            }
            else
            {
                if (_monsterWidgets.TryGetValue(unit.Id, out var widget))
                {
                    widget.SetData(hp, maxHp, attack, range);
                }
            }
        }

        public void RemoveUnitWidget(long unitId)
        {
            if (_playerWidgets.Remove(unitId, out var pWidget))
            {
                pWidget.Destroy();
            }
            if (_monsterWidgets.Remove(unitId, out var mWidget))
            {
                mWidget.Destroy();
            }
        }

        public void ClearAllWidgets()
        {
            foreach (var widget in _playerWidgets.Values) widget.Destroy();
            foreach (var widget in _monsterWidgets.Values) widget.Destroy();
            _playerWidgets.Clear();
            _monsterWidgets.Clear();
            _currentControlMode = null;
            RefreshControlModeLabel();
        }

        public void SetControlMode(int mode)
        {
            _currentControlMode = mode;
            RefreshControlModeLabel();
        }

        private void EnsureControlModeLabel()
        {
            RefreshControlModeLabel();
        }

        private void RefreshControlModeLabel()
        {
            string modeText = _currentControlMode switch
            {
                1 => "自动",
                0 => "手动",
                _ => "未知",
            };
            m_tmpControlMode.text = $"当前战斗模式：{modeText}";
        }
    }
}