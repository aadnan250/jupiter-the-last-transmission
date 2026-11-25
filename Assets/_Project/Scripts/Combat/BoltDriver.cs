using Jupiter.Creatures;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// The bolt driver: a pneumatic anchoring tool turned weapon. Fires a hitscan bolt from the camera.
    /// Body hits stagger; hits on a glowing pressure node rupture it. Every shot is loud: a 25 m airborne
    /// vibration event that anything listening will hear. Only works once the inventory holds it.
    /// </summary>
    public class BoltDriver : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Transform muzzle;           // usually the camera
        [SerializeField] float range = 40f;
        [SerializeField] float bodyDamage = 45f;
        [SerializeField] float noiseRadius = 25f;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip reloadClip;
        [SerializeField] Light flash;

        public WeaponModel Model { get; } = new WeaponModel();
        public int ShotsFired { get; private set; }

        float flashTimer;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (muzzle == null && Camera.main != null) muzzle = Camera.main.transform;
            if (flash != null) flash.enabled = false;
        }

        void Update()
        {
            Model.Tick(Time.deltaTime);
            if (flash != null && flashTimer > 0f && (flashTimer -= Time.deltaTime) <= 0f) flash.enabled = false;

            if (inventory == null || !inventory.HasBoltDriver || input == null) return;
            if (input.ReloadPressedThisFrame) Reload();
            if (input.FirePressedThisFrame)
            {
                if (Model.State == WeaponState.Empty) Reload();
                else Fire();
            }
        }

        public bool Reload()
        {
            if (!Model.TryReload()) return false;
            if (audioSource != null && reloadClip != null) audioSource.PlayOneShot(reloadClip, 0.7f);
            return true;
        }

        /// <summary>Fires one bolt if ready. Public so scripted sequences and tests can fire.</summary>
        public bool Fire()
        {
            if (!Model.TryFire()) return false;
            ShotsFired++;
            Transform from = muzzle != null ? muzzle : transform;

            if (Physics.Raycast(from.position, from.forward, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null && !hit.collider.transform.IsChildOf(transform))
                    target.TakeDamage(new DamageInfo(bodyDamage, DamageType.Bolt, hit.point, gameObject));
            }

            // Loud: everything nearby hears it (airborne, so not in vacuum).
            bool vacuum = TryGetComponent<PlayerController>(out var pc) && pc.Environment != null && pc.Environment.isVacuum;
            VibrationSystem.Emit(new VibrationEvent(from.position, noiseRadius, VibrationMedium.Air, vacuum, gameObject));

            if (audioSource != null && fireClip != null) audioSource.PlayOneShot(fireClip);
            if (flash != null) { flash.enabled = true; flashTimer = 0.05f; }
            return true;
        }
    }
}
