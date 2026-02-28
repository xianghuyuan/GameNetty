using UnityEditor;
using UnityEngine;
using YooAsset;

namespace TEngine.Editor
{
    public class YooAssetDiagnosticTool : EditorWindow
    {
        private Vector2 scrollPosition;
        
        [MenuItem("TEngine/YooAsset 诊断工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<YooAssetDiagnosticTool>("YooAsset 诊断");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("YooAsset 诊断工具", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // EditorPrefs 信息
            DrawEditorPrefsSection();
            GUILayout.Space(10);
            
            // YooAsset 状态
            DrawYooAssetStatusSection();
            GUILayout.Space(10);
            
            // 操作按钮
            DrawActionsSection();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawEditorPrefsSection()
        {
            GUILayout.Label("EditorPrefs 设置", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            bool hasKey = EditorPrefs.HasKey("EditorPlayMode");
            int value = EditorPrefs.GetInt("EditorPlayMode", -1);
            
            EditorGUILayout.LabelField("EditorPlayMode 键是否存在:", hasKey ? "是" : "否");
            EditorGUILayout.LabelField("EditorPlayMode 值:", value.ToString());
            
            if (value >= 0 && value <= 3)
            {
                string[] modeNames = { "EditorSimulateMode", "OfflinePlayMode", "HostPlayMode", "WebPlayMode" };
                EditorGUILayout.LabelField("对应模式:", modeNames[value]);
            }
            else
            {
                EditorGUILayout.HelpBox("无效的 EditorPlayMode 值！", MessageType.Error);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawYooAssetStatusSection()
        {
            GUILayout.Label("YooAsset 状态", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请在 Play 模式下查看 YooAsset 状态", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("YooAssets 已初始化:", YooAssets.Initialized ? "是" : "否");
                
                if (YooAssets.Initialized)
                {
                    var package = YooAssets.TryGetPackage("DefaultPackage");
                    if (package != null)
                    {
                        EditorGUILayout.LabelField("DefaultPackage 存在:", "是");
                        EditorGUILayout.LabelField("初始化状态:", package.InitializeStatus.ToString());
                        
                        if (package.InitializeStatus == EOperationStatus.None)
                        {
                            EditorGUILayout.HelpBox("包未初始化！请检查 ProcedureInitPackage 的日志", MessageType.Error);
                        }
                        else if (package.InitializeStatus == EOperationStatus.Processing)
                        {
                            EditorGUILayout.HelpBox("包正在初始化中...", MessageType.Warning);
                        }
                        else if (package.InitializeStatus == EOperationStatus.Failed)
                        {
                            EditorGUILayout.HelpBox("包初始化失败！", MessageType.Error);
                        }
                        else if (package.InitializeStatus == EOperationStatus.Succeed)
                        {
                            EditorGUILayout.HelpBox("包初始化成功！", MessageType.Info);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("DefaultPackage 不存在！", MessageType.Error);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActionsSection()
        {
            GUILayout.Label("操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            if (GUILayout.Button("设置为 EditorSimulateMode"))
            {
                EditorPrefs.SetInt("EditorPlayMode", 0);
                Debug.Log("[诊断工具] 已设置 EditorPlayMode = 0 (EditorSimulateMode)");
            }
            
            if (GUILayout.Button("设置为 OfflinePlayMode"))
            {
                EditorPrefs.SetInt("EditorPlayMode", 1);
                Debug.Log("[诊断工具] 已设置 EditorPlayMode = 1 (OfflinePlayMode)");
            }
            
            if (GUILayout.Button("设置为 HostPlayMode"))
            {
                EditorPrefs.SetInt("EditorPlayMode", 2);
                Debug.Log("[诊断工具] 已设置 EditorPlayMode = 2 (HostPlayMode)");
            }
            
            if (GUILayout.Button("清除 EditorPlayMode"))
            {
                EditorPrefs.DeleteKey("EditorPlayMode");
                Debug.Log("[诊断工具] 已清除 EditorPlayMode");
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
