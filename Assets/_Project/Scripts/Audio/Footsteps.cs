using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Audio
{
    /// <summary>
    /// Plays footsteps based on distance actually travelled on the ground, so step rate always
    /// matches movement speed. Running is louder, crouching is near-silent, and landing plays a heavier step.
    /// (Phase 5 will also emit these as vibration events for creature detection.)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Footsteps : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] AudioClip[] clips;

        [Header("Stride length (m)")]
        [SerializeField] float walkStride = 0.8f;
        [SerializeField] float runStride = 1.2f;
        [SerializeField] float crouchStride = 0.6f;

        [Header("Volume")]
        [SerializeField] float walkVolume = 0.22f;
        [SerializeField] float runVolume = 0.38f;
        [SerializeField] float crouchVolume = 0.06f;
        [SerializeField] float landVolume = 0.4f;

        AudioSource source;
        Vector3 lastPosition;
        float travelled;
        bool wasGrounded = true;
        int lastClip = -1;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // the player's own feet: always centred
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        void Start() => lastPosition = transform.position;

        void Update()
        {
            if (player == null || clips == null || clips.Length == 0) return;

            Vector3 pos = transform.position;
            Vector3 delta = pos - lastPosition;
            lastPosition = pos;

            bool grounded = player.IsGrounded;
            if (grounded && !wasGrounded) { Play(landVolume); travelled = 0f; }
            wasGrounded = grounded;
            if (!grounded) return;

            travelled += new Vector2(delta.x, delta.z).magnitude;

            float stride, volume;
            switch (player.State)
            {
                case MoveState.Run: stride = runStride; volume = runVolume; break;
                case MoveState.Crouch: stride = crouchStride; volume = crouchVolume; break;
                default: stride = walkStride; volume = walkVolume; break;
            }

            if (travelled >= stride)
            {
                travelled -= stride;
                Play(volume);
            }
        }

        void Play(float volume)
        {
            // Never repeat the same clip twice in a row.
            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == lastClip) i = (i + 1) % clips.Length;
            lastClip = i;
            source.pitch = Random.Range(0.93f, 1.07f);
            source.PlayOneShot(clips[i], volume);
        }
    }
}
