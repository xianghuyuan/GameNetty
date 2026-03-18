using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ET
{
    /// <summary>
    /// 战斗UI数据绑定事件
    /// </summary>
    public struct BattleUINumericUpdate
    {
        public BattleUnit BattleUnit;
        public int NumericType;
        public long OldValue;
        public long NewValue;
    }
    
    /// <summary>
    /// 战斗UI管理器
    /// 负责将BattleUnit数据绑定到UI显示
    /// </summary>
    public static class BattleUIHelper
    {
        private static readonly Dictionary<long, GameLogic.BattlePlayerInfoUI> _playerInfoUIs = new();
        private static readonly Dictionary<long, GameLogic.BattleMonsterinfoUI> _monsterInfoUIs = new();
        
        /// <summary>
        /// 创建并绑定玩家信息UI
        /// </summary>
        public static async UniTask CreatePlayerInfoUI(BattleUnit unit)
        {
            var ui = await GameModule.UI.ShowUIAsync<GameLogic.BattlePlayerInfoUI>();
            if (ui != null)
            {
                _playerInfoUIs[unit.Id] = ui;
                RefreshPlayerUI(unit);
            }
        }
        
        /// <summary>
        /// 创建并绑定怪物信息UI
        /// </summary>
        public static async UniTask CreateMonsterInfoUI(BattleUnit unit)
        {
            var ui = await GameModule.UI.ShowUIAsync<GameLogic.BattleMonsterinfoUI>();
            if (ui != null)
            {
                _monsterInfoUIs[unit.Id] = ui;
                RefreshMonsterUI(unit);
            }
        }
        
        /// <summary>
        /// 绑定玩家信息UI
        /// </summary>
        public static void BindPlayerInfoUI(long battleUnitId, GameLogic.BattlePlayerInfoUI ui)
        {
            _playerInfoUIs[battleUnitId] = ui;
        }
        
        /// <summary>
        /// 绑定怪物信息UI
        /// </summary>
        public static void BindMonsterInfoUI(long battleUnitId, GameLogic.BattleMonsterinfoUI ui)
        {
            _monsterInfoUIs[battleUnitId] = ui;
        }
        
        /// <summary>
        /// 解绑UI
        /// </summary>
        public static void UnbindUI(long battleUnitId)
        {
            _playerInfoUIs.Remove(battleUnitId);
            _monsterInfoUIs.Remove(battleUnitId);
        }
        
        /// <summary>
        /// 处理数值变化事件
        /// </summary>
        public static void OnNumericChange(BattleUnit unit, int numericType, long newValue)
        {
            // 只处理HP相关属性
            if (numericType != NumericType.Hp && numericType != NumericType.MaxHp && numericType != NumericType.Attack)
            {
                return;
            }
            
            // 根据阵营刷新对应UI
            if (unit.Camp == UnitCamp.Friend)
            {
                RefreshPlayerUI(unit);
            }
            else
            {
                RefreshMonsterUI(unit);
            }
        }
        
        /// <summary>
        /// 刷新玩家UI
        /// </summary>
        public static void RefreshPlayerUI(BattleUnit unit)
        {
            if (!_playerInfoUIs.TryGetValue(unit.Id, out var ui)) return;
            
            var numeric = unit.GetComponent<NumericComponent>();
            if (numeric == null) return;
            
            long hp = numeric.GetByKey(NumericType.Hp);
            long maxHp = numeric.GetByKey(NumericType.MaxHp);
            long attack = numeric.GetByKey(NumericType.Attack);
            
            ui.SetData(hp, maxHp, attack);
        }
        
        /// <summary>
        /// 刷新怪物UI
        /// </summary>
        public static void RefreshMonsterUI(BattleUnit unit)
        {
            if (!_monsterInfoUIs.TryGetValue(unit.Id, out var ui)) return;
            
            var numeric = unit.GetComponent<NumericComponent>();
            if (numeric == null) return;
            
            long hp = numeric.GetByKey(NumericType.Hp);
            long maxHp = numeric.GetByKey(NumericType.MaxHp);
            long attack = numeric.GetByKey(NumericType.Attack);
            
            ui.SetData(hp, maxHp, attack);
        }
        
        /// <summary>
        /// 关闭并清理指定单位的UI
        /// </summary>
        public static void CloseUnitUI(long battleUnitId)
        {
            if (_playerInfoUIs.TryGetValue(battleUnitId, out var playerUI))
            {
                GameModule.UI.CloseUI(playerUI);
                _playerInfoUIs.Remove(battleUnitId);
            }
            
            if (_monsterInfoUIs.TryGetValue(battleUnitId, out var monsterUI))
            {
                GameModule.UI.CloseUI(monsterUI);
                _monsterInfoUIs.Remove(battleUnitId);
            }
        }
        
        /// <summary>
        /// 清理所有UI绑定
        /// </summary>
        public static void ClearAll()
        {
            foreach (var ui in _playerInfoUIs.Values)
            {
                GameModule.UI.CloseUI(ui);
            }
            foreach (var ui in _monsterInfoUIs.Values)
            {
                GameModule.UI.CloseUI(ui);
            }
            
            _playerInfoUIs.Clear();
            _monsterInfoUIs.Clear();
        }
    }
    
    /// <summary>
    /// 战斗单位创建事件 - 创建信息UI
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitCreated_UI : AEvent<Scene, BattleUnitCreated>
    {
        protected override async ETTask Run(Scene scene, BattleUnitCreated args)
        {
            BattleUnit unit = args.Unit;
            
            // 根据阵营创建对应UI
            if (unit.Camp == UnitCamp.Friend)
            {
                await BattleUIHelper.CreatePlayerInfoUI(unit);
            }
            else
            {
                await BattleUIHelper.CreateMonsterInfoUI(unit);
            }
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// BattleUnit 数值变化事件处理 - 更新UI
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitNumericChange_UI : AEvent<Scene, BattleUnitNumericChange>
    {
        protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
        {
            BattleUIHelper.OnNumericChange(args.BattleUnit, args.NumericType, args.NewValue);
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 战斗单位死亡事件 - 关闭UI
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitDead_UI : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUIHelper.CloseUnitUI(args.BattleUnit.Id);
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 战斗结束事件 - 清理UI绑定
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleEnd_UI : AEvent<Scene, BattleEnd>
    {
        protected override async ETTask Run(Scene scene, BattleEnd args)
        {
            BattleUIHelper.ClearAll();
            await ETTask.CompletedTask;
        }
    }
}
