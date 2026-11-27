using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.Core
{
    /// <summary>
    /// A full-screen black overlay (and optional centred text) built at runtime, for cuts inside a level: being
    /// strapped into the launch capsule, the final image, the credits. Set <see cref="Alpha"/> and <see cref="Text"/>.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        Image black;
        Text text;

        public float Alpha
        {
            get => black != null ? black.color.a : 0f;
            set { Ensure(); black.color = new Color(0f, 0f, 0f, Mathf.Clamp01(value)); }
        }

        public string Text
        {
            get => text != null ? text.text : string.Empty;
            set { Ensure(); text.text = value; }
        }

        public float TextAlpha
        {
            set { Ensure(); var c = text.color; c.a = Mathf.Clamp01(value); text.color = c; }
        }

        public int FontSize { set { Ensure(); text.fontSize = value; } }

        void Ensure()
        {
            if (black != null) return;
            var canvasGo = new GameObject("ScreenFade");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            black = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(canvasGo.transform, false);
            black.raycastTarget = false;
            var rt = black.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            black.color = new Color(0f, 0f, 0f, 0f);

            text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(canvasGo.transform, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.92f, 0.9f, 0.85f, 0f);
            var trt = text.rectTransform;
            trt.anchorMin = new Vector2(0.1f, 0.1f); trt.anchorMax = new Vector2(0.9f, 0.9f); trt.offsetMin = trt.offsetMax = Vector2.zero;
        }
    }
}
