using System;
using System.IO;
using Jupiter.Audio;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>Writes the combat placeholder sounds (deterministic, original) to WAV assets.</summary>
    public static class CombatAudioGenerator
    {
        public const string Folder = "Assets/_Project/Audio/Generated/Combat";
        public const string BoltShot = Folder + "/sfx_bolt_shot.wav";
        public const string Reload = Folder + "/sfx_bolt_reload.wav";

        public static void Generate()
        {
            GreyboxKit.EnsureFolder(Folder);
            Write(BoltShot, CombatSynth.BoltShot());
            Write(Reload, CombatSynth.Reload());
        }

        static void Write(string path, float[] samples)
        {
            byte[] bytes = WavEncoder.Encode(samples, ProceduralAudio.SampleRate);
            string full = Path.GetFullPath(path);
            if (!File.Exists(full) || !File.ReadAllBytes(full).AsSpan().SequenceEqual(bytes)) File.WriteAllBytes(full, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        public static AudioClip Load(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
