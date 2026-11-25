using UnityEngine;

namespace Jupiter.Core
{
    /// <summary>
    /// Timing of a chapter transition: the screen fades to black, a time card fades in and holds, then the next
    /// level loads. Pure C#, so the timing is deterministic and tested.
    /// </summary>
    public static class TransitionTimeline
    {
        /// <summary>Black overlay opacity at <paramref name="t"/> seconds: rising over the fade, then fully black.</summary>
        public static float BlackAlpha(float t, float fadeSeconds) =>
            t <= 0f ? 0f : fadeSeconds <= 0f ? 1f : Mathf.Clamp01(t / fadeSeconds);

        /// <summary>Time-card text opacity: appears once the screen is black, over half a second.</summary>
        public static float CardAlpha(float t, float fadeSeconds) => Mathf.Clamp01((t - fadeSeconds) / 0.5f);

        /// <summary>True once the card has been on screen long enough to load the next level.</summary>
        public static bool ShouldLoad(float t, float fadeSeconds, float holdSeconds) => t >= fadeSeconds + holdSeconds;
    }
}
