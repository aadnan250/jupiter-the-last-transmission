using Jupiter.Narrative;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jupiter.Core
{
    /// <summary>
    /// Ends a level and starts the next one: once a conversation is completed (for the Meridian, the observation
    /// deck radio call), it waits a moment, fades to black, shows a time card ("Eleven months later...") and loads
    /// the next scene. The <see cref="GameSession"/> survives the load, so every choice carries into the next level.
    /// The overlay is built at runtime, so the level needs no extra UI.
    /// </summary>
    public class LevelTransition : MonoBehaviour
    {
        [SerializeField] string requiresCompleted;
        [Tooltip("Or/and a story flag (e.g. reaching the base door).")]
        [SerializeField] string requiresFlag;
        [SerializeField] string nextScene;
        [SerializeField] string cardTitle = "LATER";
        [SerializeField, TextArea(1, 3)] string cardSubtitle = "";
        [SerializeField] float delaySeconds = 3f;
        [SerializeField] float fadeSeconds = 1.5f;
        [SerializeField] float holdSeconds = 3.5f;
        [SerializeField] DialogueManager dialogue;

        Image black;
        Text title, subtitle;
        float waited, t = -1f;
        bool loading;

        public bool Started => t >= 0f;
        /// <summary>True once the card is fully up (and, with no next scene, staying up).</summary>
        public bool Holding => loading;
        public string NextScene => nextScene;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            BuildOverlay();
        }

        void Update()
        {
            if (loading || dialogue == null) return;
            if (!Started)
            {
                // Wait until the last conversation is over, then give the moment a few seconds to land.
                // Radio chatter on the move never holds the story up; a conversation the player is in does.
                if ((dialogue.IsActive && !dialogue.OnTheMove) || !Ready()) { waited = 0f; return; }
                if ((waited += Time.deltaTime) < delaySeconds) return;
                Begin();
            }

            t += Time.deltaTime;
            SetAlpha(TransitionTimeline.BlackAlpha(t, fadeSeconds), TransitionTimeline.CardAlpha(t, fadeSeconds));
            if (TransitionTimeline.ShouldLoad(t, fadeSeconds, holdSeconds))
            {
                loading = true;
                // No next scene: the end of what is built so far. The card stays up.
                if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene);
            }
        }

        bool Ready()
        {
            var story = dialogue.Story;
            if (!string.IsNullOrEmpty(requiresCompleted) && !story.IsCompleted(requiresCompleted)) return false;
            if (!string.IsNullOrEmpty(requiresFlag) && !story.HasFlag(requiresFlag)) return false;
            return !string.IsNullOrEmpty(requiresCompleted) || !string.IsNullOrEmpty(requiresFlag);
        }

        /// <summary>Starts the fade now. Public for tests and for a "skip to the next level" shortcut.</summary>
        public void Begin()
        {
            if (Started) return;
            t = 0f;
            var input = FindAnyObjectByType<Player.InputReader>();
            if (input != null) input.GameplayBlocked = true;
        }

        void BuildOverlay()
        {
            var canvasGo = new GameObject("TransitionOverlay");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above every HUD
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            black = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(canvasGo.transform, false);
            black.color = Color.black;
            black.raycastTarget = false;
            var rt = black.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title = CardText(canvasGo.transform, "CardTitle", font, 30, new Vector2(0, 24), cardTitle);
            subtitle = CardText(canvasGo.transform, "CardSubtitle", font, 18, new Vector2(0, -22), cardSubtitle);
            SetAlpha(0f, 0f);
        }

        static Text CardText(Transform parent, string name, Font font, int size, Vector2 pos, string text)
        {
            var t = new GameObject(name, typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(parent, false);
            t.font = font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            t.text = text;
            t.rectTransform.anchoredPosition = pos;
            t.rectTransform.sizeDelta = new Vector2(1000, 60);
            return t;
        }

        void SetAlpha(float blackAlpha, float cardAlpha)
        {
            if (black != null) black.color = new Color(0f, 0f, 0f, blackAlpha);
            if (title != null) title.color = new Color(0.92f, 0.9f, 0.85f, cardAlpha);
            if (subtitle != null) subtitle.color = new Color(0.7f, 0.75f, 0.8f, cardAlpha);
        }
    }
}
