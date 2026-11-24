using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.UI
{
    /// <summary>
    /// Developer overlay showing frame rate and frame time, toggled with F3.
    /// Used to check the performance budget on the target hardware (Intel Iris Xe, 16.6 ms / 60 fps).
    /// </summary>
    public class DevStatsOverlay : MonoBehaviour
    {
        [SerializeField] bool visible;
        [SerializeField] float sampleWindow = 0.5f;

        float accumulated;
        int frames;
        float fps, ms, worstMs, windowWorst;
        GUIStyle style;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) visible = !visible;

            float dt = Time.unscaledDeltaTime;
            accumulated += dt;
            frames++;
            windowWorst = Mathf.Max(windowWorst, dt * 1000f);

            if (accumulated >= sampleWindow)
            {
                fps = frames / accumulated;
                ms = accumulated * 1000f / frames;
                worstMs = windowWorst;
                accumulated = 0f;
                frames = 0;
                windowWorst = 0f;
            }
        }

        void OnGUI()
        {
            if (!visible) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.UpperLeft };
                style.normal.textColor = Color.white;
            }

            string budget = ms <= 16.7f ? "within 60 fps budget" : "OVER 60 fps budget";
            GUI.Box(new Rect(10, 10, 260, 66),
                $"{fps:0} fps   {ms:0.0} ms avg\nworst {worstMs:0.0} ms\n{Screen.width}x{Screen.height}   {budget}", style);
        }
    }
}
