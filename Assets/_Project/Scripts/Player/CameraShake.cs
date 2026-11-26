using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.Player
{
    /// <summary>
    /// Shakes the first-person camera for big physical events (the moon quaking). Runs after
    /// <see cref="CameraController"/> each frame and adds a noise offset on top of its pose, so look and head bob
    /// are untouched. Also rumbles the gamepad. <see cref="Shake"/> layers: the strongest request wins.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] float maxOffset = 0.12f;
        [SerializeField] float maxAngle = 3.5f;
        [SerializeField] float frequency = 11f;

        float strength, until, fadeSeconds = 1f;

        public float Strength => Time.time < until ? strength : strength * Mathf.Clamp01(1f - (Time.time - until) / fadeSeconds);

        /// <summary>Shakes at <paramref name="amount"/> (0..1) for <paramref name="seconds"/>, then fades out.</summary>
        public void Shake(float amount, float seconds, float fade = 1.5f)
        {
            if (amount < Strength) return;
            strength = Mathf.Clamp01(amount);
            until = Time.time + seconds;
            fadeSeconds = Mathf.Max(0.01f, fade);
        }

        void LateUpdate()
        {
            float s = Strength;
            var pad = Gamepad.current;
            if (s <= 0.001f)
            {
                if (pad != null && strength > 0f) { pad.SetMotorSpeeds(0f, 0f); strength = 0f; }
                return;
            }
            float t = Time.time * frequency;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 1.3f) - 0.5f, Mathf.PerlinNoise(2.7f, t) - 0.5f, 0f) * (2f * maxOffset * s);
            Vector3 angles = new Vector3(Mathf.PerlinNoise(t, 7.1f) - 0.5f, Mathf.PerlinNoise(4.2f, t) - 0.5f, Mathf.PerlinNoise(t, 9.9f) - 0.5f) * (2f * maxAngle * s);
            transform.localPosition += offset;
            transform.localRotation *= Quaternion.Euler(angles);
            if (pad != null) pad.SetMotorSpeeds(0.6f * s, 0.9f * s);
        }

        void OnDisable() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
    }
}
