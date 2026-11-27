using Jupiter.Combat;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// One of the comms junction's three relays (Chapter 4). During the blackout, resetting the faulty one (the
    /// readout shows which) restores the ship; resetting a good one throws a spark that bites through the glove.
    /// </summary>
    public class RelayPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] int number = 1;
        [SerializeField] bool faulty;
        [SerializeField] BlackoutDirector director;
        [SerializeField] Light sparkLight;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip sparkClip;
        [SerializeField] AudioClip resetClip;

        float sparkTimer;

        public bool Faulty => faulty;
        public string Prompt => $"Reset relay {number}";

        void Awake()
        {
            if (director == null) director = FindAnyObjectByType<BlackoutDirector>();
            if (sparkLight != null) sparkLight.enabled = false;
        }

        public bool CanInteract(GameObject interactor) => director != null && director.BlackedOut && !director.Restored;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (faulty)
            {
                if (audioSource != null && resetClip != null) audioSource.PlayOneShot(resetClip);
                director.Restore();
                return;
            }
            // The wrong one: live, and it lets Sam know.
            if (audioSource != null && sparkClip != null) audioSource.PlayOneShot(sparkClip);
            if (sparkLight != null) { sparkLight.enabled = true; sparkTimer = 0.12f; }
            interactor.GetComponent<IDamageable>()?.TakeDamage(new DamageInfo(8f, DamageType.Melee, transform.position, gameObject));
            var shake = interactor.GetComponentInChildren<CameraShake>();
            if (shake != null) shake.Shake(0.4f, 0.2f, 0.3f);
        }

        void Update()
        {
            if (sparkTimer > 0f && (sparkTimer -= Time.deltaTime) <= 0f && sparkLight != null) sparkLight.enabled = false;
        }
    }
}
