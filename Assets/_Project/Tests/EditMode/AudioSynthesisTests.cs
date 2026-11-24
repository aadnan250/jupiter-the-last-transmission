using System;
using System.Text;
using Jupiter.Audio;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Tests for the WAV encoder and the procedural placeholder sounds.</summary>
    public class AudioSynthesisTests
    {
        [Test]
        public void WavEncoder_WritesValidPcmHeader()
        {
            var samples = new float[100];
            byte[] wav = WavEncoder.Encode(samples, 44100);

            Assert.AreEqual(WavEncoder.HeaderSize + samples.Length * 2, wav.Length);
            Assert.AreEqual("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
            Assert.AreEqual("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
            Assert.AreEqual("fmt ", Encoding.ASCII.GetString(wav, 12, 4));
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 20), "PCM format");
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), "mono");
            Assert.AreEqual(44100, BitConverter.ToInt32(wav, 24), "sample rate");
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34), "bits per sample");
            Assert.AreEqual("data", Encoding.ASCII.GetString(wav, 36, 4));
            Assert.AreEqual(samples.Length * 2, BitConverter.ToInt32(wav, 40), "data size");
        }

        [Test]
        public void WavEncoder_ClampsOutOfRangeSamples()
        {
            byte[] wav = WavEncoder.Encode(new[] { 2f, -2f }, 44100);
            Assert.AreEqual(short.MaxValue, BitConverter.ToInt16(wav, 44));
            Assert.AreEqual(-short.MaxValue, BitConverter.ToInt16(wav, 46));
        }

        [Test]
        public void WavEncoder_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => WavEncoder.Encode(null, 44100));
            Assert.Throws<ArgumentOutOfRangeException>(() => WavEncoder.Encode(new float[1], 0));
        }

        [Test]
        public void ProceduralAudio_IsDeterministic()
        {
            CollectionAssert.AreEqual(ProceduralAudio.DoorOpen(), ProceduralAudio.DoorOpen());
            CollectionAssert.AreEqual(ProceduralAudio.RingDrone(), ProceduralAudio.RingDrone());
        }

        [Test]
        public void ProceduralAudio_StaysWithinRange()
        {
            foreach (var clip in new[] { ProceduralAudio.RingDrone(), ProceduralAudio.Ventilation(), ProceduralAudio.FootstepSoft(0), ProceduralAudio.ConsequenceSting(), ProceduralAudio.RadioSquelch(), ProceduralAudio.DoorClose() })
                foreach (float s in clip)
                    Assert.LessOrEqual(Math.Abs(s), 1f);
        }

        // A loop clicks if the jump from the last sample back to the first is much larger than a normal step.
        [TestCase("RingDrone")]
        [TestCase("Ventilation")]
        [TestCase("ElectricalHum")]
        [TestCase("MedicalMonitor")]
        public void LoopingTones_HaveNoSeamClick(string name)
        {
            float[] s = Generate(name);
            float seam = Math.Abs(s[0] - s[s.Length - 1]);

            float maxStep = 0f;
            for (int i = 1; i < s.Length; i++) maxStep = Math.Max(maxStep, Math.Abs(s[i] - s[i - 1]));

            Assert.LessOrEqual(seam, maxStep * 1.5f + 1e-4f, $"{name} loop seam jump {seam} vs max step {maxStep}");
        }

        [Test]
        public void Normalize_ScalesPeakToTarget()
        {
            float[] s = ProceduralAudio.Normalize(new[] { 0.1f, -0.5f, 0.25f }, 0.8f);
            Assert.AreEqual(-0.8f, s[1], 1e-6f);
            Assert.AreEqual(0.16f, s[0], 1e-6f);
        }

        static float[] Generate(string name)
        {
            switch (name)
            {
                case "RingDrone": return ProceduralAudio.RingDrone();
                case "Ventilation": return ProceduralAudio.Ventilation();
                case "ElectricalHum": return ProceduralAudio.ElectricalHum();
                default: return ProceduralAudio.MedicalMonitor();
            }
        }
    }
}
