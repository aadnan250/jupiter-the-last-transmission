using Jupiter.Combat;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the EVA suit: cracks from hits, the leak, the radiation dose, and sealing.</summary>
    public class SuitModelTests
    {
        [Test]
        public void Hits_CrackTheSuit_AndABreachLeaks()
        {
            var s = new SuitModel(integrityPerDamage: 0.5f, breachLine: 35f, leakDamagePerSecond: 1f, exposureSeconds: 0f);
            s.OnHit(100f);
            Assert.AreEqual(50f, s.Integrity, 0.001f);
            Assert.IsFalse(s.Breached);
            Assert.AreEqual(0f, s.Tick(1f, true), 0.001f, "cracked but holding");
            s.OnHit(40f);
            Assert.IsTrue(s.Breached);
            Assert.AreEqual(2f, s.Tick(2f, true), 0.001f, "the leak costs health every second");
        }

        [Test]
        public void Dose_BuildsOutsideOnly_AndHurtsPastTheLimit()
        {
            var s = new SuitModel(exposureSeconds: 100f, radiationDamagePerSecond: 2f);
            Assert.AreEqual(0f, s.Tick(50f, false));
            Assert.AreEqual(0f, s.Dose, "no dose indoors");
            Assert.AreEqual(0f, s.Tick(50f, true));
            Assert.AreEqual(0.5f, s.DoseFraction, 0.001f);
            Assert.AreEqual(50f, s.SecondsToLimit, 0.01f);
            s.Tick(50f, true);
            Assert.IsTrue(s.OverDose);
            Assert.AreEqual(2f, s.Tick(1f, true), 0.001f);
        }

        [Test]
        public void Seal_RestoresIntegrity_UpToFull()
        {
            var s = new SuitModel();
            s.OnHit(250f);
            Assert.AreEqual(0f, s.Integrity);
            Assert.AreEqual(40f, s.Seal(40f), 0.001f);
            s.Seal(1000f);
            Assert.AreEqual(SuitModel.MaxIntegrity, s.Integrity);
            Assert.AreEqual(0f, s.Seal(10f), "nothing to seal");
        }
    }
}
