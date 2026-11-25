using Jupiter.Audio;
using Jupiter.Combat;
using Jupiter.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the stealth rules: thrown-object noise, decoys, flares, hiding and the new suspicion hooks.</summary>
    public class StealthToolsTests
    {
        // ---------- thrown objects ----------

        [Test]
        public void ImpactNoise_SoftBumpsAreSilent_HardLandingsReachSixMetres()
        {
            Assert.AreEqual(0f, StealthRules.ImpactNoiseRadius(0f));
            Assert.AreEqual(0f, StealthRules.ImpactNoiseRadius(StealthRules.SilentImpactSpeed - 0.01f));
            Assert.Greater(StealthRules.ImpactNoiseRadius(StealthRules.SilentImpactSpeed), 0f);
            Assert.AreEqual(StealthRules.DroppedObjectRadius, StealthRules.ImpactNoiseRadius(StealthRules.LoudImpactSpeed), 1e-5f);
            Assert.AreEqual(StealthRules.DroppedObjectRadius, StealthRules.ImpactNoiseRadius(50f), 1e-5f, "capped");
        }

        [Test]
        public void ImpactNoise_GrowsWithSpeed()
        {
            float previous = 0f;
            for (float v = StealthRules.SilentImpactSpeed; v <= StealthRules.LoudImpactSpeed; v += 0.5f)
            {
                float r = StealthRules.ImpactNoiseRadius(v);
                Assert.GreaterOrEqual(r, previous);
                previous = r;
            }
        }

        [Test]
        public void StealthRadii_MatchTheDesignDocument()
        {
            Assert.AreEqual(6f, StealthRules.DroppedObjectRadius, "dropped object 6 m");
            Assert.AreEqual(15f, StealthRules.DecoyRadius, "decoy 15 m");
            Assert.Less(StealthRules.LockerRadius, 3f, "hiding is quieter than walking");
            Assert.Less(StealthRules.FlareShotRadius, 25f, "the flare pistol is quieter than the bolt driver");
        }

        // ---------- decoy ----------

        [Test]
        public void Decoy_DoesNothingUntilArmed()
        {
            var d = new DecoyModel(8f, 1.5f);
            Assert.AreEqual(0, d.Tick(10f));
            Assert.IsFalse(d.Active);
        }

        [Test]
        public void Decoy_ThumpsOnLandingThenEveryInterval_ThenStops()
        {
            var d = new DecoyModel(8f, 1.5f);
            d.Arm();
            Assert.AreEqual(1, d.Tick(0.01f), "first thump on landing");
            Assert.AreEqual(0, d.Tick(1f));
            Assert.AreEqual(1, d.Tick(0.5f), "second at 1.5 s");

            int total = 2;
            for (int i = 0; i < 200; i++) total += d.Tick(0.1f);
            Assert.AreEqual(6, total, "t = 0, 1.5, 3, 4.5, 6, 7.5");
            Assert.AreEqual(d.TotalPulses, d.PulsesEmitted);
            Assert.IsFalse(d.Active, "spent");
        }

        [Test]
        public void Decoy_LargeStepCatchesUpAndArmingTwiceDoesNotRestart()
        {
            var d = new DecoyModel(8f, 1.5f);
            d.Arm();
            Assert.AreEqual(3, d.Tick(3f), "0, 1.5 and 3 s fell due");
            d.Arm();
            Assert.AreEqual(3f, d.Elapsed, 1e-5f);
        }

        // ---------- flare ----------

        [Test]
        public void Flare_BurnsAtFullThenFadesThenGoesOut()
        {
            var f = new FlareModel(25f, 3f);
            Assert.IsTrue(f.Burning);
            Assert.AreEqual(1f, f.Intensity);
            f.Tick(21f);
            Assert.AreEqual(1f, f.Intensity, 1e-5f, "full until the last 3 s");
            f.Tick(2.5f);
            Assert.AreEqual(0.5f, f.Intensity, 1e-4f, "half-way through the fade");
            f.Tick(10f);
            Assert.IsFalse(f.Burning);
            Assert.AreEqual(0f, f.Intensity);
        }

        [Test]
        public void FlareEffect_BlindsClose_LuresInRange_IgnoredBeyond()
        {
            Assert.AreEqual(FlareEffect.Blind, StealthRules.FlareEffectAt(0f));
            Assert.AreEqual(FlareEffect.Blind, StealthRules.FlareEffectAt(StealthRules.FlareBlindRange));
            Assert.AreEqual(FlareEffect.Lure, StealthRules.FlareEffectAt(StealthRules.FlareBlindRange + 0.1f));
            Assert.AreEqual(FlareEffect.Lure, StealthRules.FlareEffectAt(StealthRules.FlareLureRange));
            Assert.AreEqual(FlareEffect.None, StealthRules.FlareEffectAt(StealthRules.FlareLureRange + 0.1f));
        }

        // ---------- hiding and suspicion ----------

        [Test]
        public void Sight_HiddenPlayerIsNeverSeen()
        {
            Assert.IsTrue(StealthRules.CanSee(false, 1f, 4f));
            Assert.IsFalse(StealthRules.CanSee(false, 4.5f, 4f), "nearly blind beyond 4 m");
            Assert.IsFalse(StealthRules.CanSee(true, 0.5f, 4f), "not even point-blank");
        }

        [Test]
        public void Suspicion_LureMakesItInvestigateTheExactPoint()
        {
            var s = new SuspicionModel();
            var flare = new Vector3(3f, 0f, 7f);
            s.Lure(flare);
            Assert.AreEqual(Alertness.Investigating, s.Level);
            Assert.AreEqual(flare, s.LastKnownPosition);
        }

        [Test]
        public void Suspicion_LureDoesNotCalmAHunter()
        {
            var s = new SuspicionModel();
            s.See(Vector3.zero, 1f);
            s.Lure(Vector3.one);
            Assert.AreEqual(Alertness.Hunting, s.Level);
            Assert.AreEqual(Vector3.one, s.LastKnownPosition, "but it now chases the light");
        }

        [Test]
        public void Suspicion_ForgetClearsEverything()
        {
            var s = new SuspicionModel();
            s.See(Vector3.one, 1f);
            s.Forget();
            Assert.AreEqual(Alertness.Calm, s.Level);
            Assert.IsNull(s.LastKnownPosition);
        }

        // ---------- inventory and audio ----------

        [Test]
        public void Inventory_ThreeFlaresFitOneSlotAndAreConsumed()
        {
            var inv = new InventoryModel();
            inv.EquipTool(PlayerInventory.FlarePistolId);
            Assert.AreEqual(3, inv.Add(PlayerInventory.Flare, 3));
            Assert.AreEqual(PlayerInventory.FlarePistolId, inv.Tool);
            for (int i = 0; i < 3; i++) Assert.IsTrue(inv.Consume(PlayerInventory.Flare));
            Assert.IsFalse(inv.Consume(PlayerInventory.Flare), "out of flares");
        }

        [Test]
        public void StealthSounds_AreDeterministicAndInRange()
        {
            CollectionAssert.AreEqual(CombatSynth.Clatter(), CombatSynth.Clatter());
            CollectionAssert.AreEqual(CombatSynth.FlareShot(), CombatSynth.FlareShot());
            foreach (var clip in new[] { CombatSynth.Clatter(), CombatSynth.FlareShot(), CombatSynth.DecoyThump(), CombatSynth.LockerDoor() })
            {
                Assert.Greater(clip.Length, 1000);
                foreach (float s in clip) Assert.LessOrEqual(System.Math.Abs(s), 1f);
            }
        }
    }
}
