namespace Jupiter.World
{
    /// <summary>
    /// The beats of the Chapter 7.2 bore eruption, in seconds after the drill seizes: the grind, Rime bursting
    /// out across the floor, the lights failing (Hana is gone when they do), the Listener climbing out, it
    /// starting to listen, and Mara's voice on the radio. Pure C#, so the order is fixed and tested.
    /// </summary>
    public static class EruptionTimeline
    {
        public const float Grind = 0f;
        public const float RimeBurst = 1.2f;
        public const float Blackout = 2.0f;
        public const float Emerge = 2.6f;
        public const float Listen = 3.0f;
        public const float Radio = 3.4f;
        public const float Duration = 3.5f;

        /// <summary>True when a step from <paramref name="previous"/> to <paramref name="now"/> passes <paramref name="beat"/>.</summary>
        public static bool Crossed(float previous, float now, float beat) =>
            beat == 0f ? previous < 0f && now >= 0f : previous < beat && now >= beat;
    }
}
