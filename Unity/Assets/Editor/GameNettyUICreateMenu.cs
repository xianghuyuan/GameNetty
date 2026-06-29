using GameLogic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameNetty.Editor
{
    public static class GameNettyUICreateMenu
    {
        private const string MenuRoot = "GameObject/UI/GameNetty/";

        [MenuItem(MenuRoot + "UIButton", false, 2001)]
        private static void CreateUIButton(MenuCommand menuCommand)
        {
            GameObject gameObject = CreateUIObject("m_btnNew", menuCommand, typeof(CanvasRenderer), typeof(Image), typeof(UIButton));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 40f);

            Image image = gameObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            UIButton button = gameObject.GetComponent<UIButton>();
            button.targetGraphic = image;

            Selection.activeGameObject = gameObject;
        }

        [MenuItem(MenuRoot + "UIImage", false, 2002)]
        private static void CreateUIImage(MenuCommand menuCommand)
        {
            GameObject gameObject = CreateUIObject("m_imgNew", menuCommand, typeof(CanvasRenderer), typeof(UIImage));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100f, 100f);

            UIImage image = gameObject.GetComponent<UIImage>();
            image.color = Color.white;
            image.raycastTarget = false;

            Selection.activeGameObject = gameObject;
        }

        [MenuItem(MenuRoot + "UIText", false, 2003)]
        private static void CreateUIText(MenuCommand menuCommand)
        {
            GameObject gameObject = CreateUIObject("m_tmpNew", menuCommand, typeof(CanvasRenderer), typeof(UIText));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 40f);

            UIText text = gameObject.GetComponent<UIText>();
            text.text = "New Text";
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            Selection.activeGameObject = gameObject;
        }

        private static GameObject CreateUIObject(string name, MenuCommand menuCommand, params System.Type[] components)
        {
            GameObject parent = ResolveParent(menuCommand);
            GameObject gameObject = ObjectFactory.CreateGameObject(name, Combine(typeof(RectTransform), components));

            GameObjectUtility.SetParentAndAlign(gameObject, parent);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchoredPosition = Vector2.zero;

            EnsureEventSystem();
            EditorGUIUtility.PingObject(gameObject);
            return gameObject;
        }

        private static GameObject ResolveParent(MenuCommand menuCommand)
        {
            GameObject context = menuCommand.context as GameObject;
            if (context != null && context.GetComponentInParent<Canvas>() != null)
            {
                return context;
            }

            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.GetComponentInParent<Canvas>() != null)
            {
                return selected;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            return canvas.gameObject;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = ObjectFactory.CreateGameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = ObjectFactory.CreateGameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));

            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static System.Type[] Combine(System.Type first, System.Type[] rest)
        {
            System.Type[] result = new System.Type[rest.Length + 1];
            result[0] = first;
            for (int i = 0; i < rest.Length; i++)
            {
                result[i + 1] = rest[i];
            }

            return result;
        }
    }
}
