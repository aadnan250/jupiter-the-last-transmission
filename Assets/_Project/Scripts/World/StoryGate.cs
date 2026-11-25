using Jupiter.Narrative;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Shows or hides objects depending on a story flag, so one level can hold several chapters: for example the
    /// Listener only exists in Conamara Base once contact has happened. Reacts to story changes instead of polling.
    /// Lives on an object that is always active; its targets can start inactive.
    /// </summary>
    public class StoryGate : MonoBehaviour
    {
        [SerializeField] string flag;
        [Tooltip("True: targets are active only while the flag is set. False: only while it is not.")]
        [SerializeField] bool activeWhenSet = true;
        [SerializeField] GameObject[] targets;
        [SerializeField] DialogueManager dialogue;

        StoryState story;

        public string Flag => flag;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            story = Core.GameSession.Current != null ? Core.GameSession.Current.Story : dialogue != null ? dialogue.Story : null;
            if (story != null) story.Changed += Apply;
            Apply();
        }

        void OnDestroy()
        {
            if (story != null) story.Changed -= Apply;
        }

        public void Apply()
        {
            bool set = story != null && !string.IsNullOrEmpty(flag) && story.HasFlag(flag);
            bool active = set == activeWhenSet;
            if (targets == null) return;
            foreach (var t in targets)
                if (t != null && t.activeSelf != active) t.SetActive(active);
        }
    }
}
