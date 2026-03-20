using System.Collections.Generic;
using ET;
using TEngine;

namespace GameLogic
{
    public partial class BattleHUDWindow : UIWindow
    {
        private readonly Dictionary<long, BattlePlayerInfoUI> _playerWidgets = new();
        private readonly Dictionary<long, BattleMonsterInfoUI> _monsterWidgets = new();

        protected override void OnCreate()
        {
            ScriptGenerator();
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
                if (_playerWidgets.ContainsKey(unit.Id)) return;
                var widget = CreateWidgetByType<BattlePlayerInfoUI>(transform);
                widget.SetBattleUnit(unit.Id);
                _playerWidgets[unit.Id] = widget;
            }
            else
            {
                if (_monsterWidgets.ContainsKey(unit.Id)) return;
                var widget = CreateWidgetByType<BattleMonsterInfoUI>(transform);
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
        }
    }
}