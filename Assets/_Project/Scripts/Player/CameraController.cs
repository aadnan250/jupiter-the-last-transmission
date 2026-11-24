using UnityEngine;

namespace Jupiter.Player
{
    /// <summary>
    /// First-person look and camera feel. Yaw rotates the player body; pitch rotates this pivot.
    /// Adds head bob tied to movement speed and lowers the eye height when crouching.
    /// Lives on the "CameraPivot" child of the player.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] PlayerController player;

        [Header("Look")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField] float stickSensitivity = 140f;
        [SerializeField] bool invertY;
        [SerializeField] float minPitch = -85f;
        [SerializeField] float maxPitch = 85f;

        [Header("Eye height")]
        [SerializeField] float standingEyeHeight = 1.62f;
        [SerializeField] float crouchingEyeHeight = 0.95f;

        [Header("Head bob")]
        [SerializeField] bool headBobEnabled = true;
        [SerializeField] float bobAmplitude = 0.035f;
        [Tooltip("Bob cycles per metre travelled.")]
        [SerializeField] float bobFrequency = 0.9f;

        float pitch;
        float bobPhase;
        float bobWeight;

        void Start()
        {
            if (input == null) input = GetComponentInParent<InputReader>();
            if (player == null) player = GetComponentInParent<PlayerController>();
            pitch = NormalizePitch(transform.localEulerAngles.x);
        }

        void LateUpdate()
        {
            if (input == null || player == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return; // no look while UI/cursor is free

            Vector2 delta = input.LookPointer * mouseSensitivity
                            + input.LookStick * (stickSensitivity * Time.deltaTime);

            player.transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch + (invertY ? delta.y : -delta.y), minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            ApplyEyeHeightAndBob();
        }

        void ApplyEyeHeightAndBob()
        {
            float eye = Mathf.Lerp(crouchingEyeHeight, standingEyeHeight, player.StandFraction);

            float speed = player.HorizontalVelocity.magnitude;
            bool bobbing = headBobEnabled && player.IsGrounded && speed > 0.1f;
            bobWeight = Mathf.MoveTowards(bobWeight, bobbing ? 1f : 0f, Time.deltaTime * 4f);
            if (bobbing) bobPhase += speed * bobFrequency * Time.deltaTime * Mathf.PI * 2f;

            float bobY = Mathf.Sin(bobPhase) * bobAmplitude * bobWeight;
            float bobX = Mathf.Cos(bobPhase * 0.5f) * bobAmplitude * 0.5f * bobWeight;
            transform.localPosition = new Vector3(bobX, eye + bobY, 0f);
        }

        static float NormalizePitch(float x) => x > 180f ? x - 360f : x;
    }
}
