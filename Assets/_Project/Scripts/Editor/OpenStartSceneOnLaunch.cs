using UnityEditor;
using UnityEditor.SceneManagement;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Opens the first level of the game when the editor starts, so Play runs the story from the beginning:
    /// always on an empty "Untitled" scene (right after cloning), and once per <see cref="ContentVersion"/> even if
    /// the editor remembered an older scene. Unity reopens the last scene you had open, so without the version
    /// check a project that was last on another level would keep opening it after the start of the game changes.
    /// It never replaces a scene with unsaved changes.
    /// </summary>
    [InitializeOnLoad]
    static class OpenStartSceneOnLaunch
    {
        /// <summary>The start of the game: the Meridian (Chapter 2), which leads on to Conamara Base (Chapters 6-7).</summary>
        public const string StartScene = MeridianGreyboxBuilder.ScenePath;
        /// <summary>Bump when a new level or chapter becomes the one to show on opening the project.</summary>
        const string ContentVersion = "europa-landing";

        const string SessionKey = "Jupiter.OpenStartSceneOnLaunch.Done";
        const string VersionKey = "Jupiter.OpenStartSceneOnLaunch.Version";

        static OpenStartSceneOnLaunch()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += TryOpen;
        }

        static void TryOpen()
        {
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScene) == null) return;

            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) return;
            bool untitled = string.IsNullOrEmpty(active.path);
            bool newContent = EditorPrefs.GetString(VersionKey, string.Empty) != ContentVersion;
            if (!untitled && !newContent) return;

            EditorPrefs.SetString(VersionKey, ContentVersion);
            if (active.path != StartScene) EditorSceneManager.OpenScene(StartScene);
        }
    }
}
