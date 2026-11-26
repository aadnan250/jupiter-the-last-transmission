using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// When things happen in the surface ambush, in seconds after the quake begins: the shaking, the panic on the
    /// radio, then the Stalkers bursting out one after another. Where they come up: a ring around the squad,
    /// spread so the player has to turn to find them. Pure C#.
    /// </summary>
    public static class AmbushTimeline
    {
        public const float QuakeSeconds = 6.5f;
        public const float RadioAt = 0.6f;
        public const float FirstEmergeAt = 3.2f;
        public const float EmergeInterval = 1.2f;

        public static float EmergeTime(int index) => FirstEmergeAt + index * EmergeInterval;

        /// <summary>
        /// A point on the ring for Stalker <paramref name="index"/> of <paramref name="count"/>: angles spaced by the
        /// golden angle (so no two come up close together), radius alternating between near and far.
        /// </summary>
        public static Vector3 EmergeOffset(int index, int count, float nearRadius, float farRadius)
        {
            float angle = index * 137.508f + 20f;
            float radius = index % 2 == 0 ? Mathf.Lerp(nearRadius, farRadius, 0.25f) : Mathf.Lerp(nearRadius, farRadius, 0.85f);
            radius += (index % 3) * 1.3f;
            float rad = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;
        }
    }
}
