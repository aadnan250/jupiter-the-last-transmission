using System;

namespace Jupiter.Combat
{
    /// <summary>
    /// Sam's EVA suit on Europa's surface. Hits crack it (integrity falls with damage); below the breach line it
    /// leaks and costs health every second until patched. Jupiter's radiation builds a dose the whole time Sam is
    /// outside; past the limit it starts to hurt. A med-patch also seals the suit. Pure logic, unit tested.
    /// </summary>
    public class SuitModel
    {
        public const float MaxIntegrity = 100f;
        public const float MaxDose = 100f;

        /// <summary>Integrity lost per point of health damage taken.</summary>
        public float IntegrityPerDamage { get; }
        /// <summary>Below this integrity the suit leaks.</summary>
        public float BreachLine { get; }
        /// <summary>Health lost per second while breached.</summary>
        public float LeakDamagePerSecond { get; }
        /// <summary>Dose gained per second outside (100 = the limit).</summary>
        public float DosePerSecond { get; }
        /// <summary>Health lost per second once over the dose limit.</summary>
        public float RadiationDamagePerSecond { get; }

        public float Integrity { get; private set; } = MaxIntegrity;
        public float Dose { get; private set; }

        public bool Breached => Integrity < BreachLine;
        public bool OverDose => Dose >= MaxDose;
        public float DoseFraction => Math.Min(1f, Dose / MaxDose);
        /// <summary>Seconds of exposure left before the dose limit (0 once reached).</summary>
        public float SecondsToLimit => DosePerSecond <= 0f ? float.PositiveInfinity : Math.Max(0f, (MaxDose - Dose) / DosePerSecond);

        public SuitModel(float integrityPerDamage = 0.4f, float breachLine = 35f, float leakDamagePerSecond = 0.5f,
            float exposureSeconds = 1500f, float radiationDamagePerSecond = 0.8f)
        {
            IntegrityPerDamage = integrityPerDamage;
            BreachLine = breachLine;
            LeakDamagePerSecond = leakDamagePerSecond;
            DosePerSecond = exposureSeconds > 0f ? MaxDose / exposureSeconds : 0f;
            RadiationDamagePerSecond = radiationDamagePerSecond;
        }

        /// <summary>A hit to Sam cracks the suit.</summary>
        public void OnHit(float damage)
        {
            if (damage <= 0f) return;
            Integrity = Math.Max(0f, Integrity - damage * IntegrityPerDamage);
        }

        /// <summary>Sealant from a med-patch. Returns the integrity restored.</summary>
        public float Seal(float amount)
        {
            if (amount <= 0f) return 0f;
            float before = Integrity;
            Integrity = Math.Min(MaxIntegrity, Integrity + amount);
            return Integrity - before;
        }

        /// <summary>Advances time outside. Returns the health damage the suit does this step (leak plus radiation).</summary>
        public float Tick(float dt, bool outside)
        {
            if (dt <= 0f || !outside) return 0f;
            Dose = Math.Min(MaxDose, Dose + DosePerSecond * dt);
            float damage = 0f;
            if (Breached) damage += LeakDamagePerSecond * dt;
            if (OverDose) damage += RadiationDamagePerSecond * dt;
            return damage;
        }
    }
}
