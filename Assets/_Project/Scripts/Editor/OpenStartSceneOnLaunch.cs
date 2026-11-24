using UnityEditor;
using UnityEditor.SceneManagement;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// When the editor opens on an empty "Untitled" scene (e.g. right after cloning the repo),
    /// open the grey-box test room so Play goes straight into the game. Runs once per editor session
    /// and never replaces a scene you already have open.
    /// </summary>
    [InitializeOnLoad]
    static class OpenStartSceneOnLaunch
    {
        const string StartScene = "Assets/_Project/Scenes/Sandbox/GreyboxTestRoom.unity";
        const string SessionKey = "Jupiter.OpenStartSceneOnLaunch.Done";

        static OpenStartSceneOnLaunch()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += TryOpen;
        }

        static void TryOpen()
        {
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var active = EditorSceneManager.GetActiveScene();
            bool untitledAndEmpty = string.IsNullOrEmpty(active.path) && !active.isDirty;
            if (!untitledAndEmpty) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScene) != null)
                EditorSceneManager.OpenScene(StartScene);
        }
    }
}
