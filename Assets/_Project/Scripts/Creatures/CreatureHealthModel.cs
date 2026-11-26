using System;

namespace Jupiter.Creatures
{
    public enum CreatureCondition { Active, Torpor, Dead }

    /// <summary>
    /// The Listener's biology as rules (story bible §8.1):
    /// body hits make it vent and eventually collapse into <b>torpor</b>, which looks like death but is resealing.
    /// After 20-40 s it recovers with partial health. It only truly dies if <see cref="NodesToKill"/> pressure nodes are
    /// ruptured, or it takes explosive damage while in torpor. Pure C# so the rules are unit tested.
    /// </summary>
    public class CreatureHealthModel
    {
        public float MaxHealth = 300f;
        public float NodeRuptureDamage = 120f;
        public int NodeCount = 6;
        public int NodesToKill = 3;
        public float TorporMinSeconds = 20f;
        public float TorporMaxSeconds = 40f;
        public float RecoverHealthFraction = 0.5f;
        /// <summary>
        /// Body damage taken while it is down (in torpor) that finishes it for good. 0 keeps the original rule:
        /// only nodes or explosives can finish it.
        /// </summary>
        public float FinishDamage;

        float downDamage;

        readonly Func<float> random01;
        readonly bool[] ruptured;

        public float Health { get; private set; }
        public CreatureCondition Condition { get; private set; } = CreatureCondition.Active;
        public float TorporRemaining { get; private set; }
        public int NodesRuptured { get; private set; }

        /// <summary>Raised whenever <see cref="Condition"/> changes (old, new).</summary>
        public event Action<CreatureCondition, CreatureCondition> ConditionChanged;

        public CreatureHealthModel(Func<float> random01 = null, int nodeCount = 6, float maxHealth = 300f)
        {
            this.random01 = random01 ?? (() => 0.5f);
            NodeCount = nodeCount;
            ruptured = new bool[nodeCount];
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public bool IsNodeRuptured(int index) => index >= 0 && index < ruptured.Length && ruptured[index];

        /// <summary>Ordinary hits. In torpor they only count towards <see cref="FinishDamage"/>, if that is set.</summary>
        public void TakeBodyDamage(float amount)
        {
            if (amount <= 0f || Condition == CreatureCondition.Dead) return;
            if (Condition == CreatureCondition.Torpor)
            {
                // Shooting it while it is down: enough, and it does not get back up.
                if (FinishDamage > 0f && (downDamage += amount) >= FinishDamage) SetCondition(CreatureCondition.Dead);
                return;
            }
            Health -= amount;
            if (Health <= 0f) EnterTorpor();
        }

        /// <summary>A hit on a glowing pressure node. Each node can rupture once.</summary>
        public void RuptureNode(int index)
        {
            if (Condition == CreatureCondition.Dead || index < 0 || index >= ruptured.Length || ruptured[index]) return;
            ruptured[index] = true;
            NodesRuptured++;
            if (NodesRuptured >= NodesToKill) { SetCondition(CreatureCondition.Dead); return; }
            if (Condition == CreatureCondition.Active)
            {
                Health -= NodeRuptureDamage;
                if (Health <= 0f) EnterTorpor();
            }
        }

        /// <summary>Explosives (seismic charges): fatal in torpor, heavy body damage otherwise.</summary>
        public void TakeExplosiveDamage(float amount)
        {
            if (Condition == CreatureCondition.Dead) return;
            if (Condition == CreatureCondition.Torpor) { SetCondition(CreatureCondition.Dead); return; }
            TakeBodyDamage(amount);
        }

        public void Tick(float deltaTime)
        {
            if (Condition != CreatureCondition.Torpor) return;
            TorporRemaining -= deltaTime;
            if (TorporRemaining <= 0f)
            {
                Health = MaxHealth * RecoverHealthFraction;
                TorporRemaining = 0f;
                SetCondition(CreatureCondition.Active);
            }
        }

        void EnterTorpor()
        {
            Health = 0f;
            downDamage = 0f;
            TorporRemaining = TorporMinSeconds + (TorporMaxSeconds - TorporMinSeconds) * Clamp01(random01());
            SetCondition(CreatureCondition.Torpor);
        }

        void SetCondition(CreatureCondition c)
        {
            if (c == Condition) return;
            var old = Condition;
            Condition = c;
            ConditionChanged?.Invoke(old, c);
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
