using Jupiter.Core;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the chapter transition: fade to black, then the time card, then the next level.</summary>
    public class TransitionTimelineTests
    {
        [Test]
        public void Black_FadesInOverTheFadeThenStays()
        {
            Assert.AreEqual(0f, TransitionTimeline.BlackAlpha(0f, 1.5f));
            Assert.AreEqual(0.5f, TransitionTimeline.BlackAlpha(0.75f, 1.5f), 1e-5f);
            Assert.AreEqual(1f, TransitionTimeline.BlackAlpha(5f, 1.5f));
            Assert.AreEqual(1f, TransitionTimeline.BlackAlpha(0.1f, 0f), "no fade: straight to black");
        }

        [Test]
        public void Card_OnlyAppearsOnceTheScreenIsBlack()
        {
            Assert.AreEqual(0f, TransitionTimeline.CardAlpha(1f, 1.5f));
            Assert.AreEqual(0f, TransitionTimeline.CardAlpha(1.5f, 1.5f));
            Assert.AreEqual(1f, TransitionTimeline.CardAlpha(2.5f, 1.5f));
        }

        [Test]
        public void NextLevel_LoadsAfterTheCardHasHeld()
        {
            Assert.IsFalse(TransitionTimeline.ShouldLoad(4.9f, 1.5f, 3.5f));
            Assert.IsTrue(TransitionTimeline.ShouldLoad(5f, 1.5f, 3.5f));
        }
    }
}
