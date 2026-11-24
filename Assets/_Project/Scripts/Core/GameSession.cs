using System;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jupiter.Core
{
    /// <summary>
    /// Persistent game-wide state that survives scene loads: the <see cref="StoryState"/> and save/load.
    /// The one deliberate global in the project, exposed through <see cref="Current"/> (a minimal service locator).
    /// F5 quick-saves, F9 quick-loads; conversations autosave when they end.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Current { get; private set; }

        public StoryState Story { get; } = new StoryState();

        /// <summary>Conversations autosave when they end. Tests switch this off so they never touch real saves.</summary>
        public bool AutosaveEnabled { get; set; } = true;

        /// <summary>Raised after a save is written (UI can show a "Saved" toast).</summary>
        public event Action Saved;

        SaveData pendingLoad;

        void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }
            Current = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Current != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Current = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f5Key.wasPressedThisFrame) SaveGame();
            if (kb.f9Key.wasPressedThisFrame) LoadGame();
        }

        /// <summary>Starts a fresh story (used by "New Game" and by tests).</summary>
        public void NewGame() => Story.Clear();

        public void SaveGame()
        {
            var player = FindAnyObjectByType<PlayerController>();
            var data = SaveData.Capture(Story, SceneManager.GetActiveScene().name,
                player != null ? player.transform.position : Vector3.zero,
                player != null ? player.transform.eulerAngles.y : 0f);
            SaveSystem.Write(data, SaveSystem.DefaultPath);
            Debug.Log($"[GameSession] Saved to {SaveSystem.DefaultPath}");
            Saved?.Invoke();
        }

        public bool LoadGame()
        {
            var data = SaveSystem.Read(SaveSystem.DefaultPath);
            if (data == null) { Debug.Log("[GameSession] No save found."); return false; }

            data.RestoreInto(Story);
            pendingLoad = data;
            SceneManager.LoadScene(data.scene);
            return true;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (pendingLoad == null || scene.name != pendingLoad.scene) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.SetPositionAndRotation(pendingLoad.Position, Quaternion.Euler(0f, pendingLoad.yaw, 0f));
                if (cc != null) cc.enabled = true;
            }
            pendingLoad = null;
        }
    }
}
