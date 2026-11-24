using System;
using UnityEngine;

namespace Jupiter.Player
{
    /// <summary>
    /// Finds the <see cref="IInteractable"/> under the crosshair and uses it when Interact is pressed.
    /// Raises <see cref="TargetChanged"/> so UI can show or hide the prompt without polling.
    /// Lives on the camera so the ray matches exactly what the player sees.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] float range = 2.2f;
        [Tooltip("Layers that can block or hold interactables.")]
        [SerializeField] LayerMask mask = ~0;

        public IInteractable Current { get; private set; }

        /// <summary>Fired with the new target (or null) whenever the looked-at interactable changes.</summary>
        public event Action<IInteractable> TargetChanged;

        GameObject interactor;

        void Start()
        {
            if (input == null) input = GetComponentInParent<InputReader>();
            interactor = input != null ? input.gameObject : gameObject;
        }

        void Update()
        {
            IInteractable found = null;
            var ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
                if (found != null && !found.CanInteract(interactor)) found = null;
            }

            if (!ReferenceEquals(found, Current))
            {
                Current = found;
                TargetChanged?.Invoke(Current);
            }

            if (Current != null && input != null && input.InteractPressedThisFrame)
            {
                Current.Interact(interactor);
                TargetChanged?.Invoke(Current); // prompt text may have changed (e.g. Open -> Close)
            }
        }
    }
}
