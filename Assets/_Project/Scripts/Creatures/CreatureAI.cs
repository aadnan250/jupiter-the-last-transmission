using System.Collections.Generic;
using Jupiter.Combat;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.Creatures
{
    public enum CreatureState { Dormant, Listening, Investigating, Hunting, Attacking, Torpor, Recovering, Dead, Blinded }

    /// <summary>
    /// The Listener's behaviour. It hunts by vibration, not sight: vibrations raise a
    /// <see cref="SuspicionModel"/>, and suspicion thresholds drive a finite state machine:
    /// Dormant → Listening (kneels, palms on the floor) → Investigating (walks to where it *thinks* the noise was,
    /// with error growing with distance) → Hunting (direct pursuit) → Attacking.
    /// Damage can drop it into Torpor; it then Recovers angry, unless its nodes were destroyed (Dead).
    /// A burning flare lures it (heat and light) and dazzles it up close (Blinded: it forgets everything).
    /// It cannot see a player hidden in a locker, so it cannot attack one either: it can only listen.
    /// See docs/AI.md.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CreatureAI : MonoBehaviour, IVibrationListener
    {
        [SerializeField] CreatureHealth health;
        [SerializeField] ListenerVisual visual;
        [Tooltip("True when the creature stands on Europa's surface: it cannot hear airborne sound.")]
        [SerializeField] bool inVacuum;

        [Header("Movement")]
        [SerializeField] float investigateSpeed = 1.6f;
        [SerializeField] float huntSpeed = 4.2f;
        [SerializeField] float sourceErrorPerMetre = 0.15f;

        [Header("Timing")]
        [SerializeField] float listenSeconds = 1.2f;
        [SerializeField] float searchSeconds = 3f;
        [SerializeField] float recoverSeconds = 2f;

        [Header("Attack")]
        [SerializeField] float attackRange = 1.8f;
        [SerializeField] float attackDamage = 20f;
        [SerializeField] float attackCooldown = 1.6f;

        NavMeshAgent agent;
        Transform player;
        IDamageable playerDamage;
        PlayerConcealment concealment;
        readonly HashSet<BurningFlare> spentFlares = new HashSet<BurningFlare>();
        float stateTime, attackTimer;
        Vector3 investigateTarget;
        bool arrived;

        public CreatureState State { get; private set; } = CreatureState.Dormant;
        public SuspicionModel Suspicion { get; } = new SuspicionModel();
        public CreatureHealth Health => health;
        public NavMeshAgent Agent => agent;
        public Vector3 InvestigateTarget => investigateTarget;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (health == null) health = GetComponent<CreatureHealth>();
            if (visual == null) visual = GetComponentInChildren<ListenerVisual>();
        }

        void OnEnable() => VibrationSystem.Register(this);
        void OnDisable() => VibrationSystem.Unregister(this);

        void Start()
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null)
            {
                player = p.transform;
                playerDamage = p.GetComponent<IDamageable>();
                concealment = p.GetComponent<PlayerConcealment>();
            }
        }

        public void OnVibration(VibrationEvent e, float radiusMultiplier)
        {
            if (State == CreatureState.Dead || State == CreatureState.Torpor || State == CreatureState.Blinded) return;
            if (e.Source == gameObject) return;
            float intensity = VibrationMath.Perceived(e, transform.position, inVacuum, radiusMultiplier);
            Suspicion.Hear(intensity, e.Position);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            attackTimer = Mathf.Max(0f, attackTimer - dt);

            // Biology overrides behaviour.
            var condition = health != null ? health.Model.Condition : CreatureCondition.Active;
            if (condition == CreatureCondition.Dead) { Enter(CreatureState.Dead); return; }
            if (condition == CreatureCondition.Torpor) { Enter(CreatureState.Torpor); return; }
            if (State == CreatureState.Torpor) { Enter(CreatureState.Recovering); return; }

            Suspicion.Tick(dt);
            if (State == CreatureState.Blinded)
            {
                if (stateTime >= StealthRules.FlareBlindSeconds) Enter(CreatureState.Listening);
                return;
            }
            if (State != CreatureState.Recovering && ReactToFlares()) return;
            LookForPlayer();

            switch (State)
            {
                case CreatureState.Dormant:
                    if (Suspicion.Level >= Alertness.Listening) Enter(CreatureState.Listening);
                    break;

                case CreatureState.Listening:
                    if (Suspicion.Level == Alertness.Calm) Enter(CreatureState.Dormant);
                    else if (Suspicion.Level == Alertness.Hunting) Enter(CreatureState.Hunting);
                    else if (Suspicion.Level == Alertness.Investigating && stateTime >= listenSeconds) Enter(CreatureState.Investigating);
                    break;

                case CreatureState.Investigating:
                    if (Suspicion.Level == Alertness.Hunting) { Enter(CreatureState.Hunting); break; }
                    if (!arrived && ReachedDestination()) { arrived = true; stateTime = 0f; }
                    if (arrived && stateTime >= searchSeconds)
                        Enter(Suspicion.Level == Alertness.Calm ? CreatureState.Dormant : CreatureState.Listening);
                    break;

                case CreatureState.Hunting:
                    if (Suspicion.Level < Alertness.Investigating) { Enter(CreatureState.Listening); break; }
                    Vector3 target = Suspicion.LastKnownPosition ?? transform.position;
                    MoveTo(target, huntSpeed);
                    if (player != null && !PlayerHidden && Vector3.Distance(Flat(transform.position), Flat(player.position)) <= attackRange)
                        Enter(CreatureState.Attacking);
                    break;

                case CreatureState.Attacking:
                    // A hidden player is not there as far as it knows: it goes back to hunting the last place it sensed them.
                    if (player == null || PlayerHidden || Vector3.Distance(Flat(transform.position), Flat(player.position)) > attackRange * 1.25f)
                    { Enter(CreatureState.Hunting); break; }
                    FaceTowards(player.position);
                    if (attackTimer <= 0f)
                    {
                        attackTimer = attackCooldown;
                        playerDamage?.TakeDamage(new DamageInfo(attackDamage, DamageType.Melee, player.position, gameObject));
                    }
                    break;

                case CreatureState.Recovering:
                    if (stateTime >= recoverSeconds)
                    {
                        Suspicion.Set(Suspicion.HuntThreshold + 0.2f); // wakes angry
                        Enter(CreatureState.Hunting);
                    }
                    break;
            }
        }

        bool PlayerHidden => concealment != null && concealment.IsHidden;

        /// <summary>
        /// A scripted stimulus (the eruption, a door giving way): it senses something at <paramref name="position"/>
        /// as strongly as a perceived vibration of <paramref name="intensity"/> (0..1).
        /// </summary>
        public void Alert(Vector3 position, float intensity)
        {
            if (State == CreatureState.Dead || State == CreatureState.Torpor || State == CreatureState.Blinded) return;
            Suspicion.Hear(intensity, position);
        }

        /// <summary>
        /// Flares beat everything except being next to the player: the nearest burning flare it has not already
        /// been dazzled by either lures it (walk straight to it) or, up close, blinds it. Returns true when blinded.
        /// </summary>
        bool ReactToFlares()
        {
            BurningFlare nearest = null;
            float best = float.MaxValue;
            foreach (var f in BurningFlare.Active)
            {
                if (f == null || spentFlares.Contains(f)) continue;
                float d = Vector3.Distance(Flat(f.Position), Flat(transform.position));
                if (d < best) { best = d; nearest = f; }
            }
            if (nearest == null || best > StealthRules.FlareLureRange || !CanSense(nearest)) return false;

            switch (StealthRules.FlareEffectAt(best))
            {
                case FlareEffect.Blind:
                    spentFlares.Add(nearest); // dazzled once per flare; afterwards it knows the light is not prey
                    Suspicion.Forget();
                    Enter(CreatureState.Blinded);
                    return true;

                case FlareEffect.Lure:
                    Suspicion.Lure(nearest.Position);
                    if (State == CreatureState.Hunting || State == CreatureState.Attacking) break; // hunting already chases LastKnownPosition
                    if (State != CreatureState.Investigating) Enter(CreatureState.Investigating);
                    if (Vector3.Distance(investigateTarget, nearest.Position) > 0.75f)
                    {
                        investigateTarget = nearest.Position; // light is easy to place: no position error
                        arrived = false;
                        MoveTo(investigateTarget, investigateSpeed);
                    }
                    break;
            }
            return false;
        }

        /// <summary>Heat and light do not pass through walls: a flare in the next room is not a lure.</summary>
        bool CanSense(BurningFlare flare)
        {
            Vector3 head = transform.position + Vector3.up * 2.4f;
            if (!Physics.Linecast(head, flare.Position, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == flare.transform || hit.transform.IsChildOf(transform);
        }

        void LookForPlayer()
        {
            if (player == null) return;
            Vector3 eye = player.position + Vector3.up * 1.5f;
            float d = Vector3.Distance(transform.position + Vector3.up * 1.5f, eye);
            if (!StealthRules.CanSee(PlayerHidden, d, Suspicion.SightRange)) return;
            // Line of sight check, ignoring our own colliders.
            if (Physics.Linecast(transform.position + Vector3.up * 2.4f, eye, out var hit, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(player) && !hit.transform.IsChildOf(transform)) return;
            Suspicion.See(player.position, d);
        }

        void Enter(CreatureState next)
        {
            if (State == next) return;
            State = next;
            stateTime = 0f;
            arrived = false;

            switch (next)
            {
                case CreatureState.Investigating:
                    Vector3 heard = Suspicion.LastKnownPosition ?? transform.position;
                    investigateTarget = SuspicionModel.EstimateSource(heard, transform.position, sourceErrorPerMetre, Random.value, Random.value);
                    MoveTo(investigateTarget, investigateSpeed);
                    break;
                case CreatureState.Hunting:
                    break;
                default:
                    Stop();
                    break;
            }
            if (agent != null) agent.enabled = next != CreatureState.Dead;
            if (visual != null) visual.SetState(next);
        }

        void MoveTo(Vector3 target, float speed)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            agent.speed = speed;
            if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas)) target = hit.position;
            agent.isStopped = false;
            agent.SetDestination(target);
        }

        void Stop()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        bool ReachedDestination() =>
            agent == null || !agent.isOnNavMesh || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f);

        void FaceTowards(Vector3 p)
        {
            Vector3 to = Flat(p) - Flat(transform.position);
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 360f * Time.deltaTime);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
