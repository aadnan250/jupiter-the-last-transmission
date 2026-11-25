using System.Text;
using Jupiter.Combat;
using Jupiter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>
    /// Health and bolt count (bottom right), plus a red flash when hurt. Hidden until the player is armed or hurt.
    /// A third line shows the stealth state: flares left, the object in hand, and whether Sam is hidden.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        [SerializeField] PlayerHealth health;
        [SerializeField] BoltDriver weapon;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Text healthText;
        [SerializeField] Text ammoText;
        [SerializeField] Image damageFlash;
        [SerializeField] PlayerThrower thrower;
        [SerializeField] PlayerConcealment concealment;
        [SerializeField] Text toolText;

        readonly StringBuilder toolLine = new StringBuilder();

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
            UpdateToolLine();
            if (damageFlash != null)
            {
                flash = Mathf.Max(0f, flash - Time.deltaTime * 2f);
                float low = health != null && health.Model.IsLow ? 0.12f + 0.06f * Mathf.Sin(Time.time * 4f) : 0f;
                var c = damageFlash.color;
                c.a = Mathf.Max(flash * 0.45f, low);
                damageFlash.color = c;
            }
        }

        void UpdateToolLine()
        {
            if (toolText == null) return;
            toolLine.Clear();
            if (concealment != null && concealment.IsHidden) toolLine.Append("<color=#9FD8E8>HIDDEN</color>   ");
            if (inventory != null && inventory.HasFlarePistol)
                toolLine.Append("FLARES ").Append(inventory.Model.Count(PlayerInventory.Flare)).Append("   ");
            if (thrower != null && thrower.Held != null)
                toolLine.Append("HOLDING ").Append(thrower.Held.DisplayName.ToUpperInvariant()).Append(" [").Append(ThrowGlyph).Append("]");

            toolText.enabled = toolLine.Length > 0;
            if (toolText.enabled) toolText.text = toolLine.ToString().TrimEnd();
        }

        static string ThrowGlyph =>
            !InputDeviceWatcher.UsingGamepad ? "G" : InputDeviceWatcher.UsingPlayStation ? "R1" : "RB";
    }
}
