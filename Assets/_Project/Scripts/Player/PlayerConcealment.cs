using System;
using Jupiter.Creatures;
using UnityEngine;

namespace Jupiter.Player
{
    /// <summary>
    /// Whether Sam is hidden (in a locker). While hidden he cannot move, look, fire or throw, and creatures
    /// cannot see him at all: they can only hear him. Climbing in or out is a small noise of its own, so
    /// hiding right under a creature's crest is still a mistake.
    /// </summary>
    public class PlayerConcealment : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] float noiseRadius = StealthRules.LockerRadius;

        CharacterController controller;

        public bool IsHidden => Spot != null;

        /// <summary>The hiding place Sam is in, or null.</summary>
        public Component Spot { get; private set; }

        /// <summary>Raised with true on hiding and false on leaving.</summary>
        public event Action<bool> Changed;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            controller = GetComponent<CharacterController>();
        }

        /// <summary>Moves Sam inside <paramref name="spot"/>, standing at <paramref name="feet"/> facing <paramref name="facing"/>.</summary>
        public bool Hide(Component spot, Vector3 feet, Quaternion facing)
        {
            if (spot == null || IsHidden) return false;
            Spot = spot;
            Teleport(feet, facing);
            if (input != null) input.GameplayBlocked = true;
            EmitNoise();
            Changed?.Invoke(true);
            return true;
        }

        /// <summary>Steps out of the current hiding place to <paramref name="feet"/>.</summary>
        public bool Leave(Vector3 feet, Quaternion facing)
        {
            if (!IsHidden) return false;
            Spot = null;
            Teleport(feet, facing);
            if (input != null) input.GameplayBlocked = false;
            EmitNoise();
            Changed?.Invoke(false);
            return true;
        }

        void Teleport(Vector3 feet, Quaternion facing)
        {
            // A CharacterController overrides transform writes while enabled.
            bool was = controller != null && controller.enabled;
            if (was) controller.enabled = false;
            transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, facing.eulerAngles.y, 0f));
            if (was) controller.enabled = true;
        }

        void EmitNoise()
        {
            var pc = GetComponent<PlayerController>();
            bool vacuum = pc != null && pc.Environment != null && pc.Environment.isVacuum;
            VibrationSystem.Emit(new VibrationEvent(transform.position, noiseRadius, VibrationMedium.Ground, vacuum, gameObject));
        }
    }
}
