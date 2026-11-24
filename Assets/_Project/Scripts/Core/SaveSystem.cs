using System;
using System.Collections.Generic;
using System.IO;
using Jupiter.Narrative;
using UnityEngine;

namespace Jupiter.Core
{
    [Serializable]
    public class TrustEntry
    {
        public string id;
        public int value;
    }

    /// <summary>
    /// Everything written to disk. Plain fields so <see cref="JsonUtility"/> can serialise it.
    /// <see cref="version"/> lets future builds migrate or reject old saves cleanly.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAtUtc;
        public string scene;
        public float[] position = new float[3];
        public float yaw;
        public List<TrustEntry> trust = new List<TrustEntry>();
        public List<string> flags = new List<string>();
        public List<string> completedConversations = new List<string>();

        public Vector3 Position => new Vector3(position[0], position[1], position[2]);

        public static SaveData Capture(StoryState story, string scene, Vector3 playerPosition, float playerYaw)
        {
            var d = new SaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                scene = scene,
                position = new[] { playerPosition.x, playerPosition.y, playerPosition.z },
                yaw = playerYaw
            };
            foreach (var kv in story.TrustValues) d.trust.Add(new TrustEntry { id = kv.Key, value = kv.Value });
            d.flags.AddRange(story.Flags);
            d.completedConversations.AddRange(story.Completed);
            d.trust.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            d.flags.Sort(string.CompareOrdinal);
            d.completedConversations.Sort(string.CompareOrdinal);
            return d;
        }

        public void RestoreInto(StoryState story)
        {
            var pairs = new List<KeyValuePair<string, int>>();
            foreach (var t in trust) pairs.Add(new KeyValuePair<string, int>(t.id, t.value));
            story.Restore(pairs, flags, completedConversations);
        }
    }

    /// <summary>Reads and writes <see cref="SaveData"/> as JSON. Writes atomically so a crash cannot corrupt a save.</summary>
    public static class SaveSystem
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save_slot0.json");

        public static void Write(SaveData data, string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, prettyPrint: true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>Returns null if there is no save, it is unreadable, or it comes from a newer version.</summary>
        public static SaveData Read(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version > SaveData.CurrentVersion || string.IsNullOrEmpty(data.scene)) return null;
                if (data.position == null || data.position.Length != 3) data.position = new float[3];
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not read {path}: {e.Message}");
                return null;
            }
        }
    }
}
