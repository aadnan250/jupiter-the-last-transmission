using System;
using System.Collections.Generic;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Everything the game remembers about the player's choices: trust with each crew member,
    /// story flags, and which conversations have been completed. Pure C# so it can be unit tested
    /// and serialised by the save system. Later chapters read this to change dialogue, help and endings.
    /// </summary>
    public class StoryState
    {
        public const int MinTrust = -5;
        public const int MaxTrust = 5;

        readonly Dictionary<string, int> trust = new Dictionary<string, int>();
        readonly HashSet<string> flags = new HashSet<string>();
        readonly HashSet<string> completed = new HashSet<string>();

        /// <summary>Raised after any change, so UI such as the objective tracker can refresh.</summary>
        public event Action Changed;

        public int GetTrust(string characterId) =>
            trust.TryGetValue(Key(characterId), out int v) ? v : 0;

        public void AddTrust(string characterId, int delta)
        {
            string k = Key(characterId);
            int value = Math.Max(MinTrust, Math.Min(MaxTrust, GetTrust(k) + delta));
            trust[k] = value;
            Changed?.Invoke();
        }

        public bool HasFlag(string flag) => flags.Contains(Key(flag));

        public void SetFlag(string flag)
        {
            if (flags.Add(Key(flag))) Changed?.Invoke();
        }

        public void ClearFlag(string flag)
        {
            if (flags.Remove(Key(flag))) Changed?.Invoke();
        }

        public bool IsCompleted(string conversationId) => completed.Contains(Key(conversationId));

        public void MarkCompleted(string conversationId)
        {
            if (completed.Add(Key(conversationId))) Changed?.Invoke();
        }

        public IEnumerable<string> Flags => flags;
        public IEnumerable<string> Completed => completed;
        public IEnumerable<KeyValuePair<string, int>> TrustValues => trust;

        public void Clear()
        {
            trust.Clear();
            flags.Clear();
            completed.Clear();
            Changed?.Invoke();
        }

        /// <summary>Copies all state from a save. Raises <see cref="Changed"/> once.</summary>
        public void Restore(IEnumerable<KeyValuePair<string, int>> trustValues, IEnumerable<string> flagValues, IEnumerable<string> completedValues)
        {
            trust.Clear();
            flags.Clear();
            completed.Clear();
            if (trustValues != null) foreach (var kv in trustValues) trust[Key(kv.Key)] = Math.Max(MinTrust, Math.Min(MaxTrust, kv.Value));
            if (flagValues != null) foreach (var f in flagValues) flags.Add(Key(f));
            if (completedValues != null) foreach (var c in completedValues) completed.Add(Key(c));
            Changed?.Invoke();
        }

        /// <summary>Ids are case-insensitive so script authors can write "Mara" or "MARA".</summary>
        static string Key(string id) => (id ?? string.Empty).Trim().ToLowerInvariant();
    }
}
