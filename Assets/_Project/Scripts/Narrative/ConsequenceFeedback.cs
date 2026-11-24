using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Makes a significant choice felt: an eerie sting, a dark pulse across the screen, a slow rumble on the
    /// gamepad, and on PlayStation controllers the light bar bleeds from cold blue to red and back.
    /// Nothing tells the player what changed, only that something did.
    /// </summary>
    public class ConsequenceFeedback : MonoBehaviour
    {
        [SerializeField] DialogueManager dialogue;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip sting;
        [SerializeField] Image screenPulse;
        [SerializeField] float pulseSeconds = 3f;
        [SerializeField, Range(0f, 1f)] float pulseAlpha = 0.45f;

        static readonly Color LightBarIdle = new Color(0.1f, 0.25f, 0.45f);
        static readonly Color LightBarDread = new Color(0.75f, 0f, 0.02f);

        Coroutine running;

        public int TimesTriggered { get; private set; }

        void OnEnable()
        {
            if (dialogue != null) dialogue.SignificantChoiceMade += OnSignificant;
            SetLightBar(LightBarIdle);
        }

        void OnDisable()
        {
            if (dialogue != null) dialogue.SignificantChoiceMade -= OnSignificant;
            StopHaptics();
        }

        void OnSignificant(DialogueChoice choice)
        {
            TimesTriggered++;
            if (audioSource != null && sting != null) audioSource.PlayOneShot(sting);
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Pulse());
        }

        IEnumerator Pulse()
        {
            var gp = Gamepad.current;
            for (float t = 0f; t < pulseSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / pulseSeconds;
                // Fast rise, slow fade, with a heartbeat-like double throb in the rumble.
                float envelope = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
                float throb = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.6f));

                if (screenPulse != null)
                {
                    var c = screenPulse.color;
                    c.a = pulseAlpha * envelope;
                    screenPulse.color = c;
                }
                if (gp != null) gp.SetMotorSpeeds(0.35f * envelope * throb, 0.15f * envelope);
                SetLightBar(Color.Lerp(LightBarIdle, LightBarDread, envelope));
                yield return null;
            }
            if (screenPulse != null) { var c = screenPulse.color; c.a = 0f; screenPulse.color = c; }
            StopHaptics();
            running = null;
        }

        void StopHaptics()
        {
            var gp = Gamepad.current;
            if (gp != null) gp.ResetHaptics();
            SetLightBar(LightBarIdle);
        }

        static void SetLightBar(Color c)
        {
            if (Gamepad.current is DualShockGamepad ds) ds.SetLightBarColor(c);
        }
    }
}
