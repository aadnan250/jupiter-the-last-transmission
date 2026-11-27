using System.Collections.Generic;
using Jupiter.Combat;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.World
{
    public enum SurfaceBeat { Ambush, ToTiamat, AtTiamat, Drilling, Boss, Aftermath, ToAscent, Siege, Boarding, Done }

    /// <summary>
    /// The story on Europa's surface after the ambush (the old base chapters, told in the open):
    /// to Tiamat's lander; its cargo manifest exposes Directive 7; Hana drills the sealed borehole while the squad
    /// holds off Stalkers; the bore erupts, the Listener climbs out, Hana is taken and Ashford goes down; once the
    /// Listener is dead, Luca's last transmission and Ashford's confession (with the ascent codes) play; then the run
    /// to the ascent vehicle. Every radio call plays on the move, one after another. Progress is story flags, so
    /// objectives and tests can follow it.
    /// </summary>
    public class SurfaceStoryDirector : MonoBehaviour
    {
        [Header("Story flags")]
        [SerializeField] string clearedFlag = "ambush_cleared";
        [SerializeField] string reachedFlag = "reached_tiamat";
        [SerializeField] string manifestFlag = "read_manifest";
        [SerializeField] string drillDoneFlag = "drill_done";
        [SerializeField] string eruptionFlag = "site_eruption";
        [SerializeField] string killedFlag = "listener_killed";
        [SerializeField] string codeFlag = "knows_ascent_code";
        [SerializeField] string unlockedFlag = "ascent_unlocked";

        [Header("Radio (on the move)")]
        [SerializeField] TextAsset arriveRadio;
        [SerializeField] TextAsset manifestRadio;
        [SerializeField] TextAsset drillRadio;
        [SerializeField] TextAsset eruptionRadio;
        [SerializeField] TextAsset lucaRadio;
        [SerializeField] TextAsset ashfordRadio;
        [SerializeField] TextAsset ascentRadio;
        [SerializeField] TextAsset halcyonRadio;
        [SerializeField] TextAsset maraRadio;
        [SerializeField] TextAsset aloneRadio;

        [Header("Chapter 8: the siege at Halcyon")]
        [SerializeField] SquadMember mara;
        [SerializeField] Transform halcyon;
        [SerializeField] StalkerAI[] siegeWave;
        [SerializeField] float siegeSeconds = 40f;
        [SerializeField] string maraLostFlag = "mara_lost";
        [SerializeField] string boardedFlag = "boarded_halcyon";

        [Header("The site")]
        [SerializeField] SquadMember hana;
        [SerializeField] SquadMember ashford;
        [SerializeField] Transform drillSpot;
        [SerializeField] DrillRig drill;
        [SerializeField] Transform borehole;
        [SerializeField] float drillSeconds = 36f;
        [SerializeField] StalkerAI[] drillWave;
        [SerializeField] GameObject[] eruptionRime;
        [SerializeField] CreatureAI listener;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip quakeClip;
        [SerializeField] DialogueManager dialogue;

        readonly Queue<TextAsset> radio = new Queue<TextAsset>();
        StoryState story;
        PlayerController player;
        float drillT;
        int waveSent;
        bool alerted;

        public SurfaceBeat Beat { get; private set; } = SurfaceBeat.Ambush;
        public float DrillSeconds { get => drillSeconds; set => drillSeconds = value; }
        public float DrillProgress => Mathf.Clamp01(drillT / drillSeconds);

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            story = dialogue != null ? dialogue.Story : null;
            player = FindAnyObjectByType<PlayerController>();
            foreach (var r in eruptionRime) if (r != null) r.SetActive(false);
        }

        void Update()
        {
            if (story == null) return;
            PlayQueuedRadio();

            switch (Beat)
            {
                case SurfaceBeat.Ambush:
                    if (story.HasFlag(clearedFlag)) Beat = SurfaceBeat.ToTiamat;
                    break;

                case SurfaceBeat.ToTiamat:
                    if (!story.HasFlag(reachedFlag)) break;
                    Queue(arriveRadio);
                    Beat = SurfaceBeat.AtTiamat;
                    break;

                case SurfaceBeat.AtTiamat:
                    if (story.HasFlag(manifestFlag)) BeginDrilling();
                    break;

                case SurfaceBeat.Drilling:
                    Drill(Time.deltaTime);
                    break;

                case SurfaceBeat.Boss:
                    if (listener != null && listener.isActiveAndEnabled && !alerted && player != null)
                    {
                        // It comes up already knowing where the drill was, and who was standing by it.
                        alerted = true;
                        listener.Alert(player.transform.position, 0.9f);
                    }
                    if (listener != null && listener.Health != null && listener.Health.Model.Condition == CreatureCondition.Dead)
                    {
                        story.SetFlag(killedFlag);
                        Queue(lucaRadio);
                        Queue(ashfordRadio);
                        Beat = SurfaceBeat.Aftermath;
                    }
                    break;

                case SurfaceBeat.Aftermath:
                    if (story.HasFlag(codeFlag)) Beat = SurfaceBeat.ToAscent;
                    break;

                case SurfaceBeat.ToAscent:
                    if (story.HasFlag(unlockedFlag)) BeginSiege();
                    break;

                case SurfaceBeat.Siege:
                    Siege(Time.deltaTime);
                    break;

                case SurfaceBeat.Boarding:
                    if (story.HasFlag(boardedFlag)) Beat = SurfaceBeat.Done;
                    break;
            }
        }

        /// <summary>Ashford orders the borehole drilled: Hana goes to the drill and the squad covers her. Public for tests.</summary>
        public void BeginDrilling()
        {
            if (Beat >= SurfaceBeat.Drilling) return;
            Queue(manifestRadio);
            if (hana != null && drillSpot != null) hana.HoldAt(drillSpot.position);
            if (drill != null) drill.Set(true);
            drillT = 0f;
            waveSent = 0;
            Beat = SurfaceBeat.Drilling;
        }

        void Drill(float dt)
        {
            float before = drillT;
            drillT += dt;
            // The drill's thumping draws them: they come up around the site in a steady trickle.
            if (drillWave != null)
                while (waveSent < drillWave.Length && drillT >= (waveSent + 1) * drillSeconds / (drillWave.Length + 1))
                {
                    var s = drillWave[waveSent];
                    Vector3 centre = borehole != null ? borehole.position : transform.position;
                    Vector3 p = centre + AmbushTimeline.EmergeOffset(waveSent + 3, drillWave.Length, 14f, 22f);
                    if (NavMesh.SamplePosition(p, out var hit, 6f, NavMesh.AllAreas)) p = hit.position;
                    if (s != null) s.Emerge(p, centre);
                    waveSent++;
                }
            if (before < drillSeconds * 0.5f && drillT >= drillSeconds * 0.5f) Queue(drillRadio);
            if (drillT >= drillSeconds) Erupt();
        }

        /// <summary>The bore gives way and the Listener comes up. Public for tests.</summary>
        public void Erupt()
        {
            if (Beat >= SurfaceBeat.Boss) return;
            story.SetFlag(drillDoneFlag);
            if (drill != null) drill.Set(false);
            foreach (var r in eruptionRime) if (r != null) r.SetActive(true);
            if (audioSource != null && quakeClip != null) audioSource.PlayOneShot(quakeClip);
            var shake = player != null ? player.GetComponentInChildren<CameraShake>() : null;
            if (shake != null) shake.Shake(1f, 3f, 2f);
            story.SetFlag(eruptionFlag);           // the story gate lets the Listener into the scene
            if (hana != null) hana.Lose();        // dragged down the bore
            if (ashford != null) ashford.Lose();  // struck down beside it
            radio.Clear();
            Queue(eruptionRadio);
            Beat = SurfaceBeat.Boss;
        }

        // ------------------------------------------------------------------ Chapter 8: the last crew member

        float siegeT;
        int siegeSent;

        /// <summary>
        /// The hatch takes the code and starts to cycle; the ice under the pad heaves and they come for the rocket.
        /// Mara and Sam hold until it opens. Public for tests.
        /// </summary>
        public void BeginSiege()
        {
            if (Beat >= SurfaceBeat.Siege) return;
            siegeT = 0f;
            siegeSent = 0;
            Queue(halcyonRadio);
            var shake = player != null ? player.GetComponentInChildren<CameraShake>() : null;
            if (shake != null) shake.Shake(0.7f, 3f, 2f);
            if (audioSource != null && quakeClip != null) audioSource.PlayOneShot(quakeClip);
            Beat = SurfaceBeat.Siege;
        }

        public float SiegeSeconds { get => siegeSeconds; set => siegeSeconds = value; }

        void Siege(float dt)
        {
            siegeT += dt;
            Vector3 centre = halcyon != null ? halcyon.position : (player != null ? player.transform.position : transform.position);
            if (siegeWave != null)
                while (siegeSent < siegeWave.Length && siegeT >= 4f + siegeSent * (siegeSeconds - 10f) / Mathf.Max(1, siegeWave.Length))
                {
                    Vector3 p = centre + AmbushTimeline.EmergeOffset(siegeSent + 5, siegeWave.Length, 12f, 20f);
                    if (NavMesh.SamplePosition(p, out var hit, 6f, NavMesh.AllAreas)) p = hit.position;
                    if (siegeWave[siegeSent] != null) siegeWave[siegeSent].Emerge(p, centre);
                    siegeSent++;
                }
            if (siegeT < siegeSeconds) return;

            // The hatch opens. Mara is behind him, and then she isn't.
            if (mara != null) mara.Lose();
            radio.Clear();
            Queue(maraRadio);
            Queue(aloneRadio);
            Beat = SurfaceBeat.Boarding;
        }

        void Queue(TextAsset conversation)
        {
            if (conversation != null && !story.IsCompleted(conversation.name)) radio.Enqueue(conversation);
        }

        void PlayQueuedRadio()
        {
            if (radio.Count == 0 || dialogue == null || dialogue.IsActive) return;
            var next = radio.Dequeue();
            var conv = CrewMember.Load(next);
            dialogue.PlayOnTheMove(conv, () => story.MarkCompleted(conv.Id));
        }
    }
}
