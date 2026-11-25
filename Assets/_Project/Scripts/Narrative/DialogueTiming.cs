using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// How long a line stays up when nobody presses Continue (radio chatter during a chase): the voice clip plus a
    /// short breath, or a reading-speed estimate when there is no clip, never less than <see cref="MinSeconds"/>.
    /// Pure C#.
    /// </summary>
    public static class DialogueTiming
    {
        public const float MinSeconds = 1.2f;
        public const float BreathSeconds = 0.35f;
        public const float SecondsPerCharacter = 0.06f;

        public static float LineSeconds(float clipSeconds, int textLength)
        {
            float spoken = clipSeconds > 0f ? clipSeconds : Mathf.Max(0, textLength) * SecondsPerCharacter;
            return Mathf.Max(MinSeconds, spoken + BreathSeconds);
        }
    }
}
