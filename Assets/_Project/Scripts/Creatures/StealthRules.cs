using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>
    /// The numbers behind stealth (docs/preproduction/04-gameplay-and-levels.md §6), kept in one place so the
    /// tools, the creature and the tests agree. Pure C#: unit tested without a scene.
    /// </summary>
    public static class StealthRules
    {
        /// <summary>A thrown can or tool hitting the floor hard: the design's "dropped object" radius.</summary>
        public const float DroppedObjectRadius = 6f;
        /// <summary>Impacts slower than this are a soft bump and make no noise worth hearing.</summary>
        public const float SilentImpactSpeed = 1.2f;
        /// <summary>Impact speed at which a thrown object reaches the full dropped-object radius.</summary>
        public const float LoudImpactSpeed = 6f;

        /// <summary>The decoy thumper: a throwable vibration emitter that pulses through the floor.</summary>
        public const float DecoyRadius = 15f;

        /// <summary>Climbing into or out of a locker: a metal door and a held breath.</summary>
        public const float LockerRadius = 1.5f;

        /// <summary>The flare pistol's pop. Quieter than the bolt driver, but not quiet.</summary>
        public const float FlareShotRadius = 10f;
        /// <summary>A burning flare draws a creature within this range (heat and light).</summary>
        public const float FlareLureRange = 14f;
        /// <summary>A creature this close to a burning flare is dazzled and loses track of everything.</summary>
        public const float FlareBlindRange = 2.5f;
        public const float FlareBlindSeconds = 5f;

        /// <summary>
        /// How far a thrown object's impact carries: nothing below <see cref="SilentImpactSpeed"/>, rising
        /// linearly to <see cref="DroppedObjectRadius"/> at <see cref="LoudImpactSpeed"/> and above.
        /// </summary>
        public static float ImpactNoiseRadius(float impactSpeed)
        {
            if (impactSpeed < SilentImpactSpeed) return 0f;
            float t = Mathf.InverseLerp(SilentImpactSpeed, LoudImpactSpeed, impactSpeed);
            return Mathf.Lerp(DroppedObjectRadius * 0.4f, DroppedObjectRadius, t);
        }

        /// <summary>
        /// Whether a creature can see the player. It is nearly blind (only <paramref name="sightRange"/> metres),
        /// and someone hidden in a locker cannot be seen at all: only heard.
        /// </summary>
        public static bool CanSee(bool playerHidden, float distance, float sightRange) =>
            !playerHidden && distance <= sightRange;

        /// <summary>A burning flare's pull: blind when very close, lured within range, otherwise ignored.</summary>
        public static FlareEffect FlareEffectAt(float distance) =>
            distance <= FlareBlindRange ? FlareEffect.Blind :
            distance <= FlareLureRange ? FlareEffect.Lure : FlareEffect.None;
    }
}
