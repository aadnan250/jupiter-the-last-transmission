using System.Collections.Generic;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.World
{
    /// <summary>
    /// The surface ambush. Once the landing party is far enough from the lander (or <see cref="Begin"/> is called),
    /// the moon quakes: the camera shakes, the controller rumbles, fissures glow, and the crew panic on the radio
    /// while the player keeps control. Then the Stalkers burst out of the ice around the squad, one after another.
    /// When the last one dies, it sets <see cref="clearedFlag"/> and the radio sends everyone to the base.
    /// </summary>
    public class AmbushDirector : MonoBehaviour
    {
        [SerializeField] Transform lander;
        [SerializeField] float triggerDistance = 45f;
        [SerializeField] string requiresFlag = "ramp_open";
        [SerializeField] string startedFlag = "quake_started";
        [SerializeField] string clearedFlag = "ambush_cleared";
        [SerializeField] StalkerAI[] stalkers;
        [SerializeField] GameObject[] fissures;
        [SerializeField] float nearRadius = 13f;
        [SerializeField] float farRadius = 26f;
        [SerializeField] TextAsset panicRadio;
        [SerializeField] TextAsset clearedRadio;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip quakeClip;
        [SerializeField] DialogueManager dialogue;

        PlayerController player;
        TextAsset pendingRadio;
        CameraShake shake;
        float t = -1f;
        int emerged;
        readonly HashSet<StalkerAI> dead = new HashSet<StalkerAI>();

        public bool Started => t >= 0f;
        public bool Cleared { get; private set; }
        public int Emerged => emerged;
        public int Remaining => stalkers.Length - dead.Count;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            player = FindAnyObjectByType<PlayerController>();
            if (player != null) shake = player.GetComponentInChildren<CameraShake>();
            foreach (var f in fissures) if (f != null) f.SetActive(false);
            StalkerAI.Killed += OnKilled;
        }

        void OnDestroy() => StalkerAI.Killed -= OnKilled;

        void Update()
        {
            if (pendingRadio != null && dialogue != null && !dialogue.IsActive) { var r = pendingRadio; pendingRadio = null; PlayRadio(r); }
            if (Cleared) return;
            if (!Started)
            {
                var story = dialogue != null ? dialogue.Story : null;
                if (story == null || player == null) return;
                if (!string.IsNullOrEmpty(requiresFlag) && !story.HasFlag(requiresFlag)) return;
                if (lander != null && Vector3.Distance(Flat(player.transform.position), Flat(lander.position)) < triggerDistance) return;
                Begin();
                return;
            }

            float previous = t;
            t += Time.deltaTime;
            if (previous < AmbushTimeline.RadioAt && t >= AmbushTimeline.RadioAt) PlayRadio(panicRadio);
            while (emerged < stalkers.Length && t >= AmbushTimeline.EmergeTime(emerged)) EmergeNext();
        }

        /// <summary>Starts the quake now. Public for tests and scripted moments.</summary>
        public void Begin()
        {
            if (Started) return;
            t = 0f;
            if (dialogue != null && !string.IsNullOrEmpty(startedFlag)) dialogue.Story.SetFlag(startedFlag);
            if (shake != null) shake.Shake(0.85f, AmbushTimeline.QuakeSeconds - 1.5f, 1.5f);
            if (audioSource != null && quakeClip != null) audioSource.PlayOneShot(quakeClip);
            foreach (var f in fissures) if (f != null) f.SetActive(true);
            VibrationSystem.Emit(new VibrationEvent(Centre(), 60f, VibrationMedium.Ground, true, gameObject));
        }

        void EmergeNext()
        {
            var s = stalkers[emerged];
            Vector3 centre = Centre();
            Vector3 point = centre + AmbushTimeline.EmergeOffset(emerged, stalkers.Length, nearRadius, farRadius);
            if (NavMesh.SamplePosition(point, out var hit, 6f, NavMesh.AllAreas)) point = hit.position;
            if (s != null) s.Emerge(point, centre);
            emerged++;
        }

        /// <summary>Where the squad is: Sam and his crew's average position.</summary>
        Vector3 Centre()
        {
            Vector3 sum = player != null ? player.transform.position : transform.position;
            int n = 1;
            foreach (var m in Combat.SquadMember.All) { sum += m.transform.position; n++; }
            return sum / n;
        }

        void OnKilled(StalkerAI s)
        {
            if (System.Array.IndexOf(stalkers, s) < 0 || !dead.Add(s)) return;
            if (dead.Count < stalkers.Length) return;
            Cleared = true;
            if (dialogue != null && !string.IsNullOrEmpty(clearedFlag)) dialogue.Story.SetFlag(clearedFlag);
            PlayRadio(clearedRadio);
        }

        void PlayRadio(TextAsset radio)
        {
            if (radio == null || dialogue == null) return;
            if (dialogue.IsActive) { pendingRadio = radio; return; }  // after the current call ends
            var conv = CrewMember.Load(radio);
            dialogue.PlayOnTheMove(conv, () => dialogue.Story.MarkCompleted(conv.Id));
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
