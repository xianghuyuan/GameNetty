#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GameLogic;
using TEngine.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameNetty.Editor.UIAutoPrefab
{
    public sealed class UiAutoPrefabImporter : EditorWindow
    {
        private const string BattleMainWindowLayoutPath =
            "spec/art/ui-auto-prefab/BattleMainWindow/ui_layout.json";

        [SerializeField] private string m_layoutPath;
        [SerializeField] private bool m_generateBindingCode = true;

        [MenuItem("Tools/GameNetty/UI Auto Prefab/Import Layout")]
        private static void Open()
        {
            GetWindow<UiAutoPrefabImporter>("UI Auto Prefab");
        }

        [MenuItem("Tools/GameNetty/UI Auto Prefab/Import Layout File...")]
        private static void ImportLayoutFile()
        {
            string path = EditorUtility.OpenFilePanel("Select ui_layout.json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ImportLayout(path, true);
        }

        [MenuItem("Tools/GameNetty/UI Auto Prefab/Import BattleMainWindow Demo")]
        public static void ImportBattleMainWindowDemo()
        {
            string path = Path.Combine(GetRepositoryRoot(), BattleMainWindowLayoutPath);
            ImportLayout(path, true);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Single PNG UI Prefab Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Run Tools/UIAutoPrefab/ui_mockup_analyzer.py first, then import the generated ui_layout.json here.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_layoutPath = EditorGUILayout.TextField("Layout JSON", m_layoutPath);
                if (GUILayout.Button("Browse", GUILayout.Width(80)))
                {
                    string path = EditorUtility.OpenFilePanel("Select ui_layout.json", Application.dataPath, "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        m_layoutPath = path;
                    }
                }
            }

            m_generateBindingCode = EditorGUILayout.Toggle("Generate Binding Code", m_generateBindingCode);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_layoutPath)))
            {
                if (GUILayout.Button("Import"))
                {
                    ImportLayout(m_layoutPath, m_generateBindingCode);
                }
            }
        }

        public static GameObject ImportLayout(string layoutJsonPath, bool generateBindingCode)
        {
            if (string.IsNullOrEmpty(layoutJsonPath) || !File.Exists(layoutJsonPath))
            {
                throw new FileNotFoundException("ui_layout.json not found.", layoutJsonPath);
            }

            string json = File.ReadAllText(layoutJsonPath);
            UiAutoPrefabLayout layout = JsonUtility.FromJson<UiAutoPrefabLayout>(json);
            layout.Validate(layoutJsonPath);

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            EnsureUnityAssetDirectory(projectRoot, layout.spriteOutputPath);
            EnsureUnityAssetDirectory(projectRoot, Path.GetDirectoryName(layout.prefabPath));
            CleanSpriteOutputDirectory(layout);

            GameObject root = CreateRoot(layout);
            try
            {
                string layoutDir = Path.GetDirectoryName(layoutJsonPath);
                Dictionary<string, Transform> nodeTransforms = new Dictionary<string, Transform>
                {
                    [layout.windowName] = root.transform,
                };

                foreach (UiAutoPrefabNode node in layout.nodes)
                {
                    Transform parent = ResolveParent(root.transform, nodeTransforms, node.parent);
                    GameObject created = CreateNode(parent, layout, node, layoutDir, projectRoot);
                    if (created != null)
                    {
                        nodeTransforms[node.id] = created.transform;
                        if (!string.IsNullOrEmpty(node.name))
                        {
                            nodeTransforms[node.name] = created.transform;
                        }
                    }
                }

                Selection.activeTransform = root.transform;
                ScriptGenerator.GenerateUIComponentScript();

                string prefabPath = NormalizeAssetPath(layout.prefabPath);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Prefab save failed: " + prefabPath);
                }

                if (generateBindingCode)
                {
                    Selection.activeTransform = root.transform;
                    ScriptGenerator.GenerateCSharpScript(
                        includeListener: false,
                        isUniTask: false,
                        isAutoGenerate: true,
                        savePath: ScriptGeneratorSetting.GetGenCodePath(),
                        className: layout.windowName,
                        uiGenTypeName: "UIWindow",
                        isGenImp: false);
                    NormalizeGeneratedScript(ScriptGeneratorSetting.GetGenCodePath(), layout.windowName, "UIWindow");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.LogFormat("UI Auto Prefab imported: {0}", prefabPath);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRoot(UiAutoPrefabLayout layout)
        {
            GameObject root = new GameObject(layout.windowName, typeof(RectTransform), typeof(UIBindComponent));
            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(layout.canvas.width, layout.canvas.height);
            rectTransform.anchoredPosition = Vector2.zero;
            return root;
        }

        private static GameObject CreateNode(
            Transform parent,
            UiAutoPrefabLayout layout,
            UiAutoPrefabNode node,
            string layoutDir,
            string projectRoot)
        {
            string component = string.IsNullOrEmpty(node.component) ? "Image" : node.component;
            string objectName = GetNodeName(node, component);
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            ApplyRect(child.GetComponent<RectTransform>(), node.rect);

            if (IsText(component))
            {
                UIText text = child.AddComponent<UIText>();
                text.text = string.IsNullOrEmpty(node.text) ? objectName : node.text;
                text.raycastTarget = false;
                if (node.fontSize > 0)
                {
                    text.fontSize = node.fontSize;
                }
                return child;
            }

            if (IsContainer(component))
            {
                return child;
            }

            Image image = child.AddComponent<Image>();
            image.sprite = LoadSprite(layout, node, layoutDir, projectRoot);
            image.raycastTarget = component == "UIButton";
            ApplyImageMode(image, node);
            if (node.nineSlice)
            {
                image.type = Image.Type.Sliced;
            }

            if (component == "UIButton")
            {
                UIButton button = child.AddComponent<UIButton>();
                button.targetGraphic = image;
            }

            return child;
        }

        private static Transform ResolveParent(
            Transform root,
            Dictionary<string, Transform> nodeTransforms,
            string parent)
        {
            if (string.IsNullOrEmpty(parent))
            {
                return root;
            }

            if (nodeTransforms.TryGetValue(parent, out Transform transform) && transform != null)
            {
                return transform;
            }

            Transform found = root.Find(parent);
            return found != null ? found : root;
        }

        private static void ApplyRect(RectTransform rectTransform, UiAutoPrefabRect rect)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(rect.x, -rect.y);
            rectTransform.sizeDelta = new Vector2(rect.width, rect.height);
        }

        private static void ApplyImageMode(Image image, UiAutoPrefabNode node)
        {
            if (image == null)
            {
                return;
            }

            if (string.Equals(node.imageType, "Filled", StringComparison.OrdinalIgnoreCase))
            {
                image.type = Image.Type.Filled;
                if (string.Equals(node.fillMethod, "Radial360", StringComparison.OrdinalIgnoreCase))
                {
                    image.fillMethod = Image.FillMethod.Radial360;
                    image.fillOrigin = GetRadial360Origin(node.fillOrigin);
                    image.fillClockwise = node.fillClockwiseSet ? node.fillClockwise : true;
                }
                else
                {
                    image.fillMethod = Image.FillMethod.Horizontal;
                    image.fillOrigin = (int)Image.OriginHorizontal.Left;
                }

                image.fillAmount = node.fillAmount > 0f ? Mathf.Clamp01(node.fillAmount) : 1f;
            }

            if (node.raycastTargetSet)
            {
                image.raycastTarget = node.raycastTarget;
            }
        }

        private static int GetRadial360Origin(string origin)
        {
            if (string.Equals(origin, "Right", StringComparison.OrdinalIgnoreCase))
            {
                return (int)Image.Origin360.Right;
            }

            if (string.Equals(origin, "Bottom", StringComparison.OrdinalIgnoreCase))
            {
                return (int)Image.Origin360.Bottom;
            }

            if (string.Equals(origin, "Left", StringComparison.OrdinalIgnoreCase))
            {
                return (int)Image.Origin360.Left;
            }

            return (int)Image.Origin360.Top;
        }


        private static Sprite LoadSprite(
            UiAutoPrefabLayout layout,
            UiAutoPrefabNode node,
            string layoutDir,
            string projectRoot)
        {
            string spritePath = !string.IsNullOrEmpty(node.sprite) ? node.sprite : node.slice;

            if (string.IsNullOrEmpty(spritePath))
            {
                return null;
            }

            string sourcePath = Path.GetFullPath(Path.Combine(layoutDir, spritePath));
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning("PNG asset file missing: " + sourcePath);
                return null;
            }

            string spriteAssetPath = NormalizeAssetPath(
                Path.Combine(layout.spriteOutputPath, Path.GetFileName(spritePath)).Replace("\\", "/"));
            string destinationPath = Path.Combine(projectRoot, spriteAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath, true);

            AssetDatabase.ImportAsset(spriteAssetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
        }

        private static void CleanSpriteOutputDirectory(UiAutoPrefabLayout layout)
        {
            string outputPath = NormalizeAssetPath(layout.spriteOutputPath);
            string absoluteOutputPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, outputPath);
            if (!Directory.Exists(absoluteOutputPath))
            {
                return;
            }

            HashSet<string> currentSpriteNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UiAutoPrefabNode node in layout.nodes)
            {
                string spritePath = !string.IsNullOrEmpty(node.sprite) ? node.sprite : node.slice;
                if (!string.IsNullOrEmpty(spritePath))
                {
                    currentSpriteNames.Add(Path.GetFileName(spritePath));
                }
            }

            foreach (string pngPath in Directory.GetFiles(absoluteOutputPath, "*.png", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(pngPath);
                if (currentSpriteNames.Contains(fileName))
                {
                    continue;
                }

                string assetPath = NormalizeAssetPath(Path.Combine(outputPath, fileName));
                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    File.Delete(pngPath);
                    string metaPath = pngPath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
            }
        }

        private static string GetNodeName(UiAutoPrefabNode node, string component)
        {
            if (!string.IsNullOrEmpty(node.name))
            {
                return node.name;
            }

            if (component == "UIButton")
            {
                return (node.bind ? "m_btn" : "auto_btn") + SanitizeId(node.id);
            }

            if (IsText(component))
            {
                return (node.bind ? "m_tmp" : "auto_tmp") + SanitizeId(node.id);
            }

            if (IsContainer(component))
            {
                return (node.bind ? "m_tf" : "auto_tf") + SanitizeId(node.id);
            }

            return (node.bind ? "m_img" : "auto_img") + SanitizeId(node.id);
        }

        private static string SanitizeId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "Node";
            }

            return id.Replace("_", string.Empty).Replace("-", string.Empty);
        }

        private static bool IsText(string component)
        {
            return component == "UIText" || component == "UITextPlaceholder" || component == "TextMeshProUGUI";
        }

        private static bool IsContainer(string component)
        {
            return component == "Container" || component == "List" || component == "RectTransform";
        }

        private static void EnsureUnityAssetDirectory(string projectRoot, string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string normalized = NormalizeAssetPath(assetPath);
            Directory.CreateDirectory(Path.Combine(projectRoot, normalized));
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace("\\", "/").TrimStart('/');
        }

        private static string GetUnityProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string GetRepositoryRoot()
        {
            return Directory.GetParent(GetUnityProjectRoot()).FullName;
        }

        private static void NormalizeGeneratedScript(string savePath, string className, string baseTypeName)
        {
            string generatedPath = Path.Combine(GetUnityProjectRoot(), NormalizeAssetPath(savePath), className + "_Gen.g.cs");
            if (!File.Exists(generatedPath))
            {
                return;
            }

            string generated = File.ReadAllText(generatedPath);
            if (generated.Contains("namespace GameLogic") && generated.Contains("partial class " + className))
            {
                return;
            }

            FileAttributes generatedAttributes = File.GetAttributes(generatedPath);
            bool wasReadOnly = (generatedAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            if (wasReadOnly)
            {
                File.SetAttributes(generatedPath, generatedAttributes & ~FileAttributes.ReadOnly);
            }

            MatchCollection matches = Regex.Matches(generated, @"SetClick\((OnClick[A-Za-z0-9_]+)\)");
            HashSet<string> eventNames = new HashSet<string>();
            foreach (Match match in matches)
            {
                eventNames.Add(match.Groups[1].Value);
            }

            string eventsBlock = string.Empty;
            foreach (string eventName in eventNames)
            {
                eventsBlock += "\n\t\tprivate partial void " + eventName + "();\n";
            }

            string normalized =
                "﻿//----------------------------------------------------------\n" +
                "// <auto-generated>\n" +
                "// -This code was generated.\n" +
                "// -Changes to this file may cause incorrect behavior.\n" +
                "// -will be lost if the code is regenerated.\n" +
                "// <auto-generated/>\n" +
                "//----------------------------------------------------------\n" +
                "using TMPro;\n" +
                "using UnityEngine;\n" +
                "using UnityEngine.UI;\n" +
                "using TEngine;\n\n" +
                "namespace GameLogic\n" +
                "{\n" +
                "\tpublic partial class " + className + " : " + baseTypeName + "\n" +
                "\t{\n" +
                generated.TrimEnd() +
                "\n\n\t\t#region 事件\n" +
                eventsBlock +
                "\n\t\t#endregion\n" +
                "\t}\n" +
                "}\n";
            File.WriteAllText(generatedPath, normalized);
            if (wasReadOnly)
            {
                File.SetAttributes(generatedPath, File.GetAttributes(generatedPath) | FileAttributes.ReadOnly);
            }

            AssetDatabase.ImportAsset(NormalizeAssetPath(Path.Combine(savePath, className + "_Gen.g.cs")), ImportAssetOptions.ForceUpdate);
        }

        [Serializable]
        private sealed class UiAutoPrefabLayout
        {
            public int version;
            public string module;
            public string windowName;
            public string prefabPath;
            public string spriteOutputPath;
            public UiAutoPrefabCanvas canvas;
            public UiAutoPrefabNode[] nodes;

            public void Validate(string sourcePath)
            {
                if (canvas == null || canvas.width <= 0 || canvas.height <= 0)
                {
                    throw new InvalidOperationException("Invalid canvas in layout: " + sourcePath);
                }

                if (string.IsNullOrEmpty(windowName))
                {
                    throw new InvalidOperationException("windowName is required in layout: " + sourcePath);
                }

                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = "Assets/AssetRaw/UI/" + module + "/" + windowName + ".prefab";
                }

                if (string.IsNullOrEmpty(spriteOutputPath))
                {
                    spriteOutputPath = "Assets/AssetRaw/UIRaw/Auto/" + module + "/" + windowName;
                }

                if (nodes == null)
                {
                    nodes = new UiAutoPrefabNode[0];
                }
            }
        }

        [Serializable]
        private sealed class UiAutoPrefabCanvas
        {
            public int width;
            public int height;
        }

        [Serializable]
        private sealed class UiAutoPrefabNode
        {
            public string id;
            public string parent;
            public string name;
            public string component;
            public bool bind;
            public bool nineSlice;
            public bool fullCanvasLayer;
            public string slice;
            public string layer;
            public string sprite;
            public string assetType;
            public string imageType;
            public string fillMethod;
            public string fillOrigin;
            public float fillAmount;
            public bool fillClockwise;
            public bool fillClockwiseSet;
            public bool raycastTargetSet;
            public bool raycastTarget;
            public string text;
            public float fontSize;
            public UiAutoPrefabRect rect;
        }

        [Serializable]
        private sealed class UiAutoPrefabRect
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }
    }
}

#endif
