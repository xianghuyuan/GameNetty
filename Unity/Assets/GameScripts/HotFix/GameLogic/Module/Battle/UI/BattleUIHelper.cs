using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 战斗UI管理器
    /// 负责维护 ET 逻辑层与 TEngine 战斗主窗口之间的少量状态桥接。
    /// </summary>
    public static class BattleUIHelper
    {
        private static GameLogic.BattleMainWindow _mainWindow;

        /// <summary>
        /// 绑定战斗主窗口 (由 BattleMainWindow.OnCreate 调用)
        /// </summary>
        public static void BindMainWindow(GameLogic.BattleMainWindow window)
        {
            _mainWindow = window;
        }

        public static void OnBattleStarted(Battle battle)
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.SetBattle(battle);
        }

        public static void RefreshUnit(BattleUnit unit)
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.RefreshUnit(unit);
        }

        public static void OnUnitDead(BattleUnit unit)
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.OnUnitDead(unit);
        }

        public static void OnWaveStarted(Battle battle, int waveNumber)
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.SetWave(waveNumber, battle?.TotalWaves ?? 0);
        }

        public static void OnWaveCompleted(Battle battle, int waveNumber)
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.SetWaveComplete(waveNumber, battle?.TotalWaves ?? 0);
        }

        public static void OnControlModeChanged(long unitId, int newMode)
        {
            if (_mainWindow == null)
            {
                return;
            }

            _mainWindow.SetControlMode(newMode);
        }

        /// <summary>
        /// 清理所有UI绑定
        /// </summary>
        public static void ClearAll()
        {
            if (_mainWindow != null)
            {
                GameModule.UI.CloseUI<GameLogic.BattleMainWindow>();
            }
            _mainWindow = null;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitNumericChange_BattleMainWindow : AEvent<Scene, BattleUnitNumericChange>
    {
        protected override async ETTask Run(Scene scene, BattleUnitNumericChange args)
        {
            if (args.NumericType == NumericType.Hp || args.NumericType == NumericType.MaxHp)
            {
                BattleUIHelper.RefreshUnit(args.BattleUnit);
            }

            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class BattleUnitDamaged_BattleMainWindow : AEvent<Scene, BattleUnitDamaged>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDamaged args)
        {
            BattleUIHelper.RefreshUnit(args.Unit);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class WaveStart_BattleMainWindow : AEvent<Scene, WaveStart>
    {
        protected override async ETTask Run(Scene scene, WaveStart args)
        {
            BattleUIHelper.OnWaveStarted(args.Battle, args.WaveNumber);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Main)]
    public class WaveComplete_BattleMainWindow : AEvent<Scene, WaveComplete>
    {
        protected override async ETTask Run(Scene scene, WaveComplete args)
        {
            BattleUIHelper.OnWaveCompleted(args.Battle, args.WaveNumber);
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
            BattleUIHelper.OnUnitDead(unit);

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
    /// 恢复单位状态和视图到存活表现。
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

            BattleUIHelper.RefreshUnit(unit);
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
