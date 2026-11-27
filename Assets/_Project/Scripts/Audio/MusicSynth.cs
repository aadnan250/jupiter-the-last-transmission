using System;

namespace Jupiter.Audio
{
    /// <summary>
    /// The score, synthesised: original, deterministic, seamless loops. A family theme (a slow piano-like
    /// arpeggio) for Earth and home; a cold pad for the ship; a low beating drone for Europa; a pulse for the
    /// fights; and a slower, barer version of the theme for the ending.
    /// </summary>
    public static class MusicSynth
    {
        const int Rate = ProceduralAudio.SampleRate;
        const double TwoPi = Math.PI * 2.0;
        const double FadeSeconds = 2.0;

        static double Hz(int midi) => 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);

        /// <summary>Renders <paramref name="seconds"/> plus a tail, then folds the tail over the head so the loop has no seam.</summary>
        static float[] Loop(double seconds, Func<double, double> sample, float peak)
        {
            int n = (int)(seconds * Rate), f = (int)(FadeSeconds * Rate);
            var raw = new double[n + f];
            for (int i = 0; i < raw.Length; i++) raw[i] = sample((double)i / Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                double v = raw[i];
                if (i < f) { double k = (double)i / f; v = raw[i] * k + raw[n + i] * (1 - k); }
                o[i] = (float)v;
            }
            return ProceduralAudio.Normalize(o, peak);
        }

        /// <summary>A soft plucked note: a few decaying harmonics, like a felt piano.</summary>
        static double Pluck(double t, double hz, double decay)
        {
            if (t < 0) return 0;
            double env = Math.Exp(-t * decay) * Math.Min(1.0, t * 200.0);
            return env * (Math.Sin(TwoPi * hz * t) + 0.35 * Math.Sin(TwoPi * hz * 2 * t) * Math.Exp(-t * 2)
                          + 0.12 * Math.Sin(TwoPi * hz * 3.01 * t) * Math.Exp(-t * 4));
        }

        static double Pad(double t, double hz, double lfo)
        {
            double detune = 1.0 + 0.003 * Math.Sin(TwoPi * lfo * t);
            return Math.Sin(TwoPi * hz * t) * 0.6 + Math.Sin(TwoPi * hz * detune * 1.002 * t) * 0.4
                   + Math.Sin(TwoPi * hz * 2.0 * t) * 0.12;
        }

        // A minor, C, F, G: the family theme's chords, one per bar.
        static readonly int[][] ThemeChords = { new[] { 57, 60, 64, 69 }, new[] { 55, 60, 64, 67 }, new[] { 53, 57, 60, 65 }, new[] { 55, 59, 62, 67 } };

        static double ThemeAt(double t, double bar, double decay, double padGain)
        {
            double beat = bar / 4.0;
            int barIndex = (int)(t / bar);
            double s = 0;
            // Arpeggio: up through the chord, one note per beat, with the previous bar still ringing.
            for (int b = barIndex - 1; b <= barIndex; b++)
            {
                if (b < 0) continue;
                var chord = ThemeChords[b % ThemeChords.Length];
                for (int k = 0; k < 4; k++)
                {
                    double start = b * bar + k * beat;
                    s += Pluck(t - start, Hz(chord[k] + 12), decay) * 0.5;
                }
            }
            var c = ThemeChords[barIndex % ThemeChords.Length];
            double pad = (Pad(t, Hz(c[0] - 12), 0.1) + Pad(t, Hz(c[2]), 0.13)) * padGain;
            return s + pad;
        }

        /// <summary>The family theme: Earth and home. Four bars of 4 s, looped twice (32 s).</summary>
        public static float[] Theme() => Loop(32.0, t => ThemeAt(t % 16.0, 4.0, 1.6, 0.18), 0.5f);

        /// <summary>The ending: the theme slower and barer, almost only the piano (40 s).</summary>
        public static float[] Ending() => Loop(40.0, t => ThemeAt(t % 20.0, 5.0, 1.1, 0.07), 0.5f);

        /// <summary>The Meridian: a cold, slowly breathing D minor pad with a high shimmer (30 s).</summary>
        public static float[] Ship()
        {
            return Loop(30.0, t =>
            {
                double breathe = 0.7 + 0.3 * Math.Sin(TwoPi * t / 15.0);
                double chord = Pad(t, Hz(38), 0.07) * 0.6 + Pad(t, Hz(50), 0.09) * 0.4 + Pad(t, Hz(53), 0.11) * 0.35
                               + Pad(t, Hz(57), 0.05) * 0.3 + Pad(t, Hz(64), 0.06) * 0.15;
                double shimmer = Math.Sin(TwoPi * Hz(81) * t) * 0.04 * (0.5 + 0.5 * Math.Sin(TwoPi * t / 10.0));
                return chord * breathe + shimmer;
            }, 0.4f);
        }

        /// <summary>Europa: a low drone that beats against itself, and metal swells that never resolve (30 s).</summary>
        public static float[] Dread()
        {
            var rng = new Random(7070);
            double lp = 0;
            var noise = new double[(int)((30.0 + FadeSeconds) * Rate) + 1];
            for (int i = 0; i < noise.Length; i++) { lp += 0.01 * ((rng.NextDouble() * 2 - 1) - lp); noise[i] = lp; }
            return Loop(30.0, t =>
            {
                double drone = Math.Sin(TwoPi * 55.0 * t) * 0.5 + Math.Sin(TwoPi * 55.6 * t) * 0.4 + Math.Sin(TwoPi * 82.0 * t) * 0.15;
                double swell = Math.Pow(Math.Max(0, Math.Sin(TwoPi * t / 10.0)), 3);
                double metal = (Math.Sin(TwoPi * 311.0 * t) + Math.Sin(TwoPi * 466.6 * t) * 0.7) * 0.08 * swell;
                double wind = noise[Math.Min(noise.Length - 1, (int)(t * Rate))] * 3.0;
                return drone + metal + wind;
            }, 0.45f);
        }

        /// <summary>The fights: a 100 bpm pulse of low drums over the drone, with a rising string-like fifth (19.2 s, 8 bars).</summary>
        public static float[] Combat()
        {
            const double beat = 0.6;
            return Loop(19.2, t =>
            {
                double inBeat = t % beat;
                int beatIndex = (int)(t / beat);
                double kick = Math.Sin(TwoPi * (60 - 30 * inBeat) * inBeat) * Math.Exp(-inBeat * 9) * 0.9;
                double off = beatIndex % 2 == 1 ? Math.Sin(TwoPi * 120 * inBeat) * Math.Exp(-inBeat * 18) * 0.3 : 0;
                double eighth = t % (beat / 2);
                double tick = Math.Sin(TwoPi * 2200 * eighth) * Math.Exp(-eighth * 80) * 0.05;
                double drone = Math.Sin(TwoPi * 55.0 * t) * 0.35 + Math.Sin(TwoPi * 82.4 * t) * 0.2;
                double rise = (t % 4.8) / 4.8;
                double fifth = (Math.Sin(TwoPi * 220.0 * t) + Math.Sin(TwoPi * 329.6 * t)) * 0.06 * rise;
                return kick + off + tick + drone + fifth;
            }, 0.55f);
        }
    }
}
