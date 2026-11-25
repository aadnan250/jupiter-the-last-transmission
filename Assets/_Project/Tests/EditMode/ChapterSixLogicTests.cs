using System;
using Jupiter.World;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the Chapter 6 logic: the code keypad and the palm-print scare timing.</summary>
    public class ChapterSixLogicTests
    {
        // ---------- keypad ----------

        [Test]
        public void Keypad_ChecksAutomaticallyWhenTheEntryIsFull()
        {
            var k = new KeypadModel("4071");
            Assert.AreEqual(KeypadResult.Pending, k.Press(4));
            Assert.AreEqual(KeypadResult.Pending, k.Press(0));
            Assert.AreEqual(KeypadResult.Pending, k.Press(7));
            Assert.AreEqual("407", k.Entry);
            Assert.AreEqual(KeypadResult.Accepted, k.Press(1));
            Assert.IsTrue(k.Unlocked);
        }

        [Test]
        public void Keypad_WrongCodeClearsAndCountsTheAttempt()
        {
            var k = new KeypadModel("4071");
            foreach (int d in new[] { 1, 7, 0 }) k.Press(d);
            Assert.AreEqual(KeypadResult.Rejected, k.Press(4));
            Assert.AreEqual(1, k.FailedAttempts);
            Assert.AreEqual(string.Empty, k.Entry);
            Assert.IsFalse(k.Unlocked);
        }

        [Test]
        public void Keypad_BackspaceAndInvalidDigits()
        {
            var k = new KeypadModel("12");
            Assert.IsFalse(k.Backspace(), "nothing to delete");
            k.Press(9);
            Assert.IsTrue(k.Backspace());
            Assert.AreEqual(KeypadResult.Pending, k.Press(10), "not a digit");
            Assert.AreEqual(string.Empty, k.Entry);
        }

        [Test]
        public void Keypad_ControllerCyclesAndWraps()
        {
            var k = new KeypadModel("90");
            k.Cycle(-1);
            Assert.AreEqual(9, k.Selected, "0 wraps down to 9");
            Assert.AreEqual(KeypadResult.Pending, k.ConfirmSelected());
            k.Cycle(1);
            Assert.AreEqual(0, k.Selected, "9 wraps up to 0");
            Assert.AreEqual(KeypadResult.Accepted, k.ConfirmSelected());
        }

        [Test]
        public void Keypad_RejectsCodesThatAreNotDigits()
        {
            Assert.Throws<ArgumentException>(() => new KeypadModel(""));
            Assert.Throws<ArgumentException>(() => new KeypadModel("40A1"));
        }

        // ---------- scare timing ----------

        [Test]
        public void Scare_LightsAreNormalOutsideTheScare()
        {
            Assert.AreEqual(1f, ScareTimeline.LightLevel(-0.1f));
            Assert.AreEqual(1f, ScareTimeline.LightLevel(ScareTimeline.Duration));
            Assert.AreEqual(1f, ScareTimeline.LightLevel(ScareTimeline.Duration + 5f));
        }

        [Test]
        public void Scare_SilhouetteOnlyShowsWhileTheLightsAreOut()
        {
            Assert.IsFalse(ScareTimeline.SilhouetteVisible(0f));
            Assert.IsTrue(ScareTimeline.SilhouetteVisible(ScareTimeline.SilhouetteStart));
            Assert.IsFalse(ScareTimeline.SilhouetteVisible(ScareTimeline.SilhouetteEnd));
            for (float t = ScareTimeline.SilhouetteStart; t < ScareTimeline.SilhouetteEnd; t += 0.01f)
                Assert.AreEqual(0f, ScareTimeline.LightLevel(t), $"dark at {t}");
            Assert.Less(ScareTimeline.SilhouetteEnd - ScareTimeline.SilhouetteStart, 0.25f, "a fraction of a second");
        }

        [Test]
        public void Scare_LightsComeBackUpByTheEnd()
        {
            float previous = 0f;
            for (float t = 1.6f; t < ScareTimeline.Duration; t += 0.1f)
            {
                float level = ScareTimeline.LightLevel(t);
                Assert.GreaterOrEqual(level, previous);
                Assert.LessOrEqual(level, 1f);
                previous = level;
            }
        }
    }
}
