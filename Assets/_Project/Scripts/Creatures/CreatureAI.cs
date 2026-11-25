using Jupiter.Combat;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.Creatures
{
    public enum CreatureState { Dormant, Listening, Investigating, Hunting, Attacking, Torpor, Recovering, Dead }

    /// <summary>
    /// The Listener's behaviour. It hunts by vibration, not sight: vibrations raise a
    /// <see cref="SuspicionModel"/>, and suspicion thresholds drive a finite state machine:
    /// Dormant → Listening (kneels, palms on the floor) → Investigating (walks to where it *thinks* the noise was,
    /// with error growing with distance) → Hunting (direct pursuit) → Attacking.
    /// Damage can drop it into Torpor; it then Recovers angry, unless its nodes were destroyed (Dead).
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
        [SerializeField] float attackDamage = 35f;
        [SerializeField] float attackCooldown = 1.6f;

        NavMeshAgent agent;
        Transform player;
        IDamageable playerDamage;
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
            }
        }

        public void OnVibration(VibrationEvent e, float radiusMultiplier)
        {
            if (State == CreatureState.Dead || State == CreatureState.Torpor) return;
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
                    if (player != null && Vector3.Distance(Flat(transform.position), Flat(player.position)) <= attackRange)
                        Enter(CreatureState.Attacking);
                    break;

                case CreatureState.Attacking:
                    if (player == null || Vector3.Distance(Flat(transform.position), Flat(player.position)) > attackRange * 1.25f)
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

        void LookForPlayer()
        {
            if (player == null) return;
            Vector3 eye = player.position + Vector3.up * 1.5f;
            float d = Vector3.Distance(transform.position + Vector3.up * 1.5f, eye);
            if (d > Suspicion.SightRange) return;
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
