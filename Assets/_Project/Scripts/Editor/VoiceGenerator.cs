using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Jupiter.Audio;
using Jupiter.Narrative;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Voices every line in every dialogue script with Piper, a local neural text-to-speech engine,
    /// using the CC BY 4.0 LibriTTS-R multi-speaker model. Each character has a fixed voice (speaker id)
    /// and pace. Output goes to Resources/Voice so <see cref="VoiceLibrary"/> finds it; recorded actor lines can
    /// replace any file later with the same name.
    ///
    /// Piper and the model are build tools kept outside the repo (default ../_tools, override with the
    /// JUPITER_TOOLS environment variable). Menu: Jupiter > Generate Voices.
    /// </summary>
    public static class VoiceGenerator
    {
        const string DialogueRoot = "Assets/_Project/Narrative/Dialogue";
        const string OutputRoot = "Assets/_Project/Resources/Voice";
        const string ModelFile = "voices/en_US-libritts_r-medium.onnx";

        /// <summary>Casting: speaker id in the model, and pace (length scale; above 1 is slower).
        /// Chosen by measured median pitch so voices are clearly distinct (see docs/VOICES.md).</summary>
        public static readonly Dictionary<string, (int speaker, float pace)> Cast = new Dictionary<string, (int, float)>
        {
            { "ASHFORD", (50, 1.08f) },   // ~93 Hz, deepest: calm, paternal
            { "SAM", (34, 1.0f) },        // ~122 Hz: soft, measured
            { "LUCA", (71, 0.92f) },      // ~140 Hz: bright, quick
            { "TOMAS", (113, 1.1f) },     // ~100 Hz: tired, kind
            { "MARA", (2, 1.0f) },        // ~176 Hz, low female: clipped, flat
            { "HANA", (112, 1.02f) },     // ~199 Hz: gentle
            { "GRACE", (45, 1.0f) },      // ~204 Hz: professional CAPCOM
            { "ADA", (77, 1.0f) },        // ~200 Hz
            { "ZARA", (54, 0.95f) },      // ~259 Hz, highest
        };

        struct Job
        {
            public string Conversation, Key, Text, Speaker;
            public int Line;
        }

        public static string ToolsDir
        {
            get
            {
                string env = Environment.GetEnvironmentVariable("JUPITER_TOOLS");
                return !string.IsNullOrEmpty(env) ? env : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "_tools"));
            }
        }

        [MenuItem("Jupiter/Generate Voices")]
        public static void GenerateAll() => Generate(force: false);

        [MenuItem("Jupiter/Regenerate All Voices")]
        public static void RegenerateAll() => Generate(force: true);

        static void Generate(bool force)
        {
            string piper = Path.Combine(ToolsDir, "piper", "piper.exe");
            string model = Path.Combine(ToolsDir, ModelFile);
            if (!File.Exists(piper) || !File.Exists(model))
            {
                Debug.LogError($"[VoiceGenerator] Piper or the voice model is missing under {ToolsDir}. See docs/VOICES.md.");
                return;
            }

            var jobs = CollectJobs();
            var todo = new List<Job>();
            foreach (var j in jobs)
                if (force || !File.Exists(OutputPath(j))) todo.Add(j);
            Debug.Log($"[VoiceGenerator] {jobs.Count} voiced lines, {todo.Count} to generate.");
            if (todo.Count == 0) return;

            string temp = Path.Combine(Path.GetTempPath(), "jupiter_voice_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                // One Piper run per character, because pace is a per-run setting.
                var bySpeaker = new Dictionary<string, List<Job>>();
                foreach (var j in todo)
                {
                    if (!bySpeaker.TryGetValue(j.Speaker, out var list)) bySpeaker[j.Speaker] = list = new List<Job>();
                    list.Add(j);
                }

                foreach (var kv in bySpeaker)
                {
                    var (speaker, pace) = Cast[kv.Key];
                    var input = new StringBuilder();
                    foreach (var j in kv.Value)
                        input.Append("{\"text\": ").Append(JsonString(j.Text))
                             .Append(", \"speaker_id\": ").Append(speaker)
                             .Append(", \"output_file\": ").Append(JsonString(Path.Combine(temp, j.Key + ".wav")))
                             .Append("}\n");
                    RunPiper(piper, model, pace, input.ToString());
                }

                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var j in todo)
                    {
                        string raw = Path.Combine(temp, j.Key + ".wav");
                        if (!File.Exists(raw)) { Debug.LogError($"[VoiceGenerator] Piper produced no audio for {j.Key}: \"{j.Text}\""); continue; }
                        var samples = WavEncoder.DecodePcm16(File.ReadAllBytes(raw), out int rate);
                        samples = ProceduralAudio.Normalize(TrimSilence(samples, rate), 0.89f);
                        string dest = OutputPath(j);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.WriteAllBytes(dest, WavEncoder.Encode(samples, rate));
                    }
                }
                finally { AssetDatabase.StopAssetEditing(); }

                AssetDatabase.Refresh();
                foreach (var j in todo) ConfigureImport(OutputPath(j));
                Debug.Log($"[VoiceGenerator] Generated {todo.Count} voice lines.");
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { /* temp cleanup is best effort */ }
            }
        }

        /// <summary>Every spoken line, including the player's replies (spoken by Sam unless written as an action).</summary>
        static List<Job> CollectJobs()
        {
            var jobs = new List<Job>();
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { DialogueRoot }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var conv = DialogueParser.Parse(asset.text, asset.name);
                foreach (var node in conv.Nodes.Values)
                {
                    foreach (var item in node.Body)
                        if (item.Kind == DialogueItem.ItemKind.Line)
                            Add(jobs, conv.Id, item.Line.SourceLine, item.Line.Speaker, item.Line.Text);
                    foreach (var choice in node.Choices)
                        if (!choice.IsAction)
                            Add(jobs, conv.Id, choice.SourceLine, DialogueRunner.PlayerSpeaker, choice.Text);
                }
            }
            return jobs;
        }

        static void Add(List<Job> jobs, string conv, int line, string speaker, string subtitle)
        {
            string text = VoiceText.Clean(subtitle);
            if (text.Length == 0) return;
            if (!Cast.ContainsKey(speaker)) { Debug.LogWarning($"[VoiceGenerator] No voice cast for {speaker}"); return; }
            jobs.Add(new Job { Conversation = conv, Line = line, Speaker = speaker, Text = text, Key = VoiceLibrary.Key(conv, line) });
        }

        static string OutputPath(Job j) => $"{OutputRoot}/{j.Conversation}/{j.Key}.wav";

        static void RunPiper(string piper, string model, float pace, string jsonLines)
        {
            var psi = new ProcessStartInfo
            {
                FileName = piper,
                Arguments = $"--model \"{model}\" --json-input --length_scale {pace.ToString(System.Globalization.CultureInfo.InvariantCulture)} --sentence_silence 0.25",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(piper)
            };
            using (var p = Process.Start(psi))
            {
                // Read stderr asynchronously so a full pipe can never deadlock the editor.
                var err = new StringBuilder();
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) err.AppendLine(e.Data); };
                p.BeginErrorReadLine();
                p.StandardInput.Write(jsonLines);
                p.StandardInput.Close();
                p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0) Debug.LogError($"[VoiceGenerator] Piper exited with {p.ExitCode}:\n{err}");
            }
        }

        /// <summary>Removes leading/trailing silence, keeping 40 ms of air at each end.</summary>
        static float[] TrimSilence(float[] s, int rate)
        {
            const float threshold = 0.01f;
            int pad = rate / 25, start = 0, end = s.Length - 1;
            while (start < s.Length && Math.Abs(s[start]) < threshold) start++;
            while (end > start && Math.Abs(s[end]) < threshold) end--;
            start = Math.Max(0, start - pad);
            end = Math.Min(s.Length - 1, end + pad);
            var o = new float[end - start + 1];
            Array.Copy(s, start, o, 0, o.Length);
            return o;
        }

        static void ConfigureImport(string assetPath)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is AudioImporter importer)) return;
            importer.forceToMono = true;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.5f;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        static string JsonString(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    default:
                        if (c < 32 || c > 126) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
