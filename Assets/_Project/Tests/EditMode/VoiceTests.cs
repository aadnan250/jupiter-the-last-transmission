using Jupiter.Audio;
using Jupiter.Narrative;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Tests for turning subtitles into speakable text and for WAV round-tripping used by the voice pipeline.</summary>
    public class VoiceTests
    {
        [TestCase("(beat) What? Asked you?", "What? Asked you?")]
        [TestCase("(laughing) She's always listening.", "She's always listening.")]
        [TestCase("Okay. (beat) Okay. Just walk.", "Okay. ... Okay. Just walk.")]
        [TestCase("Bin C-4 has contact cleaner.", "Bin C 4 has contact cleaner.")]
        [TestCase("Hana says bossa nova is \"elevator music with a tan.\"", "Hana says bossa nova is elevator music with a tan.")]
        public void Clean_MakesSubtitlesSpeakable(string subtitle, string expected)
        {
            Assert.AreEqual(expected, VoiceText.Clean(subtitle));
        }

        [TestCase("(long beat)")]
        [TestCase("(Say nothing.)")]
        [TestCase("   ")]
        public void Clean_StageDirectionOnlyIsSilent(string subtitle)
        {
            Assert.AreEqual(string.Empty, VoiceText.Clean(subtitle));
        }

        [Test]
        public void Wav_EncodeThenDecodeRoundTrips()
        {
            var samples = new[] { 0f, 0.5f, -0.5f, 0.25f };
            var decoded = WavEncoder.DecodePcm16(WavEncoder.Encode(samples, 22050), out int rate);
            Assert.AreEqual(22050, rate);
            Assert.AreEqual(samples.Length, decoded.Length);
            for (int i = 0; i < samples.Length; i++) Assert.AreEqual(samples[i], decoded[i], 1e-4f);
        }

        [Test]
        public void VoiceKeys_AreStableAndZeroPadded()
        {
            Assert.AreEqual("ch02_workshop_mara_L007", VoiceLibrary.Key("ch02_workshop_mara", 7));
            Assert.AreEqual("Voice/ch02_workshop_mara/ch02_workshop_mara_L042", VoiceLibrary.ResourcePath("ch02_workshop_mara", 42));
        }
    }
}
