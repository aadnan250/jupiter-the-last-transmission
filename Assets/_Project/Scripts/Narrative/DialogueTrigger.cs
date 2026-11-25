using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Trigger volume that plays a conversation once (per save) when the player walks in, e.g. a radio call
    /// when Sam reaches the observation deck. It can wait until other conversations are completed, and can set
    /// a flag on entry so objectives can track "reach this place". Radio chatter during a chase can play
    /// on the move, without taking control away.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] TextAsset conversation;
        [Tooltip("Only fires once all of these conversations are completed.")]
        [SerializeField] string[] requiresCompleted = new string[0];
        [Tooltip("Only fires once this story flag is set (empty = no requirement).")]
        [SerializeField] string requiresFlag;
        [SerializeField] string setsFlagOnEnter;
        [Tooltip("Play without taking control away (reply-free radio chatter).")]
        [SerializeField] bool onTheMove;
        [SerializeField] DialogueManager dialogue;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) TryFire();
        }

        /// <summary>Does what walking in does, if the requirements are met. Public for tests and scripted sequences.</summary>
        public bool TryFire()
        {
            if (dialogue == null) return false;

            var story = dialogue.Story;
            foreach (var required in requiresCompleted)
                if (!story.IsCompleted(required)) return false;
            if (!string.IsNullOrEmpty(requiresFlag) && !story.HasFlag(requiresFlag)) return false;

            if (!string.IsNullOrEmpty(setsFlagOnEnter)) story.SetFlag(setsFlagOnEnter);
            if (conversation == null || dialogue.IsActive || story.IsCompleted(conversation.name)) return false;

            var conv = CrewMember.Load(conversation);
            System.Action done = () => story.MarkCompleted(conv.Id);
            return onTheMove ? dialogue.PlayOnTheMove(conv, done) : dialogue.Play(conv, done);
        }
    }
}
