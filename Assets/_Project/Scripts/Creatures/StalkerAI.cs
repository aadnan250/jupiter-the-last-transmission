using System;
using System.Collections.Generic;
using Jupiter.Combat;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Jupiter.Creatures
{
    public enum StalkerState { Buried, Emerging, Hunting, Attacking, Dead }

    /// <summary>
    /// A Stalker: a fast surface form that waits under the ice and bursts out (<see cref="Emerge"/>). Once up it
    /// runs down the nearest person (Sam or a squad member), slashes when in reach, and dies to sustained fire.
    /// Unlike the Listener it is not subtle: it is the thing the Listener's colony sends when it has heard enough.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class StalkerAI : MonoBehaviour, IDamageable
    {
        static readonly List<StalkerAI> all = new List<StalkerAI>();

        /// <summary>Every Stalker in the scene, buried or not.</summary>
        public static IReadOnlyList<StalkerAI> All => all;
        /// <summary>Raised when any Stalker dies.</summary>
        public static event Action<StalkerAI> Killed;

        [SerializeField] StalkerRig rig;
        [SerializeField] float maxHealth = 45f;
        [SerializeField] float runSpeed = 4.6f;
        [SerializeField] float attackRange = 1.9f;
        [SerializeField] float attackDamage = 7f;
        [SerializeField] float attackCooldown = 1.5f;
        [SerializeField] float emergeSeconds = 1.6f;
        [SerializeField] float buriedDepth = 2.6f;
        [SerializeField] ParticleSystem burst;
        [SerializeField] AudioSource voice;
        [SerializeField] AudioClip shriek;
        [SerializeField] AudioClip burstClip;

        NavMeshAgent agent;
        Collider[] colliders;
        Renderer[] renderers;
        Transform target;
        IDamageable targetDamage;
        Vector3 surfacePoint;
        float stateTime, attackTimer, retarget, shriekTimer;

        public HealthModel Health { get; private set; }
        public StalkerState State { get; private set; } = StalkerState.Buried;
        public bool Alive => State != StalkerState.Dead;
        public bool Active => State == StalkerState.Hunting || State == StalkerState.Attacking;
        public Transform Target => target;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            colliders = GetComponentsInChildren<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            if (rig == null) rig = GetComponent<StalkerRig>();
            Health = new HealthModel(maxHealth, maxHealth * 0.3f);
            agent.enabled = false;
            SetVisible(false);
        }

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        /// <summary>Bursts up out of the ice at <paramref name="point"/> (on the ground), facing <paramref name="lookAt"/>.</summary>
        public void Emerge(Vector3 point, Vector3 lookAt)
        {
            if (State != StalkerState.Buried) return;
            surfacePoint = point;
            Vector3 flat = lookAt - point; flat.y = 0f;
            transform.SetPositionAndRotation(point + Vector3.down * buriedDepth,
                flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat) : transform.rotation);
            SetVisible(true);
            State = StalkerState.Emerging;
            stateTime = 0f;
            if (rig != null) rig.Pose = StalkerPose.Emerging;
            if (burst != null) { burst.transform.position = point; burst.Play(); }
            Play(burstClip, 1f);
            Play(shriek, 0.9f);
            VibrationSystem.Emit(new VibrationEvent(point, 20f, VibrationMedium.Ground, true, gameObject));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateTime += dt;
            attackTimer = Mathf.Max(0f, attackTimer - dt);

            switch (State)
            {
                case StalkerState.Emerging:
                    float k = Mathf.Clamp01(stateTime / emergeSeconds);
                    // Fast at first, then hauling the last of itself out.
                    transform.position = surfacePoint + Vector3.down * (buriedDepth * (1f - Mathf.Sqrt(k)));
                    if (k >= 1f) BeginHunting();
                    break;

                case StalkerState.Hunting:
                case StalkerState.Attacking:
                    Hunt(dt);
                    break;

                case StalkerState.Dead:
                    if (stateTime > 12f) gameObject.SetActive(false);
                    break;
            }
        }

        void BeginHunting()
        {
            State = StalkerState.Hunting;
            stateTime = 0f;
            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas)) agent.Warp(hit.position);
            agent.speed = runSpeed;
            agent.acceleration = 30f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = attackRange * 0.8f;
            if (rig != null) rig.Pose = StalkerPose.Run;
        }

        void Hunt(float dt)
        {
            if ((retarget -= dt) <= 0f) { retarget = 0.5f; PickTarget(); }
            if (target == null) { if (rig != null) { rig.Pose = StalkerPose.Idle; rig.Speed = 0f; } return; }

            float d = Vector3.Distance(Flat(transform.position), Flat(target.position));
            if (d <= attackRange)
            {
                State = StalkerState.Attacking;
                if (agent.isOnNavMesh) agent.isStopped = true;
                Vector3 to = Flat(target.position) - Flat(transform.position);
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 720f * dt);
                if (rig != null) { rig.Pose = StalkerPose.Idle; rig.Speed = 0f; }
                if (attackTimer <= 0f)
                {
                    attackTimer = attackCooldown;
                    if (rig != null) rig.Attack();
                    targetDamage?.TakeDamage(new DamageInfo(attackDamage, DamageType.Melee, target.position, gameObject));
                }
                return;
            }

            State = StalkerState.Hunting;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            if (rig != null)
            {
                rig.Pose = StalkerPose.Run;
                rig.Speed = Mathf.Clamp01(agent.velocity.magnitude / runSpeed);
            }
            if ((shriekTimer -= dt) <= 0f) { shriekTimer = UnityEngine.Random.Range(4f, 9f); Play(shriek, 0.5f); }
        }

        void PickTarget()
        {
            Transform best = null;
            IDamageable bestDamage = null;
            float bestD = float.MaxValue;
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null) Consider(player.transform, player.GetComponent<IDamageable>(), ref best, ref bestDamage, ref bestD, 0.85f);
            foreach (var m in SquadMember.All) Consider(m.transform, m, ref best, ref bestDamage, ref bestD, 1f);
            target = best;
            targetDamage = bestDamage;
        }

        void Consider(Transform t, IDamageable dmg, ref Transform best, ref IDamageable bestDamage, ref float bestD, float weight)
        {
            if (t == null) return;
            // The player counts as a little closer than they are: Sam is the one the colony wants.
            float d = Vector3.Distance(t.position, transform.position) * weight;
            if (d < bestD) { bestD = d; best = t; bestDamage = dmg; }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (State == StalkerState.Dead || State == StalkerState.Buried) return;
            Health.Damage(info.Amount);
            if (rig != null && UnityEngine.Random.value < 0.15f) rig.Flinch();
            if (Health.IsDead) Die();
        }

        void Die()
        {
            State = StalkerState.Dead;
            stateTime = 0f;
            if (agent.enabled) agent.enabled = false;
            foreach (var c in colliders) c.enabled = false;
            if (rig != null) rig.Pose = StalkerPose.Dead;
            Play(shriek, 1f);
            Killed?.Invoke(this);
        }

        void SetVisible(bool on)
        {
            foreach (var r in renderers) if (r != null) r.enabled = on;
            foreach (var c in colliders) if (c != null) c.enabled = on;
        }

        void Play(AudioClip clip, float volume)
        {
            if (voice != null && clip != null) voice.PlayOneShot(clip, volume);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
