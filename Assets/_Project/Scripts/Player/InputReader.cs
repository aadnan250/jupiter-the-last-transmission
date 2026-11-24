using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.Player
{
    /// <summary>
    /// Single place that reads the "Player" action map from the project's Input Actions asset.
    /// Gameplay scripts ask this component for input instead of touching devices directly,
    /// which keeps rebinding and gamepad support in one asset.
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Settings/Input/JupiterControls.inputactions")]
        [SerializeField] InputActionAsset actions;

        InputAction move, look, lookStick, run, crouch, jump, interact;

        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Mouse delta in pixels for this frame (not scaled by time).</summary>
        public Vector2 LookPointer => look != null ? look.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Gamepad stick, -1..1 (must be scaled by time).</summary>
        public Vector2 LookStick => lookStick != null ? lookStick.ReadValue<Vector2>() : Vector2.zero;

        public bool RunHeld => run != null && run.IsPressed();
        public bool CrouchPressedThisFrame => crouch != null && crouch.WasPressedThisFrame();
        public bool JumpPressedThisFrame => jump != null && jump.WasPressedThisFrame();
        public bool InteractPressedThisFrame => interact != null && interact.WasPressedThisFrame();

        void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(InputReader)} on '{name}' has no Input Actions asset assigned.", this);
                enabled = false;
                return;
            }

            InputActionMap map = actions.FindActionMap("Player", throwIfNotFound: true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            lookStick = map.FindAction("LookStick", true);
            run = map.FindAction("Run", true);
            crouch = map.FindAction("Crouch", true);
            jump = map.FindAction("Jump", true);
            interact = map.FindAction("Interact", true);
        }

        void OnEnable()
        {
            if (actions != null) actions.FindActionMap("Player", true).Enable();
        }

        void OnDisable()
        {
            if (actions != null) actions.FindActionMap("Player", true).Disable();
        }
    }
}
