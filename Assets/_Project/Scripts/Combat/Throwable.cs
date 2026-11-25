using Jupiter.Creatures;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Something Sam can pick up and throw to pull a creature's attention elsewhere: a can, a wrench, or a decoy
    /// thumper. A hard landing is a floor vibration (up to 6 m, scaled by impact speed, see
    /// <see cref="StealthRules.ImpactNoiseRadius"/>). A decoy, once it lands, thumps through the floor at 15 m
    /// for several seconds, then is spent.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Throwable : MonoBehaviour, IInteractable
    {
        [SerializeField] string displayName = "can";
        [SerializeField] bool isDecoy;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip impactClip;
        [SerializeField] AudioClip decoyClip;
        [SerializeField] float impactCooldown = 0.25f;

        Rigidbody body;
        Collider[] colliders;
        bool thrown;
        float lastImpact = -10f;

        public string DisplayName => displayName;
        public bool IsDecoy => isDecoy;
        public DecoyModel Decoy { get; } = new DecoyModel();
        public bool Spent => isDecoy && Decoy.Armed && !Decoy.Active;
        public int ImpactNoises { get; private set; }

        public string Prompt => Spent ? $"Pick up spent {displayName}" : $"Pick up {displayName}";

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
        }

        public bool CanInteract(GameObject interactor)
        {
            var thrower = interactor != null ? interactor.GetComponent<PlayerThrower>() : null;
            return thrower != null && thrower.Held == null && !(isDecoy && Decoy.Active);
        }

        public void Interact(GameObject interactor)
        {
            var thrower = interactor.GetComponent<PlayerThrower>();
            if (thrower != null) thrower.Hold(this);
        }

        /// <summary>Taken into Sam's hand: it leaves the world until thrown.</summary>
        public void OnPickedUp() => gameObject.SetActive(false);

        /// <summary>Back into the world at <paramref name="position"/>, flying at <paramref name="velocity"/>.</summary>
        public void Launch(Vector3 position, Vector3 velocity, Collider thrower)
        {
            transform.SetPositionAndRotation(position, Random.rotation);
            gameObject.SetActive(true);
            // IgnoreCollision resets when a collider is deactivated, so it is set again on every throw.
            if (thrower != null) foreach (var c in colliders) Physics.IgnoreCollision(c, thrower, true);
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 8f;
            thrown = true;
        }

        void OnCollisionEnter(Collision collision)
        {
            // Only after a throw: objects settling at level load must not wake anything.
            if (!thrown || Time.time - lastImpact < impactCooldown) return;
            float radius = StealthRules.ImpactNoiseRadius(collision.relativeVelocity.magnitude);
            if (radius <= 0f) return;
            lastImpact = Time.time;
            ImpactNoises++;
            Emit(radius);
            if (audioSource != null && impactClip != null)
                audioSource.PlayOneShot(impactClip, Mathf.Clamp01(radius / StealthRules.DroppedObjectRadius));
            if (isDecoy) Decoy.Arm();
        }

        void Update()
        {
            if (!isDecoy) return;
            int pulses = Decoy.Tick(Time.deltaTime);
            for (int i = 0; i < pulses; i++)
            {
                Emit(StealthRules.DecoyRadius);
                if (audioSource != null && decoyClip != null) audioSource.PlayOneShot(decoyClip);
            }
        }

        // Landings and thumps are contact vibrations: they travel through the floor, even in vacuum.
        void Emit(float radius) =>
            VibrationSystem.Emit(new VibrationEvent(transform.position, radius, VibrationMedium.Ground, false, gameObject));

        /// <summary>Sets up a throwable from code (used by the editor level builders).</summary>
        public void Configure(string name, bool decoy, AudioSource source, AudioClip impact, AudioClip thump)
        {
            displayName = name;
            isDecoy = decoy;
            audioSource = source;
            impactClip = impact;
            decoyClip = thump;
        }
    }
}
