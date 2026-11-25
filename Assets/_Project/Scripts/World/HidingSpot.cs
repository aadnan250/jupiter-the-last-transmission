using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// A locker (or any space) Sam can hide in. Interact to climb in; interact again, looking at the door from
    /// inside, to climb out. The door has a vent slit at eye height so the player can watch what is outside.
    /// A trigger collider over the door makes it targetable from both sides without blocking that view.
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        [Tooltip("Where Sam's feet stand while hidden, facing out through the door (+Z).")]
        [SerializeField] Transform inside;
        [Tooltip("Where Sam stands after climbing out.")]
        [SerializeField] Transform exit;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip doorClip;

        public Transform Inside => inside != null ? inside : transform;
        public Transform Exit => exit != null ? exit : transform;

        public string Prompt => occupant != null ? "Leave" : "Hide";

        PlayerConcealment occupant;

        public bool CanInteract(GameObject interactor)
        {
            var c = interactor != null ? interactor.GetComponent<PlayerConcealment>() : null;
            if (c == null) return false;
            return c.IsHidden ? ReferenceEquals(c.Spot, this) : occupant == null;
        }

        public void Interact(GameObject interactor)
        {
            var c = interactor.GetComponent<PlayerConcealment>();
            if (c == null) return;
            if (c.IsHidden) Leave(c);
            else Enter(c);
        }

        public bool Enter(PlayerConcealment c)
        {
            if (occupant != null || !c.Hide(this, Inside.position, Inside.rotation)) return false;
            occupant = c;
            PlayDoor();
            return true;
        }

        public bool Leave(PlayerConcealment c)
        {
            if (!ReferenceEquals(occupant, c) || !c.Leave(Exit.position, Exit.rotation)) return false;
            occupant = null;
            PlayDoor();
            return true;
        }

        void PlayDoor()
        {
            if (audioSource != null && doorClip != null) audioSource.PlayOneShot(doorClip, 0.8f);
        }
    }
}
