using Jupiter.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for vibration propagation, the suspicion model and the creature's torpor/node biology.</summary>
    public class CreatureSystemsTests
    {
        // ---------- vibration ----------

        [Test]
        public void Vibration_FallsOffLinearlyToZeroAtRadius()
        {
            var e = new VibrationEvent(Vector3.zero, 10f, VibrationMedium.Ground);
            Assert.AreEqual(1f, VibrationMath.Perceived(e, Vector3.zero, false), 1e-5f);
            Assert.AreEqual(0.5f, VibrationMath.Perceived(e, new Vector3(5f, 0f, 0f), false), 1e-5f);
            Assert.AreEqual(0f, VibrationMath.Perceived(e, new Vector3(10f, 0f, 0f), false));
            Assert.AreEqual(0f, VibrationMath.Perceived(e, new Vector3(30f, 0f, 0f), false));
        }

        [Test]
        public void Vibration_AirborneSoundDoesNotCrossVacuum()
        {
            var shot = new VibrationEvent(Vector3.zero, 25f, VibrationMedium.Air);
            Assert.Greater(VibrationMath.Perceived(shot, Vector3.one, listenerInVacuum: false), 0f);
            Assert.AreEqual(0f, VibrationMath.Perceived(shot, Vector3.one, listenerInVacuum: true), "listener in vacuum");

            shot.SourceInVacuum = true;
            Assert.AreEqual(0f, VibrationMath.Perceived(shot, Vector3.one, listenerInVacuum: false), "source in vacuum");
        }

        [Test]
        public void Vibration_GroundContactCarriesInVacuum()
        {
            // "In vacuum, you hear through your feet."
            var step = new VibrationEvent(Vector3.zero, 3f, VibrationMedium.Ground, sourceInVacuum: true);
            Assert.Greater(VibrationMath.Perceived(step, new Vector3(1f, 0f, 0f), listenerInVacuum: true), 0f);
        }

        [Test]
        public void Vibration_RimeDoublesTheRadius()
        {
            var step = new VibrationEvent(Vector3.zero, 3f, VibrationMedium.Ground);
            var far = new Vector3(4f, 0f, 0f);
            Assert.AreEqual(0f, VibrationMath.Perceived(step, far, false));
            Assert.AreEqual(1f - 4f / 6f, VibrationMath.Perceived(step, far, false, VibrationSystem.RimeMultiplier), 1e-5f);
        }

        [Test]
        public void VibrationSystem_NotifiesRegisteredListenersOnly()
        {
            var a = new RecordingListener();
            var b = new RecordingListener();
            VibrationSystem.Register(a);
            VibrationSystem.Register(b);
            VibrationSystem.Unregister(b);
            try
            {
                VibrationSystem.Emit(new VibrationEvent(Vector3.zero, 5f, VibrationMedium.Air));
                Assert.AreEqual(1, a.Count);
                Assert.AreEqual(0, b.Count);
            }
            finally { VibrationSystem.Unregister(a); }
        }

        class RecordingListener : IVibrationListener
        {
            public int Count;
            public void OnVibration(VibrationEvent e, float radiusMultiplier) => Count++;
        }

        // ---------- suspicion ----------

        [Test]
        public void Suspicion_ClimbsThroughThresholds()
        {
            var s = new SuspicionModel();
            Assert.AreEqual(Alertness.Calm, s.Level);
            s.Hear(0.2f, Vector3.one);
            Assert.AreEqual(Alertness.Listening, s.Level);
            s.Hear(0.3f, Vector3.one);
            Assert.AreEqual(Alertness.Investigating, s.Level);
            s.Hear(0.5f, Vector3.one);
            Assert.AreEqual(Alertness.Hunting, s.Level);
            Assert.AreEqual(Vector3.one, s.LastKnownPosition);
        }

        [Test]
        public void Suspicion_DecaysBackToCalmAndForgetsPosition()
        {
            var s = new SuspicionModel { DecayPerSecond = 0.1f };
            s.Hear(0.5f, Vector3.one);
            s.Tick(2f);
            Assert.AreEqual(0.3f, s.Value, 1e-5f);
            s.Tick(10f);
            Assert.AreEqual(Alertness.Calm, s.Level);
            Assert.IsNull(s.LastKnownPosition);
        }

        [Test]
        public void Suspicion_SightOnlyWithinFourMetres()
        {
            var s = new SuspicionModel();
            Assert.IsFalse(s.See(Vector3.zero, 6f));
            Assert.AreEqual(Alertness.Calm, s.Level);
            Assert.IsTrue(s.See(Vector3.zero, 3f));
            Assert.AreEqual(Alertness.Hunting, s.Level);
        }

        [Test]
        public void Suspicion_IsCapped()
        {
            var s = new SuspicionModel();
            for (int i = 0; i < 10; i++) s.Hear(1f, Vector3.zero);
            Assert.AreEqual(s.MaxValue, s.Value);
        }

        [Test]
        public void EstimateSource_ErrorGrowsWithDistance()
        {
            Vector3 truth = Vector3.zero;
            Vector3 near = SuspicionModel.EstimateSource(truth, new Vector3(2f, 0f, 0f), 0.15f, 0.25f, 1f);
            Vector3 far = SuspicionModel.EstimateSource(truth, new Vector3(20f, 0f, 0f), 0.15f, 0.25f, 1f);
            Assert.AreEqual(0.3f, Vector3.Distance(near, truth), 1e-4f);
            Assert.AreEqual(3f, Vector3.Distance(far, truth), 1e-4f);
        }

        // ---------- creature biology ----------

        [Test]
        public void Creature_BodyDamageCausesTorporThenRecovery()
        {
            var c = new CreatureHealthModel(() => 0f) { TorporMinSeconds = 20f, TorporMaxSeconds = 40f };
            c.TakeBodyDamage(299f);
            Assert.AreEqual(CreatureCondition.Active, c.Condition);
            c.TakeBodyDamage(5f);
            Assert.AreEqual(CreatureCondition.Torpor, c.Condition);
            Assert.AreEqual(20f, c.TorporRemaining, 1e-5f);

            c.Tick(19.9f);
            Assert.AreEqual(CreatureCondition.Torpor, c.Condition, "looks dead, but is resealing");
            c.Tick(0.2f);
            Assert.AreEqual(CreatureCondition.Active, c.Condition, "it gets back up");
            Assert.AreEqual(150f, c.Health, 1e-3f, "recovers with half health");
        }

        [Test]
        public void Creature_TorporDurationUsesTheRandomRange()
        {
            var c = new CreatureHealthModel(() => 1f);
            c.TakeBodyDamage(1000f);
            Assert.AreEqual(40f, c.TorporRemaining, 1e-5f);
        }

        [Test]
        public void Creature_BodyHitsInTorporDoNothing()
        {
            var c = new CreatureHealthModel();
            c.TakeBodyDamage(1000f);
            c.TakeBodyDamage(1000f);
            Assert.AreEqual(CreatureCondition.Torpor, c.Condition);
        }

        [Test]
        public void Creature_ThreeRupturedNodesKill()
        {
            var c = new CreatureHealthModel(maxHealth: 10000f);
            c.RuptureNode(0);
            c.RuptureNode(0); // the same node twice counts once
            c.RuptureNode(3);
            Assert.AreEqual(CreatureCondition.Active, c.Condition);
            Assert.AreEqual(2, c.NodesRuptured);
            c.RuptureNode(5);
            Assert.AreEqual(CreatureCondition.Dead, c.Condition);
        }

        [Test]
        public void Creature_NodesCanStillBeRupturedDuringTorpor()
        {
            var c = new CreatureHealthModel();
            c.TakeBodyDamage(1000f);
            c.RuptureNode(0); c.RuptureNode(1); c.RuptureNode(2);
            Assert.AreEqual(CreatureCondition.Dead, c.Condition);
        }

        [Test]
        public void Creature_ExplosivesKillOnlyInTorpor()
        {
            var c = new CreatureHealthModel();
            c.TakeExplosiveDamage(100f);
            Assert.AreEqual(CreatureCondition.Active, c.Condition, "an explosion is just heavy damage while it is up");
            c.TakeBodyDamage(1000f);
            c.TakeExplosiveDamage(1f);
            Assert.AreEqual(CreatureCondition.Dead, c.Condition);
        }

        [Test]
        public void Creature_RaisesConditionChanged()
        {
            var c = new CreatureHealthModel();
            CreatureCondition last = CreatureCondition.Active;
            int changes = 0;
            c.ConditionChanged += (_, now) => { last = now; changes++; };
            c.TakeBodyDamage(1000f);
            Assert.AreEqual(CreatureCondition.Torpor, last);
            c.Tick(100f);
            Assert.AreEqual(CreatureCondition.Active, last);
            Assert.AreEqual(2, changes);
        }
    }
}
