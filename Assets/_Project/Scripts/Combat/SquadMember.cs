using System.Collections.Generic;
using Jupiter.Creatures;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.Combat
{
    /// <summary>
    /// A suited crew member on the surface. Until released they hold their place (in the lander); then they keep
    /// a formation slot around Sam and walk with him. When Stalkers are up and in range they stop, shoulder their
    /// rifle and fire short beam bursts at the nearest one. They can be hit and knocked about but do not die here:
    /// the story still needs them.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class SquadMember : MonoBehaviour, IDamageable
    {
        static readonly List<SquadMember> all = new List<SquadMember>();
        public static IReadOnlyList<SquadMember> All => all;

        [SerializeField] string characterId = "MARA";
        [SerializeField] Vector3 formationOffset = new Vector3(2.5f, 0f, -1.5f);   // relative to Sam, in his frame
        [SerializeField] SuitAnimator anim;
        [SerializeField] Transform muzzle;
        [SerializeField] LineRenderer beam;
        [SerializeField] float range = 32f;
        [SerializeField] float damagePerSecond = 45f;
        [SerializeField] float burstSeconds = 1.1f;
        [SerializeField] float pauseSeconds = 0.7f;
        [SerializeField] AudioSource gunAudio;

        NavMeshAgent agent;
        Transform player;
        StalkerAI target;
        float burstClock, retarget;

        public string CharacterId => characterId;
        public bool Released { get; private set; }
        public bool Firing { get; private set; }
        public float DamageTaken { get; private set; }
        public StalkerAI CurrentTarget => target;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (anim == null) anim = GetComponentInChildren<SuitAnimator>();
            if (beam != null) beam.enabled = false;
        }

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        void Start()
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) player = p.transform;
        }

        /// <summary>Lets them leave their place and follow Sam.</summary>
        public void Release() { Released = true; holdPoint = null; }

        /// <summary>Sends them to a spot to work (Hana at the drill) and keeps them there. They still fight from it.</summary>
        public void HoldAt(Vector3 point) { Released = true; holdPoint = point; }

        /// <summary>Back to following Sam.</summary>
        public void ResumeFollowing() => holdPoint = null;

        /// <summary>Gone: taken into the ice, or down and not getting up. They leave the squad for good.</summary>
        public void Lose()
        {
            Lost = true;
            SetBeam(false);
            gameObject.SetActive(false);
        }

        public bool Lost { get; private set; }
        Vector3? holdPoint;

        void Update()
        {
            float dt = Time.deltaTime;
            if ((retarget -= dt) <= 0f) { retarget = 0.4f; target = NearestThreat(); }

            if (target != null)
            {
                Fight(dt);
            }
            else
            {
                SetBeam(false);
                Firing = false;
                if (anim != null) anim.Aiming = false;
                if (Released && holdPoint.HasValue && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = 3.2f;
                    if (Vector3.Distance(transform.position, holdPoint.Value) > 0.8f) agent.SetDestination(holdPoint.Value);
                }
                else if (Released && player != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    Vector3 slot = player.position + Quaternion.Euler(0f, player.eulerAngles.y, 0f) * formationOffset;
                    float far = Vector3.Distance(transform.position, slot);
                    agent.speed = far > 6f ? 5.5f : 2.8f;
                    if (far > 0.8f) agent.SetDestination(slot);
                }
            }
            if (anim != null) anim.Speed = agent.enabled ? agent.velocity.magnitude : 0f;
        }

        void Fight(float dt)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (anim != null) anim.Aiming = true;
            Vector3 aimPoint = target.transform.position + Vector3.up * 1.1f;
            Vector3 to = aimPoint - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 360f * dt);

            burstClock += dt;
            float cycle = burstSeconds + pauseSeconds;
            if (burstClock > cycle) burstClock -= cycle;
            bool on = burstClock < burstSeconds && Vector3.Angle(transform.forward, to) < 25f;
            Firing = on;
            SetBeam(on);
            if (!on) return;

            Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f;
            // They are good shots, not perfect ones: a little drift keeps the beams looking alive.
            Vector3 drift = new Vector3(Mathf.PerlinNoise(Time.time * 2f, 3f) - 0.5f, Mathf.PerlinNoise(5f, Time.time * 2f) - 0.5f, 0f) * 0.5f;
            Vector3 end = aimPoint + drift;
            target.TakeDamage(new DamageInfo(damagePerSecond * dt, DamageType.Beam, end, gameObject));
            if (beam != null)
            {
                beam.SetPosition(0, from);
                beam.SetPosition(1, end);
            }
        }

        StalkerAI NearestThreat()
        {
            StalkerAI best = null;
            float bestD = range;
            foreach (var s in StalkerAI.All)
            {
                if (s == null || !s.Active) continue;
                float d = Vector3.Distance(s.transform.position, transform.position);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        void SetBeam(bool on)
        {
            if (beam != null && beam.enabled != on) beam.enabled = on;
            if (gunAudio != null)
            {
                if (on && !gunAudio.isPlaying) gunAudio.Play();
                else if (!on && gunAudio.isPlaying) gunAudio.Stop();
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            DamageTaken += info.Amount;
            if (anim != null) anim.Flinch();
        }
    }
}
