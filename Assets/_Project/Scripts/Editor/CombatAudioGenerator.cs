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
        public const string Clatter = Folder + "/sfx_throw_clatter.wav";
        public const string FlareShot = Folder + "/sfx_flare_shot.wav";
        public const string DecoyThump = Folder + "/sfx_decoy_thump.wav";
        public const string LockerDoor = Folder + "/sfx_locker_door.wav";
        public const string Eruption = Folder + "/sfx_bore_eruption.wav";
        public const string DoorPound = Folder + "/sfx_door_pound.wav";
        public const string BeamHum = Folder + "/sfx_beam_hum.wav";
        public const string Shriek = Folder + "/sfx_stalker_shriek.wav";
        public const string Quake = Folder + "/sfx_quake.wav";
        public const string IceBurst = Folder + "/sfx_ice_burst.wav";
        public const string SuitBreath = Folder + "/sfx_suit_breath.wav";
        public const string PistolShot = Folder + "/sfx_pistol_shot.wav";

        public static void Generate()
        {
            GreyboxKit.EnsureFolder(Folder);
            Write(BoltShot, CombatSynth.BoltShot());
            Write(Reload, CombatSynth.Reload());
            Write(Clatter, CombatSynth.Clatter());
            Write(FlareShot, CombatSynth.FlareShot());
            Write(DecoyThump, CombatSynth.DecoyThump());
            Write(LockerDoor, CombatSynth.LockerDoor());
            Write(Eruption, CombatSynth.Eruption());
            Write(DoorPound, CombatSynth.DoorPound());
            Write(BeamHum, CombatSynth.BeamHum());
            Write(Shriek, CombatSynth.Shriek());
            Write(Quake, CombatSynth.Quake());
            Write(IceBurst, CombatSynth.IceBurst());
            Write(SuitBreath, CombatSynth.SuitBreath());
            Write(PistolShot, CombatSynth.PistolShot());
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
