using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.Player
{
    /// <summary>
    /// Locks the cursor for first-person play. Escape releases it; clicking the game view locks it again.
    /// Temporary until the pause menu exists (Phase 3, UIManager).
    /// </summary>
    public class CursorLock : MonoBehaviour
    {
        void Start() => Lock(true);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Lock(false);
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
