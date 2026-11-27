using Jupiter.Combat;
using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>
    /// Moves a <see cref="NeedleBrain"/> across the ice: runs at the player hugging the ground, lunges, bites,
    /// skitters back. Its six legs twitch as it goes. Shot or burned once, it curls up and stops.
    /// </summary>
    public class NeedleCrawler : MonoBehaviour, IDamageable
    {
        [SerializeField] float speed = 6.5f;
        [SerializeField] float lungeSpeed = 11f;
        [SerializeField] float health = 10f;
        [SerializeField] Transform[] legs;
        [SerializeField] Transform body;

        public NeedleBrain Brain { get; private set; }
        Transform target;
        PlayerHealth targetHealth;
        float gait;
        float deadFor;

        void Awake() => Brain = new NeedleBrain(health);

        /// <summary>Wakes it at <paramref name="at"/>, hunting the player.</summary>
        public void Release(Vector3 at)
        {
            gameObject.SetActive(true);
            transform.position = at;
            var player = FindAnyObjectByType<PlayerHealth>();
            if (player != null) { targetHealth = player; target = player.transform; }
            Brain.Wake();
        }

        public void TakeDamage(DamageInfo info)
        {
            if (Brain.Damage(info.Amount) && body != null)
                body.localRotation = Quaternion.Euler(180f, 0f, 0f);   // flips over, legs up
        }

        void Update()
        {
            if (!Brain.Alive)
            {
                deadFor += Time.deltaTime;
                if (deadFor > 6f) gameObject.SetActive(false);
                return;
            }
            if (target == null) return;

            Vector3 to = target.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            float bite = Brain.Tick(Time.deltaTime, dist);
            if (bite > 0f && targetHealth != null)
                targetHealth.TakeDamage(new DamageInfo(bite, DamageType.Melee, transform.position, gameObject));

            Vector3 dir = dist > 0.01f ? to / dist : transform.forward;
            float v = Brain.State == NeedleState.Lunge ? lungeSpeed : Brain.State == NeedleState.Recoil ? -speed * 0.6f : speed;
            if (Brain.State == NeedleState.Chase && dist < 0.8f) v = 0f;
            Vector3 p = transform.position + dir * (v * Time.deltaTime);
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<NeedleCrawler>() == null)
                p.y = hit.point.y;
            transform.position = p;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            gait += Time.deltaTime * Mathf.Abs(v) * 4f;
            if (legs != null)
                for (int i = 0; i < legs.Length; i++)
                    if (legs[i] != null)
                        legs[i].localRotation = Quaternion.Euler(Mathf.Sin(gait + i * 2.1f) * 25f, (i % 2 == 0 ? 1 : -1) * (60f - (i / 2) * 30f), (i % 2 == 0 ? -1 : 1) * 55f);
        }
    }
}
