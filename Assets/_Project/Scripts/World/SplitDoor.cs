using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Spaceship door made of two leaves that slide apart when the player approaches, and close behind them.
    /// A status light shows green (unlocked) or red (locked). Locked doors stay shut and answer Interact with a
    /// denied sound; unlocked doors need no button, so they show no prompt.
    /// </summary>
    public class SplitDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform leftLeaf;
        [SerializeField] Transform rightLeaf;
        [Tooltip("How far each leaf travels (local X), in metres.")]
        [SerializeField] float travel = 0.72f;
        [SerializeField] float speed = 2.6f;
        [SerializeField] float autoOpenRange = 2.3f;
        [SerializeField] bool locked;

        [Header("Status light")]
        [SerializeField] Renderer statusLight;

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip openClip;
        [SerializeField] AudioClip closeClip;
        [SerializeField] AudioClip deniedClip;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Vector3 leftClosed, rightClosed;
        Transform player;
        bool open;
        bool heldOpen;

        public bool IsOpen => open;
        public bool IsLocked => locked;
        public string Prompt => "Locked";

        void Awake()
        {
            if (leftLeaf != null) leftClosed = leftLeaf.localPosition;
            if (rightLeaf != null) rightClosed = rightLeaf.localPosition;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            ApplyStatus();
        }

        /// <summary>Only locked doors are interactable (to tell the player they are locked).</summary>
        public bool CanInteract(GameObject interactor) => locked;

        public void Interact(GameObject interactor)
        {
            if (locked) Play(deniedClip);
        }

        /// <summary>Opens the door (scripts and tests). Returns false if locked.</summary>
        public bool Open()
        {
            if (locked) return false;
            heldOpen = true;
            SetOpen(true);
            return true;
        }

        public void Close()
        {
            heldOpen = false;
            SetOpen(false);
        }

        public void SetLocked(bool value)
        {
            locked = value;
            if (locked) { heldOpen = false; SetOpen(false); }
            ApplyStatus();
        }

        void Update()
        {
            if (!locked && !heldOpen && player != null)
            {
                Vector3 d = player.position - transform.position;
                d.y = 0f;
                bool near = d.sqrMagnitude < autoOpenRange * autoOpenRange;
                if (near != open) SetOpen(near);
            }

            float t = speed * Time.deltaTime;
            if (leftLeaf != null)
                leftLeaf.localPosition = Vector3.MoveTowards(leftLeaf.localPosition, leftClosed + (open ? Vector3.left * travel : Vector3.zero), t);
            if (rightLeaf != null)
                rightLeaf.localPosition = Vector3.MoveTowards(rightLeaf.localPosition, rightClosed + (open ? Vector3.right * travel : Vector3.zero), t);
        }

        void SetOpen(bool value)
        {
            if (open == value) return;
            open = value;
            Play(open ? openClip : closeClip);
        }

        void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        void ApplyStatus()
        {
            if (statusLight == null) return;
            var c = locked ? new Color(1f, 0.12f, 0.08f) : new Color(0.25f, 1f, 0.4f);
            var block = new MaterialPropertyBlock();
            statusLight.GetPropertyBlock(block);
            block.SetColor(EmissionId, c * 3f);
            block.SetColor(BaseColorId, c * 0.5f);
            statusLight.SetPropertyBlock(block);
        }
    }
}
