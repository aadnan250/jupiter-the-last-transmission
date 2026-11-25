namespace Jupiter.World
{
    /// <summary>
    /// The timing of a short scare (Chapter 6.5, the palm prints): the lights stutter, drop out, the silhouette is
    /// there for a fraction of a second while it is dark, then the lights stagger back. Deterministic, so it plays
    /// the same every time and can be tested. Pure C#.
    /// </summary>
    public static class ScareTimeline
    {
        public const float Duration = 2.4f;
        public const float SilhouetteStart = 0.85f;
        public const float SilhouetteEnd = 1.0f;

        // (end time, light level) pairs: each level holds until its end time.
        static readonly float[] Ends = { 0.12f, 0.25f, 0.5f, 0.62f, 1.15f, 1.3f, 1.55f };
        static readonly float[] Levels = { 0.15f, 1f, 0.05f, 0.7f, 0f, 0.5f, 0.05f };

        /// <summary>Corridor light level, 0 (off) to 1 (normal), at <paramref name="t"/> seconds into the scare.</summary>
        public static float LightLevel(float t)
        {
            if (t < 0f || t >= Duration) return 1f;
            for (int i = 0; i < Ends.Length; i++)
                if (t < Ends[i]) return Levels[i];
            // Stagger back up over the remaining time.
            float last = Ends[Ends.Length - 1];
            return (t - last) / (Duration - last);
        }

        /// <summary>The crest silhouette shows only for a fraction of a second, while the lights are out.</summary>
        public static bool SilhouetteVisible(float t) => t >= SilhouetteStart && t < SilhouetteEnd;
    }
}
