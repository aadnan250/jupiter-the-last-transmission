using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// The distant silhouette: when Sam crosses this trigger (after the ambush), something enormous rises beyond
    /// the ridge against Jupiter, stands for a few seconds, and sinks back into the ice. The ground hums, the crew
    /// see it too, and it never comes closer. Fires once.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SilhouetteReveal : MonoBehaviour
    {
        [SerializeField] Transform figure;
        [SerializeField] float riseHeight = 260f;
        [SerializeField] float riseSeconds = 6f;
        [SerializeField] float holdSeconds = 5f;
        [SerializeField] float sinkSeconds = 7f;
        [Tooltip("Story flag needed before it can happen (the ambush cleared).")]
        [SerializeField] string requiresFlag;
        [SerializeField] DialogueManager dialogue;
        [SerializeField] TextAsset radio;
        [SerializeField] AudioSource rumble;

        Vector3 hiddenPos;
        float t = -1f;
        bool radioPending;

        public bool Started => t >= 0f;
        public bool Finished { get; private set; }
        /// <summary>0 hidden, 1 fully risen.</summary>
        public float Raised { get; private set; }
        float Total => riseSeconds + holdSeconds + sinkSeconds;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (figure != null)
            {
                hiddenPos = figure.position;
                figure.gameObject.SetActive(false);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) Begin();
        }

        public void Begin()
        {
            if (Started || Finished) return;
            var story = dialogue != null ? dialogue.Story : null;
            if (!string.IsNullOrEmpty(requiresFlag) && (story == null || !story.HasFlag(requiresFlag))) return;
            t = 0f;
            if (figure != null) figure.gameObject.SetActive(true);
            if (rumble != null) rumble.Play();
            var shake = FindAnyObjectByType<CameraShake>();
            if (shake != null) shake.Shake(0.25f, 3f);
            radioPending = dialogue != null && radio != null;
        }

        void Update()
        {
            if (!Started || Finished) return;
            if (radioPending && !dialogue.IsActive)
            {
                radioPending = false;
                var conv = CrewMember.Load(radio);
                dialogue.PlayOnTheMove(conv, () => dialogue.Story.MarkCompleted(conv.Id));
            }
            t += Time.deltaTime;
            if (t < riseSeconds) Raised = Mathf.SmoothStep(0f, 1f, t / riseSeconds);
            else if (t < riseSeconds + holdSeconds) Raised = 1f;
            else Raised = 1f - Mathf.SmoothStep(0f, 1f, (t - riseSeconds - holdSeconds) / sinkSeconds);
            if (figure != null) figure.position = hiddenPos + Vector3.up * (riseHeight * Raised);
            if (t >= Total)
            {
                Finished = true;
                Raised = 0f;
                if (figure != null) figure.gameObject.SetActive(false);
            }
        }
    }
}
