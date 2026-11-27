using Jupiter.Core;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the options: defaults, and every value held to its range.</summary>
    public class GameSettingsTests
    {
        [Test]
        public void Defaults_AreNeutral()
        {
            var s = new GameSettings();
            Assert.AreEqual(1f, s.Sensitivity);
            Assert.AreEqual(1f, s.SubtitleScale);
            Assert.AreEqual(1f, s.Volume);
            Assert.IsFalse(s.InvertY);
            Assert.IsTrue(s.HeadBob);
        }

        [Test]
        public void Values_AreClampedToTheirRanges()
        {
            var s = new GameSettings { Sensitivity = 10f, SubtitleScale = 0.1f, Volume = -2f };
            Assert.AreEqual(GameSettings.MaxSensitivity, s.Sensitivity);
            Assert.AreEqual(GameSettings.MinSubtitleScale, s.SubtitleScale);
            Assert.AreEqual(0f, s.Volume);
            s.Sensitivity = 0f;
            s.SubtitleScale = 9f;
            s.Volume = 3f;
            Assert.AreEqual(GameSettings.MinSensitivity, s.Sensitivity);
            Assert.AreEqual(GameSettings.MaxSubtitleScale, s.SubtitleScale);
            Assert.AreEqual(1f, s.Volume);
        }
    }
}
