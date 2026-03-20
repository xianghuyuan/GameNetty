using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ET
{
    /// <summary>
    /// 战斗UI管理器
    /// 负责将 ET 逻辑层的 BattleUnit 数据分发给 TEngine UI 层的 BattleHUDWindow 窗口
    /// </summary>
    public static class BattleUIHelper
    {
        private static GameLogic.BattleHUDWindow _hudWindow;

        /// <summary>
        /// 绑定 HUD 主窗口 (由 BattleHUDWindow.OnCreate 调用)
        /// </summary>
        public static void BindHUD(GameLogic.BattleHUDWindow window)
        {
            _hudWindow = window;
        }

        /// <summary>
        /// 创建并绑定单位UI
        /// </summary>
        public static void CreateUnitUI(BattleUnit unit)
        {
            if (_hudWindow == null) return;
            _hudWindow.AddUnitWidget(unit);
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

            if (_hudWindow == null) return;
            _hudWindow.UpdateUnitWidget(unit);
        }

        /// <summary>
        /// 关闭并清理指定单位的UI
        /// </summary>
        public static void CloseUnitUI(long battleUnitId)
        {
            if (_hudWindow == null) return;
            _hudWindow.RemoveUnitWidget(battleUnitId);
        }

        /// <summary>
        /// 清理所有UI绑定
        /// </summary>
        public static void ClearAll()
        {
            if (_hudWindow != null)
            {
                _hudWindow.ClearAllWidgets();
                GameModule.UI.CloseUI<GameLogic.BattleHUDWindow>();
            }
            _hudWindow = null;
        }
    }

    // --- 事件处理器 ---

    [Event(SceneType.Main)]
    public class BattleUnitCreated_UI : AEvent<Scene, BattleUnitCreated>
    {
        protected override async ETTask Run(Scene scene, BattleUnitCreated args)
        {
            BattleUIHelper.CreateUnitUI(args.Unit);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitNumericChange_UI : AEvent<Scene, BattleUnitNumericChange>
    {
        protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
        {
            BattleUIHelper.OnNumericChange(args.BattleUnit, args.NumericType, args.NewValue);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitDead_UI : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUIHelper.CloseUnitUI(args.BattleUnit.Id);
            await ETTask.CompletedTask;
        }
    }

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
