using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jupiter.Combat
{
    /// <summary>
    /// The player's health (<see cref="HealthModel"/>): 100 HP, no regeneration. On death the scene reloads
    /// from the last save if there is one, otherwise from the start.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float respawnDelay = 3f;

        public HealthModel Model { get; private set; }

        /// <summary>Raised for every hit (HUD flash, camera shake).</summary>
        public event Action<DamageInfo> Damaged;

        float deadTimer = -1f;

        void Awake()
        {
            Model = new HealthModel(maxHealth);
            Model.Died += () => deadTimer = respawnDelay;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (Model.IsDead) return;
            Model.Damage(info.Amount);
            Damaged?.Invoke(info);
        }

        void Update()
        {
            if (deadTimer < 0f) return;
            deadTimer -= Time.deltaTime;
            if (deadTimer > 0f) return;
            deadTimer = -1f;
            var session = Core.GameSession.Current;
            if (session == null || !session.LoadGame())
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
