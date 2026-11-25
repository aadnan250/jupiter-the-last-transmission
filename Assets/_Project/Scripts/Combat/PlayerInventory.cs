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

        [SerializeField] InputReader input;
        [SerializeField] PlayerHealth health;
        [SerializeField] float medPatchHeal = 40f;
        [Tooltip("Levels where Sam is armed start with the bolt driver equipped.")]
        [SerializeField] bool startWithBoltDriver;
        [SerializeField] int startingMedPatches;

        public InventoryModel Model { get; } = new InventoryModel();
        public bool HasBoltDriver => Model.Weapon == BoltDriverId;

        void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (startWithBoltDriver) Model.EquipWeapon(BoltDriverId);
            if (startingMedPatches > 0) Model.Add(MedPatch, startingMedPatches);
        }

        void Update()
        {
            if (input != null && input.HealPressedThisFrame) UseMedPatch();
        }

        public bool UseMedPatch()
        {
            if (health == null || health.Model.IsDead || health.Model.Current >= health.Model.Max) return false;
            if (!Model.Consume(MedPatch)) return false;
            health.Model.Heal(medPatchHeal);
            return true;
        }
    }
}
