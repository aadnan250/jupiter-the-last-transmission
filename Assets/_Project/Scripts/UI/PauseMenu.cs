using Jupiter.Core;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jupiter.UI
{
    /// <summary>
    /// Escape (or Start / Options on a controller) pauses: time stops, sound pauses, the cursor is freed. From here
    /// the player can resume, open the settings (look sensitivity, invert Y, head bob, subtitle size, volume),
    /// restart the chapter or quit. Settings apply at once and are saved. Lives on the player rig's root.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] CameraController look;
        [SerializeField] DialogueUI subtitles;

        enum Page { Main, Settings }

        Page page;
        float timeScaleBefore = 1f;
        GameSettings settings;
        GUIStyle title, button, label, small;
        Texture2D dim, buttonBg, buttonHover;

        public bool IsPaused { get; private set; }
        public GameSettings Settings => settings;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (look == null) look = GetComponentInChildren<CameraController>(true);
            if (subtitles == null) subtitles = GetComponentInChildren<DialogueUI>(true);
            settings = GameSettings.Load();
        }

        void Start() => Apply();

        void OnDisable() { if (IsPaused) Resume(); }

        void Update()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool toggle = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
            if (!toggle) return;

            if (IsPaused)
            {
                if (page == Page.Settings) page = Page.Main; else Resume();
            }
            // A document or keypad open on screen takes Escape to close itself.
            else if (input == null || !input.GameplayBlocked) Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            page = Page.Main;
            timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (input != null) input.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = timeScaleBefore;
            AudioListener.pause = false;
            if (input != null) input.enabled = true;
            settings.Save();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Pushes the current settings to the camera, the subtitles and the master volume.</summary>
        public void Apply()
        {
            if (look != null) look.ApplySettings(settings.Sensitivity, settings.InvertY, settings.HeadBob);
            if (subtitles != null) subtitles.SetTextScale(settings.SubtitleScale);
            AudioListener.volume = settings.Volume;
            GraphicsQuality.Apply(settings.Quality);
        }

        void RestartChapter()
        {
            Resume();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                ? SceneManager.GetActiveScene().name : SceneManager.GetActiveScene().path);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        void Styles(float k)
        {
            if (dim == null)
            {
                dim = Solid(new Color(0.01f, 0.02f, 0.04f, 0.82f));
                buttonBg = Solid(new Color(0.12f, 0.16f, 0.2f, 0.9f));
                buttonHover = Solid(new Color(0.2f, 0.32f, 0.38f, 0.95f));
            }
            title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(44 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.85f, 0.92f, 0.95f);
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * k), alignment = TextAnchor.MiddleLeft };
            label.normal.textColor = new Color(0.82f, 0.86f, 0.88f);
            small = new GUIStyle(label) { fontSize = Mathf.RoundToInt(15 * k), alignment = TextAnchor.MiddleCenter };
            small.normal.textColor = new Color(0.55f, 0.62f, 0.66f);
            button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22 * k), alignment = TextAnchor.MiddleCenter };
            button.normal.background = buttonBg;
            button.hover.background = buttonHover;
            button.active.background = buttonHover;
            button.normal.textColor = button.hover.textColor = button.active.textColor = new Color(0.9f, 0.95f, 0.97f);
        }

        void OnGUI()
        {
            if (!IsPaused) return;
            float k = Mathf.Max(0.6f, Screen.height / 1080f);
            Styles(k);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dim);

            float w = 520f * k, rowH = 54f * k, gap = 14f * k;
            float x = (Screen.width - w) * 0.5f, y = Screen.height * 0.2f;
            GUI.Label(new Rect(x, y, w, 70f * k), page == Page.Main ? "PAUSED" : "SETTINGS", title);
            y += 100f * k;

            if (page == Page.Main)
            {
                if (GUI.Button(new Rect(x, y, w, rowH), "Resume", button)) Resume();
                y += rowH + gap;
                if (GUI.Button(new Rect(x, y, w, rowH), "Settings", button)) page = Page.Settings;
                y += rowH + gap;
                if (GUI.Button(new Rect(x, y, w, rowH), "Restart chapter", button)) RestartChapter();
                y += rowH + gap;
                if (GUI.Button(new Rect(x, y, w, rowH), "Quit game", button)) Quit();
                y += rowH + gap * 2f;
                GUI.Label(new Rect(x, y, w, 30f * k), "Esc / Options to resume", small);
                return;
            }

            float labelW = w * 0.45f, sliderW = w * 0.55f;
            bool changed = false;
            changed |= Slider(ref y, x, labelW, sliderW, rowH, k, "Look sensitivity", settings.Sensitivity,
                GameSettings.MinSensitivity, GameSettings.MaxSensitivity, v => settings.Sensitivity = v, $"{settings.Sensitivity:0.00}x");
            changed |= Slider(ref y, x, labelW, sliderW, rowH, k, "Subtitle size", settings.SubtitleScale,
                GameSettings.MinSubtitleScale, GameSettings.MaxSubtitleScale, v => settings.SubtitleScale = v, $"{settings.SubtitleScale * 100f:0}%");
            changed |= Slider(ref y, x, labelW, sliderW, rowH, k, "Volume", settings.Volume, 0f, 1f, v => settings.Volume = v, $"{settings.Volume * 100f:0}%");
            changed |= Toggle(ref y, x, w, rowH, "Invert Y", settings.InvertY, v => settings.InvertY = v);
            changed |= Toggle(ref y, x, w, rowH, "Head bob", settings.HeadBob, v => settings.HeadBob = v);
            if (GUI.Button(new Rect(x, y, w, rowH * 0.85f), $"Graphics: {GraphicsQuality.Names[settings.Quality]}", button))
            {
                settings.Quality = (settings.Quality + 1) % GraphicsQuality.Names.Length;
                changed = true;
            }
            y += rowH;
            if (changed) Apply();

            y += gap;
            if (GUI.Button(new Rect(x, y, w, rowH), "Back", button)) { settings.Save(); page = Page.Main; }
        }

        bool Slider(ref float y, float x, float labelW, float sliderW, float rowH, float k, string name, float value,
            float min, float max, System.Action<float> set, string shown)
        {
            GUI.Label(new Rect(x, y, labelW, rowH), $"{name}   {shown}", label);
            float v = GUI.HorizontalSlider(new Rect(x + labelW, y + rowH * 0.42f, sliderW, rowH * 0.3f), value, min, max);
            y += rowH;
            if (Mathf.Approximately(v, value)) return false;
            set(v);
            return true;
        }

        bool Toggle(ref float y, float x, float w, float rowH, string name, bool value, System.Action<bool> set)
        {
            bool v = GUI.Button(new Rect(x, y, w, rowH * 0.85f), $"{name}: {(value ? "ON" : "OFF")}", button) ? !value : value;
            y += rowH;
            if (v == value) return false;
            set(v);
            return true;
        }
    }
}
