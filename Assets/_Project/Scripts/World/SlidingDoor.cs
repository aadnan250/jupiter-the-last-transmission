using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Pneumatic sliding door. Interact to open or close. The moving panel is a child transform
    /// that slides along its local X axis. Plays optional open/close sounds from an AudioSource on the door.
    /// Locked doors refuse to open (power and keycard logic arrives with the puzzle system).
    /// </summary>
    public class SlidingDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform panel;
        [SerializeField] Vector3 openOffset = new Vector3(1.6f, 0f, 0f);
        [SerializeField] float speed = 2.5f;
        [SerializeField] bool startsOpen;
        [SerializeField] bool locked;

        [Header("Audio (optional)")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip openClip;
        [SerializeField] AudioClip closeClip;
        [SerializeField] AudioClip lockedClip;

        Vector3 closedLocalPos;
        bool open;

        public bool IsOpen => open;
        public bool IsLocked => locked;
        public string Prompt => locked ? "Locked" : (open ? "Close" : "Open");

        void Awake()
        {
            if (panel == null && transform.childCount > 0) panel = transform.GetChild(0);
            if (panel != null) closedLocalPos = panel.localPosition;
            open = startsOpen;
            if (panel != null && open) panel.localPosition = closedLocalPos + openOffset;
        }

        public bool CanInteract(GameObject interactor) => panel != null;

        public void Interact(GameObject interactor)
        {
            if (locked)
            {
                PlayClip(lockedClip);
                return;
            }
            open = !open;
            PlayClip(open ? openClip : closeClip);
        }

        public void SetLocked(bool value) => locked = value;

        void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        void Update()
        {
            if (panel == null) return;
            Vector3 target = open ? closedLocalPos + openOffset : closedLocalPos;
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, speed * Time.deltaTime);
        }
    }
}
