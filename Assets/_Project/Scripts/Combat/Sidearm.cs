using System;
using Jupiter.Creatures;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Sam's sidearm: a compact semi-automatic pistol from the landing kit. One shot per press (left mouse, R2 / RT),
    /// 12 rounds, a quick reload (R). A body hit wears the Listener down, a node hit ruptures the node, and shots into
    /// it while it is down finish it. Every shot is a loud airborne noise. The pistol is drawn in Sam's hand by
    /// <see cref="PistolViewModel"/>.
    /// </summary>
    public class Sidearm : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Transform aim;               // the camera
        [SerializeField] Transform muzzle;            // the pistol's barrel tip, for the tracer
        [SerializeField] float range = 50f;
        [SerializeField] float damage = 32f;
        [SerializeField] float noiseRadius = 20f;
        [SerializeField] LineRenderer tracer;
        [SerializeField] Light flash;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip shotClip;
        [SerializeField] AudioClip reloadClip;

        public WeaponModel Model { get; } = new WeaponModel(magazine: 12, reserve: 48, maxReserve: 96, reloadSeconds: 1.3f, shotInterval: 0.18f);
        public bool Equipped => inventory != null && inventory.HasSidearm;
        public int ShotsFired { get; private set; }

        /// <summary>Raised on every shot (the view model kicks).</summary>
        public event Action Fired;

        float fxTimer;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (aim == null && Camera.main != null) aim = Camera.main.transform;
            ShowFx(false);
        }

        void Update()
        {
            Model.Tick(Time.deltaTime);
            if (fxTimer > 0f && (fxTimer -= Time.deltaTime) <= 0f) ShowFx(false);
            if (!Equipped || input == null) return;
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
            if (audioSource != null && reloadClip != null) audioSource.PlayOneShot(reloadClip, 0.6f);
            return true;
        }

        /// <summary>Fires one round if ready. Public so tests and scripted moments can fire.</summary>
        public bool Fire()
        {
            if (!Model.TryFire()) return false;
            ShotsFired++;
            Transform from = aim != null ? aim : transform;
            Vector3 end = from.position + from.forward * range;
            if (Physics.Raycast(from.position, from.forward, out var hit, range, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                end = hit.point;
                hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(new DamageInfo(damage, DamageType.Bolt, hit.point, gameObject));
            }

            bool vacuum = TryGetComponent<PlayerController>(out var pc) && pc.Environment != null && pc.Environment.isVacuum;
            VibrationSystem.Emit(new VibrationEvent(from.position, noiseRadius, VibrationMedium.Air, vacuum, gameObject));
            if (audioSource != null && shotClip != null) audioSource.PlayOneShot(shotClip);

            if (tracer != null)
            {
                tracer.SetPosition(0, muzzle != null ? muzzle.position : from.position + from.forward * 0.5f);
                tracer.SetPosition(1, end);
            }
            ShowFx(true);
            fxTimer = 0.05f;
            Fired?.Invoke();
            return true;
        }

        void ShowFx(bool on)
        {
            if (tracer != null) tracer.enabled = on;
            if (flash != null) flash.enabled = on;
        }
    }
}
