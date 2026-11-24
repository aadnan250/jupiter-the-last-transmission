using UnityEngine;

namespace Jupiter.Player
{
    public enum MoveState
    {
        Idle,
        Walk,
        Run,
        Crouch
    }

    /// <summary>
    /// Pure movement maths with no scene dependencies, so it can be unit tested in EditMode.
    /// <see cref="PlayerController"/> feeds it input and applies the results to a CharacterController.
    /// </summary>
    public static class MovementModel
    {
        /// <summary>Small downward velocity applied while grounded so the controller stays snapped to slopes.</summary>
        public const float GroundedStickVelocity = -1.5f;

        /// <summary>Decides the movement state. Crouch wins over run: you cannot sprint while crouched.</summary>
        public static MoveState ResolveState(bool hasMoveInput, bool runHeld, bool crouched)
        {
            if (crouched) return MoveState.Crouch;
            if (!hasMoveInput) return MoveState.Idle;
            return runHeld ? MoveState.Run : MoveState.Walk;
        }

        /// <summary>Top horizontal speed for a state, in m/s.</summary>
        public static float SpeedFor(MoveState state, float walk, float run, float crouch)
        {
            switch (state)
            {
                case MoveState.Run: return run;
                case MoveState.Walk: return walk;
                case MoveState.Crouch: return crouch;
                default: return 0f;
            }
        }

        /// <summary>
        /// Initial upward velocity needed to reach <paramref name="jumpHeight"/> under <paramref name="gravity"/>.
        /// From v² = 2gh.
        /// </summary>
        public static float JumpVelocity(float jumpHeight, float gravity)
        {
            if (jumpHeight <= 0f || gravity <= 0f) return 0f;
            return Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        /// <summary>
        /// Integrates vertical velocity for one step. While grounded and not moving upward,
        /// velocity is pinned to <see cref="GroundedStickVelocity"/>. Falling is clamped to terminal speed.
        /// </summary>
        public static float ApplyGravity(float verticalVelocity, float gravity, float deltaTime, bool grounded, float terminalFallSpeed)
        {
            if (grounded && verticalVelocity <= 0f) return GroundedStickVelocity;

            float v = verticalVelocity - gravity * deltaTime;
            return Mathf.Max(v, -Mathf.Abs(terminalFallSpeed));
        }

        /// <summary>
        /// Moves horizontal velocity toward the target at a fixed rate. Airborne control is scaled down.
        /// </summary>
        public static Vector3 Accelerate(Vector3 current, Vector3 target, float acceleration, float deltaTime, bool grounded, float airControl)
        {
            float rate = acceleration * (grounded ? 1f : Mathf.Clamp01(airControl));
            Vector3 flatCurrent = new Vector3(current.x, 0f, current.z);
            Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
            return Vector3.MoveTowards(flatCurrent, flatTarget, rate * deltaTime);
        }
    }
}
