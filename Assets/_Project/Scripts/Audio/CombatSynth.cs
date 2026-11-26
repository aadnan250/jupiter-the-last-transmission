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

        /// <summary>The bore erupting: a drill motor straining and stalling, ice cracking, a long low rumble.</summary>
        public static float[] Eruption()
        {
            int n = (int)(3.2 * Rate);
            var rng = new Random(7070);
            var o = new float[n];
            double lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                lp += 0.02 * (white - lp);
                lp2 += 0.25 * (white - lp2);
                // Motor whine sliding down as the bit is held, then cutting out at 1.1 s.
                double motor = t < 1.1 ? Math.Sin(TwoPi * (180 - 110 * t) * t) * 0.3 * (0.6 + 0.4 * Math.Sin(TwoPi * 13 * t)) : 0;
                double rumble = lp * 6.0 * Math.Min(1, t / 0.4) * Math.Exp(-Math.Max(0, t - 1.8) * 1.4);
                double crack = 0;
                foreach (double at in new[] { 1.15, 1.32, 1.6, 2.05 })
                {
                    double u = t - at;
                    if (u >= 0) crack += lp2 * Math.Exp(-u * 35) * 1.2 + Math.Sin(TwoPi * 90 * u) * Math.Exp(-u * 12) * 0.5;
                }
                o[i] = (float)(motor + rumble + crack);
            }
            return ProceduralAudio.Normalize(o, 0.95f);
        }

        /// <summary>Something heavy hitting the far side of a pressure door: a dull boom and a metal shudder.</summary>
        public static float[] DoorPound()
        {
            int n = (int)(0.9 * Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double boom = Math.Sin(TwoPi * (60 - 20 * t) * t) * Math.Exp(-t * 7);
                double shudder = Math.Sin(TwoPi * 410 * t) * Math.Sin(TwoPi * 23 * t) * Math.Exp(-t * 9) * 0.25;
                o[i] = (float)(boom + shudder);
            }
            return ProceduralAudio.Normalize(o, 0.9f);
        }

        /// <summary>The arc beam: a seamless 2 s loop of an electric hum with a crackling edge (loops cleanly).</summary>
        public static float[] BeamHum()
        {
            int n = 2 * Rate;
            var rng = new Random(3131);
            var o = new float[n];
            double crackle = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                // Whole-number cycles in 2 s, so the loop point is seamless.
                double hum = Math.Sin(TwoPi * 110 * t) * 0.35 + Math.Sin(TwoPi * 220 * t) * 0.2 + Math.Sin(TwoPi * 331 * t) * 0.12;
                double wobble = 0.8 + 0.2 * Math.Sin(TwoPi * 3 * t);
                if (rng.NextDouble() < 0.004) crackle = 1;
                crackle *= 0.93;
                o[i] = (float)(hum * wobble + (rng.NextDouble() * 2 - 1) * crackle * 0.5);
            }
            return ProceduralAudio.Normalize(o, 0.7f);
        }

        /// <summary>A Stalker's shriek: a rising, tearing screech made of detuned harmonics and noise.</summary>
        public static float[] Shriek()
        {
            int n = (int)(1.4 * Rate);
            var rng = new Random(6661);
            var o = new float[n];
            double hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double env = Math.Min(1, t / 0.08) * Math.Exp(-Math.Max(0, t - 0.5) * 3.5);
                double f = 620 + 900 * t + 60 * Math.Sin(TwoPi * 17 * t);
                double tone = Math.Sin(TwoPi * f * t) + 0.6 * Math.Sin(TwoPi * f * 1.51 * t) + 0.4 * Math.Sin(TwoPi * f * 2.03 * t);
                double white = rng.NextDouble() * 2 - 1;
                hp = 0.9 * (hp + white - prev); prev = white;
                o[i] = (float)((tone * 0.4 + hp * 0.6) * env);
            }
            return ProceduralAudio.Normalize(o, 0.85f);
        }

        /// <summary>The moon quaking: six seconds of sub-bass rumble with grinding ice, felt through the boots.</summary>
        public static float[] Quake()
        {
            int n = 6 * Rate;
            var rng = new Random(4040);
            var o = new float[n];
            double lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                lp += 0.008 * (white - lp);
                lp2 += 0.06 * (white - lp2);
                double env = Math.Min(1, t / 0.8) * Math.Min(1, (6 - t) / 1.5);
                double sub = Math.Sin(TwoPi * (28 + 4 * Math.Sin(TwoPi * 0.7 * t)) * t) * 0.5;
                o[i] = (float)((lp * 14 + sub + lp2 * 1.2 * (0.5 + 0.5 * Math.Sin(TwoPi * 1.3 * t))) * env);
            }
            return ProceduralAudio.Normalize(o, 0.95f);
        }

        /// <summary>Ice bursting open from below: a sharp crack, shards, a heavy thump.</summary>
        public static float[] IceBurst()
        {
            int n = (int)(1.1 * Rate);
            var rng = new Random(5150);
            var o = new float[n];
            double hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                hp = 0.85 * (hp + white - prev); prev = white;
                double crack = hp * Math.Exp(-t * 18);
                double thump = Math.Sin(TwoPi * (70 - 30 * t) * t) * Math.Exp(-t * 6) * 0.9;
                double shards = 0;
                foreach (double at in new[] { 0.12, 0.2, 0.31, 0.45 })
                {
                    double u = t - at;
                    if (u >= 0) shards += Math.Sin(TwoPi * (2600 + at * 3000) * u) * Math.Exp(-u * 60) * 0.25;
                }
                o[i] = (float)(crack + thump + shards);
            }
            return ProceduralAudio.Normalize(o, 0.9f);
        }

        /// <summary>Breathing inside a sealed helmet: a slow 4 s loop of filtered in and out breaths.</summary>
        public static float[] SuitBreath()
        {
            int n = 4 * Rate;
            var rng = new Random(2020);
            var o = new float[n];
            double lp = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                lp += 0.05 * (white - lp);
                double inhale = t < 1.6 ? Math.Sin(Math.PI * t / 1.6) : 0;
                double exhale = t >= 2.0 && t < 3.6 ? Math.Sin(Math.PI * (t - 2.0) / 1.6) : 0;
                o[i] = (float)(lp * (inhale * 0.8 + exhale));
            }
            return ProceduralAudio.Normalize(o, 0.5f);
        }

        /// <summary>The pistol: a hard, short crack with a metallic snap of the slide and a quick echo.</summary>
        public static float[] PistolShot()
        {
            int n = (int)(0.7 * Rate);
            var rng = new Random(4545);
            var o = new float[n];
            double hp = 0, prev = 0, lp = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double white = rng.NextDouble() * 2 - 1;
                hp = 0.88 * (hp + white - prev); prev = white;
                lp += 0.2 * (white - lp);
                double crack = hp * Math.Exp(-t * 70) * 1.2;
                double body = Math.Sin(TwoPi * (190 - 90 * t) * t) * Math.Exp(-t * 30) * 0.8;
                double slide = Clack(t - 0.03) * 0.35;
                double echo = t > 0.12 ? lp * Math.Exp(-(t - 0.12) * 9) * 0.3 : 0;
                o[i] = (float)(crack + body + slide + echo);
            }
            return ProceduralAudio.Normalize(o, 0.9f);
        }

        static double Clack(double t) => t < 0 ? 0 : Math.Exp(-t * 70) * (Math.Sin(TwoPi * 900 * t) * 0.5 + Math.Sin(TwoPi * 210 * t));
    }
}
