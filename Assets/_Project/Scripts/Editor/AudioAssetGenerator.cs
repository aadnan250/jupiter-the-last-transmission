using System;
using System.IO;
using Jupiter.Audio;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Writes the project's procedural placeholder sounds to WAV assets and applies import settings
    /// chosen for low memory use (compressed loops, ADPCM one-shots, mono).
    /// Output is deterministic, so regenerating produces identical files and no git changes.
    /// </summary>
    public static class AudioAssetGenerator
    {
        public const string Folder = "Assets/_Project/Audio/Generated";

        public const string RingDrone = Folder + "/amb_ring_drone.wav";
        public const string Ventilation = Folder + "/amb_ventilation.wav";
        public const string ElectricalHum = Folder + "/amb_electrical_hum.wav";
        public const string MedicalMonitor = Folder + "/amb_medical_monitor.wav";
        public const string DoorOpen = Folder + "/sfx_door_open.wav";
        public const string DoorClose = Folder + "/sfx_door_close.wav";
        public const string SwitchClick = Folder + "/sfx_switch_click.wav";
        public const int FootstepVariants = 4;

        public static string Footstep(int i) => $"{Folder}/sfx_footstep_metal_{i + 1:00}.wav";

        [MenuItem("Jupiter/Generate Placeholder Audio")]
        public static void Generate()
        {
            GreyboxKit.EnsureFolder(Folder);

            Write(RingDrone, ProceduralAudio.RingDrone(), loop: true);
            Write(Ventilation, ProceduralAudio.Ventilation(), loop: true);
            Write(ElectricalHum, ProceduralAudio.ElectricalHum(), loop: true);
            Write(MedicalMonitor, ProceduralAudio.MedicalMonitor(), loop: true);
            Write(DoorOpen, ProceduralAudio.DoorOpen(), loop: false);
            Write(DoorClose, ProceduralAudio.DoorClose(), loop: false);
            Write(SwitchClick, ProceduralAudio.SwitchClick(), loop: false);
            for (int i = 0; i < FootstepVariants; i++)
                Write(Footstep(i), ProceduralAudio.FootstepMetal(i), loop: false);

            Debug.Log("[AudioAssetGenerator] Placeholder audio generated.");
        }

        public static AudioClip Load(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogError($"[AudioAssetGenerator] Missing clip {path}. Run Jupiter > Generate Placeholder Audio.");
            return clip;
        }

        public static AudioClip[] LoadFootsteps()
        {
            var clips = new AudioClip[FootstepVariants];
            for (int i = 0; i < FootstepVariants; i++) clips[i] = Load(Footstep(i));
            return clips;
        }

        static void Write(string assetPath, float[] samples, bool loop)
        {
            byte[] bytes = WavEncoder.Encode(samples, ProceduralAudio.SampleRate);
            string full = Path.GetFullPath(assetPath);

            bool unchanged = File.Exists(full) && FilesEqual(full, bytes);
            if (!unchanged) File.WriteAllBytes(full, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(assetPath) is AudioImporter importer)
            {
                importer.forceToMono = true;
                var s = importer.defaultSampleSettings;
                s.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                s.quality = 0.6f;
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
        }

        static bool FilesEqual(string path, byte[] bytes)
        {
            var existing = File.ReadAllBytes(path);
            return existing.Length == bytes.Length && existing.AsSpan().SequenceEqual(bytes);
        }
    }
}
