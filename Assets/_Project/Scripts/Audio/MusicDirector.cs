using System;
using Jupiter.Narrative;
using UnityEngine;

namespace Jupiter.Audio
{
    /// <summary>
    /// The level's score. A list of cues, each tied to a story flag (empty = from the start); the last cue whose
    /// flag is set is the one that plays, and changes crossfade. A cue with no clip fades to silence.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [Serializable]
        public struct Cue
        {
            public string flag;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] Cue[] cues = new Cue[0];
        [SerializeField] DialogueManager dialogue;
        [SerializeField] float fadeSeconds = 3f;

        AudioSource a, b;
        int current = -2;

        /// <summary>Index of the cue playing now (-1 = nothing).</summary>
        public int CurrentCue => current;
        public AudioClip CurrentClip => current >= 0 ? cues[current].clip : null;

        void Awake()
        {
            a = MakeSource();
            b = MakeSource();
        }

        AudioSource MakeSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        /// <summary>The cue that should play for the current story state.</summary>
        public int Pick()
        {
            var story = dialogue != null ? dialogue.Story : null;
            int pick = -1;
            for (int i = 0; i < cues.Length; i++)
                if (string.IsNullOrEmpty(cues[i].flag) || (story != null && story.HasFlag(cues[i].flag))) pick = i;
            return pick;
        }

        void Update()
        {
            int want = Pick();
            if (want != current)
            {
                current = want;
                (a, b) = (b, a);   // a is now the incoming source
                var clip = current >= 0 ? cues[current].clip : null;
                if (clip != null) { a.clip = clip; a.volume = 0f; a.Play(); }
                else a.Stop();
            }
            float target = current >= 0 && cues[current].clip != null ? cues[current].volume : 0f;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
            a.volume = Mathf.MoveTowards(a.volume, target, step * Mathf.Max(0.05f, target));
            b.volume = Mathf.MoveTowards(b.volume, 0f, step);
            if (b.isPlaying && b.volume <= 0f) b.Stop();
        }
    }
}
