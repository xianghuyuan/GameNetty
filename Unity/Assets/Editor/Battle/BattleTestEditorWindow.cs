#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ET;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif
using UnityEditor;
using UnityEngine;
namespace GameNetty.EditorTools
{
    public enum BattleTestToolTab
    {
#if ODIN_INSPECTOR
        [LabelText("战斗总览")]
#endif
        [InspectorName("战斗总览")]
        BattleOverview,
#if ODIN_INSPECTOR
        [LabelText("刷怪")]
#endif
        [InspectorName("刷怪")]
        Spawn,
#if ODIN_INSPECTOR
        [LabelText("发射器预设")]
#endif
        [InspectorName("发射器预设")]
        EmitterPreset,
#if ODIN_INSPECTOR
        [LabelText("关卡预设")]
#endif
        [InspectorName("关卡预设")]
        LevelPreset,
#if ODIN_INSPECTOR
        [LabelText("玩家发射器")]
#endif
        [InspectorName("玩家发射器")]
        PlayerEmitter,
#if ODIN_INSPECTOR
        [LabelText("单位监视")]
#endif
        [InspectorName("单位监视")]
        UnitMonitor,
    }

    /// <summary>
    /// 战斗测试面板。
    /// 在一个窗口内统一处理战斗、玩家、刷怪、单位监视、发射器和 Camera / World 诊断。
    /// 菜单: Tools/GameNetty/战斗/战斗测试
    /// </summary>
    public sealed class BattleTestEditorWindow :
#if ODIN_INSPECTOR
        OdinEditorWindow
#else
        EditorWindow
#endif
    {
        private const string MenuPath = "Tools/GameNetty/战斗/战斗测试";
        private const string EmitterPresetFolder = "Assets/Editor/Battle/EmitterPresets";
        private const string LevelPresetFolder = "Assets/Editor/Battle/LevelPresets";

#if ODIN_INSPECTOR
        [ShowInInspector, EnumToggleButtons, HideLabel, PropertyOrder(-100)]
#endif
        private BattleTestToolTab _activeToolTab = BattleTestToolTab.BattleOverview;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, HideLabel, MultiLineProperty(3), PropertyOrder(-99)]
#endif
        private string ToolScope => GetToolScope();

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, HideLabel, BoxGroup("状态快照"), ShowIf("@_activeToolTab == BattleTestToolTab.BattleOverview"), PropertyOrder(-80)]
        private BattleStatusSnapshot BattleStatus => BuildBattleStatusSnapshot();

        [ShowInInspector, TableList(AlwaysExpanded = true), BoxGroup("单位状态"), ShowIf("@_activeToolTab == BattleTestToolTab.UnitMonitor"), PropertyOrder(-80)]
        private List<UnitStatusRow> UnitStatus => BuildUnitStatusRows();

        [ShowInInspector, ReadOnly, TableList(AlwaysExpanded = true), BoxGroup("发射器状态"), ShowIf("@_activeToolTab == BattleTestToolTab.PlayerEmitter"), PropertyOrder(-80)]
        private List<EmitterStatusRow> EquippedEmitterStatus => BuildEmitterStatusRows();
#endif

#if !ODIN_INSPECTOR
        private Vector2 _fallbackScrollPos;
#endif

        #region Battle Tab State

        private int _spawnCount = 1;
        private int _monsterHp = 1;
        private int _monsterAtk = 10;
        private int _monsterDef = 1;
        private float _monsterSpeed = 1f;
        private float _spawnOffset = 1f;
        private float _spreadRange = 3f;
        private bool _autoExpandUnitList = true;

        private int _healAmount = 500;
        private long _cachedPlayerUnitId;
        private int _cachedPlayerConfigId = 1001;
        private bool _cameraDebugLogging;

        private Vector2 _unitListScroll;
        private bool _showUnitList;

        #endregion

        #region Vehicle Tab State

        private int _vehicleAttackCd = 1000;
        private float _vehicleAttackRange = 3.0f;
        private int _vehicleBuffSlotCount = 4;
        private int _vehicleCreateCount = 1;
        private int _vehicleEditIndex;
        private bool _vehicleCanMoveCast;
        private int _buffShardId;
        private int _vehicleBuffSlotIndex;
        private Vector2 _buffListScroll;
        private string _buffSearchFilter = "";
        private BattleEmitterPresetAsset _emitterPresetAsset;
        private BattleLevelPresetAsset _levelPresetAsset;

        #endregion

        private string _statusMsg = "";
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _summaryStyle;

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            BattleTestEditorWindow window = GetWindow<BattleTestEditorWindow>();
            window.titleContent = new GUIContent("战斗测试");
            window.minSize = new Vector2(460f, 720f);
            window.Show();
            window.Focus();
        }

        private void OnDisable()
        {
            BattleCameraHelper.SetDebugLogging(false);
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

#if ODIN_INSPECTOR
        [OnInspectorGUI, PropertyOrder(0)]
        private void DrawOdinGui()
        {
            DrawWindowGui();
        }
#else
        private void OnGUI()
        {
            _fallbackScrollPos = EditorGUILayout.BeginScrollView(_fallbackScrollPos);
            DrawWindowGui();
            EditorGUILayout.EndScrollView();
        }
#endif

        private void DrawWindowGui()
        {
            EnsureStyles();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("这个面板只在 Unity Play 模式下可用。先进入 Play，再用它启动离线战斗、刷怪和调试发射器。", MessageType.Warning);
            }

#if !ODIN_INSPECTOR
            DrawFallbackToolTabs();
#endif
            EditorGUILayout.Space(6);
            DrawStatusBar();
            DrawBattleDebugPage();
        }

#if !ODIN_INSPECTOR
        private void DrawFallbackToolTabs()
        {
            string[] tabs =
            {
                "战斗总览",
                "刷怪",
                "发射器预设",
                "关卡预设",
                "玩家发射器",
                "单位监视",
            };
            _activeToolTab = (BattleTestToolTab)GUILayout.Toolbar((int)_activeToolTab, tabs);
            EditorGUILayout.HelpBox(GetToolScope(), MessageType.Info);
        }
#endif

        // =====================================================================
        //  主面板
        // =====================================================================

        private void DrawBattleDebugPage()
        {
            switch (_activeToolTab)
            {
                case BattleTestToolTab.BattleOverview:
#if !ODIN_INSPECTOR
                    DrawOverviewSection();
                    EditorGUILayout.Space(8);
#endif
                    DrawBattleQuickActionsSection();
                    break;

                case BattleTestToolTab.Spawn:
                    DrawSpawnSection();
                    break;

                case BattleTestToolTab.EmitterPreset:
                    DrawEmitterPresetSection(FindPlayerUnit(GetCurrentBattle()));
                    break;

                case BattleTestToolTab.LevelPreset:
                    DrawLevelPresetSection();
                    break;

                case BattleTestToolTab.PlayerEmitter:
                    DrawPlayerEmitterTool();
                    break;

                case BattleTestToolTab.UnitMonitor:
#if !ODIN_INSPECTOR
                    DrawUnitMonitorSection();
#else
                    EditorGUILayout.HelpBox("单位状态已由上方 Odin 表格展示；每行可直接执行击杀。", MessageType.Info);
#endif
                    break;
            }
        }

        #region 战斗控制

        private void DrawOverviewSection()
        {
            EditorGUILayout.LabelField("战斗概览", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("先进入 Play 模式，然后在这里统一查看战斗状态、玩家状态、怪物数量和镜头数据。", MessageType.Warning);
                return;
            }

            Scene root = Init.Root;
            BattleComponent battleComponent = root?.GetComponent<BattleComponent>();
            EventBridgeComponent bridgeComponent = root?.GetComponent<EventBridgeComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Init.Root: {(root != null ? "OK" : "Missing")}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"BattleComponent: {(battleComponent != null ? "OK" : "Missing")}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"EventBridge: {(bridgeComponent != null ? "OK" : "Missing")}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            if (battle == null)
            {
                EditorGUILayout.HelpBox("当前没有进行中的战斗。点击下方“启动离线战斗”后，这里会聚合显示整场战斗的核心状态。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            List<BattleUnit> allUnits = battle.GetAllBattleUnits();
            int friendCount = allUnits.Count(u => u.Camp == UnitCamp.Friend && !u.IsDead);
            int enemyCount = allUnits.Count(u => u.Camp == UnitCamp.Enemy && !u.IsDead);
            int deadCount = allUnits.Count(u => u.IsDead);
            long elapsed = TimeInfo.Instance.ClientNow() - battle.StartTime;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"状态: {GetBattleStateName(battle.State)}", GUILayout.Width(140));
            EditorGUILayout.LabelField($"耗时: {elapsed / 1000f:F1}s", GUILayout.Width(110));
            EditorGUILayout.LabelField($"波次: {battle.CurrentWave}/{battle.TotalWaves}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"AI Tick: {(battle.GetComponent<ClientPlayerAITickComponent>() != null ? "OK" : "Missing")}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"友方: {friendCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"敌方: {enemyCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"死亡: {deadCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"BattleId: {battle.BattleId}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            if (player == null)
            {
                EditorGUILayout.HelpBox("当前战斗中未找到玩家 BattleUnit。可以在下方玩家区域执行“重建玩家”。", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            NumericComponent num = player.GetComponent<NumericComponent>();
            int hp = num?.GetAsInt(NumericType.Hp) ?? 0;
            int maxHp = num?.GetAsInt(NumericType.MaxHp) ?? 0;
            float speed = num?.GetAsFloat(NumericType.Speed) ?? 0f;
            float hpRatio = maxHp > 0 ? (float)hp / maxHp : 0f;

            Rect hpRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
            EditorGUI.ProgressBar(hpRect, hpRatio, $"玩家HP: {hp}/{maxHp} {(player.IsDead ? "[死亡]" : string.Empty)}");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"PlayerX: {player.Position.x:F2}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Speed: {speed:F2}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"ConfigId: {player.ConfigId}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"UnitId: {player.Id}");
            EditorGUILayout.EndHorizontal();

            float cameraX = BattleCameraHelper.GetCameraX();
            float cameraWidth = BattleCameraHelper.GetCameraWidth();
            BattleUnit nearestEnemy = FindNearestEnemy(battle, player);

            EditorGUILayout.BeginHorizontal();
            bool newCameraDebugLogging = EditorGUILayout.ToggleLeft("相机日志", _cameraDebugLogging, GUILayout.Width(90));
            if (newCameraDebugLogging != _cameraDebugLogging)
            {
                _cameraDebugLogging = newCameraDebugLogging;
                BattleCameraHelper.SetDebugLogging(_cameraDebugLogging);
            }

            EditorGUILayout.LabelField($"CameraX: {cameraX:F2}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Width: {cameraWidth:F2}", GUILayout.Width(100));

            if (nearestEnemy != null)
            {
                float worldDistance = Mathf.Abs(nearestEnemy.Position.x - player.Position.x);
                float enemyScreenOffset = nearestEnemy.Position.x - cameraX;
                EditorGUILayout.LabelField($"最近敌人: {nearestEnemy.Id}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Dist: {worldDistance:F2}", GUILayout.Width(90));
                EditorGUILayout.LabelField($"Screen: {enemyScreenOffset:F2}");
            }
            else
            {
                EditorGUILayout.LabelField("最近敌人: 无");
            }
            EditorGUILayout.EndHorizontal();

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>();
            BattleAttackComponent attackComp = player.GetComponent<BattleAttackComponent>();
            if (vehicleComp != null && vehicleComp.HasVehicleEquipped)
            {
                VehicleData vehicle = vehicleComp.EquippedVehicle;
                BattleAttackRuntime equippedAttack = attackComp?.Attacks.FirstOrDefault(a => a.AttackRuntimeId == vehicle.VehicleId);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"发射器: {vehicle.VehicleId}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Buff槽: {vehicle.SlottedBuffIds?.Count ?? 0}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"CD: {equippedAttack?.CooldownMs ?? vehicle.AttackCooldownMs}ms", GUILayout.Width(110));
                EditorGUILayout.LabelField($"Range: {(equippedAttack?.AttackRange ?? vehicle.AttackRange):F1}");
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("发射器: 未装备");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBattleQuickActionsSection()
        {
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

            Battle battle = GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);
            bool canOperateBattle = battle != null && battle.State == BattleState.Fighting;
            bool canRevive = player != null && player.IsDead;

            EditorGUILayout.BeginVertical("box");

            _levelPresetAsset = (BattleLevelPresetAsset)EditorGUILayout.ObjectField("关卡预设:", _levelPresetAsset, typeof(BattleLevelPresetAsset), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("启动离线战斗", GUILayout.Height(28)))
            {
                StartOfflineBattleInternal();
            }
            EditorGUI.BeginDisabledGroup(_levelPresetAsset == null);
            if (GUILayout.Button("按关卡启动", GUILayout.Height(28)))
            {
                StartOfflineLevelInternal();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(battle == null);
            if (GUILayout.Button("退出战斗", GUILayout.Height(28)))
            {
                ExitOfflineBattleInternal();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!canOperateBattle || player == null);
            if (GUILayout.Button("满血", GUILayout.Height(24)))
            {
                FullHealPlayer(player);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canRevive);
            if (GUILayout.Button("复活玩家", GUILayout.Height(24)))
            {
                RevivePlayer(player);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(battle == null || battle.State != BattleState.Fighting);
            if (GUILayout.Button("暂停", GUILayout.Height(24)))
            {
                battle.Pause();
                SetStatus("战斗已暂停");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(battle == null || battle.State != BattleState.Paused);
            if (GUILayout.Button("继续", GUILayout.Height(24)))
            {
                battle.Resume();
                SetStatus("战斗已继续");
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!canOperateBattle);
            if (GUILayout.Button("清空敌人", GUILayout.Height(24)))
            {
                KillAllEnemies(battle);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMsg))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_statusMsg, MessageType.None);
            }

            EditorGUILayout.LabelField("快捷刷怪");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!canOperateBattle);
            if (GUILayout.Button("单只近身", GUILayout.Height(24)))
            {
                SpawnMonsterPreset(battle, 1, 1f, 0f);
            }
            if (GUILayout.Button("5只散开", GUILayout.Height(24)))
            {
                SpawnMonsterPreset(battle, 5, 2.5f, 3f);
            }
            if (GUILayout.Button("10只压测", GUILayout.Height(24)))
            {
                SpawnMonsterPreset(battle, 10, 3.5f, 6f);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawBattleStatusSection()
        {
            EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);

            Battle battle = GetCurrentBattle();
            Scene root = Init.Root;

            if (!Application.isPlaying || root == null)
            {
                EditorGUILayout.HelpBox("运行态未就绪。需要先进入 Play 模式，并确保主场景已经初始化完成。", MessageType.Warning);
                return;
            }

            if (battle == null)
            {
                EditorGUILayout.HelpBox("当前没有进行中的战斗", MessageType.Warning);
                EditorGUILayout.LabelField("点击上面的“启动离线战斗”即可进入空战斗模式。");
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"状态: {GetBattleStateName(battle.State)}", GUILayout.Width(160));

                long elapsed = TimeInfo.Instance.ClientNow() - battle.StartTime;
                EditorGUILayout.LabelField($"已用时: {elapsed / 1000f:F1}秒");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"战斗ID: {battle.BattleId}", GUILayout.Width(200));
                EditorGUILayout.LabelField($"波次: {battle.CurrentWave}/{battle.TotalWaves}");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"玩家AI Tick: {(battle.GetComponent<ClientPlayerAITickComponent>() != null ? "已挂载" : "未挂载")}");
            }
        }

        private static string GetBattleStateName(BattleState state)
        {
            return state switch
            {
                BattleState.None => "无",
                BattleState.Preparing => "准备中",
                BattleState.Fighting => "战斗中",
                BattleState.Paused => "已暂停",
                BattleState.Ended => "已结束",
                _ => state.ToString()
            };
        }

        #endregion

        #region 视角诊断

        private void DrawCameraWorldSection()
        {
            bool newCameraDebugLogging = EditorGUILayout.ToggleLeft("启用相机调试日志", _cameraDebugLogging);
            if (newCameraDebugLogging != _cameraDebugLogging)
            {
                _cameraDebugLogging = newCameraDebugLogging;
                BattleCameraHelper.SetDebugLogging(_cameraDebugLogging);
            }

            Battle battle = GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);

            if (battle == null || player == null)
            {
                EditorGUILayout.HelpBox("进入战斗后，这里会显示玩家世界坐标、相机坐标、视口边界和最近怪物的屏幕相对偏移。", MessageType.None);
                return;
            }

            float cameraX = BattleCameraHelper.GetCameraX();
            float cameraWidth = BattleCameraHelper.GetCameraWidth();
            float cameraLeft = cameraX - cameraWidth * 0.5f;
            float cameraRight = cameraX + cameraWidth * 0.5f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"PlayerX: {player.Position.x:F2}", GUILayout.Width(140));
            EditorGUILayout.LabelField($"CameraX: {cameraX:F2}", GUILayout.Width(140));
            EditorGUILayout.LabelField($"Width: {cameraWidth:F2}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"CameraBounds: [{cameraLeft:F2}, {cameraRight:F2}]");

            BattleUnit nearestEnemy = FindNearestEnemy(battle, player);
            if (nearestEnemy == null)
            {
                EditorGUILayout.LabelField("NearestEnemy: none");
                return;
            }

            float worldDistance = Mathf.Abs(nearestEnemy.Position.x - player.Position.x);
            float enemyScreenOffset = nearestEnemy.Position.x - cameraX;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"EnemyX: {nearestEnemy.Position.x:F2}", GUILayout.Width(140));
            EditorGUILayout.LabelField($"WorldDist: {worldDistance:F2}", GUILayout.Width(140));
            EditorGUILayout.LabelField($"ScreenOffset: {enemyScreenOffset:F2}");
            EditorGUILayout.EndHorizontal();
        }

        private static BattleUnit FindNearestEnemy(Battle battle, BattleUnit player)
        {
            if (battle == null || player == null)
            {
                return null;
            }

            BattleUnit nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsDead) continue;
                if (unit.Camp == player.Camp) continue;

                float dist = Mathf.Abs(unit.Position.x - player.Position.x);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = unit;
                }
            }

            return nearest;
        }

        private string GetToolScope()
        {
            return _activeToolTab switch
            {
                BattleTestToolTab.BattleOverview => "战斗总览：启动/退出离线战斗，查看战斗状态，执行暂停、继续、清场、回血等全局操作。",
                BattleTestToolTab.Spawn => "刷怪：配置怪物属性、生成数量和怪物发射器预设，只影响后续生成的怪物。",
                BattleTestToolTab.EmitterPreset => "发射器预设：创建、编辑、保存 ScriptableObject 预设，不要求当前战斗存在玩家。",
                BattleTestToolTab.LevelPreset => "关卡预设：用 ScriptableObject 保存玩家发射器和刷怪波次，并一键启动离线关卡。",
                BattleTestToolTab.PlayerEmitter => "玩家发射器：把面板字段或预设装备到当前玩家，并实时调整已装备发射器和 Buff。",
                BattleTestToolTab.UnitMonitor => "单位监视：查看当前战斗内单位列表，并执行单个单位击杀调试。",
                _ => string.Empty,
            };
        }

#if ODIN_INSPECTOR
        private BattleStatusSnapshot BuildBattleStatusSnapshot()
        {
            Scene root = Init.Root;
            Battle battle = GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);
            List<BattleUnit> units = battle?.GetAllBattleUnits() ?? new List<BattleUnit>();
            NumericComponent numeric = player?.GetComponent<NumericComponent>();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();

            return new BattleStatusSnapshot
            {
                Root = root != null ? "OK" : "Missing",
                Battle = battle != null ? GetBattleStateName(battle.State) : "None",
                BattleId = battle?.BattleId.ToString() ?? "-",
                Wave = battle != null ? $"{battle.CurrentWave}/{battle.TotalWaves}" : "-",
                Elapsed = battle != null ? $"{(TimeInfo.Instance.ClientNow() - battle.StartTime) / 1000f:F1}s" : "-",
                Friend = units.Count(u => u.Camp == UnitCamp.Friend && !u.IsDead),
                Enemy = units.Count(u => u.Camp == UnitCamp.Enemy && !u.IsDead),
                Dead = units.Count(u => u.IsDead),
                Player = player != null ? player.Id.ToString() : "-",
                PlayerHp = numeric != null ? $"{numeric.GetAsInt(NumericType.Hp)}/{numeric.GetAsInt(NumericType.MaxHp)}" : "-",
                PlayerX = player != null ? player.Position.x.ToString("F2") : "-",
                PlayerEmitterCount = vehicleComponent?.GetEquippedVehicles().Count ?? 0,
            };
        }

        private List<UnitStatusRow> BuildUnitStatusRows()
        {
            Battle battle = GetCurrentBattle();
            List<UnitStatusRow> rows = new();
            if (battle == null)
            {
                return rows;
            }

            foreach (BattleUnit unit in battle.GetAllBattleUnits())
            {
                NumericComponent numeric = unit.GetComponent<NumericComponent>();
                rows.Add(new UnitStatusRow
                {
                    Unit = unit,
                    Camp = unit.Camp.ToString(),
                    Id = unit.Id,
                    ConfigId = unit.ConfigId,
                    Hp = numeric != null ? $"{numeric.GetAsInt(NumericType.Hp)}/{numeric.GetAsInt(NumericType.MaxHp)}" : "-",
                    X = unit.Position.x,
                    Dead = unit.IsDead,
                    Boss = unit.IsBoss,
                });
            }

            return rows;
        }

        private List<EmitterStatusRow> BuildEmitterStatusRows()
        {
            BattleUnit player = FindPlayerUnit(GetCurrentBattle());
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            BattleAttackComponent attackComponent = player?.GetComponent<BattleAttackComponent>();
            List<EmitterStatusRow> rows = new();
            if (vehicleComponent == null)
            {
                return rows;
            }

            foreach (VehicleData vehicle in vehicleComponent.GetEquippedVehicles())
            {
                BattleAttackRuntime attack = attackComponent?.Attacks.FirstOrDefault(a => a.AttackRuntimeId == vehicle.VehicleId);
                rows.Add(new EmitterStatusRow
                {
                    Id = vehicle.VehicleId,
                    CooldownMs = attack?.CooldownMs ?? vehicle.AttackCooldownMs,
                    Range = attack?.AttackRange ?? vehicle.AttackRange,
                    CanMoveCast = vehicle.CanMoveCast,
                    BuffSlots = vehicle.SlottedBuffIds?.Count ?? 0,
                    ActiveBuffs = CountNonZeroBuffs(vehicle.SlottedBuffIds),
                });
            }

            return rows;
        }
#endif

        private void EnsureStyles()
        {
            _sectionTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fixedHeight = 22f,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 0, 2, 2),
            };

            _cardStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 10),
                margin = new RectOffset(0, 0, 4, 8),
            };

            _mutedLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
            };

            _summaryStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 4, 6),
                fontSize = 11,
                wordWrap = true,
            };
        }

        private void DrawSectionHeader(string title, string subtitle = null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(title, _sectionTitleStyle);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, _mutedLabelStyle);
            }
        }

        private void BeginCard()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
        }

        private void EndCard()
        {
            EditorGUILayout.EndVertical();
        }

        private void DrawSummaryLine(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                EditorGUILayout.LabelField(text, _summaryStyle);
            }
        }

        #endregion

        #region 玩家操作

        private void DrawPlayerSection()
        {
            Battle battle = GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);

            if (player != null)
            {
                NumericComponent num = player.GetComponent<NumericComponent>();
                int hp = num?.GetAsInt(NumericType.Hp) ?? 0;
                int maxHp = num?.GetAsInt(NumericType.MaxHp) ?? 0;
                float speed = num?.GetAsFloat(NumericType.Speed) ?? 0f;
                bool dead = player.IsDead;

                if (dead)
                {
                    EditorGUILayout.HelpBox("玩家已死亡", MessageType.Error);
                }
                else
                {
                    float hpRatio = maxHp > 0 ? (float)hp / maxHp : 0f;
                    Rect barRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.ProgressBar(barRect, hpRatio, $"生命: {hp}/{maxHp}");
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"速度: {speed:F1}", GUILayout.Width(90));
                EditorGUILayout.LabelField($"位置: {player.Position.x:F1}");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("治疗:", GUILayout.Width(45));
                _healAmount = EditorGUILayout.IntField(_healAmount, GUILayout.Width(70));
                if (GUILayout.Button($"+{_healAmount}", GUILayout.Width(60)))
                {
                    HealPlayer(player, _healAmount);
                }
                if (GUILayout.Button("满血", GUILayout.Width(65)))
                {
                    FullHealPlayer(player);
                }
                EditorGUILayout.EndHorizontal();

                if (dead && GUILayout.Button("复活玩家", GUILayout.Height(28)))
                {
                    RevivePlayer(player);
                }
            }
            else if (battle != null)
            {
                EditorGUILayout.HelpBox("玩家单位已销毁", MessageType.Warning);
                if (GUILayout.Button("重建玩家", GUILayout.Height(28)))
                {
                    RebuildPlayer(battle);
                }
            }
            else
            {
                EditorGUILayout.LabelField("（无玩家）");
            }
        }

        private void HealPlayer(BattleUnit player, int amount)
        {
            if (player.IsDead) return;
            NumericComponent num = player.GetComponent<NumericComponent>();
            if (num == null) return;
            int maxHp = num.GetAsInt(NumericType.MaxHp);
            int hp = num.GetAsInt(NumericType.Hp) + amount;
            if (hp > maxHp) hp = maxHp;
            player.SetNumeric(NumericType.Hp, hp);
            _statusMsg = $"治疗 {amount}，生命: {hp}/{maxHp}";
        }

        private void FullHealPlayer(BattleUnit player)
        {
            if (player.IsDead) return;
            NumericComponent num = player.GetComponent<NumericComponent>();
            if (num == null) return;
            player.SetNumeric(NumericType.Hp, num.GetAsInt(NumericType.MaxHp));
            _statusMsg = "已满血";
        }

        private void RevivePlayer(BattleUnit player)
        {
            if (!player.IsDead) return;
            player.IsDead = false;
            NumericComponent num = player.GetComponent<NumericComponent>();
            if (num != null)
                player.SetNumeric(NumericType.Hp, num.GetAsInt(NumericType.MaxHp));

            BattleUnitView view = player.GetComponent<BattleUnitView>();
            if (view != null && !view.IsDisposed)
            {
                view.DeathCancelled = true;
                if (view.GameObject != null) view.GameObject.SetActive(true);
                if (view.SkeletonAnimation != null)
                {
                    view.CurrentAnimName = "idle";
                    view.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
                }
                view.Initialized = true;
            }

            EventSystem.Instance.Publish(player.Scene(), new BattleUnitRevived { BattleUnit = player });
            _statusMsg = "玩家已复活";
        }

        private void RebuildPlayer(Battle battle)
        {
            RebuildPlayerAsync(battle).Forget();
        }

        private async UniTaskVoid RebuildPlayerAsync(Battle battle)
        {
            long unitId = IdGenerater.Instance.GenerateInstanceId();

            BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, _cachedPlayerConfigId);
            unit.Camp = UnitCamp.Friend;
            unit.Position = new Unity.Mathematics.float3(-5f, BattleAreaConfig.BattleUnitSpawnY, 0);
            unit.Forward = new Unity.Mathematics.float3(1f, 0, 0);
            unit.FaceDirection = 1f;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            numeric.Set(NumericType.Hp, 1000);
            numeric.Set(NumericType.MaxHp, 1000);
            numeric.Set(NumericType.Speed, 3f);

            unit.AddComponent<BattleUnitCombatComponent, float>(3f);

            unit.AddComponent<VehicleComponent>();
            unit.AddComponent<BattleAttackComponent>();
            unit.AddComponent<ClientPlayerAIComponent>();

            BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, Unity.Mathematics.float3>(unit.Camp, unit.Position);
            await view.InitViewAsync();
            BattleCameraHelper.SetupCameraFollow(view.GameObject.transform);

            if (battle.GetComponent<ClientPlayerAITickComponent>() == null)
            {
                battle.AddComponent<ClientPlayerAITickComponent>();
            }

            _cachedPlayerUnitId = unit.Id;
            _statusMsg = $"已重建玩家单位，ID={unit.Id}";
        }

        #endregion

        #region 关卡预设

        private void DrawLevelPresetSection()
        {
            DrawSectionHeader("关卡预设", "选择 ScriptableObject 关卡并启动；关卡数据包含玩家发射器和刷怪波次。");
            BeginCard();

            _levelPresetAsset = (BattleLevelPresetAsset)EditorGUILayout.ObjectField("关卡预设:", _levelPresetAsset, typeof(BattleLevelPresetAsset), false);

            EditorGUI.BeginDisabledGroup(_levelPresetAsset == null);
            if (GUILayout.Button("按关卡启动", GUILayout.Height(32)))
            {
                StartOfflineLevelInternal();
            }
            EditorGUI.EndDisabledGroup();

            if (_levelPresetAsset == null)
            {
                EditorGUILayout.HelpBox($"示例关卡在 {LevelPresetFolder}/Level_001.asset。", MessageType.Info);
                EndCard();
                return;
            }

            DrawSummaryLine($"关卡: {_levelPresetAsset.LevelName}");
            DrawSummaryLine($"玩家: X={_levelPresetAsset.PlayerSpawnX:F1} HP={_levelPresetAsset.PlayerHp} Speed={_levelPresetAsset.PlayerSpeed:F1}");
            DrawSummaryLine($"玩家发射器: {(_levelPresetAsset.PlayerEmitterPreset != null ? _levelPresetAsset.PlayerEmitterPreset.name : "未配置")}");

            int totalSpawns = 0;
            int totalMonsters = 0;
            if (_levelPresetAsset.Waves != null)
            {
                foreach (BattleLevelWaveEntry wave in _levelPresetAsset.Waves)
                {
                    if (wave?.Spawns == null) continue;
                    totalSpawns += wave.Spawns.Count;
                    foreach (BattleLevelSpawnEntry spawn in wave.Spawns)
                    {
                        totalMonsters += Mathf.Max(0, spawn?.Count ?? 0);
                    }
                }
            }

            DrawSummaryLine($"波次: {_levelPresetAsset.Waves?.Count ?? 0}  批次: {totalSpawns}  怪物总数: {totalMonsters}");

            if (_levelPresetAsset.Waves != null)
            {
                foreach (BattleLevelWaveEntry wave in _levelPresetAsset.Waves)
                {
                    if (wave == null) continue;
                    EditorGUILayout.LabelField($"{wave.Name}  Delay {wave.DelayMs}ms", _mutedLabelStyle);
                    if (wave.Spawns == null) continue;
                    foreach (BattleLevelSpawnEntry spawn in wave.Spawns)
                    {
                        if (spawn == null) continue;
                        string emitterName = spawn.EmitterPreset != null ? spawn.EmitterPreset.name : "默认近战";
                        EditorGUILayout.LabelField($"  {spawn.Count}x HP {spawn.Hp} ATK {spawn.Attack} Speed {spawn.Speed:F1}  Emitter {emitterName}", _mutedLabelStyle);
                    }
                }
            }

            EndCard();
        }

        #endregion

        #region 刷怪

        private void DrawSpawnSection()
        {
            DrawSectionHeader("刷怪", "生成调试怪物；若选择发射器预设，怪物会按预设挂载发射器。");
            BeginCard();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("常规预设", GUILayout.Width(80)))
            {
                ApplySpawnPreset(1, 10, 1, 1f, 1, 1f, 3f);
            }
            if (GUILayout.Button("围攻预设", GUILayout.Width(80)))
            {
                ApplySpawnPreset(5, 15, 2, 1.5f, 5, 2f, 4f);
            }
            if (GUILayout.Button("压测预设", GUILayout.Width(80)))
            {
                ApplySpawnPreset(20, 30, 5, 2f, 20, 4f, 8f);
            }
            if (GUILayout.Button("重置", GUILayout.Width(60)))
            {
                ApplySpawnPreset(1, 10, 1, 1f, 1, 1f, 3f);
            }
            EditorGUILayout.EndHorizontal();

            _monsterHp = EditorGUILayout.IntField("生命:", _monsterHp);
            _monsterAtk = EditorGUILayout.IntField("攻击:", _monsterAtk);
            _monsterDef = EditorGUILayout.IntField("防御:", _monsterDef);
            _monsterSpeed = EditorGUILayout.FloatField("速度:", _monsterSpeed);
            _spawnCount = EditorGUILayout.IntField("数量:", _spawnCount);

            _spawnOffset = EditorGUILayout.Slider("偏移:", _spawnOffset, 0f, 15f);
            _spreadRange = EditorGUILayout.Slider("散布:", _spreadRange, 0f, 10f);
            _emitterPresetAsset = (BattleEmitterPresetAsset)EditorGUILayout.ObjectField("发射器预设:", _emitterPresetAsset, typeof(BattleEmitterPresetAsset), false);

            Battle battle = GetCurrentBattle();
            EditorGUI.BeginDisabledGroup(battle == null || battle.State != BattleState.Fighting);
            if (GUILayout.Button("生成怪物", GUILayout.Height(32)))
            {
                SpawnMonsterWithCurrentEmitterSettings(battle, _spawnCount, _spawnOffset, _spreadRange);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(2);
            EditorGUI.BeginDisabledGroup(battle == null || battle.State != BattleState.Fighting);
            if (GUILayout.Button("击杀全部敌人", GUILayout.Height(26)))
            {
                KillAllEnemies(battle);
            }
            EditorGUI.EndDisabledGroup();

            EndCard();
        }

        private void KillAllEnemies(Battle battle)
        {
            if (battle == null) return;
            int killed = 0;
            foreach (var child in battle.Children.Values.ToList())
            {
                if (child is BattleUnit unit && unit.Camp == UnitCamp.Enemy && !unit.IsDead)
                {
                    unit.IsDead = true;
                    BattleUnitCombatComponent combat = unit.GetComponent<BattleUnitCombatComponent>();
                    if (combat != null)
                    {
                        unit.SetNumeric(NumericType.Hp, 0);
                    }
                    EventSystem.Instance.Publish(unit.Scene(), new BattleUnitDead { BattleUnit = unit });
                    killed++;
                }
            }
            _statusMsg = $"已击杀 {killed} 个敌人";
        }

        #endregion

        #region 单位监视

        private void DrawUnitMonitorSection()
        {
            EditorGUILayout.BeginHorizontal();
            _showUnitList = EditorGUILayout.Foldout(_showUnitList, "单位监视", true);
            GUILayout.FlexibleSpace();
            _autoExpandUnitList = EditorGUILayout.ToggleLeft("战斗时自动展开", _autoExpandUnitList, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            Battle battle = GetCurrentBattle();
            if (_autoExpandUnitList && battle != null && battle.State == BattleState.Fighting)
            {
                _showUnitList = true;
            }

            if (!_showUnitList) return;

            if (battle == null)
            {
                EditorGUILayout.LabelField("  （无战斗）");
                return;
            }

            var allUnits = battle.GetAllBattleUnits();
            int friendCount = allUnits.Count(u => u.Camp == UnitCamp.Friend && !u.IsDead);
            int enemyCount = allUnits.Count(u => u.Camp == UnitCamp.Enemy && !u.IsDead);
            int deadCount = allUnits.Count(u => u.IsDead);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"友方: {friendCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"敌方: {enemyCount}", GUILayout.Width(90));
            EditorGUILayout.LabelField($"已死亡: {deadCount}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            _unitListScroll = EditorGUILayout.BeginScrollView(_unitListScroll, GUILayout.Height(180));

            foreach (BattleUnit unit in allUnits)
            {
                if (unit.IsDisposed) continue;

                NumericComponent num = unit.GetComponent<NumericComponent>();
                int hp = num?.GetAsInt(NumericType.Hp) ?? 0;
                int maxHp = num?.GetAsInt(NumericType.MaxHp) ?? 0;

                string campTag = unit.Camp == UnitCamp.Friend ? "友" : "敌";
                string statusTag = unit.IsDead ? "已死亡" : $"{hp}/{maxHp}";
                string bossTag = unit.IsBoss ? " [BOSS]" : "";

                EditorGUILayout.BeginHorizontal("box");

                Color prevColor = GUI.color;
                if (unit.IsDead) GUI.color = Color.gray;
                else if (unit.Camp == UnitCamp.Enemy) GUI.color = new Color(1f, 0.6f, 0.6f);
                else GUI.color = new Color(0.6f, 1f, 0.6f);

                EditorGUILayout.LabelField($"[{campTag}] ID={unit.Id}{bossTag}", GUILayout.Width(200));
                EditorGUILayout.LabelField(statusTag, GUILayout.Width(100));
                EditorGUILayout.LabelField($"X={unit.Position.x:F1}", GUILayout.Width(70));

                GUI.color = prevColor;

                if (!unit.IsDead && GUILayout.Button("击杀", GUILayout.Width(40)))
                {
                    unit.IsDead = true;
                    unit.SetNumeric(NumericType.Hp, 0);
                    EventSystem.Instance.Publish(unit.Scene(), new BattleUnitDead { BattleUnit = unit });
                    _statusMsg = $"已击杀单位 {unit.Id}";
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        // =====================================================================
        //  发射器区域
        // =====================================================================

        private void DrawVehicleSection()
        {
            Battle battle = GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);

            DrawEmitterPresetSection(player);

            if (player == null)
            {
                EditorGUILayout.HelpBox("当前没有玩家单位。可以先创建和保存发射器预设；实时编辑已装备发射器需要进入战斗并找到玩家。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6);
            DrawPlayerEmitterCreateSection(player);
            EditorGUILayout.Space(6);
            DrawVehicleStatusSection(player);
            EditorGUILayout.Space(6);
            DrawBuffSlotSection(player);
            EditorGUILayout.Space(6);
            DrawBuffListSection(player);
        }

        private void DrawPlayerEmitterTool()
        {
            Battle battle = GetCurrentBattle();
            BattleUnit player = FindPlayerUnit(battle);
            if (player == null)
            {
                EditorGUILayout.HelpBox("当前没有玩家单位。启动战斗并创建玩家后，才能装备和实时编辑玩家发射器。", MessageType.Warning);
                return;
            }

            DrawPlayerEmitterCreateSection(player);
            EditorGUILayout.Space(6);
            DrawVehicleStatusSection(player);
            EditorGUILayout.Space(6);
            DrawBuffSlotSection(player);
            EditorGUILayout.Space(6);
            DrawBuffListSection(player);
        }

        #region 发射器状态

        private void DrawVehicleStatusSection(BattleUnit player)
        {
            DrawSectionHeader("已装备发射器", "选择一个发射器后，可实时调整冷却、射程、可移动释放和 Buff 槽。");
            BeginCard();

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>();
            BattleAttackComponent attackComp = player.GetComponent<BattleAttackComponent>();

            if (vehicleComp != null && vehicleComp.HasVehicleEquipped)
            {
                List<VehicleData> equippedVehicles = vehicleComp.GetEquippedVehicles();
                if (equippedVehicles.Count == 0)
                {
                    EditorGUILayout.LabelField("未装备发射器");
                }
                else
                {
                    _vehicleEditIndex = Mathf.Clamp(_vehicleEditIndex, 0, equippedVehicles.Count - 1);
                    EditorGUILayout.LabelField($"已装备: {equippedVehicles.Count} 个发射器");

                    for (int vehicleIndex = 0; vehicleIndex < equippedVehicles.Count; vehicleIndex++)
                    {
                        VehicleData vehicle = equippedVehicles[vehicleIndex];
                        BattleAttackRuntime equippedAttack = attackComp?.Attacks.FirstOrDefault(a => a.AttackRuntimeId == vehicle.VehicleId);

                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.BeginHorizontal();
                        bool selected = _vehicleEditIndex == vehicleIndex;
                        bool newSelected = EditorGUILayout.ToggleLeft("编辑", selected, GUILayout.Width(52));
                        if (newSelected && !selected)
                        {
                            _vehicleEditIndex = vehicleIndex;
                            vehicleComp.EquippedVehicle = vehicle;
                            vehicleComp.EquippedVehicleId = vehicle.VehicleId;
                        }

                        EditorGUILayout.LabelField($"ID={vehicle.VehicleId}", GUILayout.Width(150));
                        EditorGUILayout.LabelField($"CD={(equippedAttack?.CooldownMs ?? vehicle.AttackCooldownMs)}ms", GUILayout.Width(110));
                        EditorGUILayout.LabelField($"Range={(equippedAttack?.AttackRange ?? vehicle.AttackRange):F1}", GUILayout.Width(90));
                        EditorGUILayout.LabelField($"MoveCast={(vehicle.CanMoveCast ? "Y" : "N")}");
                        EditorGUILayout.EndHorizontal();

                        if (selected)
                        {
                            DrawVehicleRuntimeEditor(player, vehicleComp, vehicle);
                            DrawVehicleBuffSlots(player, vehicleComp, vehicle);
                        }

                        EditorGUILayout.EndVertical();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("未装备发射器");
            }

            EditorGUILayout.Space(4);

            if (vehicleComp != null && vehicleComp.HasVehicleEquipped)
            {
                if (GUILayout.Button("卸下全部发射器", GUILayout.Height(26)))
                {
                    vehicleComp.UnequipVehicle();
                    player.GetComponent<BattleAttackComponent>()?.SyncFromVehicleComponent(vehicleComp);
                    _vehicleEditIndex = 0;
                    _statusMsg = "已卸下全部发射器";
                    Repaint();
                }
            }

            EndCard();
        }

        private void DrawVehicleRuntimeEditor(BattleUnit player, VehicleComponent vehicleComp, VehicleData vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            vehicle.SlottedBuffIds ??= new List<int>();

            EditorGUILayout.LabelField("实时参数:");
            EditorGUI.BeginChangeCheck();

            int cooldownMs = Mathf.Max(1, EditorGUILayout.IntField("  冷却(ms):", vehicle.AttackCooldownMs));
            float attackRange = Mathf.Max(0.1f, EditorGUILayout.FloatField("  射程:", vehicle.AttackRange));
            bool canMoveCast = EditorGUILayout.Toggle("  可移动释放:", vehicle.CanMoveCast);
            int buffSlotCount = Mathf.Max(0, EditorGUILayout.IntField("  Buff槽数:", vehicle.SlottedBuffIds.Count));

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            vehicle.AttackCooldownMs = cooldownMs;
            vehicle.AttackRange = attackRange;
            vehicle.CanMoveCast = canMoveCast;
            ResizeBuffSlots(vehicle.SlottedBuffIds, buffSlotCount);

            vehicleComp.EquippedVehicle = vehicle;
            vehicleComp.EquippedVehicleId = vehicle.VehicleId;
            BattleAttackComponent attackComponent = player.GetComponent<BattleAttackComponent>();
            if (attackComponent != null)
            {
                attackComponent.SyncFromVehicleComponent(vehicleComp);
                attackComponent.ResetEmitterCooldown(vehicle.VehicleId);
            }
            _vehicleBuffSlotCount = buffSlotCount;
            _statusMsg = $"已实时更新发射器 {vehicle.VehicleId}";
            Repaint();
        }

        private void DrawVehicleBuffSlots(BattleUnit player, VehicleComponent vehicleComp, VehicleData vehicle)
        {
            if (vehicle.SlottedBuffIds == null)
            {
                vehicle.SlottedBuffIds = new List<int>();
            }

            EditorGUILayout.LabelField("已镶嵌 Buff:");
            for (int i = 0; i < vehicle.SlottedBuffIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                int buffGroupId = vehicle.SlottedBuffIds[i];
                if (buffGroupId != 0)
                {
                    BuffGroupConfig cfg = ConfigHelper.BuffGroupConfig?.GetOrDefault(buffGroupId);
                    string buffList = cfg == null || cfg.BuffIds == null || cfg.BuffIds.Length == 0 ? "空" : string.Join(",", cfg.BuffIds);
                    EditorGUILayout.LabelField($"  [{i}] Group#{buffGroupId} -> [{buffList}]");
                    if (GUILayout.Button("卸下", GUILayout.Width(40)))
                    {
                        vehicleComp.EquippedVehicle = vehicle;
                        vehicleComp.EquippedVehicleId = vehicle.VehicleId;
                        vehicleComp.UnslotBuff(i);
                        player.GetComponent<BattleAttackComponent>()?.SyncFromVehicleComponent(vehicleComp);
                        _statusMsg = $"已从发射器 {vehicle.VehicleId} 槽位 {i} 卸下 Buff组";
                        Repaint();
                        return;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"  [{i}] （空）");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void ResizeBuffSlots(List<int> slots, int slotCount)
        {
            if (slots == null)
            {
                return;
            }

            while (slots.Count < slotCount)
            {
                slots.Add(0);
            }

            while (slots.Count > slotCount)
            {
                slots.RemoveAt(slots.Count - 1);
            }
        }

        #endregion

        #region 创建发射器

        private void DrawEmitterPresetSection(BattleUnit player)
        {
            DrawSectionHeader("发射器预设", "编辑可复用的 ScriptableObject 模板；不直接修改当前战斗。");
            BeginCard();

            _vehicleAttackCd = EditorGUILayout.IntField("冷却(ms):", _vehicleAttackCd);
            _vehicleAttackRange = EditorGUILayout.FloatField("射程:", _vehicleAttackRange);
            _vehicleBuffSlotCount = Mathf.Max(0, EditorGUILayout.IntField("Buff槽数:", _vehicleBuffSlotCount));
            _vehicleCreateCount = Mathf.Max(1, EditorGUILayout.IntField("数量:", _vehicleCreateCount));
            _vehicleCanMoveCast = EditorGUILayout.Toggle("可移动释放:", _vehicleCanMoveCast);
            _emitterPresetAsset = (BattleEmitterPresetAsset)EditorGUILayout.ObjectField("发射器预设:", _emitterPresetAsset, typeof(BattleEmitterPresetAsset), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新建预设", GUILayout.Height(24)))
            {
                CreateEmitterPresetFromPanel();
            }
            EditorGUI.BeginDisabledGroup(_emitterPresetAsset == null);
            if (GUILayout.Button("从面板覆盖", GUILayout.Height(24)))
            {
                SavePanelToEmitterPreset(_emitterPresetAsset);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(_emitterPresetAsset == null || player == null);
            if (GUILayout.Button("从已装备覆盖", GUILayout.Height(24)))
            {
                SaveEquippedEmittersToPreset(player, _emitterPresetAsset);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (_emitterPresetAsset != null)
            {
                DrawEmitterPresetAssetEditor(_emitterPresetAsset);
                DrawEmitterPresetPreview(_emitterPresetAsset);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("应用到面板", GUILayout.Height(24)))
                {
                    ApplyEmitterPresetToPanel(_emitterPresetAsset);
                }
                EditorGUILayout.EndHorizontal();
            }

            EndCard();
        }

        private void DrawPlayerEmitterCreateSection(BattleUnit player)
        {
            DrawSectionHeader("玩家发射器", "把预设或当前面板字段装备到玩家；装备后可在下方实时修改参数。");
            BeginCard();

            Battle battle = GetCurrentBattle();
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(player == null || _emitterPresetAsset == null || battle == null || battle.State != BattleState.Fighting);
            if (GUILayout.Button("按预设装备", GUILayout.Height(24)))
            {
                CreateAndEquipVehicle(player, true);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(player == null || battle == null || battle.State != BattleState.Fighting);
            if (GUILayout.Button("按面板字段装备", GUILayout.Height(24)))
            {
                CreateAndEquipVehicle(player, false);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EndCard();
        }

        private void CreateAndEquipVehicle(BattleUnit player, bool usePresetAsset)
        {
            if (player == null) return;

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>();
            if (vehicleComp == null)
            {
                vehicleComp = player.AddComponent<VehicleComponent>();
            }

            List<BattleEmitterPresetEntry> presetEntries = usePresetAsset ? GetValidPresetEntries(_emitterPresetAsset) : new List<BattleEmitterPresetEntry>();
            if (usePresetAsset && presetEntries.Count > 0)
            {
                CreateVehiclesFromPreset(player, vehicleComp, presetEntries);
                return;
            }

            if (usePresetAsset)
            {
                SetStatus("发射器预设没有有效条目，未创建发射器");
                return;
            }

            int createCount = Mathf.Max(1, _vehicleCreateCount);
            VehicleData lastVehicle = null;
            for (int i = 0; i < createCount; i++)
            {
                VehicleData vehicle = vehicleComp.AddNewVehicle(1);
                vehicle.SlottedBuffIds = new List<int>(new int[_vehicleBuffSlotCount]);
                vehicle.AttackCooldownMs = _vehicleAttackCd;
                vehicle.AttackRange = _vehicleAttackRange;
                vehicle.CanMoveCast = _vehicleCanMoveCast;
                vehicle.State = VehicleState.Equipped;
                lastVehicle = vehicle;
            }

            if (lastVehicle != null)
            {
                vehicleComp.EquippedVehicleId = lastVehicle.VehicleId;
                vehicleComp.EquippedVehicle = lastVehicle;
            }

            BattleAttackComponent attackComp = player.GetComponent<BattleAttackComponent>();
            if (attackComp == null)
            {
                attackComp = player.AddComponent<BattleAttackComponent>();
            }
            attackComp.SyncFromVehicleComponent(vehicleComp);
            _vehicleEditIndex = Mathf.Max(0, vehicleComp.GetEquippedVehicles().Count - 1);

            _statusMsg = $"已创建 {createCount} 个发射器 冷却={_vehicleAttackCd}ms 射程={_vehicleAttackRange:F1} 槽位={_vehicleBuffSlotCount} 可移动释放={_vehicleCanMoveCast}";
            Repaint();
        }

        private void CreateVehiclesFromPreset(BattleUnit player, VehicleComponent vehicleComp, List<BattleEmitterPresetEntry> presetEntries)
        {
            VehicleData lastVehicle = null;
            foreach (BattleEmitterPresetEntry entry in presetEntries)
            {
                VehicleData vehicle = vehicleComp.AddNewVehicle(1);
                vehicle.SlottedBuffIds = BuildPresetBuffSlots(entry);
                vehicle.AttackCooldownMs = entry.CooldownMs;
                vehicle.AttackRange = entry.Range;
                vehicle.CanMoveCast = entry.CanMoveCast;
                vehicle.State = VehicleState.Equipped;
                lastVehicle = vehicle;
            }

            if (lastVehicle != null)
            {
                vehicleComp.EquippedVehicleId = lastVehicle.VehicleId;
                vehicleComp.EquippedVehicle = lastVehicle;
            }

            BattleAttackComponent attackComp = player.GetComponent<BattleAttackComponent>();
            if (attackComp == null)
            {
                attackComp = player.AddComponent<BattleAttackComponent>();
            }

            attackComp.SyncFromVehicleComponent(vehicleComp);
            _vehicleEditIndex = Mathf.Max(0, vehicleComp.GetEquippedVehicles().Count - 1);
            _statusMsg = $"已按发射器预设 {_emitterPresetAsset.name} 创建 {presetEntries.Count} 个发射器";
            Repaint();
        }

        private void CreateEmitterPresetFromPanel()
        {
            EnsureEmitterPresetFolder();

            BattleEmitterPresetAsset asset = CreateInstance<BattleEmitterPresetAsset>();
            asset.Emitters = BuildPanelPresetEntries();

            string path = AssetDatabase.GenerateUniqueAssetPath($"{EmitterPresetFolder}/NewEmitterPreset.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _emitterPresetAsset = asset;
            Selection.activeObject = asset;
            _statusMsg = $"已创建发射器预设: {path}";
            Repaint();
        }

        private void SavePanelToEmitterPreset(BattleEmitterPresetAsset asset)
        {
            if (asset == null)
            {
                CreateEmitterPresetFromPanel();
                return;
            }

            Undo.RecordObject(asset, "Save Emitter Preset From Panel");
            asset.Emitters = BuildPanelPresetEntries();
            SaveEmitterPresetAsset(asset, $"已从面板字段覆盖发射器预设 {asset.name}");
        }

        private void SaveEquippedEmittersToPreset(BattleUnit player, BattleEmitterPresetAsset asset)
        {
            if (player == null || asset == null)
            {
                return;
            }

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>();
            List<VehicleData> equippedVehicles = vehicleComp?.GetEquippedVehicles();
            if (equippedVehicles == null || equippedVehicles.Count == 0)
            {
                SetStatus("当前玩家没有已装备发射器");
                return;
            }

            List<BattleEmitterPresetEntry> entries = new(equippedVehicles.Count);
            for (int i = 0; i < equippedVehicles.Count; i++)
            {
                VehicleData vehicle = equippedVehicles[i];
                entries.Add(new BattleEmitterPresetEntry
                {
                    Name = $"Equipped {i + 1}",
                    CooldownMs = vehicle.AttackCooldownMs,
                    Range = vehicle.AttackRange,
                    BuffSlotCount = vehicle.SlottedBuffIds?.Count ?? 0,
                    CanMoveCast = vehicle.CanMoveCast,
                    BuffGroupIds = vehicle.SlottedBuffIds != null ? new List<int>(vehicle.SlottedBuffIds) : new List<int>(),
                });
            }

            Undo.RecordObject(asset, "Save Emitter Preset From Equipped");
            asset.Emitters = entries;
            SaveEmitterPresetAsset(asset, $"已从玩家已装备发射器覆盖预设 {asset.name}");
        }

        #endregion

        #region Buff镶嵌

        private void DrawBuffSlotSection(BattleUnit player)
        {
            EditorGUILayout.LabelField("Buff镶嵌", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buff组ID:", GUILayout.Width(55));
            _buffShardId = EditorGUILayout.IntField(_buffShardId, GUILayout.Width(80));
            EditorGUILayout.LabelField("槽位:", GUILayout.Width(40));
            _vehicleBuffSlotIndex = Mathf.Max(0, EditorGUILayout.IntField(_vehicleBuffSlotIndex, GUILayout.Width(40)));

            if (GUILayout.Button("镶嵌", GUILayout.Width(45)))
            {
                SlotBuffToVehicle(player, _vehicleBuffSlotIndex, _buffShardId);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("卸下全部", GUILayout.Height(24)))
            {
                UnslotAllBuffs(player);
            }
        }

        private void SlotBuffToVehicle(BattleUnit player, int slotIndex, int buffGroupId)
        {
            if (player == null || buffGroupId == 0) return;
            slotIndex = Mathf.Max(0, slotIndex);

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>();
            if (vehicleComp == null || !vehicleComp.HasVehicleEquipped)
            {
                _statusMsg = "未装备发射器";
                return;
            }

            BuffGroupConfig group = ConfigHelper.BuffGroupConfig?.GetOrDefault(buffGroupId);
            if (group == null)
            {
                _statusMsg = $"BuffGroupConfig {buffGroupId} 未找到";
                return;
            }

            VehicleData editableVehicle = GetEditableVehicle(vehicleComp);
            if (editableVehicle == null)
            {
                _statusMsg = "未选择可编辑发射器";
                return;
            }

            vehicleComp.EquippedVehicle = editableVehicle;
            vehicleComp.EquippedVehicleId = editableVehicle.VehicleId;
            vehicleComp.SlotBuff(slotIndex, buffGroupId);
            player.GetComponent<BattleAttackComponent>()?.SyncFromVehicleComponent(vehicleComp);
            string buffList = group.BuffIds == null || group.BuffIds.Length == 0 ? "空" : string.Join(",", group.BuffIds);
            _statusMsg = $"已将 Buff组 #{buffGroupId} (Buff:[{buffList}]) 镶嵌到发射器 {editableVehicle.VehicleId} 槽位 {slotIndex}";
            Repaint();
        }

        private void UnslotAllBuffs(BattleUnit player)
        {
            if (player == null) return;

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>();
            if (vehicleComp == null || !vehicleComp.HasVehicleEquipped) return;

            VehicleData editableVehicle = GetEditableVehicle(vehicleComp);
            if (editableVehicle == null) return;
            if (editableVehicle.SlottedBuffIds == null)
            {
                editableVehicle.SlottedBuffIds = new List<int>();
            }

            vehicleComp.EquippedVehicle = editableVehicle;
            vehicleComp.EquippedVehicleId = editableVehicle.VehicleId;
            for (int i = 0; i < editableVehicle.SlottedBuffIds.Count; i++)
            {
                vehicleComp.UnslotBuff(i);
            }
            player.GetComponent<BattleAttackComponent>()?.SyncFromVehicleComponent(vehicleComp);
            _statusMsg = $"已卸下发射器 {editableVehicle.VehicleId} 的全部 Buff组";
            Repaint();
        }

        private VehicleData GetEditableVehicle(VehicleComponent vehicleComp)
        {
            if (vehicleComp == null)
            {
                return null;
            }

            List<VehicleData> equippedVehicles = vehicleComp.GetEquippedVehicles();
            if (equippedVehicles.Count == 0)
            {
                return null;
            }

            _vehicleEditIndex = Mathf.Clamp(_vehicleEditIndex, 0, equippedVehicles.Count - 1);
            return equippedVehicles[_vehicleEditIndex];
        }

        #endregion

        #region Buff列表

        private void DrawBuffListSection(BattleUnit player)
        {
            EditorGUILayout.LabelField("Buff组配置列表", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("搜索:", GUILayout.Width(45));
            _buffSearchFilter = EditorGUILayout.TextField(_buffSearchFilter);
            if (GUILayout.Button("清除", GUILayout.Width(40)))
                _buffSearchFilter = "";
            EditorGUILayout.EndHorizontal();

            BuffGroupConfigCategory buffConfigs = ConfigHelper.BuffGroupConfig;
            if (buffConfigs == null || buffConfigs.DataList.Count == 0)
            {
                EditorGUILayout.LabelField("（BuffGroupConfig 未加载）");
                return;
            }

            string filter = _buffSearchFilter?.ToLowerInvariant() ?? "";

            _buffListScroll = EditorGUILayout.BeginScrollView(_buffListScroll, GUILayout.Height(180));

            foreach (BuffGroupConfig config in buffConfigs.DataList)
            {
                string buffList = config.BuffIds == null || config.BuffIds.Length == 0 ? "" : string.Join(",", config.BuffIds);
                if (!string.IsNullOrEmpty(filter))
                {
                    if (!config.Id.ToString().Contains(filter) &&
                        !buffList.ToLowerInvariant().Contains(filter))
                        continue;
                }

                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.LabelField($"#{config.Id}", GUILayout.Width(45));
                EditorGUILayout.LabelField($"Buff:[{buffList}]", GUILayout.Width(250));

                if (GUILayout.Button("镶嵌", GUILayout.Width(40)))
                {
                    _buffShardId = config.Id;
                    SlotBuffToVehicle(player, _vehicleBuffSlotIndex, config.Id);
                    _vehicleBuffSlotIndex = (_vehicleBuffSlotIndex + 1) % Mathf.Max(1, _vehicleBuffSlotCount);
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        // =====================================================================
        //  公共辅助
        // =====================================================================

        #region Helpers

        private static Battle GetCurrentBattle()
        {
            BattleComponent bc = Init.Root?.GetComponent<BattleComponent>();
            return bc?.GetCurrentBattle();
        }

        private void StartOfflineBattleInternal()
        {
            StartOfflineBattleAsync().Coroutine();
        }

        private void StartOfflineLevelInternal()
        {
            StartOfflineLevelAsync().Coroutine();
        }

        private async ETTask StartOfflineBattleAsync()
        {
            if (!EnsureRuntimeReady("启动离线战斗", out Scene root))
            {
                return;
            }

            EnsureBattleRuntimeDependencies(root);
            SetStatus("正在启动离线战斗...");

            try
            {
                BattleStartResult result = await BattleEntry.StartBattle(root, new BattleStartRequest
                {
                    Mode = BattleStartMode.Debug,
                });
                if (!result.IsSuccess || result.Battle == null)
                {
                    BattleComponent battleComponent = root.GetComponent<BattleComponent>();
                    string detail = battleComponent == null ? "BattleComponent 缺失" : (result.ErrorMessage ?? "Battle 创建返回 null");
                    SetStatus($"启动离线战斗失败：{detail}，请查看 Console 日志");
                    return;
                }

                Battle battle = result.Battle;
                BattleUnit player = FindPlayerUnit(battle);
                if (player != null)
                {
                    _cachedPlayerUnitId = player.Id;
                    _cachedPlayerConfigId = player.ConfigId;
                }

                SetStatus($"离线战斗已启动，BattleId={battle.BattleId}");
            }
            catch (System.Exception e)
            {
                Log.Error(e);
                SetStatus($"启动离线战斗异常：{e.GetType().Name} - {e.Message}");
            }
        }

        private async ETTask StartOfflineLevelAsync()
        {
            if (_levelPresetAsset == null)
            {
                SetStatus("请先选择关卡预设 SO");
                return;
            }

            if (!EnsureRuntimeReady("启动离线关卡", out Scene root))
            {
                return;
            }

            EnsureBattleRuntimeDependencies(root);
            SetStatus($"正在启动关卡：{_levelPresetAsset.LevelName}");

            try
            {
                Battle currentBattle = GetCurrentBattle();
                if (currentBattle != null)
                {
                    await BattleEntry.ExitCurrentBattle(root);
                }

                BattleStartResult result = await BattleEntry.StartBattle(root, new BattleStartRequest
                {
                    Mode = BattleStartMode.Debug,
                });
                if (!result.IsSuccess || result.Battle == null)
                {
                    SetStatus($"启动关卡失败：{result.ErrorMessage ?? "Battle 创建返回 null"}");
                    return;
                }

                Battle battle = result.Battle;
                BattleUnit player = FindPlayerUnit(battle);
                ApplyLevelPlayerConfig(player, _levelPresetAsset);

                _cachedPlayerUnitId = player?.Id ?? 0;
                _cachedPlayerConfigId = player?.ConfigId ?? _cachedPlayerConfigId;

                RunLevelWavesAsync(battle, _levelPresetAsset).Coroutine();
                SetStatus($"关卡已启动：{_levelPresetAsset.LevelName}");
            }
            catch (System.Exception e)
            {
                Log.Error(e);
                SetStatus($"启动关卡异常：{e.GetType().Name} - {e.Message}");
            }
        }

        private void ExitOfflineBattleInternal()
        {
            ExitOfflineBattleAsync().Coroutine();
        }

        private async ETTask ExitOfflineBattleAsync()
        {
            if (!EnsureRuntimeReady("退出离线战斗", out Scene root))
            {
                return;
            }

            Battle battle = GetCurrentBattle();
            if (battle == null)
            {
                SetStatus("当前没有可退出的战斗");
                return;
            }

            bool success = await BattleEntry.ExitCurrentBattle(root);
            SetStatus(success ? "战斗已结束" : "战斗退出失败");
        }

        private void SpawnMonsterPreset(Battle battle, int count, float offset, float spread)
        {
            if (battle == null || battle.State != BattleState.Fighting)
            {
                return;
            }

            SpawnMonsterWithCurrentEmitterSettings(battle, count, offset, spread);
        }

        private void SpawnMonsterWithCurrentEmitterSettings(Battle battle, int count, float offset, float spread)
        {
            List<BattleEmitterPresetEntry> presetEntries = GetValidPresetEntries(_emitterPresetAsset);
            if (presetEntries.Count > 0)
            {
                BattleDebugSpawnHelper.SpawnMonster(
                    battle,
                    _monsterHp,
                    _monsterAtk,
                    _monsterDef,
                    _monsterSpeed,
                    count,
                    offset,
                    spread,
                    BuildMonsterEmitterSpecs(presetEntries));
                SetStatus($"已按发射器预设 {_emitterPresetAsset.name} 生成 {count} 只怪物，每只 {presetEntries.Count} 个发射器");
                return;
            }

            bool fallbackFromInvalidPreset = _emitterPresetAsset != null;

            BattleDebugSpawnHelper.SpawnMonster(
                battle,
                _monsterHp,
                _monsterAtk,
                _monsterDef,
                _monsterSpeed,
                count,
                offset,
                spread,
                _vehicleCreateCount,
                _vehicleAttackCd,
                _vehicleAttackRange,
                _vehicleCanMoveCast);
            SetStatus(fallbackFromInvalidPreset
                ? $"发射器预设 {_emitterPresetAsset.name} 没有有效条目，已改用面板字段生成 {count} 只怪物"
                : $"已生成 {count} 只怪物，每只 {_vehicleCreateCount} 个发射器");
        }

        private void ApplyLevelPlayerConfig(BattleUnit player, BattleLevelPresetAsset level)
        {
            if (player == null || level == null)
            {
                return;
            }

            player.Position = new Unity.Mathematics.float3(level.PlayerSpawnX, BattleAreaConfig.BattleUnitSpawnY, 0f);
            player.Forward = Unity.Mathematics.float3.zero;

            NumericComponent numeric = player.GetComponent<NumericComponent>();
            if (numeric != null)
            {
                numeric.Set(NumericType.Hp, Mathf.Max(1, level.PlayerHp));
                numeric.Set(NumericType.MaxHp, Mathf.Max(1, level.PlayerHp));
                numeric.Set(NumericType.Speed, Mathf.Max(0.1f, level.PlayerSpeed));
            }

            VehicleComponent vehicleComp = player.GetComponent<VehicleComponent>() ?? player.AddComponent<VehicleComponent>();
            vehicleComp.OwnedVehicles.Clear();
            vehicleComp.EquippedVehicle = null;
            vehicleComp.EquippedVehicleId = 0;

            List<BattleEmitterPresetEntry> playerEmitters = GetValidPresetEntries(level.PlayerEmitterPreset);
            if (playerEmitters.Count > 0)
            {
                CreateVehiclesFromPreset(player, vehicleComp, playerEmitters);
            }

            BattleAttackComponent attackComp = player.GetComponent<BattleAttackComponent>() ?? player.AddComponent<BattleAttackComponent>();
            attackComp.SyncFromVehicleComponent(vehicleComp);
        }

        private async ETTask RunLevelWavesAsync(Battle battle, BattleLevelPresetAsset level)
        {
            if (battle == null || level?.Waves == null)
            {
                return;
            }

            battle.TotalWaves = level.Waves.Count;
            battle.CurrentWave = 0;

            for (int waveIndex = 0; waveIndex < level.Waves.Count; waveIndex++)
            {
                if (battle.IsDisposed || battle.State == BattleState.Ended)
                {
                    return;
                }

                BattleLevelWaveEntry wave = level.Waves[waveIndex];
                if (wave == null)
                {
                    continue;
                }

                int waveDelay = Mathf.Max(0, wave.DelayMs);
                if (waveDelay > 0)
                {
                    await battle.Root().GetComponent<TimerComponent>().WaitAsync(waveDelay);
                    if (battle.IsDisposed || battle.State == BattleState.Ended)
                    {
                        return;
                    }
                }

                battle.CurrentWave = waveIndex + 1;
                EventSystem.Instance.Publish(battle.Scene(), new WaveStart
                {
                    Battle = battle,
                    WaveNumber = battle.CurrentWave,
                });

                if (wave.Spawns != null)
                {
                    foreach (BattleLevelSpawnEntry spawn in wave.Spawns)
                    {
                        if (spawn == null)
                        {
                            continue;
                        }

                        int spawnDelay = Mathf.Max(0, spawn.DelayMs);
                        if (spawnDelay > 0)
                        {
                            await battle.Root().GetComponent<TimerComponent>().WaitAsync(spawnDelay);
                            if (battle.IsDisposed || battle.State == BattleState.Ended)
                            {
                                return;
                            }
                        }

                        List<long> spawnedUnitIds = SpawnLevelEntry(battle, spawn);
                        await WaitForLevelSpawnClearAsync(battle, spawnedUnitIds);
                    }
                }

                EventSystem.Instance.Publish(battle.Scene(), new WaveComplete
                {
                    Battle = battle,
                    WaveNumber = battle.CurrentWave,
                });
            }

            if (!battle.IsDisposed && battle.State != BattleState.Ended)
            {
                battle.End(true);
            }
        }

        private static List<long> SpawnLevelEntry(Battle battle, BattleLevelSpawnEntry spawn)
        {
            List<long> spawnedUnitIds = new();
            if (battle == null || spawn == null || spawn.Count <= 0)
            {
                return spawnedUnitIds;
            }

            List<BattleUnit> spawnedUnits = BattleDebugSpawnHelper.SpawnMonster(
                battle,
                Mathf.Max(1, spawn.Hp),
                Mathf.Max(0, spawn.Attack),
                Mathf.Max(0, spawn.Defense),
                Mathf.Max(0.1f, spawn.Speed),
                Mathf.Max(1, spawn.Count),
                Mathf.Max(0f, spawn.OffsetFromCamera),
                Mathf.Max(0f, spawn.SpreadRange),
                BuildMonsterEmitterSpecs(spawn.EmitterPreset));

            foreach (BattleUnit unit in spawnedUnits)
            {
                if (unit != null)
                {
                    spawnedUnitIds.Add(unit.Id);
                }
            }

            return spawnedUnitIds;
        }

        private static async ETTask WaitForLevelSpawnClearAsync(Battle battle, List<long> spawnedUnitIds)
        {
            if (battle == null || spawnedUnitIds == null || spawnedUnitIds.Count == 0)
            {
                return;
            }

            TimerComponent timerComponent = battle.Root().GetComponent<TimerComponent>();
            while (!battle.IsDisposed && battle.State != BattleState.Ended)
            {
                bool hasAliveUnit = false;
                foreach (long unitId in spawnedUnitIds)
                {
                    BattleUnit unit = battle.GetChild<BattleUnit>(unitId);
                    if (unit != null && !unit.IsDisposed && !unit.IsDead && unit.Camp == UnitCamp.Enemy)
                    {
                        hasAliveUnit = true;
                        break;
                    }
                }

                if (!hasAliveUnit)
                {
                    return;
                }

                await timerComponent.WaitAsync(200);
            }
        }

        private void ApplySpawnPreset(int hp, int atk, int def, float speed, int count, float offset, float spread)
        {
            _monsterHp = hp;
            _monsterAtk = atk;
            _monsterDef = def;
            _monsterSpeed = speed;
            _spawnCount = count;
            _spawnOffset = offset;
            _spreadRange = spread;
            SetStatus("已应用刷怪预设");
        }

        private BattleUnit FindPlayerUnit(Battle battle)
        {
            if (battle == null) return null;

            if (_cachedPlayerUnitId > 0)
            {
                BattleUnit cached = battle.GetChild<BattleUnit>(_cachedPlayerUnitId);
                if (cached != null && !cached.IsDisposed) return cached;
            }

            foreach (var child in battle.Children.Values)
            {
                if (child is BattleUnit unit && unit.Camp == UnitCamp.Friend)
                {
                    _cachedPlayerUnitId = unit.Id;
                    _cachedPlayerConfigId = unit.ConfigId;
                    return unit;
                }
            }
            return null;
        }

        private static string GetEffectTypeName(int effectType)
        {
            return effectType switch
            {
                1 => "伤害",
                2 => "冰冻",
                3 => "击退",
                4 => "治疗",
                5 => "眩晕",
                6 => "减速",
                7 => "吸血",
                8 => "护盾",
                9 => "攻击增益",
                10 => "防御增益",
                11 => "持续伤害",
                _ => $"类型{effectType}"
            };
        }

        private void DrawStatusBar()
        {
            if (!string.IsNullOrEmpty(_statusMsg))
            {
                EditorGUILayout.Space(4);
                DrawSummaryLine(_statusMsg);
            }
        }

        private void SetStatus(string message)
        {
            _statusMsg = message;
            Repaint();
        }

        private void DrawEmitterPresetAssetEditor(BattleEmitterPresetAsset asset)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("预设内容", EditorStyles.boldLabel);

            SerializedObject serializedObject = new(asset);
            SerializedProperty emittersProperty = serializedObject.FindProperty("Emitters");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(emittersProperty, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            if (GUILayout.Button("保存预设", GUILayout.Height(24)))
            {
                SaveEmitterPresetAsset(asset, $"已保存发射器预设 {asset.name}");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEmitterPresetPreview(BattleEmitterPresetAsset asset)
        {
            List<BattleEmitterPresetEntry> entries = GetValidPresetEntries(asset);
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("发射器预设没有有效条目。", MessageType.Warning);
                return;
            }

            DrawSummaryLine($"有效发射器: {entries.Count}");
            foreach (BattleEmitterPresetEntry entry in entries)
            {
                string name = string.IsNullOrWhiteSpace(entry.Name) ? "Emitter" : entry.Name;
                int buffCount = CountNonZeroBuffs(entry.BuffGroupIds);
                EditorGUILayout.LabelField($"{name}    CD {entry.CooldownMs}ms    Range {entry.Range:F1}    Buff {buffCount}", _mutedLabelStyle);
            }
        }

        private void ApplyEmitterPresetToPanel(BattleEmitterPresetAsset asset)
        {
            List<BattleEmitterPresetEntry> entries = GetValidPresetEntries(asset);
            if (entries.Count == 0)
            {
                SetStatus("发射器预设没有有效条目");
                return;
            }

            BattleEmitterPresetEntry first = entries[0];
            _vehicleAttackCd = first.CooldownMs;
            _vehicleAttackRange = first.Range;
            _vehicleBuffSlotCount = Mathf.Max(0, first.BuffSlotCount);
            _vehicleCreateCount = entries.Count;
            _vehicleCanMoveCast = first.CanMoveCast;
            SetStatus($"已把发射器预设 {asset.name} 的首个发射器应用到面板字段");
        }

        private List<BattleEmitterPresetEntry> BuildPanelPresetEntries()
        {
            int createCount = Mathf.Max(1, _vehicleCreateCount);
            int slotCount = Mathf.Max(0, _vehicleBuffSlotCount);
            List<BattleEmitterPresetEntry> entries = new(createCount);

            for (int i = 0; i < createCount; i++)
            {
                entries.Add(new BattleEmitterPresetEntry
                {
                    Name = createCount == 1 ? "Emitter" : $"Emitter {i + 1}",
                    CooldownMs = Mathf.Max(1, _vehicleAttackCd),
                    Range = Mathf.Max(0.1f, _vehicleAttackRange),
                    BuffSlotCount = slotCount,
                    CanMoveCast = _vehicleCanMoveCast,
                    BuffGroupIds = new List<int>(new int[slotCount]),
                });
            }

            return entries;
        }

        private static void EnsureEmitterPresetFolder()
        {
            if (AssetDatabase.IsValidFolder(EmitterPresetFolder))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Editor/Battle"))
            {
                AssetDatabase.CreateFolder("Assets/Editor", "Battle");
            }

            AssetDatabase.CreateFolder("Assets/Editor/Battle", "EmitterPresets");
        }

        private void SaveEmitterPresetAsset(BattleEmitterPresetAsset asset, string message)
        {
            if (asset == null)
            {
                return;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus(message);
        }

        private static List<BattleEmitterPresetEntry> GetValidPresetEntries(BattleEmitterPresetAsset asset)
        {
            List<BattleEmitterPresetEntry> entries = new();
            if (asset?.Emitters == null)
            {
                return entries;
            }

            foreach (BattleEmitterPresetEntry entry in asset.Emitters)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.CooldownMs <= 0 || entry.Range <= 0f)
                {
                    continue;
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static List<int> BuildPresetBuffSlots(BattleEmitterPresetEntry entry)
        {
            int slotCount = Mathf.Max(0, entry.BuffSlotCount);
            if (entry.BuffGroupIds != null && entry.BuffGroupIds.Count > slotCount)
            {
                slotCount = entry.BuffGroupIds.Count;
            }

            List<int> slots = new(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                int buffGroupId = entry.BuffGroupIds != null && i < entry.BuffGroupIds.Count ? entry.BuffGroupIds[i] : 0;
                slots.Add(buffGroupId);
            }

            return slots;
        }

        private static List<BattleDebugEmitterSpec> BuildMonsterEmitterSpecs(List<BattleEmitterPresetEntry> presetEntries)
        {
            List<BattleDebugEmitterSpec> specs = new(presetEntries.Count);
            foreach (BattleEmitterPresetEntry entry in presetEntries)
            {
                specs.Add(new BattleDebugEmitterSpec
                {
                    CooldownMs = entry.CooldownMs,
                    Range = entry.Range,
                    CanMoveCast = entry.CanMoveCast,
                    PayloadType = BattleAttackPayloadType.VehicleBuff,
                    BuffGroupIds = BuildPresetBuffSlots(entry),
                });
            }

            return specs;
        }

        private static List<BattleDebugEmitterSpec> BuildMonsterEmitterSpecs(BattleEmitterPresetAsset asset)
        {
            List<BattleEmitterPresetEntry> entries = GetValidPresetEntries(asset);
            if (entries.Count > 0)
            {
                return BuildMonsterEmitterSpecs(entries);
            }

            return new List<BattleDebugEmitterSpec>
            {
                new()
                {
                    CooldownMs = 1200,
                    Range = 1.2f,
                    CanMoveCast = false,
                    PayloadType = BattleAttackPayloadType.VehicleBuff,
                    BuffGroupIds = new List<int> { 61021 },
                },
            };
        }

        private static int CountNonZeroBuffs(List<int> buffIds)
        {
            if (buffIds == null)
            {
                return 0;
            }

            int count = 0;
            foreach (int buffId in buffIds)
            {
                if (buffId != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private bool EnsureRuntimeReady(string actionName, out Scene root)
        {
            root = Init.Root;

            if (!Application.isPlaying)
            {
                SetStatus($"{actionName}失败：请先进入 Play 模式");
                return false;
            }

            if (root == null)
            {
                SetStatus($"{actionName}失败：Init.Root 尚未初始化");
                return false;
            }

            return true;
        }

        private void EnsureBattleRuntimeDependencies(Scene root)
        {
            if (root == null)
            {
                return;
            }

            if (root.GetComponent<BattleComponent>() == null)
            {
                root.AddComponent<BattleComponent>();
            }

            if (root.GetComponent<EventBridgeComponent>() == null)
            {
                root.AddComponent<EventBridgeComponent>();
            }
        }

#if ODIN_INSPECTOR
        public sealed class BattleStatusSnapshot
        {
            [LabelText("Root")]
            public string Root;
            [LabelText("战斗")]
            public string Battle;
            [LabelText("BattleId")]
            public string BattleId;
            [LabelText("波次")]
            public string Wave;
            [LabelText("耗时")]
            public string Elapsed;
            [LabelText("友方")]
            public int Friend;
            [LabelText("敌方")]
            public int Enemy;
            [LabelText("死亡")]
            public int Dead;
            [LabelText("玩家")]
            public string Player;
            [LabelText("玩家HP")]
            public string PlayerHp;
            [LabelText("玩家X")]
            public string PlayerX;
            [LabelText("玩家发射器")]
            public int PlayerEmitterCount;
        }

        public sealed class UnitStatusRow
        {
            [HideInInspector]
            public BattleUnit Unit;
            [ReadOnly, LabelText("阵营")]
            public string Camp;
            [ReadOnly, LabelText("ID")]
            public long Id;
            [ReadOnly, LabelText("配置")]
            public int ConfigId;
            [ReadOnly, LabelText("HP")]
            public string Hp;
            [ReadOnly, LabelText("X")]
            public float X;
            [ReadOnly, LabelText("死亡")]
            public bool Dead;
            [ReadOnly, LabelText("Boss")]
            public bool Boss;

            [Button("击杀"), DisableIf("@Dead || Unit == null || Unit.IsDisposed")]
            private void Kill()
            {
                if (Unit == null || Unit.IsDisposed || Unit.IsDead)
                {
                    return;
                }

                Unit.IsDead = true;
                Unit.SetNumeric(NumericType.Hp, 0);
                EventSystem.Instance.Publish(Unit.Scene(), new BattleUnitDead { BattleUnit = Unit });
                Dead = true;
                Hp = "0/0";
            }
        }

        public sealed class EmitterStatusRow
        {
            [LabelText("ID")]
            public long Id;
            [LabelText("CD(ms)")]
            public int CooldownMs;
            [LabelText("射程")]
            public float Range;
            [LabelText("移动释放")]
            public bool CanMoveCast;
            [LabelText("槽位")]
            public int BuffSlots;
            [LabelText("Buff")]
            public int ActiveBuffs;
        }
#endif

        #endregion
    }
}
#endif
