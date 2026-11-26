using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.Player
{
    /// <summary>
    /// Single place that reads the project's Input Actions asset ("Player" and "Dialogue" maps).
    /// Gameplay scripts ask this component for input instead of touching devices directly,
    /// which keeps rebinding and gamepad support (including PS5 DualSense) in one asset.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Settings/Input/JupiterControls.inputactions")]
        [SerializeField] InputActionAsset actions;

        InputActionMap playerMap, dialogueMap;
        InputAction move, look, lookStick, run, crouch, jump, interact;
        InputAction fire, reload, heal;
        // Defined in code, not in the .inputactions asset, so the stealth tools need no asset edit. See CreateStealthActions.
        InputAction throwItem, fireFlare;
        InputAction advance;
        readonly InputAction[] choices = new InputAction[4];

        /// <summary>
        /// When true, movement, look, run, crouch and jump read as idle (e.g. while reading a document).
        /// Interact still works so the same key can close what it opened.
        /// </summary>
        public bool GameplayBlocked { get; set; }

        /// <summary>True while a conversation is running: the Player map is off and the Dialogue map is on.</summary>
        public bool DialogueMode { get; private set; }

        public Vector2 Move => Active && move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Mouse delta in pixels for this frame (not scaled by time).</summary>
        public Vector2 LookPointer => Active && look != null ? look.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Gamepad stick, -1..1 (must be scaled by time).</summary>
        public Vector2 LookStick => Active && lookStick != null ? lookStick.ReadValue<Vector2>() : Vector2.zero;

        public bool RunHeld => Active && run != null && run.IsPressed();
        public bool CrouchPressedThisFrame => Active && crouch != null && crouch.WasPressedThisFrame();
        public bool JumpPressedThisFrame => Active && jump != null && jump.WasPressedThisFrame();
        public bool InteractPressedThisFrame => !DialogueMode && interact != null && interact.WasPressedThisFrame();

        public bool FirePressedThisFrame => Active && fire != null && fire.WasPressedThisFrame();
        /// <summary>Fire held down: for the beam rifle, which fires continuously.</summary>
        public bool FireHeld => Active && fire != null && fire.IsPressed();
        public bool ReloadPressedThisFrame => Active && reload != null && reload.WasPressedThisFrame();
        public bool HealPressedThisFrame => Active && heal != null && heal.WasPressedThisFrame();

        /// <summary>Throw the held object (G, R1 on PlayStation, RB on Xbox).</summary>
        public bool ThrowPressedThisFrame => Active && throwItem != null && throwItem.WasPressedThisFrame();

        /// <summary>Fire the flare pistol (T, or D-pad down).</summary>
        public bool FlarePressedThisFrame => Active && fireFlare != null && fireFlare.WasPressedThisFrame();

        public bool AdvancePressedThisFrame => DialogueMode && advance != null && advance.WasPressedThisFrame();

        /// <summary>Choice buttons: keys 1-4, or Triangle / Circle / Cross / Square on a PlayStation controller.</summary>
        public bool ChoicePressedThisFrame(int index) =>
            DialogueMode && index >= 0 && index < choices.Length && choices[index] != null && choices[index].WasPressedThisFrame();

        bool Active => !GameplayBlocked && !DialogueMode;

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(InputReader)} on '{name}' has no Input Actions asset assigned.", this);
                enabled = false;
                return;
            }

            playerMap = actions.FindActionMap("Player", throwIfNotFound: true);
            move = playerMap.FindAction("Move", true);
            look = playerMap.FindAction("Look", true);
            lookStick = playerMap.FindAction("LookStick", true);
            run = playerMap.FindAction("Run", true);
            crouch = playerMap.FindAction("Crouch", true);
            jump = playerMap.FindAction("Jump", true);
            interact = playerMap.FindAction("Interact", true);
            fire = playerMap.FindAction("Fire", true);
            reload = playerMap.FindAction("Reload", true);
            heal = playerMap.FindAction("Heal", true);
            CreateStealthActions();

            dialogueMap = actions.FindActionMap("Dialogue", throwIfNotFound: true);
            advance = dialogueMap.FindAction("Advance", true);
            for (int i = 0; i < choices.Length; i++) choices[i] = dialogueMap.FindAction($"Choice{i + 1}", true);
        }

        /// <summary>Switches between walking around and talking. Disabling the Player map stops stray jumps and interacts.</summary>
        public void SetDialogueMode(bool on)
        {
            DialogueMode = on;
            if (playerMap == null) return;
            if (on) { playerMap.Disable(); SetStealthActions(false); dialogueMap.Enable(); }
            else { dialogueMap.Disable(); if (isActiveAndEnabled) { playerMap.Enable(); SetStealthActions(true); } }
        }

        void OnEnable()
        {
            if (playerMap == null) return;
            if (DialogueMode) dialogueMap.Enable(); else { playerMap.Enable(); SetStealthActions(true); }
        }

        void OnDisable()
        {
            playerMap?.Disable();
            dialogueMap?.Disable();
            SetStealthActions(false);
        }

        void OnDestroy()
        {
            throwItem?.Dispose();
            fireFlare?.Dispose();
        }

        /// <summary>
        /// Throw and Flare are built here rather than in JupiterControls.inputactions so they can ship without
        /// regenerating the asset. They follow the Player map's enabled state exactly. They can move into the
        /// asset later without changing any caller.
        /// </summary>
        void CreateStealthActions()
        {
            throwItem = new InputAction("Throw", InputActionType.Button);
            throwItem.AddBinding("<Keyboard>/g");
            throwItem.AddBinding("<Gamepad>/rightShoulder");

            fireFlare = new InputAction("Flare", InputActionType.Button);
            fireFlare.AddBinding("<Keyboard>/t");
            fireFlare.AddBinding("<Gamepad>/dpad/down");
        }

        void SetStealthActions(bool on)
        {
            if (on) { throwItem?.Enable(); fireFlare?.Enable(); }
            else { throwItem?.Disable(); fireFlare?.Disable(); }
        }
    }
}
