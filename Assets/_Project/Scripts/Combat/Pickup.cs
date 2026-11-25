using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>A hand-placed supply: bolts for the bolt driver or a med-patch. Interact to take it.</summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        public enum Kind { Bolts, MedPatch }

        [SerializeField] Kind kind = Kind.Bolts;
        [SerializeField] int amount = 6;

        public Kind ItemKind => kind;
        public string Prompt => kind == Kind.Bolts ? $"Take bolts ({amount})" : "Take med-patch";

        public bool CanInteract(GameObject interactor) => interactor != null;

        public void Interact(GameObject interactor)
        {
            bool taken = false;
            if (kind == Kind.Bolts)
            {
                var driver = interactor.GetComponent<BoltDriver>();
                taken = driver != null && driver.Model.AddAmmo(amount) > 0;
            }
            else
            {
                var inv = interactor.GetComponent<PlayerInventory>();
                taken = inv != null && inv.Model.Add(PlayerInventory.MedPatch, amount) > 0;
            }
            if (taken) Destroy(gameObject);
        }

        public void Configure(Kind k, int n) { kind = k; amount = n; }
    }
}
