using Jupiter.Audio;
using Jupiter.Core;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the score (lengths, levels, seamless loops) and the graphics presets.</summary>
    public class MusicSynthTests
    {
        static void AssertLoop(float[] s, float seconds)
        {
            Assert.AreEqual((int)(seconds * ProceduralAudio.SampleRate), s.Length);
            float peak = 0f;
            foreach (var v in s) { Assert.IsFalse(float.IsNaN(v)); peak = System.Math.Max(peak, System.Math.Abs(v)); }
            Assert.Greater(peak, 0.1f, "audible");
            Assert.LessOrEqual(peak, 1f, "no clipping");
            Assert.Less(System.Math.Abs(s[s.Length - 1] - s[0]), 0.1f, "the end runs into the start without a click");
        }

        [Test] public void Theme_Loops() => AssertLoop(MusicSynth.Theme(), 32f);
        [Test] public void Ship_Loops() => AssertLoop(MusicSynth.Ship(), 30f);
        [Test] public void Dread_Loops() => AssertLoop(MusicSynth.Dread(), 30f);
        [Test] public void Combat_Loops() => AssertLoop(MusicSynth.Combat(), 19.2f);
        [Test] public void Ending_Loops() => AssertLoop(MusicSynth.Ending(), 40f);

        [Test]
        public void GraphicsPresets_ScaleDownForIntegratedGraphics()
        {
            Assert.Less(GraphicsQuality.RenderScaleFor(GraphicsQuality.Low), GraphicsQuality.RenderScaleFor(GraphicsQuality.Medium));
            Assert.AreEqual(1f, GraphicsQuality.RenderScaleFor(GraphicsQuality.High));
            Assert.AreEqual(GraphicsQuality.High, GraphicsQuality.Clamp(9));
            var s = new GameSettings();
            Assert.AreEqual(GraphicsQuality.Medium, s.Quality, "Medium by default");
            s.Quality = -3;
            Assert.AreEqual(GraphicsQuality.Low, s.Quality);
        }
    }
}
