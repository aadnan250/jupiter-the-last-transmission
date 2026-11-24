using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Jupiter.Player
{
    /// <summary>
    /// Tracks whether the player is using keyboard/mouse or a gamepad, so on-screen prompts show the right buttons.
    /// Switches on real input only (button presses, stick or mouse movement), not on idle sensor noise,
    /// because a DualSense reports motion data continuously.
    /// </summary>
    public class InputDeviceWatcher : MonoBehaviour
    {
        public static bool UsingGamepad { get; private set; }
        public static bool UsingPlayStation { get; private set; }
        public static event Action Changed;

        void Update()
        {
            var gp = Gamepad.current;
            if (gp != null && GamepadActuated(gp))
            {
                Set(true, gp is DualShockGamepad);
                return;
            }

            var kb = Keyboard.current;
            var mouse = Mouse.current;
            bool kbm = (kb != null && kb.anyKey.wasPressedThisFrame)
                       || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 9f));
            if (kbm) Set(false, false);
        }

        static bool GamepadActuated(Gamepad gp) =>
            gp.buttonSouth.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame ||
            gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame ||
            gp.startButton.wasPressedThisFrame || gp.selectButton.wasPressedThisFrame ||
            gp.leftShoulder.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame ||
            gp.leftStickButton.wasPressedThisFrame || gp.rightStickButton.wasPressedThisFrame ||
            gp.dpad.ReadValue().sqrMagnitude > 0.25f ||
            gp.leftStick.ReadValue().sqrMagnitude > 0.16f || gp.rightStick.ReadValue().sqrMagnitude > 0.16f ||
            gp.leftTrigger.ReadValue() > 0.4f || gp.rightTrigger.ReadValue() > 0.4f;

        static void Set(bool gamepad, bool playStation)
        {
            if (UsingGamepad == gamepad && UsingPlayStation == playStation) return;
            UsingGamepad = gamepad;
            UsingPlayStation = playStation;
            Changed?.Invoke();
        }
    }

    /// <summary>Button labels for prompts, matching the active device. PlayStation symbols use their standard colours.</summary>
    public static class ButtonGlyphs
    {
        // Glyphs chosen from characters available in the default UI font.
        const string Triangle = "<color=#3EC9A7>▲</color>";
        const string Circle = "<color=#E5536B>●</color>";
        const string Cross = "<color=#7CA8F2>X</color>";
        const string Square = "<color=#E68AC2>■</color>";

        public static string Interact => InputDeviceWatcher.UsingGamepad ? (InputDeviceWatcher.UsingPlayStation ? Square : "X") : "E";

        public static string Advance => InputDeviceWatcher.UsingGamepad ? (InputDeviceWatcher.UsingPlayStation ? Cross : "A") : "E";

        /// <summary>Choice 0-3: keys 1-4, or Triangle, Circle, Cross, Square (Y, B, A, X on Xbox).</summary>
        public static string Choice(int index)
        {
            if (!InputDeviceWatcher.UsingGamepad) return (index + 1).ToString();
            if (InputDeviceWatcher.UsingPlayStation)
                return index == 0 ? Triangle : index == 1 ? Circle : index == 2 ? Cross : Square;
            return index == 0 ? "Y" : index == 1 ? "B" : index == 2 ? "A" : "X";
        }

        public static string ControlsHelp
        {
            get
            {
                if (!InputDeviceWatcher.UsingGamepad)
                    return "WASD move   Shift run   Ctrl/C crouch   Space jump   E interact   F5 save   F9 load   F3 performance   Esc free cursor";
                if (InputDeviceWatcher.UsingPlayStation)
                    return $"L-stick move   R-stick look   L3 run   {Circle} crouch   {Cross} jump   {Square} interact";
                return "L-stick move   R-stick look   L3 run   B crouch   A jump   X interact";
            }
        }
    }
}
