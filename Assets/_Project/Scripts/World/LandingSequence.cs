using Jupiter.Combat;
using Jupiter.Narrative;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Touchdown on Europa, inside the lander Wren. The crew talk on the radio while Sam can look around the cabin;
    /// then the cabin vents, the rear hatch swings down into a ramp onto the ice, and the landing party walks out
    /// together. Sets <see cref="rampFlag"/> when the ramp is down (the ambush waits for it).
    /// </summary>
    public class LandingSequence : MonoBehaviour
    {
        [SerializeField] TextAsset landingRadio;
        [SerializeField] Transform ramp;
        [SerializeField] Vector3 rampOpenEuler = new Vector3(-117f, 0f, 0f);
        [SerializeField] float rampSeconds = 3.5f;
        [SerializeField] float openAt = 11f;
        [SerializeField] string rampFlag = "ramp_open";
        [SerializeField] SquadMember[] squad;
        [SerializeField] Light[] cabinLights;
        [SerializeField] Color ventColor = new Color(1f, 0.3f, 0.15f);
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip hissClip;
        [SerializeField] DialogueManager dialogue;

        Quaternion rampClosed, rampOpen;
        float t, rampT = -1f;

        public bool RampOpen { get; private set; }
        public float OpenAt { get => openAt; set => openAt = value; }

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            if (ramp != null)
            {
                rampClosed = ramp.localRotation;
                rampOpen = Quaternion.Euler(rampOpenEuler);
            }
            if (landingRadio != null && dialogue != null)
            {
                var conv = CrewMember.Load(landingRadio);
                dialogue.PlayOnTheMove(conv, () => dialogue.Story.MarkCompleted(conv.Id));
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            if (rampT < 0f && t >= openAt) OpenRamp();
            if (rampT < 0f || RampOpen) return;

            rampT += Time.deltaTime / rampSeconds;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rampT));
            if (ramp != null) ramp.localRotation = Quaternion.Slerp(rampClosed, rampOpen, k);
            if (rampT >= 1f) Finish();
        }

        /// <summary>Starts venting the cabin and lowering the ramp now. Public for tests.</summary>
        public void OpenRamp()
        {
            if (rampT >= 0f) return;
            rampT = 0f;
            if (audioSource != null && hissClip != null) audioSource.PlayOneShot(hissClip);
            if (cabinLights != null)
                foreach (var l in cabinLights) if (l != null) l.color = ventColor;
        }

        void Finish()
        {
            RampOpen = true;
            if (ramp != null) ramp.localRotation = rampOpen;
            if (dialogue != null && !string.IsNullOrEmpty(rampFlag)) dialogue.Story.SetFlag(rampFlag);
            // They walk out together, a beat apart, in order: the commander first.
            if (squad != null)
                foreach (var m in squad) if (m != null) m.Release();
        }
    }
}
