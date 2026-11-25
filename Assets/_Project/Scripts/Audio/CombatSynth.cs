using System;

namespace Jupiter.Audio
{
    /// <summary>
    /// Deterministic synthesis of combat and stealth placeholder sounds (bolt driver, flare pistol, thrown objects,
    /// decoy thumper, locker door). Original, licence-free.
    /// </summary>
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

        /// <summary>A thrown can or tool landing: a bright metallic hit and two smaller bounces.</summary>
        public static float[] Clatter()
        {
            int n = (int)(0.7 * Rate);
            var rng = new Random(4242);
            var o = new float[n];
            double[] at = { 0.0, 0.18, 0.31 };
            double[] gain = { 1.0, 0.45, 0.2 };
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate, s = 0;
                double noise = rng.NextDouble() * 2 - 1;
                for (int k = 0; k < at.Length; k++)
                {
                    double u = t - at[k];
                    if (u < 0) continue;
                    double env = Math.Exp(-u * 38);
                    s += gain[k] * env * (Math.Sin(TwoPi * 2300 * u) * 0.35 + Math.Sin(TwoPi * 3170 * u) * 0.25
                                          + Math.Sin(TwoPi * 640 * u) * 0.3 + noise * 0.25 * Math.Exp(-u * 120));
                }
                o[i] = (float)s;
            }
            return ProceduralAudio.Normalize(o, 0.75f);
        }

        /// <summary>Flare pistol: a hollow pop, then the hiss of the flare catching.</summary>
        public static float[] FlareShot()
        {
            int n = (int)(1.2 * Rate);
            var rng = new Random(1717);
            var o = new float[n];
            double hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                hp = 0.97 * (hp + white - prev); prev = white;
                double pop = Math.Sin(TwoPi * (220 - 120 * t) * t) * Math.Exp(-t * 28) * 0.9;
                double hiss = hp * (t < 0.05 ? t / 0.05 : Math.Exp(-(t - 0.05) * 2.2)) * 0.35;
                o[i] = (float)(pop + hiss);
            }
            return ProceduralAudio.Normalize(o, 0.75f);
        }

        /// <summary>Decoy thumper: one deep, felt-more-than-heard thud through the floor.</summary>
        public static float[] DecoyThump()
        {
            int n = (int)(0.5 * Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double body = Math.Sin(TwoPi * (70 - 30 * t) * t) * Math.Exp(-t * 9);
                double knock = Math.Sin(TwoPi * 310 * t) * Math.Exp(-t * 60) * 0.3;
                o[i] = (float)(body + knock);
            }
            return ProceduralAudio.Normalize(o, 0.85f);
        }

        /// <summary>Locker door: a latch click and a thin sheet-metal flex.</summary>
        public static float[] LockerDoor()
        {
            int n = (int)(0.5 * Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double latch = Clack(t - 0.02) * 0.6;
                double flex = Math.Sin(TwoPi * (480 + 140 * Math.Sin(TwoPi * 7 * t)) * t) * Math.Exp(-t * 11) * 0.2;
                o[i] = (float)(latch + flex);
            }
            return ProceduralAudio.Normalize(o, 0.6f);
        }

        static double Clack(double t) => t < 0 ? 0 : Math.Exp(-t * 70) * (Math.Sin(TwoPi * 900 * t) * 0.5 + Math.Sin(TwoPi * 210 * t));
    }
}
