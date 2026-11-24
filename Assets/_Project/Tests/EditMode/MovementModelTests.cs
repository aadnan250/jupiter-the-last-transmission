using Jupiter.Player;
using NUnit.Framework;
using UnityEngine;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the pure movement maths used by the first-person controller.</summary>
    public class MovementModelTests
    {
        const float EarthG = 9.81f;
        const float EuropaG = 1.315f; // 0.134 g, real value

        [Test]
        public void ResolveState_CrouchOverridesRun()
        {
            Assert.AreEqual(MoveState.Crouch, MovementModel.ResolveState(hasMoveInput: true, runHeld: true, crouched: true));
        }

        [Test]
        public void ResolveState_NoInputIsIdle()
        {
            Assert.AreEqual(MoveState.Idle, MovementModel.ResolveState(false, true, false));
        }

        [TestCase(false, MoveState.Walk)]
        [TestCase(true, MoveState.Run)]
        public void ResolveState_MovingWalksOrRuns(bool runHeld, MoveState expected)
        {
            Assert.AreEqual(expected, MovementModel.ResolveState(true, runHeld, false));
        }

        [Test]
        public void SpeedFor_ReturnsMatchingSpeed()
        {
            Assert.AreEqual(0f, MovementModel.SpeedFor(MoveState.Idle, 2f, 4f, 1f));
            Assert.AreEqual(2f, MovementModel.SpeedFor(MoveState.Walk, 2f, 4f, 1f));
            Assert.AreEqual(4f, MovementModel.SpeedFor(MoveState.Run, 2f, 4f, 1f));
            Assert.AreEqual(1f, MovementModel.SpeedFor(MoveState.Crouch, 2f, 4f, 1f));
        }

        [Test]
        public void JumpVelocity_ReachesRequestedApex()
        {
            const float height = 0.45f;
            float v = MovementModel.JumpVelocity(height, EarthG);
            float apex = v * v / (2f * EarthG); // h = v^2 / 2g
            Assert.AreEqual(height, apex, 1e-4f);
        }

        [Test]
        public void JumpVelocity_SameImpulseFliesHigherOnEuropa()
        {
            // The same take-off speed reaches 9.81 / 1.315 ≈ 7.46× higher in Europa's gravity.
            float v = MovementModel.JumpVelocity(0.45f, EarthG);
            float europaApex = v * v / (2f * EuropaG);
            Assert.AreEqual(0.45f * EarthG / EuropaG, europaApex, 1e-3f);
        }

        [Test]
        public void JumpVelocity_InvalidInputsGiveZero()
        {
            Assert.AreEqual(0f, MovementModel.JumpVelocity(0f, EarthG));
            Assert.AreEqual(0f, MovementModel.JumpVelocity(1f, 0f));
        }

        [Test]
        public void ApplyGravity_GroundedPinsToStickVelocity()
        {
            float v = MovementModel.ApplyGravity(-12f, EarthG, 0.016f, grounded: true, terminalFallSpeed: 30f);
            Assert.AreEqual(MovementModel.GroundedStickVelocity, v);
        }

        [Test]
        public void ApplyGravity_RisingWhileGroundedIsNotPinned()
        {
            // The frame a jump starts, the controller is still grounded but velocity is upward.
            float v = MovementModel.ApplyGravity(4f, EarthG, 0.02f, grounded: true, terminalFallSpeed: 30f);
            Assert.AreEqual(4f - EarthG * 0.02f, v, 1e-5f);
        }

        [Test]
        public void ApplyGravity_ClampsToTerminalSpeed()
        {
            float v = MovementModel.ApplyGravity(-29.9f, EarthG, 1f, grounded: false, terminalFallSpeed: 30f);
            Assert.AreEqual(-30f, v);
        }

        [Test]
        public void Accelerate_DoesNotOvershootTarget()
        {
            Vector3 result = MovementModel.Accelerate(Vector3.zero, new Vector3(2f, 0f, 0f), 100f, 1f, true, 1f);
            Assert.AreEqual(2f, result.x, 1e-5f);
        }

        [Test]
        public void Accelerate_AirControlReducesChange()
        {
            Vector3 ground = MovementModel.Accelerate(Vector3.zero, Vector3.forward * 4f, 10f, 0.1f, true, 0.25f);
            Vector3 air = MovementModel.Accelerate(Vector3.zero, Vector3.forward * 4f, 10f, 0.1f, false, 0.25f);
            Assert.AreEqual(1f, ground.z, 1e-5f);
            Assert.AreEqual(0.25f, air.z, 1e-5f);
        }

        [Test]
        public void Accelerate_IgnoresVerticalComponent()
        {
            Vector3 result = MovementModel.Accelerate(new Vector3(0f, 5f, 0f), new Vector3(0f, 9f, 1f), 100f, 1f, true, 1f);
            Assert.AreEqual(0f, result.y);
        }
    }
}
