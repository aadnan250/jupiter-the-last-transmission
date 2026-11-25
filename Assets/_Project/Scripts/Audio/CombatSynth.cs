using System;

namespace Jupiter.Audio
{
    /// <summary>Deterministic synthesis of combat placeholder sounds (bolt driver shot and reload). Original, licence-free.</summary>
    public static class CombatSynth
    {
        const int Rate = ProceduralAudio.SampleRate;
        const double TwoPi = Math.PI * 2.0;

        /// <summary>Pneumatic bolt driver: a sharp air crack, a metallic slam and a short hiss.</summary>
        public static float[] BoltShot()
        {
            int n = (int)(0.6 * Rate);
            var rng = new Random(9001);
            var o = new float[n];
            double lp = 0, hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                lp += 0.35 * (white - lp);
                hp = 0.92 * (hp + white - prev); prev = white;
                double crack = hp * Math.Exp(-t * 90) * 1.1;
                double slam = Math.Sin(TwoPi * (140 - 60 * t) * t) * Math.Exp(-t * 22) * 0.9;
                double ring = Math.Sin(TwoPi * 1250 * t) * Math.Exp(-t * 30) * 0.15;
                double hiss = lp * Math.Exp(-t * 9) * 0.25;
                o[i] = (float)(crack + slam + ring + hiss);
            }
            return ProceduralAudio.Normalize(o, 0.9f);
        }

        /// <summary>Reload: two mechanical clacks and a pressure recharge whine.</summary>
        public static float[] Reload()
        {
            int n = (int)(1.1 * Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double c1 = Clack(t - 0.05), c2 = Clack(t - 0.85);
                double whine = t > 0.2 && t < 0.8 ? Math.Sin(TwoPi * (300 + 900 * (t - 0.2)) * t) * 0.12 * Math.Sin(Math.PI * (t - 0.2) / 0.6) : 0;
                o[i] = (float)(c1 + c2 + whine);
            }
            return ProceduralAudio.Normalize(o, 0.7f);
        }

        static double Clack(double t) => t < 0 ? 0 : Math.Exp(-t * 70) * (Math.Sin(TwoPi * 900 * t) * 0.5 + Math.Sin(TwoPi * 210 * t));
    }
}
