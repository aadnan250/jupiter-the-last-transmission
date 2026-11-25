using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Chapter 7.3: Ashford holds the drill-chamber pressure door. Once the lights have failed
    /// (<see cref="requiresFlag"/>), the first time Sam gets through, the door slams shut behind him and Ashford's
    /// radio plays on the move. The creature pounds on it; after <see cref="HoldSeconds"/> it gives way and the
    /// Listener comes through, hunting. The door panel carries a carving NavMeshObstacle, so while it is shut the
    /// creature genuinely cannot path out.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressureDoorHold : MonoBehaviour
    {
        [SerializeField] SlidingDoor door;
        [SerializeField] string requiresFlag = "eruption_blackout";
        [SerializeField] string setsFlagOnClose = "left_drill_chamber";
        [SerializeField] string setsFlagOnBreak = "drill_door_broken";
        [SerializeField] float holdSeconds = 30f;
        [SerializeField] float poundInterval = 2.6f;
        [SerializeField] TextAsset radio;
        [SerializeField] CreatureAI creature;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip poundClip;
        [SerializeField] AudioClip breakClip;
        [SerializeField] DialogueManager dialogue;

        float held = -1f, nextPound;

        public float HoldSeconds { get => holdSeconds; set => holdSeconds = value; }
        public bool Holding => held >= 0f;
        public bool Broken { get; private set; }
        public SlidingDoor Door => door;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            var story = dialogue != null ? dialogue.Story : null;
            if (story != null && story.HasFlag(setsFlagOnBreak)) Broken = true; // loaded after it gave way: leave it open
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) TryClose();
        }

        /// <summary>Slams the door if the eruption has happened and it has not already been held. Public for tests.</summary>
        public bool TryClose()
        {
            if (Holding || Broken || door == null || dialogue == null) return false;
            var story = dialogue.Story;
            if (!string.IsNullOrEmpty(requiresFlag) && !story.HasFlag(requiresFlag)) return false;
            if (!string.IsNullOrEmpty(setsFlagOnClose) && story.HasFlag(setsFlagOnClose)) return false;

            door.SetOpen(false);
            door.SetLocked(true);
            held = 0f;
            nextPound = poundInterval;
            if (!string.IsNullOrEmpty(setsFlagOnClose)) story.SetFlag(setsFlagOnClose);
            if (radio != null && !dialogue.IsActive && !story.IsCompleted(radio.name))
            {
                var conv = CrewMember.Load(radio);
                dialogue.PlayOnTheMove(conv, () => story.MarkCompleted(conv.Id));
            }
            return true;
        }

        void Update()
        {
            if (!Holding) return;
            held += Time.deltaTime;
            if (held >= nextPound && held < holdSeconds)
            {
                nextPound += poundInterval;
                if (audioSource != null && poundClip != null) audioSource.PlayOneShot(poundClip);
            }
            if (held >= holdSeconds) Break();
        }

        void Break()
        {
            held = -1f;
            Broken = true;
            door.SetLocked(false);
            door.SetOpen(true);
            if (audioSource != null && breakClip != null) audioSource.PlayOneShot(breakClip);
            if (dialogue != null && !string.IsNullOrEmpty(setsFlagOnBreak)) dialogue.Story.SetFlag(setsFlagOnBreak);
            VibrationSystem.Emit(new VibrationEvent(door.transform.position, 25f, VibrationMedium.Ground, false, gameObject));

            // Through the door, it goes after the last thing that ran from it.
            var player = FindAnyObjectByType<PlayerController>();
            if (creature != null && creature.isActiveAndEnabled && player != null)
                creature.Alert(player.transform.position, 0.9f);
        }
    }
}
