using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Makes the game fill the screen. In the editor, the Game view is set to "Play Maximized", so pressing Play
    /// enlarges it over the whole editor window (Shift+Space toggles it back). In a built game, the player starts in
    /// borderless full screen at the monitor's native resolution. Applied automatically when the editor loads,
    /// and from the menu: Jupiter > Apply Full Screen Settings.
    /// </summary>
    [InitializeOnLoad]
    public static class FullScreenSetup
    {
        const string SessionKey = "Jupiter.FullScreenSetup.Done";

        static FullScreenSetup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                SessionState.SetBool(SessionKey, true);
                ApplyPlayerSettings();
                if (Application.isBatchMode) return; // no editor windows in batch mode (tests, CI)
                MaximizeGameViewOnPlay();
            };
        }

        [MenuItem("Jupiter/Apply Full Screen Settings")]
        public static void Apply()
        {
            ApplyPlayerSettings();
            bool ok = MaximizeGameViewOnPlay();
            Debug.Log(ok ? "[FullScreenSetup] Builds start full screen; the Game view plays maximized."
                         : "[FullScreenSetup] Builds start full screen. Set the Game view's Play menu to 'Play Maximized' by hand.");
        }

        /// <summary>Built game: borderless full screen at native resolution (only touched when different, so nothing is dirtied needlessly).</summary>
        public static void ApplyPlayerSettings()
        {
            bool changed = false;
            if (PlayerSettings.fullScreenMode != FullScreenMode.FullScreenWindow) { PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; changed = true; }
            if (!PlayerSettings.defaultIsNativeResolution) { PlayerSettings.defaultIsNativeResolution = true; changed = true; }
            if (changed) AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The Game view's "Play Maximized" option is not public API, so it is set through reflection, guarded so a
        /// future Unity version that renames it only skips this step instead of breaking the editor.
        /// </summary>
        public static bool MaximizeGameViewOnPlay()
        {
            try
            {
                var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (gameViewType == null) return false;
                var views = Resources.FindObjectsOfTypeAll(gameViewType);
                var view = views.Length > 0 ? views[0] : EditorWindow.GetWindow(gameViewType);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

                var behaviour = gameViewType.GetProperty("enterPlayModeBehavior", flags);
                if (behaviour != null && behaviour.CanWrite && behaviour.PropertyType.IsEnum)
                {
                    behaviour.SetValue(view, Enum.Parse(behaviour.PropertyType, "PlayMaximized"));
                    return true;
                }
                var maximize = gameViewType.GetProperty("maximizeOnPlay", flags);
                if (maximize != null && maximize.CanWrite)
                {
                    maximize.SetValue(view, true);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FullScreenSetup] Could not set the Game view to Play Maximized: {e.Message}");
            }
            return false;
        }
    }
}
