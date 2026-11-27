using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// The player's carried items (<see cref="InventoryModel"/>). Using a med-patch (H, or D-pad up) heals 40.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public const string MedPatch = "med_patch";
        public const string BoltDriverId = "bolt_driver";
        /// <summary>Mara's flare pistol lives in the tool slot; its flares are consumables (3 per slot).</summary>
        public const string FlarePistolId = "flare_pistol";
        /// <summary>The arc beam rifle (weapon slot), used on the surface.</summary>
        public const string BeamRifleId = "beam_rifle";
        /// <summary>Sam's pistol (weapon slot), carried inside the base.</summary>
        public const string SidearmId = "sidearm";
        public const string Flare = "flare";

        [SerializeField] InputReader input;
        [SerializeField] PlayerHealth health;
        [SerializeField] float medPatchHeal = 40f;
        [Tooltip("Levels where Sam is armed start with the bolt driver equipped.")]
        [SerializeField] bool startWithBoltDriver;
        [Tooltip("Levels where the landing party is armed start with the beam rifle (overrides the bolt driver).")]
        [SerializeField] bool startWithBeamRifle;
        [Tooltip("Start with the pistol (the base).")]
        [SerializeField] bool startWithSidearm;
        [SerializeField] int startingMedPatches;

        public InventoryModel Model { get; } = new InventoryModel();
        public bool HasBoltDriver => Model.Weapon == BoltDriverId;
        public bool HasFlarePistol => Model.Tool == FlarePistolId;
        public bool HasBeamRifle => Model.Weapon == BeamRifleId;
        public bool HasSidearm => Model.Weapon == SidearmId;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (startWithBoltDriver) Model.EquipWeapon(BoltDriverId);
            if (startWithBeamRifle) Model.EquipWeapon(BeamRifleId);
            if (startWithSidearm) Model.EquipWeapon(SidearmId);
            if (startingMedPatches > 0) Model.Add(MedPatch, startingMedPatches);
        }

        void Update()
        {
            if (input != null && input.HealPressedThisFrame) UseMedPatch();
        }

        public bool UseMedPatch()
        {
            if (health == null || health.Model.IsDead) return false;
            var suit = GetComponent<PlayerSuit>();
            bool suitNeedsIt = suit != null && suit.NeedsSeal;
            if (health.Model.Current >= health.Model.Max && !suitNeedsIt) return false;
            if (!Model.Consume(MedPatch)) return false;
            health.Model.Heal(medPatchHeal);
            if (suit != null) suit.Seal();   // the patch kit carries suit sealant too
            return true;
        }
    }
}
