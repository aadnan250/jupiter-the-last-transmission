using Jupiter.Creatures;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// The arc beam rifle: hold Fire for a continuous cutting beam from the camera (left mouse, R2 / RT); R reloads a
    /// power cell. Damage is per second of beam on target. The visible beam starts at the gun's muzzle in Sam's
    /// hands (<see cref="WeaponViewModel"/>) and ends where it hits. Only works once the inventory holds it.
    /// </summary>
    public class BeamRifle : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Transform aim;                 // the camera
        [SerializeField] Transform muzzle;              // the gun's barrel tip, for the visible beam
        [SerializeField] LineRenderer beam;
        [SerializeField] Light beamLight;
        [SerializeField] float range = 60f;
        [SerializeField] float damagePerSecond = 120f;
        [SerializeField] float noiseRadius = 12f;
        [SerializeField] AudioSource loop;
        [SerializeField] AudioSource oneShots;
        [SerializeField] AudioClip reloadClip;

        public BeamModel Model { get; } = new BeamModel();
        public bool Equipped => inventory != null && inventory.HasBeamRifle;
        public bool BeamVisible => beam != null && beam.enabled;
        public Vector3 LastHitPoint { get; private set; }

        float noiseTimer;
        CharacterController controller;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (aim == null && Camera.main != null) aim = Camera.main.transform;
            controller = GetComponent<CharacterController>();
            SetBeam(false);
        }

        void Update()
        {
            bool held = Equipped && input != null && input.FireHeld;
            if (Equipped && input != null && input.ReloadPressedThisFrame) Reload();
            if (Equipped && held && Model.State == BeamState.Empty) Reload();
            Fire(Time.deltaTime, held);
        }

        public bool Reload()
        {
            if (!Model.TryReload()) return false;
            if (oneShots != null && reloadClip != null) oneShots.PlayOneShot(reloadClip, 0.7f);
            return true;
        }

        /// <summary>Runs the beam for <paramref name="deltaTime"/> seconds. Public so tests and scripted moments can fire.</summary>
        public float Fire(float deltaTime, bool held)
        {
            float fired = Model.Tick(deltaTime, held);
            if (fired <= 0f) { SetBeam(false); return 0f; }

            Transform from = aim != null ? aim : transform;
            Vector3 end = from.position + from.forward * range;
            if (Physics.Raycast(from.position, from.forward, out var hit, range, ~0, QueryTriggerInteraction.Ignore)
                && (controller == null || hit.collider != controller))
            {
                end = hit.point;
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && !hit.collider.transform.IsChildOf(transform))
                    target.TakeDamage(new DamageInfo(damagePerSecond * fired, DamageType.Beam, hit.point, gameObject));
            }
            LastHitPoint = end;

            // The beam hums through the suit and the ground: a creature nearby feels it.
            if ((noiseTimer -= deltaTime) <= 0f)
            {
                noiseTimer = 0.5f;
                VibrationSystem.Emit(new VibrationEvent(from.position, noiseRadius, VibrationMedium.Ground, false, gameObject));
            }

            SetBeam(true);
            if (beam != null)
            {
                beam.SetPosition(0, muzzle != null ? muzzle.position : from.position + from.forward * 0.6f);
                beam.SetPosition(1, end);
                float w = 0.035f + 0.015f * Mathf.PerlinNoise(Time.time * 30f, 0f);
                beam.startWidth = w;
                beam.endWidth = w * 0.6f;
            }
            if (beamLight != null)
            {
                beamLight.transform.position = end - from.forward * 0.3f;
                beamLight.intensity = 3f + 2f * Mathf.PerlinNoise(0f, Time.time * 25f);
            }
            return fired;
        }

        void SetBeam(bool on)
        {
            if (beam != null && beam.enabled != on) beam.enabled = on;
            if (beamLight != null && beamLight.enabled != on) beamLight.enabled = on;
            if (loop != null)
            {
                if (on && !loop.isPlaying) loop.Play();
                else if (!on && loop.isPlaying) loop.Stop();
            }
        }
    }
}
