using Jupiter.Narrative;
using Jupiter.World;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for Chapter 7 logic: the eruption's beat order and how long on-the-move lines stay up.</summary>
    public class ChapterSevenLogicTests
    {
        [Test]
        public void Eruption_BeatsHappenInStoryOrder()
        {
            float[] beats = { EruptionTimeline.Grind, EruptionTimeline.RimeBurst, EruptionTimeline.Blackout,
                              EruptionTimeline.Emerge, EruptionTimeline.Listen, EruptionTimeline.Radio, EruptionTimeline.Duration };
            for (int i = 1; i < beats.Length; i++) Assert.Greater(beats[i], beats[i - 1], $"beat {i}");
            Assert.Greater(EruptionTimeline.Emerge, EruptionTimeline.Blackout, "it comes out in the dark");
        }

        [Test]
        public void Eruption_EachBeatIsCrossedExactlyOnce()
        {
            float[] beats = { EruptionTimeline.Grind, EruptionTimeline.RimeBurst, EruptionTimeline.Blackout, EruptionTimeline.Emerge };
            foreach (float beat in beats)
            {
                int crossings = 0;
                float previous = -1f;
                for (float t = 0f; t <= EruptionTimeline.Duration; t += 0.016f)
                {
                    if (EruptionTimeline.Crossed(previous, t, beat)) crossings++;
                    previous = t;
                }
                Assert.AreEqual(1, crossings, $"beat at {beat}");
            }
        }

        [Test]
        public void Eruption_ALongFrameStillCrossesEveryBeatInIt()
        {
            Assert.IsTrue(EruptionTimeline.Crossed(1f, 3f, EruptionTimeline.Blackout));
            Assert.IsTrue(EruptionTimeline.Crossed(1f, 3f, EruptionTimeline.Emerge));
            Assert.IsFalse(EruptionTimeline.Crossed(1f, 3f, EruptionTimeline.Radio));
        }

        [Test]
        public void LineTiming_FollowsTheVoiceClipPlusABreath()
        {
            Assert.AreEqual(3f + DialogueTiming.BreathSeconds, DialogueTiming.LineSeconds(3f, 10), 1e-5f);
        }

        [Test]
        public void LineTiming_WithoutAClipEstimatesFromTheText_NeverTooShort()
        {
            Assert.AreEqual(100 * DialogueTiming.SecondsPerCharacter + DialogueTiming.BreathSeconds, DialogueTiming.LineSeconds(0f, 100), 1e-4f);
            Assert.AreEqual(DialogueTiming.MinSeconds, DialogueTiming.LineSeconds(0f, 2), 1e-5f);
            Assert.AreEqual(DialogueTiming.MinSeconds, DialogueTiming.LineSeconds(0.2f, 0), 1e-5f);
        }
    }
}
