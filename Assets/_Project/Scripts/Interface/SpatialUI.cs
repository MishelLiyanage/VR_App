using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CSIVR.Interface
{
    public enum ButtonStyle { Primary, Secondary }

    /// <summary>Builds world-space canvases that work with XR rays and the desktop ray alike, in one shared look.</summary>
    public static class SpatialUI
    {
        public static readonly Color Teal = new Color(0.13f, 0.74f, 0.69f, 1f);
        public static readonly Color TealLight = new Color(0.36f, 0.88f, 0.82f, 1f);
        public static readonly Color TealDark = new Color(0.06f, 0.48f, 0.46f, 1f);
        public static readonly Color Navy = new Color(0.07f, 0.12f, 0.19f, 0.97f);
        public static readonly Color NavyLight = new Color(0.15f, 0.23f, 0.34f, 1f);
        public static readonly Color Frame = new Color(0.20f, 0.23f, 0.28f, 1f);
        public static readonly Color Paper = new Color(0.96f, 0.97f, 0.98f, 1f);
        public static readonly Color PaperDim = new Color(0.88f, 0.91f, 0.94f, 1f);
        public static readonly Color Ink = new Color(0.09f, 0.14f, 0.23f, 1f);
        public static readonly Color Muted = new Color(0.38f, 0.45f, 0.54f, 1f);
        public static readonly Color Amber = new Color(0.98f, 0.66f, 0.15f, 1f);
        public static readonly Color Light = new Color(0.93f, 0.96f, 0.99f, 1f);

        /// <param name="pixelSize">Canvas resolution in UI units.</param>
        /// <param name="widthMeters">Physical width in the room; height follows the aspect ratio.</param>
        /// <param name="interactive">Adds the XR ray raycaster. Display-only canvases leave it off.</param>
        public static Canvas CreateCanvas(string name, Transform parent, Vector2 pixelSize, float widthMeters, bool interactive = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = pixelSize;
            go.transform.localScale = Vector3.one * (widthMeters / pixelSize.x);

            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;

            if (interactive)
            {
                // Walls and furniture block rays so UI cannot be clicked through them.
                var raycaster = go.AddComponent<TrackedDeviceGraphicRaycaster>();
                raycaster.checkFor3DOcclusion = true;
                raycaster.blockingMask = LayerMask.GetMask("Environment");
            }
            return canvas;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions align, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color ?? Light;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(Transform parent, string label, UnityAction onClick, float width = 300f, float height = 80f,
            float fontSize = 34f, ButtonStyle style = ButtonStyle.Primary)
        {
            var go = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            var image = go.GetComponent<Image>();
            image.sprite = UISprites.Rounded;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            bool primary = style == ButtonStyle.Primary;
            colors.normalColor = primary ? Teal : NavyLight;
            colors.highlightedColor = primary ? TealLight : new Color(0.25f, 0.36f, 0.50f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = primary ? TealDark : new Color(0.10f, 0.16f, 0.26f, 1f);
            colors.disabledColor = new Color(0.55f, 0.60f, 0.66f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Label", fontSize, TextAlignmentOptions.Center, Color.white);
            text.fontStyle = FontStyles.Bold;
            Stretch((RectTransform)text.transform, 10, 10, 4, 4);
            return button;
        }

        public static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Pin a rect to the top-left with a fixed size.</summary>
        public static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static void TopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Stretch horizontally at a fixed distance from the top.</summary>
        public static void TopStretch(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(y + height));
            rect.offsetMax = new Vector2(-right, -y);
        }

        public static void BottomStretch(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, y);
            rect.offsetMax = new Vector2(-right, y + height);
        }
    }
}
