using Jupiter.Audio;
using Jupiter.Narrative;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>Adds a level's <see cref="MusicDirector"/> with its cues: (story flag, clip path, volume), in order.</summary>
    public static class MusicKit
    {
        public static MusicDirector Add(DialogueManager dialogue, params (string flag, string clip, float volume)[] cues)
        {
            var music = new GameObject("Music").AddComponent<MusicDirector>();
            var so = new SerializedObject(music);
            so.FindProperty("dialogue").objectReferenceValue = dialogue;
            var list = so.FindProperty("cues");
            list.arraySize = cues.Length;
            for (int i = 0; i < cues.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("flag").stringValue = cues[i].flag ?? "";
                e.FindPropertyRelative("clip").objectReferenceValue =
                    string.IsNullOrEmpty(cues[i].clip) ? null : CombatAudioGenerator.Load(cues[i].clip);
                e.FindPropertyRelative("volume").floatValue = cues[i].volume;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return music;
        }
    }
}
