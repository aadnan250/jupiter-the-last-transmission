using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Physical and movement settings for a location (Earth, the Meridian's spin ring, Europa).
    /// Systems read the active profile instead of hard-coding gravity or speeds, so the same
    /// player controller works everywhere in the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Jupiter/Environment Profile", fileName = "EnvironmentProfile")]
    public class EnvironmentProfile : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Earth";

        [Header("Physics")]
        [Tooltip("Downward acceleration in m/s². Earth 9.81, Meridian ring 5.89 (0.6 g), Europa 1.315 (0.134 g).")]
        [Min(0.01f)] public float gravity = 9.81f;

        [Tooltip("Maximum falling speed in m/s. Keeps low-gravity falls readable and prevents tunnelling.")]
        [Min(0.1f)] public float terminalFallSpeed = 30f;

        [Tooltip("True where there is no air (Europa's surface). Airborne sound does not propagate.")]
        public bool isVacuum;

        [Header("Movement")]
        [Min(0f)] public float walkSpeed = 2.2f;
        [Min(0f)] public float runSpeed = 4.0f;
        [Min(0f)] public float crouchSpeed = 1.1f;

        [Tooltip("Target jump apex height in metres. Velocity is derived from gravity.")]
        [Min(0f)] public float jumpHeight = 0.45f;

        [Tooltip("How quickly horizontal speed changes on the ground (m/s²).")]
        [Min(0.1f)] public float groundAcceleration = 18f;

        [Tooltip("How much control the player has while airborne, 0-1. Low on Europa.")]
        [Range(0f, 1f)] public float airControl = 0.35f;

        void OnValidate()
        {
            runSpeed = Mathf.Max(runSpeed, walkSpeed);
            crouchSpeed = Mathf.Min(crouchSpeed, walkSpeed);
        }
    }
}
