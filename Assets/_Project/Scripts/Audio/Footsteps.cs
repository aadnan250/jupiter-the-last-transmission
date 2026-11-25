using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Audio
{
    /// <summary>
    /// Plays footsteps based on distance actually travelled on the ground, so step rate always
    /// matches movement speed. Running is louder, crouching is near-silent, and landing plays a heavier step.
    /// Each step is also a ground vibration event that creatures can hear (walk 3 m, run 9 m, crouch 0.8 m).
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
        const float TeleportDistance = 0.5f;
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
            // A jump this large in one frame is a teleport (climbing into a locker, a scripted move), not a step.
            if (delta.sqrMagnitude > TeleportDistance * TeleportDistance) { travelled = 0f; return; }

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
                EmitStep(player.State == MoveState.Run ? runNoise : player.State == MoveState.Crouch ? crouchNoise : walkNoise);
            }
        }

        [Header("Noise radius heard by creatures (m)")]
        [SerializeField] float walkNoise = 3f;
        [SerializeField] float runNoise = 9f;
        [SerializeField] float crouchNoise = 0.8f;
        [SerializeField] float landNoise = 5f;

        /// <summary>Footsteps are contact vibrations: they travel through the floor, so they carry even in vacuum.</summary>
        void EmitStep(float radius)
        {
            bool vacuum = player.Environment != null && player.Environment.isVacuum;
            Creatures.VibrationSystem.Emit(new Creatures.VibrationEvent(player.transform.position, radius,
                Creatures.VibrationMedium.Ground, vacuum, player.gameObject));
        }

        void Play(float volume)
        {
            if (volume >= landVolume) EmitStep(landNoise);
            // Never repeat the same clip twice in a row.
            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == lastClip) i = (i + 1) % clips.Length;
            lastClip = i;
            source.pitch = Random.Range(0.93f, 1.07f);
            source.PlayOneShot(clips[i], volume);
        }
    }
}
