using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Holds one <see cref="Throwable"/> at a time and throws it from the camera (G, R1 / RB) in a soft arc.
    /// One object in hand keeps the choice simple: throw it now, or keep it for the next corridor.
    /// </summary>
    public class PlayerThrower : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] Transform aim;            // usually the camera
        [SerializeField] float throwSpeed = 9f;
        [SerializeField] float upwardSpeed = 1.5f;
        [SerializeField] float spawnDistance = 0.6f;

        CharacterController controller;

        public Throwable Held { get; private set; }

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (aim == null && Camera.main != null) aim = Camera.main.transform;
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (Held != null && input != null && input.ThrowPressedThisFrame) Throw();
        }

        public bool Hold(Throwable t)
        {
            if (t == null || Held != null) return false;
            Held = t;
            t.OnPickedUp();
            return true;
        }

        /// <summary>Throws the held object along the aim direction. Public for scripted sequences and tests.</summary>
        public bool Throw()
        {
            if (Held == null) return false;
            Transform from = aim != null ? aim : transform;
            var t = Held;
            Held = null;
            t.Launch(from.position + from.forward * spawnDistance, from.forward * throwSpeed + Vector3.up * upwardSpeed, controller);
            return true;
        }
    }
}
