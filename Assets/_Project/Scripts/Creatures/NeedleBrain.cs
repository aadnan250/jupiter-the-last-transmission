namespace Jupiter.Creatures
{
    public enum NeedleState { Dormant, Chase, Lunge, Recoil, Dead }

    /// <summary>
    /// A Needle: a hand-sized crawler that pours out of vents in the ice. It runs straight at Sam, lunges when
    /// close, bites once, skitters back and comes again. One good hit kills it. Pure logic, unit tested.
    /// </summary>
    public class NeedleBrain
    {
        public float LungeRange { get; }
        public float LungeSeconds { get; }
        public float RecoilSeconds { get; }
        public float BiteDamage { get; }

        public NeedleState State { get; private set; } = NeedleState.Dormant;
        public float Health { get; private set; }
        float timer;
        bool bitThisLunge;

        public NeedleBrain(float health = 10f, float lungeRange = 1.6f, float lungeSeconds = 0.35f, float recoilSeconds = 1.6f, float biteDamage = 3f)
        {
            Health = health;
            LungeRange = lungeRange;
            LungeSeconds = lungeSeconds;
            RecoilSeconds = recoilSeconds;
            BiteDamage = biteDamage;
        }

        public bool Alive => State != NeedleState.Dead;

        public void Wake() { if (State == NeedleState.Dormant) State = NeedleState.Chase; }

        /// <summary>Returns true if this damage killed it.</summary>
        public bool Damage(float amount)
        {
            if (!Alive || amount <= 0f) return false;
            Health -= amount;
            if (Health > 0f) { if (State == NeedleState.Dormant) State = NeedleState.Chase; return false; }
            Health = 0f;
            State = NeedleState.Dead;
            return true;
        }

        /// <summary>Advances the brain. Returns the bite damage dealt this step (0 most of the time).</summary>
        public float Tick(float dt, float distanceToTarget)
        {
            switch (State)
            {
                case NeedleState.Chase:
                    if (distanceToTarget <= LungeRange) { State = NeedleState.Lunge; timer = 0f; bitThisLunge = false; }
                    return 0f;
                case NeedleState.Lunge:
                    timer += dt;
                    float bite = 0f;
                    if (!bitThisLunge && distanceToTarget <= LungeRange * 0.8f) { bitThisLunge = true; bite = BiteDamage; }
                    if (timer >= LungeSeconds) { State = NeedleState.Recoil; timer = 0f; }
                    return bite;
                case NeedleState.Recoil:
                    timer += dt;
                    if (timer >= RecoilSeconds) State = NeedleState.Chase;
                    return 0f;
                default:
                    return 0f;
            }
        }
    }
}
