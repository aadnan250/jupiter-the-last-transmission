using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// A hand-placed supply: the bolt driver itself, bolts for it, a med-patch, Mara's flare pistol (with its flares) or
    /// spare flares. Interact to take it.
    /// </summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        // Appended only: the value is serialized as a number in the generated scenes.
        public enum Kind { Bolts, MedPatch, FlarePistol, Flares, BoltDriver }

        [SerializeField] Kind kind = Kind.Bolts;
        [SerializeField] int amount = 6;

        public Kind ItemKind => kind;

        public string Prompt
        {
            get
            {
                switch (kind)
                {
                    case Kind.Bolts: return $"Take bolts ({amount})";
                    case Kind.MedPatch: return "Take med-patch";
                    case Kind.FlarePistol: return "Take flare pistol";
                    case Kind.BoltDriver: return "Take bolt driver";
                    default: return amount == 1 ? "Take flare" : $"Take flares ({amount})";
                }
            }
        }

        public bool CanInteract(GameObject interactor) => interactor != null;

        public void Interact(GameObject interactor)
        {
            bool taken = false;
            var inv = interactor.GetComponent<PlayerInventory>();
            switch (kind)
            {
                case Kind.Bolts:
                    var driver = interactor.GetComponent<BoltDriver>();
                    taken = driver != null && driver.Model.AddAmmo(amount) > 0;
                    break;
                case Kind.MedPatch:
                    taken = inv != null && inv.Model.Add(PlayerInventory.MedPatch, amount) > 0;
                    break;
                case Kind.FlarePistol:
                    if (inv == null) break;
                    inv.Model.EquipTool(PlayerInventory.FlarePistolId);
                    inv.Model.Add(PlayerInventory.Flare, amount);
                    taken = true; // the pistol itself always fits the tool slot
                    break;
                case Kind.BoltDriver:
                    if (inv == null || inv.HasBoltDriver) break;
                    inv.Model.EquipWeapon(PlayerInventory.BoltDriverId);
                    taken = true;
                    break;
                case Kind.Flares:
                    taken = inv != null && inv.Model.Add(PlayerInventory.Flare, amount) > 0;
                    break;
            }
            if (taken) Destroy(gameObject);
        }

        public void Configure(Kind k, int n) { kind = k; amount = n; }
    }
}
