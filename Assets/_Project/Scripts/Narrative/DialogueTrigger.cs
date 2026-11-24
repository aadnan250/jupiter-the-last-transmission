using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Trigger volume that plays a conversation once (per save) when the player walks in, e.g. a radio call
    /// when Sam reaches the observation deck. It can wait until other conversations are completed, and can set
    /// a flag on entry so objectives can track "reach this place".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] TextAsset conversation;
        [Tooltip("Only fires once all of these conversations are completed.")]
        [SerializeField] string[] requiresCompleted = new string[0];
        [SerializeField] string setsFlagOnEnter;
        [SerializeField] DialogueManager dialogue;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (dialogue == null || other.GetComponentInParent<PlayerController>() == null) return;

            var story = dialogue.Story;
            foreach (var required in requiresCompleted)
                if (!story.IsCompleted(required)) return;

            if (!string.IsNullOrEmpty(setsFlagOnEnter)) story.SetFlag(setsFlagOnEnter);
            if (conversation == null || dialogue.IsActive || story.IsCompleted(conversation.name)) return;

            var conv = CrewMember.Load(conversation);
            dialogue.Play(conv, () => story.MarkCompleted(conv.Id));
        }
    }
}
