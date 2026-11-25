using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>How a vibration travels. Airborne sound needs air; ground vibration travels through ice and decks.</summary>
    public enum VibrationMedium { Air, Ground }

    /// <summary>
    /// A noise in the world: a footstep, a gunshot, a dropped object. <see cref="Radius"/> is how far it carries
    /// at full strength falling linearly to zero (walk 3 m, run 9 m, crouch 0.8 m, bolt driver 25 m).
    /// </summary>
    public struct VibrationEvent
    {
        public Vector3 Position;
        public float Radius;
        public VibrationMedium Medium;
        /// <summary>True when the source is standing in vacuum (Europa's surface).</summary>
        public bool SourceInVacuum;
        public GameObject Source;

        public VibrationEvent(Vector3 position, float radius, VibrationMedium medium, bool sourceInVacuum = false, GameObject source = null)
        {
            Position = position;
            Radius = radius;
            Medium = medium;
            SourceInVacuum = sourceInVacuum;
            Source = source;
        }
    }

    /// <summary>Anything that can hear: creatures, and Rime patches that amplify.</summary>
    public interface IVibrationListener
    {
        /// <param name="radiusMultiplier">Amplification from Rime near the source (1 = none).</param>
        void OnVibration(VibrationEvent e, float radiusMultiplier);
    }

    /// <summary>
    /// Pure maths for how strongly a listener perceives a vibration.
    /// The project's core scientific rule: <b>sound cannot travel through vacuum</b>, so an airborne event is
    /// inaudible if either the source or the listener is in vacuum. Ground vibration always carries.
    /// </summary>
    public static class VibrationMath
    {
        /// <summary>0 (not perceived) to 1 (at the source), linear falloff over the (amplified) radius.</summary>
        public static float Perceived(VibrationEvent e, Vector3 listener, bool listenerInVacuum, float radiusMultiplier = 1f)
        {
            if (e.Medium == VibrationMedium.Air && (e.SourceInVacuum || listenerInVacuum)) return 0f;
            float radius = e.Radius * Mathf.Max(0f, radiusMultiplier);
            if (radius <= 0f) return 0f;
            float distance = Vector3.Distance(e.Position, listener);
            return distance >= radius ? 0f : 1f - distance / radius;
        }
    }

    /// <summary>
    /// The noise bus. Emitters call <see cref="Emit"/>; it works out Rime amplification at the source and
    /// notifies every registered listener. A static registry is used deliberately: noise is global by nature and
    /// listeners register/unregister with their own lifetime, so nothing leaks between scenes.
    /// </summary>
    public static class VibrationSystem
    {
        /// <summary>Rime doubles the radius of anything that happens near it.</summary>
        public const float RimeMultiplier = 2f;

        static readonly List<IVibrationListener> Listeners = new List<IVibrationListener>();
        static readonly List<RimePatch> Rime = new List<RimePatch>();

        /// <summary>Raised for every emitted event (debug views, tests).</summary>
        public static event Action<VibrationEvent> Emitted;

        public static void Register(IVibrationListener l) { if (!Listeners.Contains(l)) Listeners.Add(l); }
        public static void Unregister(IVibrationListener l) => Listeners.Remove(l);
        public static void RegisterRime(RimePatch r) { if (!Rime.Contains(r)) Rime.Add(r); }
        public static void UnregisterRime(RimePatch r) => Rime.Remove(r);

        public static int ListenerCount => Listeners.Count;

        public static void Emit(VibrationEvent e)
        {
            float multiplier = 1f;
            foreach (var r in Rime)
            {
                if (r == null || !r.Contains(e.Position)) continue;
                multiplier = RimeMultiplier;
                r.Flash();
            }

            Emitted?.Invoke(e);
            // Copy: a listener may unregister itself (e.g. on death) while being notified.
            foreach (var l in Listeners.ToArray()) l.OnVibration(e, multiplier);
        }
    }
}
