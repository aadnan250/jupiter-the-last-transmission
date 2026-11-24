using Jupiter.World;
using UnityEngine;

namespace Jupiter.Player
{
    /// <summary>
    /// First-person movement: walk, run, crouch (toggle) and jump, driven by the active
    /// <see cref="EnvironmentProfile"/> so gravity and speeds change between Earth, the ship and Europa.
    /// Rotation (yaw) is applied by <see cref="CameraController"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] EnvironmentProfile defaultEnvironment;

        [Header("Crouch")]
        [SerializeField] float standingHeight = 1.8f;
        [SerializeField] float crouchingHeight = 1.1f;
        [SerializeField] float crouchTransitionSpeed = 8f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        bool crouched;

        public EnvironmentProfile Environment { get; private set; }
        public MoveState State { get; private set; }
        public bool IsGrounded => controller.isGrounded;
        public Vector3 HorizontalVelocity => horizontalVelocity;

        /// <summary>Current capsule height, 0..1 between crouching and standing. Used by the camera.</summary>
        public float StandFraction => Mathf.InverseLerp(crouchingHeight, standingHeight, controller.height);

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<InputReader>();
            SetEnvironment(defaultEnvironment);
        }

        /// <summary>Switches the physics/movement profile (called by <see cref="EnvironmentZone"/>).</summary>
        public void SetEnvironment(EnvironmentProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning($"{name}: null EnvironmentProfile ignored.", this);
                return;
            }
            Environment = profile;
        }

        public EnvironmentProfile DefaultEnvironment => defaultEnvironment;

        void Update()
        {
            if (Environment == null || input == null) return;
            float dt = Time.deltaTime;

            if (input.CrouchPressedThisFrame)
            {
                // Only stand up if there is headroom.
                if (!crouched || CanStand()) crouched = !crouched;
            }
            UpdateCapsuleHeight(dt);

            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
            State = MovementModel.ResolveState(move.sqrMagnitude > 0.01f, input.RunHeld, crouched);
            float speed = MovementModel.SpeedFor(State, Environment.walkSpeed, Environment.runSpeed, Environment.crouchSpeed);

            Vector3 wishDir = transform.right * move.x + transform.forward * move.y;
            horizontalVelocity = MovementModel.Accelerate(
                horizontalVelocity, wishDir * speed, Environment.groundAcceleration, dt,
                controller.isGrounded, Environment.airControl);

            if (controller.isGrounded && !crouched && input.JumpPressedThisFrame)
                verticalVelocity = MovementModel.JumpVelocity(Environment.jumpHeight, Environment.gravity);
            else
                verticalVelocity = MovementModel.ApplyGravity(
                    verticalVelocity, Environment.gravity, dt, controller.isGrounded, Environment.terminalFallSpeed);

            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            // Bonk: stop rising when the head hits a ceiling.
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        }

        void UpdateCapsuleHeight(float dt)
        {
            float target = crouched ? crouchingHeight : standingHeight;
            float h = Mathf.MoveTowards(controller.height, target, crouchTransitionSpeed * dt);
            controller.height = h;
            controller.center = new Vector3(0f, h * 0.5f, 0f);
        }

        bool CanStand()
        {
            // Probe the space the head would occupy when standing. The sphere sits above the
            // crouched capsule, so it never overlaps the player's own collider.
            float radius = controller.radius * 0.95f;
            Vector3 head = transform.position + Vector3.up * (standingHeight - radius);
            return !Physics.CheckSphere(head, radius, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
