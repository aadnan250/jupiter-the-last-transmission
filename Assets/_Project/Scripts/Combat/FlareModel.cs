using System;

namespace Jupiter.Combat
{
    /// <summary>
    /// A flare's burn: full brightness for most of <see cref="BurnSeconds"/>, fading over the last
    /// <see cref="FadeSeconds"/>, then out. Pure C#, time passed in.
    /// </summary>
    public class FlareModel
    {
        public float BurnSeconds { get; }
        public float FadeSeconds { get; }
        public float Elapsed { get; private set; }

        public bool Burning => Elapsed < BurnSeconds;

        /// <summary>1 while burning, falling linearly to 0 over the fade, 0 once out.</summary>
        public float Intensity
        {
            get
            {
                if (!Burning) return 0f;
                float remaining = BurnSeconds - Elapsed;
                return remaining >= FadeSeconds ? 1f : remaining / FadeSeconds;
            }
        }

        public FlareModel(float burnSeconds = 25f, float fadeSeconds = 3f)
        {
            BurnSeconds = Math.Max(0.01f, burnSeconds);
            FadeSeconds = Math.Min(Math.Max(0.01f, fadeSeconds), BurnSeconds);
        }

        public void Tick(float deltaTime) => Elapsed = Math.Min(BurnSeconds, Elapsed + Math.Max(0f, deltaTime));
    }
}
