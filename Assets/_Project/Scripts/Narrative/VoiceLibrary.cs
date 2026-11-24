using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Finds the recorded or generated voice clip for a dialogue line.
    /// Clips live in Resources/Voice/&lt;conversation&gt;/&lt;conversation&gt;_L&lt;line&gt; where &lt;line&gt; is the line number
    /// in the script file, so any voice source (synthesised now, actors later) drops in without code changes.
    /// A player reply uses the line number of the reply in the script.
    /// </summary>
    public static class VoiceLibrary
    {
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static string Key(string conversationId, int sourceLine) => $"{conversationId}_L{sourceLine:000}";

        public static string ResourcePath(string conversationId, int sourceLine) =>
            $"Voice/{conversationId}/{Key(conversationId, sourceLine)}";

        /// <summary>Returns the clip, or null if this line has no voice yet (subtitles still show).</summary>
        public static AudioClip Find(string conversationId, int sourceLine)
        {
            if (string.IsNullOrEmpty(conversationId) || sourceLine <= 0) return null;
            string path = ResourcePath(conversationId, sourceLine);
            if (!Cache.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>(path);
                Cache[path] = clip;
            }
            return clip;
        }
    }
}
