using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Chapter 7.2, first contact. When the drill seizes (<see cref="triggerFlag"/>, set by the confrontation
    /// script) it plays <see cref="EruptionTimeline"/>: grinding, Rime spreading from the bore, the lights failing
    /// to emergency red (<see cref="blackoutFlag"/> hides Hana, Ashford and Mara through story gates), the Listener
    /// let in (<see cref="contactFlag"/>) and alerted to Sam, then Mara on the radio while the player runs.
    /// The player never loses control. A loaded save after the eruption restores the dark, frosted chamber.
    /// </summary>
    public class EruptionSequence : MonoBehaviour
    {
        [SerializeField] string triggerFlag = "drill_seized";
        [SerializeField] string blackoutFlag = "eruption_blackout";
        [SerializeField] string contactFlag = "ch7_contact";
        [SerializeField] Light[] lights;
        [SerializeField] Color emergencyColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField, Range(0f, 1f)] float emergencyLevel = 0.18f;
        [SerializeField] GameObject[] rimeBurst;
        [SerializeField] Transform bore;
        [SerializeField] CreatureAI creature;
        [SerializeField] TextAsset radio;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip eruptionClip;
        [SerializeField] DialogueManager dialogue;

        StoryState story;
        float t = -1f;

        public bool Playing => t >= 0f;
        public bool Finished { get; private set; }

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            story = dialogue != null ? dialogue.Story : null;
            if (story == null) return;

            if (story.HasFlag(blackoutFlag))
            {
                // Loaded after the eruption: skip straight to how it left the room.
                SetRime(true);
                Blackout();
                Finished = true;
                return;
            }
            SetRime(false);
            story.Changed += OnStory;
        }

        void OnDestroy() { if (story != null) story.Changed -= OnStory; }

        void OnStory()
        {
            if (!Playing && !Finished && story.HasFlag(triggerFlag)) Begin();
        }

        /// <summary>Starts the eruption now. Public for tests and scripted sequences.</summary>
        public void Begin()
        {
            if (Playing || Finished) return;
            t = 0f;
            Step(-1f, 0f);
        }

        void Update()
        {
            if (!Playing) return;
            float previous = t;
            t += Time.deltaTime;
            Step(previous, t);
            if (t >= EruptionTimeline.Duration) { t = -1f; Finished = true; }
        }

        void Step(float previous, float now)
        {
            if (EruptionTimeline.Crossed(previous, now, EruptionTimeline.Grind))
            {
                if (audioSource != null && eruptionClip != null) audioSource.PlayOneShot(eruptionClip);
                // Felt through the whole base.
                if (bore != null) VibrationSystem.Emit(new VibrationEvent(bore.position, 30f, VibrationMedium.Ground, false, gameObject));
            }
            if (EruptionTimeline.Crossed(previous, now, EruptionTimeline.RimeBurst)) SetRime(true);
            if (EruptionTimeline.Crossed(previous, now, EruptionTimeline.Blackout))
            {
                Blackout();
                story.SetFlag(blackoutFlag);
            }
            if (EruptionTimeline.Crossed(previous, now, EruptionTimeline.Emerge)) story.SetFlag(contactFlag);
            if (EruptionTimeline.Crossed(previous, now, EruptionTimeline.Listen) && creature != null && creature.isActiveAndEnabled)
            {
                var player = FindAnyObjectByType<PlayerController>();
                // It knows roughly where the noise came from: enough to start investigating, not to hunt.
                if (player != null) creature.Alert(player.transform.position, 0.55f);
            }
            if (EruptionTimeline.Crossed(previous, now, EruptionTimeline.Radio) && radio != null && dialogue != null && !story.IsCompleted(radio.name))
            {
                var conv = CrewMember.Load(radio);
                dialogue.PlayOnTheMove(conv, () => story.MarkCompleted(conv.Id));
            }
        }

        void Blackout()
        {
            if (lights == null) return;
            foreach (var l in lights)
            {
                if (l == null) continue;
                l.color = emergencyColor;
                l.intensity *= emergencyLevel;
            }
        }

        void SetRime(bool on)
        {
            if (rimeBurst == null) return;
            foreach (var r in rimeBurst) if (r != null) r.SetActive(on);
        }
    }
}
