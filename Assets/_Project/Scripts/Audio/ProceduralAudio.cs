using System;

namespace Jupiter.Audio
{
    /// <summary>
    /// Deterministic synthesis of placeholder ship sounds (room tones, doors, footsteps).
    /// Every sound is generated from a fixed seed, so the assets are original and reproducible.
    /// Looping sounds use whole-number cycle counts and a crossfaded noise seam so they loop without clicks.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;
        const double TwoPi = Math.PI * 2.0;

        // ---------------- Looping room tones ----------------

        /// <summary>Deep, constant hum of the rotating habitat ring. 10 s loop.</summary>
        public static float[] RingDrone()
        {
            const float seconds = 10f;
            int n = Len(seconds);
            var noise = LoopingNoise(n, 11, cutoffHz: 120f);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double swell = 0.85 + 0.15 * Math.Sin(TwoPi * 0.2 * t);  // 2 cycles per loop
                o[i] = (float)(swell * (0.55 * Math.Sin(TwoPi * 38 * t) + 0.25 * Math.Sin(TwoPi * 76 * t)) + 0.6 * noise[i]);
            }
            return Normalize(o, 0.7f);
        }

        /// <summary>Ventilation airflow: soft filtered noise with a slow flutter. 8 s loop.</summary>
        public static float[] Ventilation()
        {
            const float seconds = 8f;
            int n = Len(seconds);
            var noise = LoopingNoise(n, 23, cutoffHz: 900f);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double flutter = 0.8 + 0.2 * Math.Sin(TwoPi * 0.5 * t); // 4 cycles per loop
                o[i] = (float)(noise[i] * flutter);
            }
            return Normalize(o, 0.6f);
        }

        /// <summary>Equipment hum: mains-style harmonics with a slight buzz. 6 s loop.</summary>
        public static float[] ElectricalHum()
        {
            const float seconds = 6f;
            int n = Len(seconds);
            var noise = LoopingNoise(n, 37, cutoffHz: 3000f);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double hum = 0.5 * Math.Sin(TwoPi * 60 * t) + 0.3 * Math.Sin(TwoPi * 120 * t)
                           + 0.15 * Math.Sin(TwoPi * 180 * t) + 0.08 * Math.Sign(Math.Sin(TwoPi * 120 * t));
                o[i] = (float)(hum + 0.05 * noise[i]);
            }
            return Normalize(o, 0.45f);
        }

        /// <summary>Medical monitor: a soft 880 Hz beep every 2 seconds over faint hum. 4 s loop.</summary>
        public static float[] MedicalMonitor()
        {
            const float seconds = 4f;
            int n = Len(seconds);
            var o = new float[n];
            int beepLen = Len(0.12f);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                int inPeriod = i % Len(2f);
                double beep = 0;
                if (inPeriod < beepLen)
                {
                    double env = Math.Sin(Math.PI * inPeriod / beepLen); // smooth on/off
                    beep = env * Math.Sin(TwoPi * 880 * t);
                }
                o[i] = (float)(0.35 * beep + 0.05 * Math.Sin(TwoPi * 60 * t));
            }
            return Normalize(o, 0.5f);
        }

        // ---------------- One-shots ----------------

        /// <summary>Pneumatic door opening: pressure hiss with a low mechanical thunk.</summary>
        public static float[] DoorOpen() => DoorSound(seed: 101, closing: false);

        /// <summary>Door closing: hiss followed by a latch click.</summary>
        public static float[] DoorClose() => DoorSound(seed: 103, closing: true);

        static float[] DoorSound(int seed, bool closing)
        {
            int n = Len(0.9f);
            var rng = new Random(seed);
            var o = new float[n];
            double lp = 0, alpha = Alpha(2500f);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double white = rng.NextDouble() * 2 - 1;
                lp += alpha * (white - lp);
                double hissEnv = Math.Min(1, t / 0.02) * Math.Exp(-t * 4.5);
                double thunk = Math.Exp(-t * 18) * Math.Sin(TwoPi * 70 * t) * (closing ? 0.3 : 0.8);
                double latch = 0;
                if (closing && t > 0.62)
                {
                    double lt = t - 0.62;
                    latch = Math.Exp(-lt * 60) * (Math.Sin(TwoPi * 1400 * lt) * 0.6 + Math.Sin(TwoPi * 90 * lt));
                }
                o[i] = (float)(lp * hissEnv * 1.2 + thunk + latch);
            }
            return Normalize(o, 0.8f);
        }

        /// <summary>Footstep on a metal deck plate. Variants differ by seed.</summary>
        public static float[] FootstepMetal(int variant)
        {
            int n = Len(0.28f);
            var rng = new Random(500 + variant);
            double baseFreq = 95 + variant * 9;
            double ring = 430 + variant * 37;
            var o = new float[n];
            double lp = 0, alpha = Alpha(1800f);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double white = rng.NextDouble() * 2 - 1;
                lp += alpha * (white - lp);
                double thump = Math.Exp(-t * 32) * Math.Sin(TwoPi * baseFreq * t);
                double scuff = Math.Exp(-t * 55) * lp;
                double plate = Math.Exp(-t * 22) * Math.Sin(TwoPi * ring * t) * 0.15;
                o[i] = (float)(thump + scuff * 0.8 + plate);
            }
            return Normalize(o, 0.8f);
        }

        /// <summary>Short switch click.</summary>
        public static float[] SwitchClick()
        {
            int n = Len(0.08f);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                o[i] = (float)(Math.Exp(-t * 120) * (Math.Sin(TwoPi * 2200 * t) * 0.5 + Math.Sin(TwoPi * 180 * t)));
            }
            return Normalize(o, 0.6f);
        }

        // ---------------- Helpers ----------------

        static int Len(float seconds) => (int)Math.Round(seconds * SampleRate);

        static double Alpha(float cutoffHz) => 1.0 - Math.Exp(-TwoPi * cutoffHz / SampleRate);

        /// <summary>Low-passed noise that loops seamlessly (the tail is crossfaded into the head).</summary>
        static float[] LoopingNoise(int length, int seed, float cutoffHz)
        {
            int fade = Math.Min(length / 4, SampleRate / 2);
            var rng = new Random(seed);
            var raw = new double[length + fade];
            double lp = 0, alpha = Alpha(cutoffHz);
            // Warm up the filter so the first samples are not quieter.
            for (int i = 0; i < 2000; i++) lp += alpha * ((rng.NextDouble() * 2 - 1) - lp);
            for (int i = 0; i < raw.Length; i++)
            {
                lp += alpha * ((rng.NextDouble() * 2 - 1) - lp);
                raw[i] = lp;
            }
            var o = new float[length];
            for (int i = 0; i < length; i++)
            {
                double v = raw[i];
                if (i < fade)
                {
                    double k = (double)i / fade;
                    v = raw[i] * k + raw[length + i] * (1 - k);
                }
                o[i] = (float)v;
            }
            return Normalize(o, 1f);
        }

        /// <summary>Scales samples so the loudest one equals <paramref name="peak"/>.</summary>
        public static float[] Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float s in samples) max = Math.Max(max, Math.Abs(s));
            if (max < 1e-6f) return samples;
            float k = peak / max;
            for (int i = 0; i < samples.Length; i++) samples[i] *= k;
            return samples;
        }
    }
}
