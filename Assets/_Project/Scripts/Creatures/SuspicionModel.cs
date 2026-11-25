using UnityEngine;

namespace Jupiter.Creatures
{
    public enum Alertness { Calm, Listening, Investigating, Hunting }

    /// <summary>
    /// How suspicious a creature is, as one number. Perceived vibrations add to it, sight of the player pins it
    /// at hunting level, and it decays over time. Thresholds turn the number into behaviour.
    /// Pure C#: unit tested without a scene.
    /// </summary>
    public class SuspicionModel
    {
        public float ListenThreshold = 0.15f;
        public float InvestigateThreshold = 0.4f;
        public float HuntThreshold = 0.85f;
        public float DecayPerSecond = 0.06f;
        public float MaxValue = 1.5f;
        public float SightRange = 4f;

        public float Value { get; private set; }
        public Vector3? LastKnownPosition { get; private set; }
        public float LastIntensity { get; private set; }

        public Alertness Level =>
            Value >= HuntThreshold ? Alertness.Hunting :
            Value >= InvestigateThreshold ? Alertness.Investigating :
            Value >= ListenThreshold ? Alertness.Listening : Alertness.Calm;

        /// <summary>Adds a perceived vibration (0..1, from <see cref="VibrationMath.Perceived"/>).</summary>
        public void Hear(float intensity, Vector3 position)
        {
            if (intensity <= 0f) return;
            Value = Mathf.Min(MaxValue, Value + intensity);
            LastKnownPosition = position;
            LastIntensity = intensity;
        }

        /// <summary>The creature is nearly blind: it only "sees" within <see cref="SightRange"/>.</summary>
        public bool See(Vector3 position, float distance)
        {
            if (!StealthRules.CanSee(false, distance, SightRange)) return false;
            Value = Mathf.Max(Value, HuntThreshold + 0.1f);
            LastKnownPosition = position;
            return true;
        }

        /// <summary>
        /// A lure (a burning flare) drags attention to a point: at least investigating, and the point becomes
        /// where it thinks the noise is. Heat and light are easy to place, so there is no position error.
        /// </summary>
        public void Lure(Vector3 position)
        {
            Value = Mathf.Max(Value, InvestigateThreshold + 0.05f);
            LastKnownPosition = position;
        }

        /// <summary>Dazzled: it loses everything it had, including where it thought the player was.</summary>
        public void Forget()
        {
            Value = 0f;
            LastKnownPosition = null;
            LastIntensity = 0f;
        }

        public void Tick(float deltaTime)
        {
            Value = Mathf.Max(0f, Value - DecayPerSecond * deltaTime);
            if (Value <= 0f) LastKnownPosition = null;
        }

        /// <summary>Forces a level, e.g. a creature waking from torpor angry.</summary>
        public void Set(float value) => Value = Mathf.Clamp(value, 0f, MaxValue);

        /// <summary>
        /// Where the creature thinks a noise came from: the true position plus an error that grows with distance
        /// (a quiet, far noise is hard to place). <paramref name="random01"/> supplies two 0..1 values for testability.
        /// </summary>
        public static Vector3 EstimateSource(Vector3 truePosition, Vector3 listener, float errorPerMetre, float r1, float r2)
        {
            float error = Vector3.Distance(truePosition, listener) * errorPerMetre;
            float angle = r1 * Mathf.PI * 2f;
            float radius = r2 * error;
            return truePosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }
}
