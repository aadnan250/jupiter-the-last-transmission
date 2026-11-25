using Jupiter.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>Health and bolt count (bottom right), plus a red flash when hurt. Hidden until the player is armed or hurt.</summary>
    public class CombatHUD : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] BoltDriver weapon;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Text healthText;
        [SerializeField] Text ammoText;
        [SerializeField] Image damageFlash;

        float flash;

        void OnEnable() { if (health != null) health.Damaged += OnDamaged; }
        void OnDisable() { if (health != null) health.Damaged -= OnDamaged; }

        void OnDamaged(DamageInfo _) => flash = 1f;

        void Update()
        {
            if (health != null && healthText != null)
            {
                var m = health.Model;
                bool show = m.Current < m.Max || (inventory != null && inventory.HasBoltDriver);
                healthText.enabled = show;
                int patches = inventory != null ? inventory.Model.Count(PlayerInventory.MedPatch) : 0;
                string colour = m.IsLow ? "#FF5A5A" : "#E6EEF2";
                healthText.text = $"<color={colour}>HEALTH {Mathf.CeilToInt(m.Current)}</color>   med-patches {patches}";
            }
            if (weapon != null && ammoText != null)
            {
                bool armed = inventory != null && inventory.HasBoltDriver;
                ammoText.enabled = armed;
                if (armed)
                {
                    var w = weapon.Model;
                    string state = w.State == WeaponState.Reloading ? "  RELOADING" : w.State == WeaponState.Empty ? "  EMPTY" : "";
                    ammoText.text = $"BOLTS {w.Loaded} / {w.Reserve}{state}";
                }
            }
            if (damageFlash != null)
            {
                flash = Mathf.Max(0f, flash - Time.deltaTime * 2f);
                float low = health != null && health.Model.IsLow ? 0.12f + 0.06f * Mathf.Sin(Time.time * 4f) : 0f;
                var c = damageFlash.color;
                c.a = Mathf.Max(flash * 0.45f, low);
                damageFlash.color = c;
            }
        }
    }
}
