#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace GameNetty.EditorTools
{
#if ODIN_INSPECTOR
    public sealed class LubanExportPanel : OdinEditorWindow
#else
    public sealed class LubanExportPanel : EditorWindow
#endif
    {
        private const string MenuPath = "Tools/Luban/Export Panel";

        private const string LubanRelativePath = "Tools/Luban/LubanRelease/Luban.dll";
        private const string GameConfigRelativePath = "Config/Excel/GameConfig";
        private const string StartConfigRelativePath = "Config/Excel/StartConfig";
        private const string ClientCodeOutputRelativePath = "Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config";
        private const string ClientDataOutputRelativePath = "Unity/Assets/AssetRaw/Configs";
        private const string ServerCodeOutputRelativePath = "Server/Model/Generate/Config";
        private const string ServerDataOutputRelativePath = "Config/Generate";
        private const string StartConfigCodeOutputRelativePath = "Server/Model/Generate/Config/StartConfig";
        private const string StartConfigDataOutputRelativePath = "Config/Generate/StartConfig/Localhost";

        private static readonly string[] SystemTables = { "__tables__.xlsx", "__beans__.xlsx", "__enums__.xlsx" };

        private bool _isGenerating;
        private string _logText = "";
        private readonly object _logLock = new();

        // Excel editor state
        private LubanTable _currentTable;
        private string _selectedFilePath = "";
        private bool _isDirty;

        // Cached Odin-friendly data list (synced with _currentTable)
        private List<TableRowData> _rowDataList = new();

        // IMGUI scroll
        private Vector2 _mainScrollPos;

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            LubanExportPanel window = GetWindow<LubanExportPanel>();
            window.titleContent = new GUIContent("Luban Export");
            window.minSize = new Vector2(500f, 400f);
            window.Show();
        }

        #region Paths

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string LubanDllPath => Path.Combine(ProjectRoot, LubanRelativePath);
        private static string GameConfigDir => Path.Combine(ProjectRoot, GameConfigRelativePath);
        private static string StartConfigDir => Path.Combine(ProjectRoot, StartConfigRelativePath);
        private static string ClientCodeOutput => Path.Combine(ProjectRoot, ClientCodeOutputRelativePath);
        private static string ClientDataOutput => Path.Combine(ProjectRoot, ClientDataOutputRelativePath);
        private static string ServerCodeOutput => Path.Combine(ProjectRoot, ServerCodeOutputRelativePath);
        private static string ServerDataOutput => Path.Combine(ProjectRoot, ServerDataOutputRelativePath);
        private static string StartConfigCodeOutput => Path.Combine(ProjectRoot, StartConfigCodeOutputRelativePath);
        private static string StartConfigDataOutput => Path.Combine(ProjectRoot, StartConfigDataOutputRelativePath);
        private static string ToolsLubanDir => Path.Combine(ProjectRoot, "Tools", "Luban");

        #endregion

        #region dotnet detection

        private static string FindDotnetPath()
        {
            string[] candidates = { "/opt/homebrew/bin/dotnet", "/usr/local/bin/dotnet" };
            foreach (string c in candidates)
            {
                if (File.Exists(c)) return c;
            }

            try
            {
                ProcessStartInfo psi = new("which")
                {
                    Arguments = "dotnet", UseShellExecute = false,
                    RedirectStandardOutput = true, CreateNoWindow = true,
                };
                using Process p = Process.Start(psi);
                string result = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                if (p.ExitCode == 0 && !string.IsNullOrEmpty(result) && File.Exists(result))
                    return result;
            }
            catch { }

            return "dotnet";
        }

        #endregion

        #region Excel scanning

        private static List<ExcelFileInfo> ScanExcelFiles()
        {
            var files = new List<ExcelFileInfo>();
            string dir = GameConfigDir;
            if (!Directory.Exists(dir)) return files;

            foreach (string file in Directory.GetFiles(dir, "*.xlsx"))
            {
                string name = Path.GetFileName(file);
                if (Array.IndexOf(SystemTables, name) >= 0) continue;

                var fi = new FileInfo(file);
                files.Add(new ExcelFileInfo
                {
                    FileName = name,
                    FilePath = file,
                    Size = FormatFileSize(fi.Length),
                    LastModified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                });
            }
            return files;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        #endregion

        #region Excel Editor

        private void LoadExcel(string filePath)
        {
            try
            {
                _currentTable = LubanExcelRW.Read(filePath);
                _isDirty = false;
                SyncRowDataList();
                if (_currentTable != null)
                    AppendLog($"[{DateTime.Now:HH:mm:ss}] 已加载 {_currentTable.FileName}，{_currentTable.Data.Count} 行 x {_currentTable.Schema.Count} 列");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                AppendLog($"[ERROR] 加载失败: {e.Message}");
            }
        }

        private void SaveExcel()
        {
            if (_currentTable == null) return;
            // Sync Odin list → LubanTable before save
            SyncFromRowDataList();
            try
            {
                LubanExcelRW.Write(_currentTable.FilePath, _currentTable);
                _isDirty = false;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] 已保存 {_currentTable.FileName}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                AppendLog($"[ERROR] 保存失败: {e.Message}");
            }
        }

        private void AddRow()
        {
            if (_currentTable == null) return;
            var row = new Dictionary<string, string>();
            foreach (var col in _currentTable.Schema)
                row[col.Name] = "";
            _currentTable.Data.Add(row);
            _isDirty = true;
            SyncRowDataList();
        }

        private void DeleteRow(int index)
        {
            if (_currentTable == null || index < 0 || index >= _currentTable.Data.Count) return;
            _currentTable.Data.RemoveAt(index);
            _isDirty = true;
            SyncRowDataList();
        }

        private void DuplicateRow(int index)
        {
            if (_currentTable == null || index < 0 || index >= _currentTable.Data.Count) return;
            var copy = new Dictionary<string, string>(_currentTable.Data[index]);
            _currentTable.Data.Insert(index + 1, copy);
            _isDirty = true;
            SyncRowDataList();
        }

        /// <summary>
        /// Sync LubanTable.Data → _rowDataList (Odin-friendly)
        /// </summary>
        private void SyncRowDataList()
        {
            _rowDataList = new List<TableRowData>();
            if (_currentTable == null) return;

            foreach (var row in _currentTable.Data)
            {
                var rd = new TableRowData { Fields = new List<TableFieldData>() };
                foreach (var col in _currentTable.Schema)
                {
                    string val = row.TryGetValue(col.Name, out string v) ? v : "";
                    rd.Fields.Add(new TableFieldData(col.Name, val, col.BaseType, col.IsReadOnly));
                }
                _rowDataList.Add(rd);
            }
        }

        /// <summary>
        /// Sync _rowDataList → LubanTable.Data (before save)
        /// </summary>
        private void SyncFromRowDataList()
        {
            if (_currentTable == null) return;
            for (int i = 0; i < _rowDataList.Count && i < _currentTable.Data.Count; i++)
            {
                var row = _currentTable.Data[i];
                foreach (var field in _rowDataList[i].Fields)
                {
                    if (row.ContainsKey(field.FieldName))
                        row[field.FieldName] = field.Value;
                }
            }
        }

        /// <summary>
        /// Called by Odin when user edits a field in the list.
        /// </summary>
        private void OnFieldValueChanged()
        {
            _isDirty = true;
        }

        #endregion

        #region Generation

        private void GenerateClient()
        {
            string templateDir = Path.Combine(ToolsLubanDir, "CustomTemplate");
            string confFile = Path.Combine(GameConfigDir, "__luban__.conf");
            string args = $"\"{LubanDllPath}\" --customTemplateDir \"{templateDir}\" -t Client -c cs-bin -d bin " +
                          $"--conf \"{confFile}\" -x outputCodeDir=\"{ClientCodeOutput}\" -x bin.outputDataDir=\"{ClientDataOutput}\" -x lineEnding=LF";
            RunGeneration(FindDotnetPath(), args, "Client");
        }

        private void GenerateServer()
        {
            string templateDir = Path.Combine(ToolsLubanDir, "ServerTemplate");
            string gameConf = Path.Combine(GameConfigDir, "__luban__.conf");
            string gameArgs = $"\"{LubanDllPath}\" --customTemplateDir \"{templateDir}\" -t All -c cs-bin -d bin " +
                              $"--conf \"{gameConf}\" -x outputCodeDir=\"{ServerCodeOutput}\" -x bin.outputDataDir=\"{ServerDataOutput}\" -x lineEnding=LF";
            string startConf = Path.Combine(StartConfigDir, "__luban__.conf");
            string startArgs = $"\"{LubanDllPath}\" --customTemplateDir \"{templateDir}\" -t Localhost -c cs-bin -d bin " +
                               $"--conf \"{startConf}\" -x outputCodeDir=\"{StartConfigCodeOutput}\" -x bin.outputDataDir=\"{StartConfigDataOutput}\" -x lineEnding=LF";

            RunGeneration(FindDotnetPath(), gameArgs, "Server GameConfig", () =>
            {
                RunGeneration(FindDotnetPath(), startArgs, "Server StartConfig", RefreshAssetDatabase);
            });
        }

        private void GenerateAll() => GenerateClient();

        private void RunGeneration(string dotnet, string args, string label, Action onComplete = null)
        {
            _isGenerating = true;
            AppendLog($"[{DateTime.Now:HH:mm:ss}] ===== {label} 开始 =====");
            new Thread(() =>
            {
                try
                {
                    ProcessStartInfo psi = new(dotnet)
                    {
                        Arguments = args, UseShellExecute = false, CreateNoWindow = true,
                        WorkingDirectory = ToolsLubanDir, RedirectStandardOutput = true, RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
                    };
                    using Process process = Process.Start(psi);
                    string line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                        AppendLog($"[{label}] {line}");
                    string err = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(err)) AppendLog($"[{label}/ERROR] {err}");
                    process.WaitForExit();
                    AppendLog(process.ExitCode == 0
                        ? $"[{DateTime.Now:HH:mm:ss}] ===== {label} 完成 =====\n"
                        : $"[{DateTime.Now:HH:mm:ss}] ===== {label} 失败 (exit: {process.ExitCode}) =====\n");
                }
                catch (Exception e) { AppendLog($"[{label}/EXCEPTION] {e.Message}\n"); }
                finally { _isGenerating = false; onComplete?.Invoke(); }
            }).Start();
        }

        private void RefreshAssetDatabase() => EditorApplication.delayCall += AssetDatabase.Refresh;

        private void AppendLog(string msg)
        {
            lock (_logLock)
            {
                _logText += msg + "\n";
                if (_logText.Length > 50000)
                    _logText = _logText.Substring(_logText.Length - 40000);
            }
        }

        private void ClearLog() { lock (_logLock) { _logText = ""; } }

        #endregion

        #region Utility

        private static void OpenExternal(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path) && !Directory.Exists(path))
            { UnityEngine.Debug.LogError($"路径不存在: {path}"); return; }
            try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        #endregion

        // ================================================================
        // Odin GUI
        // ================================================================

#if ODIN_INSPECTOR

        [TitleGroup("数据编辑")]
        [ShowInInspector, ValueDropdown("GetExcelFileOptions")]
        [OnValueChanged("OnFileSelectionChanged")]
        [LabelText("选择配置表")]
        private string SelectedFile
        {
            get => _selectedFilePath;
            set => _selectedFilePath = value;
        }

        [TitleGroup("数据编辑")]
        [HorizontalGroup("数据编辑/Actions")]
        [Button("加载", ButtonSizes.Small)]
        private void DoLoad() { if (!string.IsNullOrEmpty(_selectedFilePath)) LoadExcel(_selectedFilePath); }

        [TitleGroup("数据编辑")]
        [HorizontalGroup("数据编辑/Actions")]
        [Button("保存", ButtonSizes.Small)]
        [EnableIf("@_isDirty")]
        private void DoSave() => SaveExcel();

        [TitleGroup("数据编辑")]
        [HorizontalGroup("数据编辑/Actions")]
        [Button("刷新", ButtonSizes.Small)]
        private void DoRefresh() { if (_currentTable != null) LoadExcel(_currentTable.FilePath); }

        [TitleGroup("数据编辑")]
        [HorizontalGroup("数据编辑/Actions")]
        [Button("添加行", ButtonSizes.Small)]
        [EnableIf("@_currentTable != null")]
        private void DoAddRow() => AddRow();

        [TitleGroup("数据编辑")]
        [HorizontalGroup("数据编辑/Actions")]
        [Button("打开配置目录", ButtonSizes.Small)]
        private void OpenConfigDir() => OpenExternal(GameConfigDir);

        [TitleGroup("数据编辑")]
        [ShowInInspector, ReadOnly, LabelText("当前表")]
        private string CurrentTableName => _currentTable?.FileName ?? "(未选择)";

        [TitleGroup("数据编辑")]
        [ShowInInspector, ReadOnly, LabelText("信息")]
        private string TableInfo => _currentTable != null ? $"{_currentTable.Data.Count} 行 x {_currentTable.Schema.Count} 列" : "0";

        [TitleGroup("数据编辑")]
        [ShowInInspector, ReadOnly, LabelText("状态")]
        private string DirtyStatus => _isDirty ? "<color=orange>已修改未保存</color>" : "<color=green>无修改</color>";

        [TitleGroup("数据编辑")]
        [ShowInInspector, ListDrawerSettings(Expanded = true, ShowPaging = true, NumberOfItemsPerPage = 50)]
        [LabelText("数据")]
        [OnValueChanged("OnFieldValueChanged")]
        private List<TableRowData> RowDataList
        {
            get => _rowDataList;
            set
            {
                _rowDataList = value ?? new List<TableRowData>();
                _isDirty = true;
            }
        }

        [TitleGroup("导表")]
        [HorizontalGroup("导表/Buttons")]
        [Button("转表 - Client", ButtonSizes.Medium)]
        [EnableIf("@!_isGenerating")]
        private void GenClient() => GenerateClient();

        [TitleGroup("导表")]
        [HorizontalGroup("导表/Buttons")]
        [Button("转表 - Server", ButtonSizes.Medium)]
        [EnableIf("@!_isGenerating")]
        private void GenServer() => GenerateServer();

        [TitleGroup("导表")]
        [HorizontalGroup("导表/Buttons")]
        [Button("转表 - All", ButtonSizes.Medium)]
        [EnableIf("@!_isGenerating")]
        private void GenAll() => GenerateAll();

        [FoldoutGroup("环境信息", Expanded = false)]
        [ShowInInspector, ReadOnly, LabelText("dotnet")]
        private string DotnetPath => FindDotnetPath();

        [FoldoutGroup("环境信息")]
        [ShowInInspector, ReadOnly, LabelText("Luban.dll")]
        private string LubanDll => LubanDllPath;

        [FoldoutGroup("环境信息")]
        [ShowInInspector, ReadOnly, LabelText("GameConfig")]
        private string GameConfig => GameConfigDir;

        [FoldoutGroup("环境信息")]
        [ShowInInspector, ReadOnly, LabelText("StartConfig")]
        private string StartConfig => StartConfigDir;

        [FoldoutGroup("输出路径", Expanded = false)]
        [ShowInInspector, ReadOnly, HorizontalGroup("输出路径/R0"), LabelText("Client 代码")]
        private string CC => ClientCodeOutput;
        [FoldoutGroup("输出路径"), HorizontalGroup("输出路径/R0"), Button("打开", ButtonSizes.Small)]
        private void O0() => OpenExternal(ClientCodeOutput);

        [FoldoutGroup("输出路径")]
        [ShowInInspector, ReadOnly, HorizontalGroup("输出路径/R1"), LabelText("Client 数据")]
        private string CD => ClientDataOutput;
        [FoldoutGroup("输出路径"), HorizontalGroup("输出路径/R1"), Button("打开", ButtonSizes.Small)]
        private void O1() => OpenExternal(ClientDataOutput);

        [FoldoutGroup("输出路径")]
        [ShowInInspector, ReadOnly, HorizontalGroup("输出路径/R2"), LabelText("Server 代码")]
        private string SC => ServerCodeOutput;
        [FoldoutGroup("输出路径"), HorizontalGroup("输出路径/R2"), Button("打开", ButtonSizes.Small)]
        private void O2() => OpenExternal(ServerCodeOutput);

        [FoldoutGroup("输出路径")]
        [ShowInInspector, ReadOnly, HorizontalGroup("输出路径/R3"), LabelText("Server 数据")]
        private string SD => ServerDataOutput;
        [FoldoutGroup("输出路径"), HorizontalGroup("输出路径/R3"), Button("打开", ButtonSizes.Small)]
        private void O3() => OpenExternal(ServerDataOutput);

        [FoldoutGroup("输出路径")]
        [ShowInInspector, ReadOnly, HorizontalGroup("输出路径/R4"), LabelText("StartConfig")]
        private string StC => StartConfigCodeOutput;
        [FoldoutGroup("输出路径"), HorizontalGroup("输出路径/R4"), Button("打开", ButtonSizes.Small)]
        private void O4() => OpenExternal(StartConfigCodeOutput);

        [FoldoutGroup("生成日志", Expanded = false)]
        [Button("清空日志", ButtonSizes.Small)]
        private void ClearLogBtn() => ClearLog();

        [FoldoutGroup("生成日志")]
        [ShowInInspector, ReadOnly, LabelText("状态")]
        private string StatusText => _isGenerating ? "<color=yellow>生成中...</color>" : "<color=green>空闲</color>";

        /// <summary>
        /// Draw selectable log text area at the bottom of the window.
        /// </summary>
        [OnInspectorGUI]
        private void DrawSelectableLog()
        {
            string logSnapshot;
            lock (_logLock) { logSnapshot = _logText; }
            EditorGUILayout.Space(4f);
            EditorGUILayout.TextArea(logSnapshot, GUILayout.ExpandHeight(true), GUILayout.MinHeight(60f));
        }

        private IEnumerable<ValueDropdownItem<string>> GetExcelFileOptions()
        {
            var items = new List<ValueDropdownItem<string>>();
            foreach (var f in ScanExcelFiles())
                items.Add(new ValueDropdownItem<string>($"{f.FileName}  ({f.Size}, {f.LastModified})", f.FilePath));
            return items;
        }

        private void OnFileSelectionChanged()
        {
            if (!string.IsNullOrEmpty(_selectedFilePath))
                LoadExcel(_selectedFilePath);
        }

#endif

        // ================================================================
        // IMGUI fallback
        // ================================================================

#if !ODIN_INSPECTOR
        private void OnGUI()
        {
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

            DrawSection_DataEdit_IMGUI();
            DrawSection_Export_IMGUI();

            // Collapsible: Environment
            _envFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_envFoldout, "环境信息");
            if (_envFoldout)
            {
                EditorGUI.indentLevel++;
                DrawReadOnlyField("dotnet", FindDotnetPath());
                DrawReadOnlyField("Luban.dll", LubanDllPath);
                DrawReadOnlyField("GameConfig", GameConfigDir);
                DrawReadOnlyField("StartConfig", StartConfigDir);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Collapsible: Output paths
            _outputFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_outputFoldout, "输出路径");
            if (_outputFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPathWithButton("Client 代码", ClientCodeOutput);
                DrawPathWithButton("Client 数据", ClientDataOutput);
                DrawPathWithButton("Server 代码", ServerCodeOutput);
                DrawPathWithButton("Server 数据", ServerDataOutput);
                DrawPathWithButton("StartConfig", StartConfigCodeOutput);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Collapsible: Log
            _logFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_logFoldout, "生成日志");
            if (_logFoldout)
            {
                EditorGUI.indentLevel++;
                string logSnapshot;
                lock (_logLock) { logSnapshot = _logText; }
                EditorGUILayout.TextArea(logSnapshot, GUILayout.ExpandHeight(true), GUILayout.MinHeight(60f));
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("清空日志", GUILayout.Width(80f))) ClearLog();
                EditorGUILayout.LabelField(_isGenerating ? "状态: 生成中..." : "状态: 空闲");
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndScrollView();
        }

        private bool _envFoldout;
        private bool _outputFoldout;
        private bool _logFoldout;

        private void DrawSection_DataEdit_IMGUI()
        {
            EditorGUILayout.LabelField("数据编辑", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            var files = ScanExcelFiles();
            var names = new string[files.Count];
            var paths = new string[files.Count];
            for (int i = 0; i < files.Count; i++)
            {
                names[i] = files[i].FileName;
                paths[i] = files[i].FilePath;
            }

            int currentIdx = -1;
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] == _selectedFilePath) { currentIdx = i; break; }
            }

            int newIdx = EditorGUILayout.Popup("选择配置表", currentIdx, names);
            if (newIdx != currentIdx && newIdx >= 0 && newIdx < paths.Length)
            {
                _selectedFilePath = paths[newIdx];
                LoadExcel(paths[newIdx]);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("加载", GUILayout.Width(50f)))
            { if (!string.IsNullOrEmpty(_selectedFilePath)) LoadExcel(_selectedFilePath); }
            if (GUILayout.Button("保存", GUILayout.Width(50f))) SaveExcel();
            if (GUILayout.Button("刷新", GUILayout.Width(50f)))
            { if (_currentTable != null) LoadExcel(_currentTable.FilePath); }
            if (GUILayout.Button("添加行", GUILayout.Width(60f))) AddRow();
            if (GUILayout.Button("打开目录", GUILayout.Width(70f))) OpenExternal(GameConfigDir);
            EditorGUILayout.EndHorizontal();

            if (_currentTable != null)
            {
                string status = _isDirty ? "已修改" : "无修改";
                EditorGUILayout.LabelField($"当前表: {_currentTable.FileName}  |  {_currentTable.Data.Count} x {_currentTable.Schema.Count}  |  {status}");
            }

            // Draw rows as foldout objects
            if (_currentTable != null)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("数据", EditorStyles.boldLabel);

                for (int r = 0; r < _currentTable.Data.Count; r++)
                {
                    var row = _currentTable.Data[r];
                    string id = _currentTable.Schema.Count > 0
                        ? (row.TryGetValue(_currentTable.Schema[0].Name, out string idVal) ? idVal : "")
                        : "";
                    string label = $"Row {r + 1} (Id={id})";

                    bool wasExpanded = r < _rowFoldouts.Count && _rowFoldouts[r];
                    bool isExpanded = EditorGUILayout.Foldout(wasExpanded, label);
                    if (r < _rowFoldouts.Count)
                        _rowFoldouts[r] = isExpanded;
                    else
                        _rowFoldouts.Add(isExpanded);

                    if (isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        for (int c = 0; c < _currentTable.Schema.Count; c++)
                        {
                            var col = _currentTable.Schema[c];
                            string currentVal = row.TryGetValue(col.Name, out string v) ? v : "";

                            if (col.IsReadOnly)
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                EditorGUILayout.TextField($"{col.Name} ({col.BaseType})", currentVal);
                                EditorGUI.EndDisabledGroup();
                            }
                            else
                            {
                                EditorGUI.BeginChangeCheck();
                                string newVal = EditorGUILayout.TextField($"{col.Name} ({col.BaseType})", currentVal);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    row[col.Name] = newVal;
                                    _isDirty = true;
                                }
                            }
                        }

                        // Delete / Duplicate buttons
                        EditorGUILayout.BeginHorizontal();
                        int ri = r;
                        if (GUILayout.Button("删除此行"))
                        {
                            if (EditorUtility.DisplayDialog("确认", $"删除第 {ri + 1} 行?", "删除", "取消"))
                                DeleteRow(ri);
                        }
                        if (GUILayout.Button("复制此行")) DuplicateRow(ri);
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void DrawSection_Export_IMGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("导表", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            EditorGUI.BeginDisabledGroup(_isGenerating);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("转表 - Client", GUILayout.Height(28f))) GenerateClient();
            if (GUILayout.Button("转表 - Server", GUILayout.Height(28f))) GenerateServer();
            if (GUILayout.Button("转表 - All", GUILayout.Height(28f))) GenerateAll();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            if (_isGenerating) EditorGUILayout.HelpBox("正在生成中...", MessageType.Info);
            EditorGUILayout.Space(4f);
        }

        private static void DrawReadOnlyField(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space(2f);
        }

        private static void DrawPathWithButton(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(100f));
            EditorGUILayout.SelectableLabel(path, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("打开", GUILayout.Width(50f))) OpenExternal(path);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);
        }
#endif

        // ================================================================
        // Data Classes
        // ================================================================

        [Serializable]
        private sealed class ExcelFileInfo
        {
            [TableColumnWidth(180)]
            public string FileName;
            [TableColumnWidth(70)]
            public string Size;
            [TableColumnWidth(130)]
            public string LastModified;
            [HideInInspector]
            public string FilePath;
        }

        /// <summary>
        /// Odin-friendly wrapper for one data row. Each row is a foldout with key-value fields.
        /// </summary>
        [Serializable]
        private sealed class TableRowData
        {
            [HideInInspector]
            public int RowIndex;

            [ShowInInspector, ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = true)]
            [LabelText("字段")]
            public List<TableFieldData> Fields;
        }

        /// <summary>
        /// One field in a row. The label is dynamically set via "$FieldName" expression.
        /// </summary>
        [Serializable]
        private sealed class TableFieldData
        {
            [HideInInspector]
            public string FieldName;

            [HideInInspector]
            public string FieldType;

            [HideInInspector]
            public bool ReadOnly;

            [ShowInInspector]
            [LabelText("$FieldName")]
            [Tooltip("$FieldType")]
            [EnableIf("@!ReadOnly")]
            public string Value;

            public TableFieldData(string fieldName, string value, string fieldType, bool readOnly)
            {
                FieldName = fieldName;
                Value = value;
                FieldType = fieldType;
                ReadOnly = readOnly;
            }
        }

        // Track foldout states for IMGUI mode
#if !ODIN_INSPECTOR
        private readonly List<bool> _rowFoldouts = new();
#endif
    }
}
#endif
