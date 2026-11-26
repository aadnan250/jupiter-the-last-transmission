using Jupiter.Audio;
using Jupiter.Combat;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the Europa landing: the beam rifle's power cells and the ambush timing and layout.</summary>
    public class EuropaLogicTests
    {
        [Test]
        public void Beam_DrainsOnlyWhileTheTriggerIsHeld()
        {
            var b = new BeamModel(100f, 20f, 1.5f, 2);
            Assert.AreEqual(0f, b.Tick(1f, false));
            Assert.AreEqual(100f, b.Charge);
            Assert.AreEqual(1f, b.Tick(1f, true), 1e-5f);
            Assert.AreEqual(80f, b.Charge, 1e-4f);
            Assert.AreEqual(BeamState.Firing, b.State);
        }

        [Test]
        public void Beam_ReportsExactFiringTimeWhenTheCellRunsDry()
        {
            var b = new BeamModel(100f, 20f, 1.5f, 2);
            b.Tick(4.5f, true);                           // 10 left = 0.5 s
            Assert.AreEqual(0.5f, b.Tick(2f, true), 1e-4f);
            Assert.AreEqual(BeamState.Empty, b.State);
        }

        [Test]
        public void Beam_ReloadSwapsACellAfterADelay()
        {
            var b = new BeamModel(100f, 20f, 1.5f, 1);
            Assert.IsFalse(b.TryReload(), "a full cell is not swapped");
            b.Tick(5f, true);
            Assert.IsTrue(b.TryReload());
            Assert.AreEqual(0, b.SpareCells);
            Assert.AreEqual(BeamState.Reloading, b.State);
            Assert.AreEqual(0f, b.Tick(1f, true), "no beam while swapping");
            b.Tick(0.6f, false);
            Assert.AreEqual(100f, b.Charge);
            b.Tick(5f, true);
            Assert.IsFalse(b.TryReload(), "no spare cells left");
        }

        [Test]
        public void Beam_CellsAreCapped()
        {
            var b = new BeamModel(100f, 20f, 1.5f, 6, maxCells: 8);
            Assert.AreEqual(2, b.AddCells(5));
            Assert.AreEqual(8, b.SpareCells);
        }

        [Test]
        public void Ambush_StalkersComeUpOneAfterAnotherAfterTheQuakeBegins()
        {
            Assert.Greater(AmbushTimeline.EmergeTime(0), AmbushTimeline.RadioAt, "the radio panics first");
            for (int i = 1; i < 10; i++) Assert.Greater(AmbushTimeline.EmergeTime(i), AmbushTimeline.EmergeTime(i - 1));
            Assert.Less(AmbushTimeline.EmergeTime(9), 10f, "all ten within ten seconds");
        }

        [Test]
        public void Ambush_EmergencePointsRingTheSquad_NeverOnTopOfThem()
        {
            for (int i = 0; i < 10; i++)
            {
                float r = AmbushTimeline.EmergeOffset(i, 10, 13f, 26f).magnitude;
                Assert.GreaterOrEqual(r, 13f);
                Assert.LessOrEqual(r, 26f + 3f);
                for (int j = 0; j < i; j++)
                    Assert.Greater(Vector3.Distance(AmbushTimeline.EmergeOffset(i, 10, 13f, 26f), AmbushTimeline.EmergeOffset(j, 10, 13f, 26f)), 3f, $"{i} and {j} apart");
            }
        }

        [Test]
        public void SurfaceSounds_AreDeterministicAndInRange()
        {
            CollectionAssert.AreEqual(CombatSynth.Shriek(), CombatSynth.Shriek());
            foreach (var clip in new[] { CombatSynth.BeamHum(), CombatSynth.Shriek(), CombatSynth.Quake(), CombatSynth.IceBurst(), CombatSynth.SuitBreath() })
            {
                Assert.Greater(clip.Length, 1000);
                foreach (float s in clip) Assert.LessOrEqual(System.Math.Abs(s), 1f);
            }
        }
    }
}
