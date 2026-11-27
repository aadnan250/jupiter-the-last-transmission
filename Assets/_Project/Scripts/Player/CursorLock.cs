using Jupiter.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.Player
{
    /// <summary>
    /// Locks the cursor for first-person play; clicking the game view locks it again after focus is lost.
    /// When the rig has a <see cref="PauseMenu"/>, Escape belongs to the menu, and nothing relocks while paused.
    /// </summary>
    public class CursorLock : MonoBehaviour
    {
        PauseMenu menu;

        void Start()
        {
            menu = GetComponent<PauseMenu>();
            Lock(true);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (menu != null && menu.IsPaused) return;

            if (menu == null && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Lock(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) Lock(true);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Lock(false);
        }

        static void Lock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
