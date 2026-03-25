#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace GameNetty.EditorTools
{
#if ODIN_INSPECTOR
    public sealed class BattleAutoStrategyEditorWindow : OdinEditorWindow
#else
    public sealed class BattleAutoStrategyEditorWindow : EditorWindow
#endif
    {
        private const string MenuPath = "Tools/Battle/Auto Battle Strategy Panel";

        private static readonly List<RuleGuideRow> RuleGuideRows = new()
        {
            new RuleGuideRow("技能规则", 1, "ShortestMoveThenPriority", "优先选择所需位移最短的技能，位移相同再看技能优先级"),
            new RuleGuideRow("技能规则", 2, "HighestPriorityThenMove", "优先选择技能优先级更高的技能，优先级相同再看位移成本"),
            new RuleGuideRow("目标规则", 1, "NearestEnemy", "优先最近目标"),
            new RuleGuideRow("目标规则", 2, "KeepCurrentThenNearest", "尽量保持当前目标，不满足容差时再切到最近目标"),
            new RuleGuideRow("目标规则", 3, "LowestHp", "优先当前血量更低的目标"),
        };

#if !ODIN_INSPECTOR
        private Vector2 _scrollPosition;
#endif

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            BattleAutoStrategyEditorWindow window = GetWindow<BattleAutoStrategyEditorWindow>();
            window.titleContent = new GUIContent("Auto Strategy");
            window.minSize = new Vector2(760f, 520f);
            window.Show();
        }

#if ODIN_INSPECTOR
        [InfoBox("用于统一打开自动战斗策略配置、触发转表，并查看当前规则编号。启用 ODIN_INSPECTOR 时会自动使用 Odin 面板。")]
        [TitleGroup("配置表")]
        [ShowInInspector, ReadOnly, LabelText("自动战斗策略表")]
        private string AutoBattleStrategyTablePath => GetConfigTablePath("AutoBattleStrategyConfig.xlsx");

        [TitleGroup("配置表")]
        [ShowInInspector, ReadOnly, LabelText("单位战斗表")]
        private string UnitCombatTablePath => GetConfigTablePath("UnitCombatConfig.xlsx");

        [TitleGroup("生成代码")]
        [ShowInInspector, ReadOnly, LabelText("Server 生成类")]
        private string ServerGeneratedConfigPath => GetServerGeneratedPath();

        [TitleGroup("生成代码")]
        [ShowInInspector, ReadOnly, LabelText("Client 生成类")]
        private string ClientGeneratedConfigPath => GetClientGeneratedPath();

        [TitleGroup("配置表")]
        [HorizontalGroup("配置表/Actions")]
        [Button("打开策略表")]
        private void OpenStrategyTable() => OpenExternal(AutoBattleStrategyTablePath);

        [TitleGroup("配置表")]
        [HorizontalGroup("配置表/Actions")]
        [Button("打开单位战斗表")]
        private void OpenUnitCombatTable() => OpenExternal(UnitCombatTablePath);

        [TitleGroup("配置表")]
        [HorizontalGroup("配置表/Actions")]
        [Button("打开配置目录")]
        private void OpenConfigFolder() => OpenExternal(GetConfigFolderPath());

        [TitleGroup("生成")]
        [HorizontalGroup("生成/Buttons")]
        [Button("转表 Client")]
        private void GenerateClientConfig() => RunLubanScript("GenConfig_Client");

        [TitleGroup("生成")]
        [HorizontalGroup("生成/Buttons")]
        [Button("转表 Server")]
        private void GenerateServerConfig() => RunLubanScript("GenConfig_Server");

        [TitleGroup("生成")]
        [HorizontalGroup("生成/Buttons")]
        [Button("转表 All")]
        private void GenerateAllConfig()
        {
            GenerateClientConfig();
            GenerateServerConfig();
        }

        [TitleGroup("生成")]
        [HorizontalGroup("生成/OpenGenerated")]
        [Button("打开 Server 生成类")]
        private void OpenServerGenerated() => OpenExternal(ServerGeneratedConfigPath);

        [TitleGroup("生成")]
        [HorizontalGroup("生成/OpenGenerated")]
        [Button("打开 Client 生成类")]
        private void OpenClientGenerated() => OpenExternal(ClientGeneratedConfigPath);

        [TitleGroup("规则说明")]
        [ShowInInspector, TableList(AlwaysExpanded = true)]
        private List<RuleGuideRow> RuleGuide => RuleGuideRows;
#else
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.HelpBox("用于统一打开自动战斗策略配置、触发转表，并查看当前规则编号。若启用 ODIN_INSPECTOR，将自动切换为 Odin 面板。", MessageType.Info);

            DrawPathSection("自动战斗策略表", GetConfigTablePath("AutoBattleStrategyConfig.xlsx"));
            DrawPathSection("单位战斗表", GetConfigTablePath("UnitCombatConfig.xlsx"));
            DrawPathSection("Server 生成类", GetServerGeneratedPath());
            DrawPathSection("Client 生成类", GetClientGeneratedPath());

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开策略表", GUILayout.Height(28f)))
            {
                OpenExternal(GetConfigTablePath("AutoBattleStrategyConfig.xlsx"));
            }
            if (GUILayout.Button("打开单位战斗表", GUILayout.Height(28f)))
            {
                OpenExternal(GetConfigTablePath("UnitCombatConfig.xlsx"));
            }
            if (GUILayout.Button("打开配置目录", GUILayout.Height(28f)))
            {
                OpenExternal(GetConfigFolderPath());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("转表 Client", GUILayout.Height(28f)))
            {
                RunLubanScript("GenConfig_Client");
            }
            if (GUILayout.Button("转表 Server", GUILayout.Height(28f)))
            {
                RunLubanScript("GenConfig_Server");
            }
            if (GUILayout.Button("转表 All", GUILayout.Height(28f)))
            {
                RunLubanScript("GenConfig_Client");
                RunLubanScript("GenConfig_Server");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("规则说明", EditorStyles.boldLabel);
            foreach (RuleGuideRow row in RuleGuideRows)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"{row.Category} / {row.Value} / {row.Name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(row.Meaning, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawPathSection(string label, string path)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(path, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space(4f);
        }
#endif

        private static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }

        private static string GetConfigFolderPath()
        {
            return Path.Combine(GetProjectRootPath(), "Config", "Excel", "GameConfig");
        }

        private static string GetConfigTablePath(string fileName)
        {
            return Path.Combine(GetConfigFolderPath(), fileName);
        }

        private static string GetServerGeneratedPath()
        {
            return Path.Combine(GetProjectRootPath(), "Server", "Model", "Generate", "Config", "AutoBattleStrategyConfig.cs");
        }

        private static string GetClientGeneratedPath()
        {
            return Path.Combine(GetProjectRootPath(), "Unity", "Assets", "GameScripts", "HotFix", "GameProto", "Generate", "Config", "AutoBattleStrategyConfig.cs");
        }

        private static void OpenExternal(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || (!File.Exists(path) && !Directory.Exists(path)))
            {
                UnityEngine.Debug.LogError($"路径不存在: {path}");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void RunLubanScript(string scriptName)
        {
            string workDir = Path.Combine(GetProjectRootPath(), "Tools", "Luban");
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            string script = $"./{scriptName}.sh";
#else
            string script = $"{scriptName}.bat";
#endif
            UnityEngine.Debug.Log($"执行转表脚本: {script}, 工作目录: {workDir}");
            ET.ShellHelper.Run(script, workDir);
            AssetDatabase.Refresh();
        }

        private sealed class RuleGuideRow
        {
            public RuleGuideRow(string category, int value, string name, string meaning)
            {
                Category = category;
                Value = value;
                Name = name;
                Meaning = meaning;
            }

            public string Category;
            public int Value;
            public string Name;
            public string Meaning;
        }
    }
}
#endif
