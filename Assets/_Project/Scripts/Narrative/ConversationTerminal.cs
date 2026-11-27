using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// A machine that starts a conversation: a mail cache, a recorder, a console with someone on the other end.
    /// Plays once (per save), optionally only after some story flags are set. The lines are spoken by whoever the
    /// script names, from their own voice source if they are in the scene.
    /// </summary>
    public class ConversationTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] TextAsset conversation;
        [SerializeField] string prompt = "Use";
        [SerializeField] string[] requiresFlags = new string[0];
        [Tooltip("Optional sound played when the conversation starts (e.g. the Feature 7 pulse through the speakers).")]
        [SerializeField] AudioSource startSource;
        [SerializeField] AudioClip startClip;
        [SerializeField] DialogueManager dialogue;

        public string Prompt => prompt;
        public TextAsset Conversation => conversation;

        void Awake()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (dialogue == null || conversation == null || dialogue.IsActive) return false;
            var story = dialogue.Story;
            if (story.IsCompleted(conversation.name)) return false;
            foreach (var f in requiresFlags)
                if (!string.IsNullOrEmpty(f) && !story.HasFlag(f)) return false;
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            var conv = CrewMember.Load(conversation);
            if (startSource != null && startClip != null) startSource.PlayOneShot(startClip);
            dialogue.Play(conv, () => dialogue.Story.MarkCompleted(conv.Id));
        }
    }
}
