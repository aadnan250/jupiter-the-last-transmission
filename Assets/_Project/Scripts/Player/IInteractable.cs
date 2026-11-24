using UnityEngine;

namespace Jupiter.Player
{
    /// <summary>
    /// Anything the player can use by looking at it and pressing Interact:
    /// doors, terminals, documents, valves, lockers.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short verb shown next to the crosshair, e.g. "Open", "Read".</summary>
        string Prompt { get; }

        bool CanInteract(GameObject interactor);

        void Interact(GameObject interactor);
    }
}
