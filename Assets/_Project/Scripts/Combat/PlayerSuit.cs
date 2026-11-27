using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Combat
{
    /// <summary>
    /// Runs <see cref="SuitModel"/> on the player whenever the environment is vacuum (Europa's surface): hits
    /// crack the suit, a breach leaks, the radiation dose climbs. Elsewhere it does nothing. Lives on the rig root.
    /// </summary>
    public class PlayerSuit : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] PlayerController player;
        [Tooltip("Seconds outside before the radiation dose limit.")]
        [SerializeField] float exposureSeconds = 1500f;
        [Tooltip("Integrity restored by a med-patch.")]
        [SerializeField] float sealPerPatch = 40f;

        public SuitModel Model { get; private set; }

        /// <summary>True while Sam is in vacuum and the suit is doing anything.</summary>
        public bool Active => player != null && player.Environment != null && player.Environment.isVacuum;

        void Awake()
        {
            if (health == null) health = GetComponent<PlayerHealth>();
            if (player == null) player = GetComponent<PlayerController>();
            Model = new SuitModel(exposureSeconds: exposureSeconds);
        }

        void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
        void OnDisable() { if (health != null) health.Damaged -= OnDamaged; }

        void OnDamaged(DamageInfo info) { if (Active) Model.OnHit(info.Amount); }

        /// <summary>Called when a med-patch is used: the sealant goes on the suit too.</summary>
        public bool Seal() => Active && Model.Seal(sealPerPatch) > 0f;

        public bool NeedsSeal => Active && Model.Integrity < SuitModel.MaxIntegrity;

        void Update()
        {
            if (health == null || health.Model.IsDead) return;
            float damage = Model.Tick(Time.deltaTime, Active);
            if (damage > 0f) health.Model.Damage(damage);   // a slow leak: no hit flash
        }
    }
}
