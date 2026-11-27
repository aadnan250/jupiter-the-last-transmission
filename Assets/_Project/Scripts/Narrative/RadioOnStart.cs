using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>Plays a reply-free radio call on the move a moment after the level starts (once per save).</summary>
    public class RadioOnStart : MonoBehaviour
    {
        [SerializeField] TextAsset conversation;
        [SerializeField] float delaySeconds = 2f;
        [SerializeField] DialogueManager dialogue;

        float t;
        bool done;

        void Start() { if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>(); }

        void Update()
        {
            if (done || dialogue == null || conversation == null) return;
            if ((t += Time.deltaTime) < delaySeconds || dialogue.IsActive) return;
            done = true;
            if (dialogue.Story.IsCompleted(conversation.name)) return;
            var conv = CrewMember.Load(conversation);
            dialogue.PlayOnTheMove(conv, () => dialogue.Story.MarkCompleted(conv.Id));
        }
    }
}
