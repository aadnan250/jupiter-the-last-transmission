using Jupiter.Creatures;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Mara's flare pistol (T, or D-pad down). A utility, not a weapon: it fires a burning flare that draws
    /// creatures to it with heat and light and dazzles one that gets close. The pop is a 10 m airborne noise,
    /// so it also gives away where it was fired from. Needs the pistol in the tool slot and a flare to fire.
    /// </summary>
    public class FlarePistol : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Transform muzzle;           // usually the camera
        [SerializeField] float launchSpeed = 16f;
        [SerializeField] float burnSeconds = 25f;
        [SerializeField] Material flareMaterial;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip fireClip;

        CharacterController controller;

        public BurningFlare LastFlare { get; private set; }
        public int FlaresFired { get; private set; }

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (muzzle == null && Camera.main != null) muzzle = Camera.main.transform;
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            if (input != null && input.FlarePressedThisFrame) Fire();
        }

        /// <summary>Fires one flare if Sam has the pistol and a flare. Public for scripted sequences and tests.</summary>
        public bool Fire()
        {
            if (inventory == null || !inventory.HasFlarePistol) return false;
            if (!inventory.Model.Consume(PlayerInventory.Flare)) return false;

            Transform from = muzzle != null ? muzzle : transform;
            LastFlare = BurningFlare.Spawn(from.position + from.forward * 0.5f, from.forward * launchSpeed,
                flareMaterial, burnSeconds, controller);
            FlaresFired++;

            bool vacuum = TryGetComponent<PlayerController>(out var pc) && pc.Environment != null && pc.Environment.isVacuum;
            VibrationSystem.Emit(new VibrationEvent(from.position, StealthRules.FlareShotRadius, VibrationMedium.Air, vacuum, gameObject));
            if (audioSource != null && fireClip != null) audioSource.PlayOneShot(fireClip, 0.8f);
            return true;
        }
    }
}
