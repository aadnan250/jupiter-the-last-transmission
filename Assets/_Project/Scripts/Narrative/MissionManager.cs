using System;
using System.Collections.Generic;
using Jupiter.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Tracks the current objective for a level. Objectives are data (set in the Inspector or by the level builder):
    /// each one is complete when its required conversations and flags are done. Reacts to story changes
    /// instead of polling, and shows progress such as "Check in with the crew (1/3)".
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        [Serializable]
        public class Objective
        {
            public string text;
            [Tooltip("Conversation ids (file names) that must be completed.")]
            public string[] requiredConversations = Array.Empty<string>();
            [Tooltip("Story flags that must be set.")]
            public string[] requiredFlags = Array.Empty<string>();
        }

        [SerializeField] List<Objective> objectives = new List<Objective>();
        [SerializeField] string completeText = "Chapter complete";
        [SerializeField] Text objectiveText;
        [SerializeField] DialogueManager dialogue;

        StoryState story;

        public int CurrentIndex { get; private set; } = -1;
        public string CurrentText { get; private set; } = string.Empty;

        void Start()
        {
            story = GameSession.Current != null ? GameSession.Current.Story : dialogue != null ? dialogue.Story : new StoryState();
            story.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (story != null) story.Changed -= Refresh;
        }

        public void Refresh()
        {
            CurrentIndex = objectives.Count;
            CurrentText = completeText;
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                int done = 0, total = o.requiredConversations.Length + o.requiredFlags.Length;
                foreach (var c in o.requiredConversations) if (story.IsCompleted(c)) done++;
                foreach (var f in o.requiredFlags) if (story.HasFlag(f)) done++;
                if (done < total)
                {
                    CurrentIndex = i;
                    CurrentText = total > 1 ? $"{o.text} ({done}/{total})" : o.text;
                    break;
                }
            }
            if (objectiveText != null) objectiveText.text = $"<size=12><color=#9AA4AE>OBJECTIVE</color></size>\n{CurrentText}";
        }
    }
}
