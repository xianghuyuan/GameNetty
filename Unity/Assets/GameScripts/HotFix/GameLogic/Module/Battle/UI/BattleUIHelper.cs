using Cysharp.Threading.Tasks;
using UnityEngine;

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

        public static void OnControlModeChanged(long unitId, int newMode)
        {
            if (_hudWindow == null)
            {
                return;
            }

            _hudWindow.SetControlMode(newMode);
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
    /// 单位死亡表现：播放死亡动画 → 延迟后清理。
    /// 乐观模式下客户端本地先触发，等服务端确认后最终清理。
    /// 若服务端纠错（复活），BattleUnitView.DeathCancelled 会被置为 true，延迟销毁将被跳过。
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitDeadView : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUnit unit = args.BattleUnit;
            BattleUIHelper.CloseUnitUI(unit.Id);

            var view = unit.GetComponent<BattleUnitView>();
            if (view != null && view.SkeletonAnimation != null)
            {
                view.PlayDeathAnimation();

                // 等死亡动画播放完毕后再清理
                float duration = view.SkeletonAnimation.Skeleton.Data.FindAnimation("die")?.Duration ?? 0.8f;
                int delayMs = (int)(duration * 1000) + 100;
                await scene.Root().GetComponent<TimerComponent>().WaitAsync(delayMs);

                // 服务端纠错：复活标志被设置则取消销毁
                if (view.IsDisposed || view.DeathCancelled)
                {
                    await ETTask.CompletedTask;
                    return;
                }

                // 销毁视图 GameObject
                if (view.GameObject != null)
                {
                    UnityEngine.Object.Destroy(view.GameObject);
                    view.GameObject = null;
                }
                view.Initialized = false;
            }

            // 销毁整个 BattleUnit 实体（含所有子组件）
            if (unit != null && !unit.IsDisposed)
            {
                unit.Dispose();
            }
        }
    }

    /// <summary>
    /// 单位复活表现：服务端纠错时回滚客户端乐观死亡。
    /// 恢复单位状态、重建 UI、恢复视图到存活表现。
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitRevivedView : AEvent<Scene, BattleUnitRevived>
    {
        protected override async ETTask Run(Scene scene, BattleUnitRevived args)
        {
            BattleUnit unit = args.BattleUnit;

            // 取消正在等待的死亡销毁
            var view = unit.GetComponent<BattleUnitView>();
            if (view != null && !view.IsDisposed)
            {
                view.DeathCancelled = true;

                // 恢复存活动画
                if (view.SkeletonAnimation != null)
                {
                    view.CurrentAnimName = "idle";
                    view.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
                }

                // 重新显示 GameObject（若被隐藏）
                if (view.GameObject != null)
                {
                    view.GameObject.SetActive(true);
                }
            }

            // 重建单位 UI
            BattleUIHelper.CreateUnitUI(unit);

            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleEnd_UI : AEvent<Scene, BattleEnd>
    {
        protected override async ETTask Run(Scene scene, BattleEnd args)
        {
            BattleUIHelper.ClearAll();
            BattleCameraHelper.Cleanup();
            GameModule.UI.GetUIAsync<GameLogic.BattleUI>(ui =>
            {
                if (ui != null) ui.SetBattleActive(false);
            });
            await ETTask.CompletedTask;
        }
    }
}
