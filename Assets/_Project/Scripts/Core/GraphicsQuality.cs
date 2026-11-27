using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jupiter.Core
{
    /// <summary>
    /// Graphics presets for the settings menu, tuned for integrated graphics (Iris Xe at 720p-1080p): render scale,
    /// shadow distance and anti-aliasing on the active URP asset, plus a 60 fps cap with vsync. The URP asset is
    /// reached by reflection so the runtime assembly needs no URP reference; in the editor the original values
    /// are put back when play stops, so the project's asset is never changed.
    /// </summary>
    public static class GraphicsQuality
    {
        public const int Low = 0, Medium = 1, High = 2;
        public static readonly string[] Names = { "LOW", "MEDIUM", "HIGH" };

        static readonly float[] RenderScale = { 0.7f, 0.85f, 1f };
        static readonly float[] ShadowDistance = { 25f, 50f, 90f };
        static readonly int[] Msaa = { 1, 1, 2 };

        static bool saved;
        static object savedScale, savedShadow, savedMsaa;

        public static int Clamp(int level) => Mathf.Clamp(level, Low, High);

        /// <summary>The render scale a preset uses (for tests and the menu).</summary>
        public static float RenderScaleFor(int level) => RenderScale[Clamp(level)];

        public static void Apply(int level)
        {
            level = Clamp(level);
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            var asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null) return;
            var type = asset.GetType();
            if (!saved)
            {
                saved = true;
                savedScale = Get(type, asset, "renderScale");
                savedShadow = Get(type, asset, "shadowDistance");
                savedMsaa = Get(type, asset, "msaaSampleCount");
                Application.quitting += Restore;
            }
            Set(type, asset, "renderScale", RenderScale[level]);
            Set(type, asset, "shadowDistance", ShadowDistance[level]);
            Set(type, asset, "msaaSampleCount", Msaa[level]);
        }

        /// <summary>Puts the URP asset back as it was (the editor saves assets; a player build does not care).</summary>
        public static void Restore()
        {
            if (!saved) return;
            var asset = GraphicsSettings.currentRenderPipeline;
            if (asset != null)
            {
                var type = asset.GetType();
                if (savedScale != null) Set(type, asset, "renderScale", savedScale);
                if (savedShadow != null) Set(type, asset, "shadowDistance", savedShadow);
                if (savedMsaa != null) Set(type, asset, "msaaSampleCount", savedMsaa);
            }
            saved = false;
            Application.quitting -= Restore;
        }

        static object Get(System.Type t, object o, string name)
        {
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return p != null && p.CanRead ? p.GetValue(o) : null;
        }

        static void Set(System.Type t, object o, string name, object value)
        {
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;
            try { p.SetValue(o, System.Convert.ChangeType(value, p.PropertyType)); }
            catch (System.Exception) { /* a URP version without this setter: leave it */ }
        }
    }
}
