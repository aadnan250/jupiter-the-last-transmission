using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// A one-off scripted scare in a trigger volume (Chapter 6.5): once its conversation is done, walking in plays
    /// <see cref="ScareTimeline"/> on the nearby lights, flashes a silhouette, plays a sting, reveals what was left
    /// behind (frosted palm prints) and sets a flag. It never hurts the player: it promises, it does not deliver.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScareEvent : MonoBehaviour
    {
        [Tooltip("Only armed once this conversation is completed.")]
        [SerializeField] string requiresCompleted;
        [SerializeField] string setsFlag;
        [SerializeField] Light[] lights;
        [SerializeField] GameObject[] reveal;
        [SerializeField] GameObject silhouette;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip sting;
        [SerializeField] DialogueManager dialogue;

        float[] baseIntensity;
        float t = -1f;

        public bool Fired { get; private set; }
        public bool Playing => t >= 0f;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            baseIntensity = new float[lights != null ? lights.Length : 0];
            for (int i = 0; i < baseIntensity.Length; i++) baseIntensity[i] = lights[i] != null ? lights[i].intensity : 0f;
            if (silhouette != null) silhouette.SetActive(false);

            bool already = dialogue != null && !string.IsNullOrEmpty(setsFlag) && dialogue.Story.HasFlag(setsFlag);
            Fired = already;
            SetReveal(already);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) Fire();
        }

        /// <summary>Plays the scare if it is armed and has not played. Public for tests and scripted sequences.</summary>
        public bool Fire()
        {
            if (Fired || dialogue == null) return false;
            if (!string.IsNullOrEmpty(requiresCompleted) && !dialogue.Story.IsCompleted(requiresCompleted)) return false;
            Fired = true;
            t = 0f;
            SetReveal(true);
            if (!string.IsNullOrEmpty(setsFlag)) dialogue.Story.SetFlag(setsFlag);
            if (audioSource != null && sting != null) audioSource.PlayOneShot(sting);
            return true;
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.deltaTime;
            float level = ScareTimeline.LightLevel(t);
            for (int i = 0; i < baseIntensity.Length; i++)
                if (lights[i] != null) lights[i].intensity = baseIntensity[i] * level;
            if (silhouette != null) silhouette.SetActive(ScareTimeline.SilhouetteVisible(t));
            if (t >= ScareTimeline.Duration) t = -1f;
        }

        void SetReveal(bool on)
        {
            if (reveal == null) return;
            foreach (var r in reveal) if (r != null) r.SetActive(on);
        }
    }
}
