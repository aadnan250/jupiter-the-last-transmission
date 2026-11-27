using Jupiter.Creatures;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the Needles: chase, one bite per lunge, recoil, and one hit to kill.</summary>
    public class NeedleBrainTests
    {
        [Test]
        public void Dormant_UntilWoken()
        {
            var n = new NeedleBrain();
            Assert.AreEqual(0f, n.Tick(1f, 0.5f));
            Assert.AreEqual(NeedleState.Dormant, n.State);
            n.Wake();
            Assert.AreEqual(NeedleState.Chase, n.State);
        }

        [Test]
        public void Lunge_BitesOnce_ThenRecoils_ThenComesAgain()
        {
            var n = new NeedleBrain(lungeRange: 1.6f, lungeSeconds: 0.4f, recoilSeconds: 1f, biteDamage: 4f);
            n.Wake();
            n.Tick(0.1f, 1.5f);
            Assert.AreEqual(NeedleState.Lunge, n.State);
            float total = 0f;
            for (int i = 0; i < 4; i++) total += n.Tick(0.1f, 0.5f);
            Assert.AreEqual(4f, total, 0.001f, "one bite per lunge");
            Assert.AreEqual(NeedleState.Recoil, n.State);
            n.Tick(1.1f, 3f);
            Assert.AreEqual(NeedleState.Chase, n.State);
        }

        [Test]
        public void OneGoodHit_Kills()
        {
            var n = new NeedleBrain(health: 10f);
            n.Wake();
            Assert.IsFalse(n.Damage(4f));
            Assert.IsTrue(n.Damage(32f));
            Assert.AreEqual(NeedleState.Dead, n.State);
            Assert.AreEqual(0f, n.Tick(1f, 0.1f), "the dead don't bite");
        }
    }
}
