#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GameNetty.EditorTools
{
    public sealed class BattleArtWorkflowEditorWindow : EditorWindow
    {
        private const string MenuPath = "Tools/GameNetty/美术/美术工作流";
        private const string DefaultAssetFolder = "Assets/Editor/ArtWorkflows";
        private const float ToolbarHeight = 28f;
        private const float LeftPaneWidth = 300f;

        private BattleArtWorkflowAsset _workflow;
        private BattleArtWorkflowNode _selectedNode;
        private TreeViewState _treeState;
        private ArtWorkflowTreeView _treeView;
        private Vector2 _scroll;
        private string _generatedCommand = string.Empty;

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            BattleArtWorkflowEditorWindow window = GetWindow<BattleArtWorkflowEditorWindow>();
            window.titleContent = new GUIContent("美术工作流");
            window.minSize = new Vector2(860f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            _treeState ??= new TreeViewState();
            RebuildTree();
        }

        private void OnGUI()
        {
            DrawToolbar();

            Rect body = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
            Rect left = new Rect(body.x, body.y, LeftPaneWidth, body.height);
            Rect right = new Rect(left.xMax + 1f, body.y, body.width - LeftPaneWidth - 1f, body.height);

            DrawTreePane(left);
            DrawDetailPane(right);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(new Rect(0f, 0f, position.width, ToolbarHeight), EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            _workflow = (BattleArtWorkflowAsset)EditorGUILayout.ObjectField(_workflow, typeof(BattleArtWorkflowAsset), false, GUILayout.Width(280f));
            if (EditorGUI.EndChangeCheck())
            {
                _selectedNode = null;
                RebuildTree();
            }

            if (GUILayout.Button("创建", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                CreateWorkflowAsset();
            }

            if (GUILayout.Button("战斗模板", EditorStyles.toolbarButton, GUILayout.Width(96f)))
            {
                CreateBattleTemplateAsset();
            }

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(72f)))
            {
                RebuildTree();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawTreePane(Rect rect)
        {
            GUILayout.BeginArea(rect);

            if (_workflow == null)
            {
                EditorGUILayout.HelpBox("选择或创建一个美术工作流。", MessageType.Info);
                GUILayout.EndArea();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加根节点"))
            {
                _workflow.Roots.Add(BattleArtWorkflowNode.Create("新节点"));
                MarkDirty();
                RebuildTree();
            }
            EditorGUILayout.EndHorizontal();

            Rect treeRect = GUILayoutUtility.GetRect(rect.width, rect.height - 32f);
            _treeView?.OnGUI(treeRect);

            GUILayout.EndArea();
        }

        private void DrawDetailPane(Rect rect)
        {
            GUILayout.BeginArea(rect);

            if (_workflow == null)
            {
                GUILayout.EndArea();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawWorkflowFields();

            if (_selectedNode == null)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox("选择一个节点编辑 Prompt，或点击生成命令。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            EditorGUILayout.Space(10f);
            DrawNodeFields();
            EditorGUILayout.Space(10f);
            DrawCommandBox();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawWorkflowFields()
        {
            EditorGUILayout.LabelField("默认参数", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _workflow.WorkflowName = EditorGUILayout.TextField("名称", _workflow.WorkflowName);
            _workflow.Model = EditorGUILayout.TextField("model", _workflow.Model);
            _workflow.Size = EditorGUILayout.TextField("size", _workflow.Size);
            _workflow.Quality = EditorGUILayout.TextField("quality", _workflow.Quality);
            _workflow.OutputFormat = EditorGUILayout.TextField("output_format", _workflow.OutputFormat);
            _workflow.OutputDir = EditorGUILayout.TextField("output_dir", _workflow.OutputDir);
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }
        }

        private void DrawNodeFields()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_selectedNode.Name, EditorStyles.boldLabel);
            if (GUILayout.Button("添加子节点", GUILayout.Width(96f)))
            {
                _selectedNode.Children.Add(BattleArtWorkflowNode.Create("新节点"));
                MarkDirty();
                RebuildTree();
            }
            if (GUILayout.Button("删除", GUILayout.Width(64f)))
            {
                DeleteSelectedNode();
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _selectedNode.Name = EditorGUILayout.TextField("名称", _selectedNode.Name);
            _selectedNode.SizeOverride = EditorGUILayout.TextField("size 覆盖", _selectedNode.SizeOverride);
            _selectedNode.OutputDirOverride = EditorGUILayout.TextField("output_dir 覆盖", _selectedNode.OutputDirOverride);
            _selectedNode.OverrideParentPrompt = EditorGUILayout.Toggle("覆盖上层 Prompt", _selectedNode.OverrideParentPrompt);
            _selectedNode.OverrideParentNegativePrompt = EditorGUILayout.Toggle("覆盖上层 Negative", _selectedNode.OverrideParentNegativePrompt);
            EditorGUILayout.LabelField("Prompt", EditorStyles.miniBoldLabel);
            _selectedNode.Prompt = EditorGUILayout.TextArea(_selectedNode.Prompt, GUILayout.MinHeight(120f));
            EditorGUILayout.LabelField("Negative Prompt", EditorStyles.miniBoldLabel);
            _selectedNode.NegativePrompt = EditorGUILayout.TextArea(_selectedNode.NegativePrompt, GUILayout.MinHeight(72f));
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
                RebuildTree();
            }
        }

        private void DrawCommandBox()
        {
            EditorGUILayout.LabelField("生成命令", EditorStyles.boldLabel);
            if (GUILayout.Button("生成命令", GUILayout.Height(28f)))
            {
                _generatedCommand = BuildGenerationCommand(_selectedNode);
                EditorGUIUtility.systemCopyBuffer = _generatedCommand;
            }

            if (!string.IsNullOrWhiteSpace(_generatedCommand))
            {
                EditorGUILayout.TextArea(_generatedCommand, GUILayout.MinHeight(160f));
                if (GUILayout.Button("复制命令", GUILayout.Width(96f)))
                {
                    EditorGUIUtility.systemCopyBuffer = _generatedCommand;
                }
            }
        }

        private void CreateWorkflowAsset()
        {
            EnsureFolder(DefaultAssetFolder);
            string path = EditorUtility.SaveFilePanelInProject("创建美术工作流", "ArtWorkflow", "asset", "选择保存路径。", DefaultAssetFolder);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            BattleArtWorkflowAsset asset = CreateInstance<BattleArtWorkflowAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            _workflow = asset;
            _selectedNode = null;
            RebuildTree();
        }

        private void CreateBattleTemplateAsset()
        {
            EnsureFolder(DefaultAssetFolder);
            string path = EditorUtility.SaveFilePanelInProject("创建战斗美术工作流", "BattleArtWorkflow", "asset", "选择保存路径。", DefaultAssetFolder);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            BattleArtWorkflowAsset asset = CreateInstance<BattleArtWorkflowAsset>();
            asset.Roots = BuildBattleTemplate();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            _workflow = asset;
            _selectedNode = null;
            RebuildTree();
        }

        private static List<BattleArtWorkflowNode> BuildBattleTemplate()
        {
            BattleArtWorkflowNode global = BattleArtWorkflowNode.Create("Global");
            global.Prompt = "生成一个独立的游戏美术资源，透明背景。";
            global.NegativePrompt = "完整界面 mockup、背景场景、角色、文字、数字。";

            BattleArtWorkflowNode battle = BattleArtWorkflowNode.Create("战斗场景");
            battle.Prompt = "必须是 2D 横版 ARPG 使用的奇幻暗童话手工剪纸纸偶剧场风格。";
            battle.NegativePrompt = "现代扁平 UI、科幻机械、写实 3D、亮色卡通。";

            BattleArtWorkflowNode hud = BattleArtWorkflowNode.Create("战斗 HUD");
            hud.Prompt = "生成一个独立的 HUD 小图资源，透明背景。整体形状干净、可读、低噪声，并具有精致游戏 UI 质感。";
            hud.NegativePrompt = "角色、背景场景、完整 HUD 截图、相邻 UI 元素。";

            BattleArtWorkflowNode hp = BattleArtWorkflowNode.Create("玩家血条底图");
            hp.Prompt = "生成一个细长横向的玩家血条底图，由分层剪纸、哑光旧纸板和少量布条边饰组成。中心区域必须是空的深红褐色凹槽，用于后续叠放血量填充图。整体形状要干净、可读、低噪声，并适合横向 9-slice 拉伸。使用宽大的剪纸层边缘、柔和层次阴影、轻微手工边缘不规则感和精致游戏 UI 质感。";
            hp.NegativePrompt = "血量填充、边框高光、文字、数字、图标、角色、背景场景、完整界面 mockup。";

            hud.Children.Add(hp);
            battle.Children.Add(hud);
            global.Children.Add(battle);
            return new List<BattleArtWorkflowNode> { global };
        }

        private void DeleteSelectedNode()
        {
            if (_selectedNode == null)
            {
                return;
            }

            if (RemoveNode(_workflow.Roots, _selectedNode.NodeId))
            {
                _selectedNode = null;
                _generatedCommand = string.Empty;
                MarkDirty();
                RebuildTree();
            }
        }

        private static bool RemoveNode(List<BattleArtWorkflowNode> nodes, string nodeId)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].NodeId == nodeId)
                {
                    nodes.RemoveAt(i);
                    return true;
                }

                if (RemoveNode(nodes[i].Children, nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        private string BuildGenerationCommand(BattleArtWorkflowNode target)
        {
            List<BattleArtWorkflowNode> path = GetNodePath(target);
            string prompt = BuildPrompt(path);
            string size = GetEffectiveSize(path);
            string outputDir = GetEffectiveOutputDir(path);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"model\": \"{EscapeJson(_workflow.Model)}\",");
            sb.AppendLine($"  \"size\": \"{EscapeJson(size)}\",");
            sb.AppendLine($"  \"quality\": \"{EscapeJson(_workflow.Quality)}\",");
            sb.AppendLine($"  \"output_format\": \"{EscapeJson(_workflow.OutputFormat)}\",");
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                sb.AppendLine($"  \"output_dir\": \"{EscapeJson(outputDir)}\",");
            }
            sb.AppendLine($"  \"prompt\": \"{EscapeJson(prompt)}\"");
            sb.Append("}");
            return sb.ToString();
        }

        private List<BattleArtWorkflowNode> GetNodePath(BattleArtWorkflowNode target)
        {
            List<BattleArtWorkflowNode> path = new List<BattleArtWorkflowNode>();
            foreach (BattleArtWorkflowNode root in _workflow.Roots)
            {
                if (TryBuildNodePath(root, target.NodeId, path))
                {
                    return path;
                }
            }

            return path;
        }

        private static bool TryBuildNodePath(BattleArtWorkflowNode current, string targetId, List<BattleArtWorkflowNode> path)
        {
            path.Add(current);
            if (current.NodeId == targetId)
            {
                return true;
            }

            foreach (BattleArtWorkflowNode child in current.Children)
            {
                if (TryBuildNodePath(child, targetId, path))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        private static string BuildPrompt(List<BattleArtWorkflowNode> path)
        {
            StringBuilder prompt = new StringBuilder();
            StringBuilder negative = new StringBuilder();

            foreach (BattleArtWorkflowNode node in path)
            {
                if (node.OverrideParentPrompt)
                {
                    prompt.Clear();
                }
                if (node.OverrideParentNegativePrompt)
                {
                    negative.Clear();
                }
                AppendPart(prompt, node.Prompt);
                AppendPart(negative, node.NegativePrompt);
            }

            if (negative.Length > 0)
            {
                if (prompt.Length > 0)
                {
                    prompt.AppendLine();
                }
                prompt.Append("不要包含：");
                prompt.Append(negative);
            }

            return prompt.ToString().Trim();
        }

        private string GetEffectiveSize(List<BattleArtWorkflowNode> path)
        {
            string value = _workflow.Size;
            foreach (BattleArtWorkflowNode node in path)
            {
                if (!string.IsNullOrWhiteSpace(node.SizeOverride))
                {
                    value = node.SizeOverride.Trim();
                }
            }
            return value;
        }

        private string GetEffectiveOutputDir(List<BattleArtWorkflowNode> path)
        {
            string value = _workflow.OutputDir;
            foreach (BattleArtWorkflowNode node in path)
            {
                if (!string.IsNullOrWhiteSpace(node.OutputDirOverride))
                {
                    value = node.OutputDirOverride.Trim();
                }
            }
            return value;
        }

        private static void AppendPart(StringBuilder sb, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }
            sb.Append(value.Trim());
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private void MarkDirty()
        {
            if (_workflow != null)
            {
                EditorUtility.SetDirty(_workflow);
            }
        }

        private void RebuildTree()
        {
            _treeState ??= new TreeViewState();
            _treeView = new ArtWorkflowTreeView(_treeState, _workflow, OnTreeSelectionChanged);
            _treeView.Reload();
        }

        private void OnTreeSelectionChanged(BattleArtWorkflowNode node)
        {
            _selectedNode = node;
            _generatedCommand = string.Empty;
            Repaint();
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }

    internal sealed class ArtWorkflowTreeView : TreeView
    {
        private readonly BattleArtWorkflowAsset _workflow;
        private readonly Action<BattleArtWorkflowNode> _onSelected;
        private readonly Dictionary<int, BattleArtWorkflowNode> _nodeById = new();
        private int _nextId;

        public ArtWorkflowTreeView(TreeViewState state, BattleArtWorkflowAsset workflow, Action<BattleArtWorkflowNode> onSelected) : base(state)
        {
            _workflow = workflow;
            _onSelected = onSelected;
            rowHeight = 20f;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
        }

        protected override TreeViewItem BuildRoot()
        {
            _nodeById.Clear();
            _nextId = 1;

            TreeViewItem root = new TreeViewItem(0, -1, _workflow != null ? _workflow.WorkflowName : "美术工作流")
            {
                children = new List<TreeViewItem>(),
            };

            if (_workflow != null)
            {
                foreach (BattleArtWorkflowNode node in _workflow.Roots)
                {
                    root.AddChild(BuildNode(node, 0));
                }
            }

            if (root.children.Count == 0)
            {
                root.AddChild(new TreeViewItem(_nextId++, 0, "(empty)"));
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void SingleClickedItem(int id)
        {
            if (_nodeById.TryGetValue(id, out BattleArtWorkflowNode node))
            {
                _onSelected?.Invoke(node);
            }
        }

        private TreeViewItem BuildNode(BattleArtWorkflowNode node, int depth)
        {
            int id = _nextId++;
            _nodeById[id] = node;

            TreeViewItem item = new TreeViewItem(id, depth, node.Name)
            {
                children = new List<TreeViewItem>(),
            };

            foreach (BattleArtWorkflowNode child in node.Children)
            {
                item.AddChild(BuildNode(child, depth + 1));
            }

            return item;
        }
    }
}
#endif
