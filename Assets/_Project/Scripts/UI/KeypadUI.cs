using Jupiter.Player;
using Jupiter.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>
    /// The code-entry panel for a <see cref="KeypadTerminal"/>. Keyboard: type digits, Backspace to delete,
    /// Esc (or Interact) to close. Controller: D-pad up/down picks a digit, Cross / A enters it, Circle / B deletes
    /// (or closes when empty). Like the reading panel it reads the devices directly, because it only ever runs
    /// while normal gameplay input is blocked.
    /// </summary>
    public class KeypadUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text entryText;
        [SerializeField] Text statusText;
        [SerializeField] Text hintText;
        [SerializeField] InputReader input;

        static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        KeypadTerminal current;
        string status = string.Empty;
        float statusTimer;

        public bool IsOpen => current != null;

        void Awake() { if (panel != null) panel.SetActive(false); }

        void OnEnable()
        {
            KeypadTerminal.Opened += Show;
            KeypadTerminal.Closed += Hide;
        }

        void OnDisable()
        {
            KeypadTerminal.Opened -= Show;
            KeypadTerminal.Closed -= Hide;
            if (current != null) current.Close();
        }

        void Show(KeypadTerminal terminal)
        {
            current = terminal;
            status = string.Empty;
            if (panel != null) panel.SetActive(true);
            if (input != null) input.GameplayBlocked = true;
            Refresh();
        }

        void Hide(KeypadTerminal terminal)
        {
            if (terminal != current) return;
            current = null;
            if (panel != null) panel.SetActive(false);
            if (input != null) input.GameplayBlocked = false;
        }

        void Update()
        {
            if (statusTimer > 0f && (statusTimer -= Time.deltaTime) <= 0f) { status = string.Empty; Refresh(); }
            if (current == null) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { current.Close(); return; }
                if (kb.backspaceKey.wasPressedThisFrame) current.Model.Backspace();
                for (int d = 0; d < 10; d++)
                    if (kb[DigitKeys[d]].wasPressedThisFrame || kb[NumpadKeys[d]].wasPressedThisFrame) Enter(d);
            }

            var gp = Gamepad.current;
            if (gp != null && current != null)
            {
                if (gp.dpad.up.wasPressedThisFrame) current.Model.Cycle(1);
                if (gp.dpad.down.wasPressedThisFrame) current.Model.Cycle(-1);
                if (gp.buttonSouth.wasPressedThisFrame) Enter(current.Model.Selected);
                if (gp.buttonEast.wasPressedThisFrame && !current.Model.Backspace()) { current.Close(); return; }
            }
            Refresh();
        }

        void Enter(int digit)
        {
            if (current == null) return;
            var terminal = current;
            var result = terminal.Press(digit);
            if (result == KeypadResult.Rejected) { status = "<color=#FF6A5A>ACCESS DENIED</color>"; statusTimer = 1.5f; }
        }

        void Refresh()
        {
            if (current == null) { if (statusText != null) statusText.text = status; return; }
            var m = current.Model;
            if (entryText != null)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < m.Code.Length; i++)
                {
                    if (i < m.Entry.Length) sb.Append(m.Entry[i]);
                    else if (i == m.Entry.Length && InputDeviceWatcher.UsingGamepad) sb.Append("<color=#9FD8E8>").Append(m.Selected).Append("</color>");
                    else sb.Append('_');
                    if (i < m.Code.Length - 1) sb.Append("  ");
                }
                entryText.text = sb.ToString();
            }
            if (statusText != null) statusText.text = status;
            if (hintText != null)
                hintText.text = !InputDeviceWatcher.UsingGamepad ? "Type the code   Backspace delete   Esc close"
                    : InputDeviceWatcher.UsingPlayStation ? "D-pad choose digit   X enter   ● delete / close"
                    : "D-pad choose digit   A enter   B delete / close";
        }
    }
}
