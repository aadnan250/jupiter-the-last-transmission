using Jupiter.Core;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Chapter 1's launch. When Sam reaches the tower (<see cref="triggerFlag"/>), the screen cuts to black and he is
    /// strapped into the capsule at the top of the stack. The countdown plays on the crew channel; the capsule shakes
    /// harder and harder to liftoff and staging, then goes quiet at engine cut-off. The level's transition takes it
    /// from there once the launch radio has finished.
    /// </summary>
    public class LaunchSequence : MonoBehaviour
    {
        [SerializeField] string triggerFlag = "at_tower";
        [SerializeField] Transform seat;                 // where Sam sits in the capsule
        [SerializeField] TextAsset launchRadio;
        [SerializeField] float fadeSeconds = 1f;
        [SerializeField] float liftoffAt = 16f;         // seconds after the countdown starts
        [SerializeField] float cutoffAt = 30f;
        [SerializeField] AudioSource rumble;
        [SerializeField] DialogueManager dialogue;

        ScreenFade fade;
        PlayerController player;
        CameraShake shake;
        float t = -1f;
        bool moved, radioStarted;

        public bool Started => t >= 0f;
        public bool InCapsule => moved;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            player = FindAnyObjectByType<PlayerController>();
            if (player != null) shake = player.GetComponentInChildren<CameraShake>();
            fade = gameObject.AddComponent<ScreenFade>();
        }

        void Update()
        {
            if (dialogue == null) return;
            if (!Started)
            {
                if (dialogue.Story.HasFlag(triggerFlag)) Begin();
                return;
            }
            t += Time.deltaTime;

            // Cut to black, move Sam into his seat, cut back.
            if (!moved)
            {
                fade.Alpha = t / fadeSeconds;
                if (t >= fadeSeconds) MoveToSeat();
                return;
            }
            float since = t - fadeSeconds;
            fade.Alpha = 1f - since / fadeSeconds;

            if (!radioStarted && since > fadeSeconds && launchRadio != null && !dialogue.IsActive)
            {
                radioStarted = true;
                var conv = CrewMember.Load(launchRadio);
                dialogue.PlayOnTheMove(conv, () => dialogue.Story.MarkCompleted(conv.Id));
            }

            // Engines: a growing tremble, a violent climb, then the silence of engine cut-off.
            float strength = since < liftoffAt ? Mathf.InverseLerp(liftoffAt - 6f, liftoffAt, since) * 0.35f
                           : since < cutoffAt ? 0.75f : 0f;
            if (shake != null && strength > 0.01f) shake.Shake(strength, 0.2f, 0.4f);
            if (rumble != null)
            {
                if (strength > 0.01f && !rumble.isPlaying) rumble.Play();
                rumble.volume = strength;
                if (strength <= 0.01f && rumble.isPlaying && since > cutoffAt) rumble.Stop();
            }
        }

        /// <summary>Starts the launch now. Public for tests.</summary>
        public void Begin()
        {
            if (Started) return;
            t = 0f;
        }

        void MoveToSeat()
        {
            moved = true;
            if (player == null || seat == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(0f, seat.eulerAngles.y, 0f));
            if (cc != null) cc.enabled = true;
        }
    }
}
