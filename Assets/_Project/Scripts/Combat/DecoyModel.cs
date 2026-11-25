using System;

namespace Jupiter.Combat
{
    /// <summary>
    /// The decoy thumper's timer: once armed (it lands), it thumps every <see cref="Interval"/> seconds for
    /// <see cref="Duration"/> seconds, starting immediately. Time is passed in so it is deterministic. Pure C#.
    /// </summary>
    public class DecoyModel
    {
        public float Duration { get; }
        public float Interval { get; }

        public bool Armed { get; private set; }
        public float Elapsed { get; private set; }
        public int PulsesEmitted { get; private set; }
        public bool Active => Armed && PulsesEmitted < TotalPulses;
        public int TotalPulses => (int)Math.Floor(Duration / Interval) + 1;

        public DecoyModel(float duration = 8f, float interval = 1.5f)
        {
            Duration = Math.Max(0f, duration);
            Interval = Math.Max(0.05f, interval);
        }

        /// <summary>Starts the pulses. Arming twice does nothing: a decoy only lands once.</summary>
        public void Arm()
        {
            if (Armed) return;
            Armed = true;
            Elapsed = 0f;
        }

        /// <summary>Advances time and returns how many pulses fell due in this step (usually 0 or 1).</summary>
        public int Tick(float deltaTime)
        {
            if (!Active) return 0;
            Elapsed += Math.Max(0f, deltaTime);
            // +1: the first thump is on landing, at time zero.
            int shouldHave = Math.Min(TotalPulses, (int)Math.Floor(Elapsed / Interval) + 1);
            int due = shouldHave - PulsesEmitted;
            PulsesEmitted += due;
            return due;
        }
    }
}
