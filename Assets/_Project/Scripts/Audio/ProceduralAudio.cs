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

        /// <summary>
        /// Soft rubber-soled boot step on a padded deck: a muffled heel strike followed ~80 ms later by a lighter
        /// toe roll, both band-limited noise with a gentle low body thud. No tonal ringing, so it never sounds metallic.
        /// Variants differ in timing, weight and brightness.
        /// </summary>
        public static float[] FootstepSoft(int variant)
        {
            int n = Len(0.32f);
            var rng = new Random(700 + variant * 13);
            double toeDelay = 0.07 + 0.012 * (variant % 3);
            double heelCenter = 520 + 70 * variant;     // Hz
            double toeCenter = 900 + 110 * variant;
            double bodyFreq = 52 + 6 * (variant % 4);

            var heelBp = Biquad.BandPass(heelCenter, 0.9);
            var toeBp = Biquad.BandPass(toeCenter, 1.1);
            var soften = Biquad.LowPass(2600, 0.7);

            var o = new double[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double white = rng.NextDouble() * 2 - 1;

                // Heel: 3 ms attack, ~35 ms decay
                double heelEnv = Math.Min(1, t / 0.003) * Math.Exp(-t / 0.035);
                double heel = heelBp.Process(white) * heelEnv;

                // Toe roll: quieter, shorter
                double tt = t - toeDelay;
                double toeEnv = tt < 0 ? 0 : Math.Min(1, tt / 0.004) * Math.Exp(-tt / 0.025);
                double toe = toeBp.Process(white) * toeEnv * 0.55;

                // Body thud felt more than heard
                double body = Math.Sin(TwoPi * bodyFreq * t) * Math.Exp(-t / 0.03) * 0.35;

                o[i] = soften.Process(heel + toe) + body;
            }
            return Normalize(ToFloat(o), 0.7f);
        }

        /// <summary>
        /// The consequence sting. A reversed breath swells in, a deep impact lands, and a dissonant cluster of close
        /// tones (they beat against each other) sags slowly out of tune over a whispering, formant-filtered noise bed.
        /// About 4 seconds.
        /// </summary>
        public static float[] ConsequenceSting()
        {
            const double hit = 0.9;
            int n = Len(4.2f);
            var rng = new Random(4117);
            var whisperA = Biquad.BandPass(650, 4.0);
            var whisperB = Biquad.BandPass(1750, 5.0);
            var swellHp = Biquad.HighPass(1800, 0.7);
            double[] cluster = { 466.16, 493.88, 523.25, 932.33 }; // A#4, B4, C5 and a high A#5 shimmer
            double[] phases = new double[cluster.Length];
            double subPhase = 0, wobble = rng.NextDouble() * 6.28;

            var o = new double[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double white = rng.NextDouble() * 2 - 1;
                double s = 0;

                // 1. Reversed breath: high-passed noise swelling exponentially into the hit.
                if (t < hit) s += swellHp.Process(white) * Math.Pow(t / hit, 3.2) * 0.55;

                if (t >= hit)
                {
                    double th = t - hit;

                    // 2. Impact: a sub boom that sinks from 55 Hz to 32 Hz.
                    double subF = 32 + 23 * Math.Exp(-th * 3);
                    subPhase += TwoPi * subF / SampleRate;
                    s += Math.Sin(subPhase) * Math.Exp(-th * 1.6) * 0.9;

                    // 3. Dissonant cluster: fast attack, long decay, drifting 3% flat, slow uneasy tremolo.
                    double drift = 1 - 0.03 * Math.Min(1, th / 3.0);
                    double trem = 0.75 + 0.25 * Math.Sin(TwoPi * 4.7 * th + wobble);
                    double env = Math.Min(1, th / 0.02) * Math.Exp(-th * 0.9);
                    for (int k = 0; k < cluster.Length; k++)
                    {
                        phases[k] += TwoPi * cluster[k] * drift / SampleRate;
                        s += Math.Sin(phases[k]) * env * trem * (k == 3 ? 0.08 : 0.16);
                    }

                    // 4. Whisper bed: noise through two vowel-like resonances, fluttering irregularly.
                    double flutter = 0.5 + 0.5 * Math.Sin(TwoPi * 2.3 * th) * Math.Sin(TwoPi * 0.7 * th + 1.3);
                    double wEnv = Math.Min(1, th / 0.4) * Math.Exp(-th * 0.7);
                    s += (whisperA.Process(white) + whisperB.Process(white) * 0.7) * wEnv * flutter * 0.9;
                }
                o[i] = s;
            }

            // Fade the last 150 ms so the tail never clicks off.
            int fade = Len(0.15f);
            for (int i = 0; i < fade; i++) o[n - 1 - i] *= (double)i / fade;
            return Normalize(ToFloat(o), 0.85f);
        }

        /// <summary>Radio squelch: a short burst of band-limited static with a key-click, played when a radio line starts.</summary>
        public static float[] RadioSquelch()
        {
            int n = Len(0.16f);
            var rng = new Random(77);
            var bp = Biquad.BandPass(1700, 1.2);
            var o = new double[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SampleRate;
                double env = Math.Min(1, t / 0.005) * Math.Exp(-t / 0.06);
                double click = t < 0.004 ? Math.Sin(TwoPi * 2500 * t) * (1 - t / 0.004) : 0;
                o[i] = bp.Process(rng.NextDouble() * 2 - 1) * env + click * 0.6;
            }
            return Normalize(ToFloat(o), 0.5f);
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

        static float[] ToFloat(double[] d)
        {
            var f = new float[d.Length];
            for (int i = 0; i < d.Length; i++) f[i] = (float)d[i];
            return f;
        }

        /// <summary>Second-order IIR filter (RBJ audio-EQ cookbook), used for band-limited noise.</summary>
        sealed class Biquad
        {
            readonly double b0, b1, b2, a1, a2;
            double x1, x2, y1, y2;

            Biquad(double b0, double b1, double b2, double a0, double a1, double a2)
            {
                this.b0 = b0 / a0; this.b1 = b1 / a0; this.b2 = b2 / a0;
                this.a1 = a1 / a0; this.a2 = a2 / a0;
            }

            public static Biquad BandPass(double freq, double q)
            {
                double w = TwoPi * freq / SampleRate, alpha = Math.Sin(w) / (2 * q), cos = Math.Cos(w);
                return new Biquad(alpha, 0, -alpha, 1 + alpha, -2 * cos, 1 - alpha);
            }

            public static Biquad LowPass(double freq, double q)
            {
                double w = TwoPi * freq / SampleRate, alpha = Math.Sin(w) / (2 * q), cos = Math.Cos(w);
                return new Biquad((1 - cos) / 2, 1 - cos, (1 - cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);
            }

            public static Biquad HighPass(double freq, double q)
            {
                double w = TwoPi * freq / SampleRate, alpha = Math.Sin(w) / (2 * q), cos = Math.Cos(w);
                return new Biquad((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);
            }

            public double Process(double x)
            {
                double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }

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
