using System;

namespace Jupiter.Combat
{
    public enum BeamState { Ready, Firing, Reloading, Empty }

    /// <summary>
    /// The arc beam rifle's power: a cell holds <see cref="CellCharge"/> units and drains while the trigger is held;
    /// spare cells swap in on reload (slow, two hands). Time is passed in, so it is deterministic. Pure C#.
    /// </summary>
    public class BeamModel
    {
        public float CellCharge { get; }
        public float DrainPerSecond { get; }
        public float ReloadSeconds { get; }
        public int MaxCells { get; }

        public float Charge { get; private set; }
        public int SpareCells { get; private set; }
        public bool Firing { get; private set; }

        float reloadRemaining;

        public BeamModel(float cellCharge = 100f, float drainPerSecond = 22f, float reloadSeconds = 1.8f, int spareCells = 4, int maxCells = 8)
        {
            CellCharge = cellCharge;
            DrainPerSecond = drainPerSecond;
            ReloadSeconds = reloadSeconds;
            MaxCells = maxCells;
            Charge = cellCharge;
            SpareCells = Math.Min(spareCells, maxCells);
        }

        /// <summary>0..1 charge left in the current cell.</summary>
        public float ChargeFraction => CellCharge <= 0f ? 0f : Charge / CellCharge;

        public BeamState State =>
            reloadRemaining > 0f ? BeamState.Reloading :
            Charge <= 0f ? BeamState.Empty :
            Firing ? BeamState.Firing : BeamState.Ready;

        /// <summary>
        /// Advances time with the trigger held or not. Returns how many seconds the beam actually fired this step
        /// (less than <paramref name="deltaTime"/> when the cell runs dry part-way), so damage stays exact.
        /// </summary>
        public float Tick(float deltaTime, bool triggerHeld)
        {
            deltaTime = Math.Max(0f, deltaTime);
            if (reloadRemaining > 0f)
            {
                Firing = false;
                reloadRemaining -= deltaTime;
                if (reloadRemaining <= 0f) { reloadRemaining = 0f; Charge = CellCharge; }
                return 0f;
            }
            if (!triggerHeld || Charge <= 0f) { Firing = false; return 0f; }

            float fired = Math.Min(deltaTime, Charge / DrainPerSecond);
            Charge = Math.Max(0f, Charge - fired * DrainPerSecond);
            Firing = Charge > 0f;
            return fired;
        }

        /// <summary>Swaps in a spare cell (whatever is left in the current one is lost). False if none or full.</summary>
        public bool TryReload()
        {
            if (reloadRemaining > 0f || SpareCells <= 0 || Charge >= CellCharge) return false;
            SpareCells--;
            Charge = 0f;
            Firing = false;
            reloadRemaining = ReloadSeconds;
            return true;
        }

        public int AddCells(int cells)
        {
            int added = Math.Max(0, Math.Min(cells, MaxCells - SpareCells));
            SpareCells += added;
            return added;
        }
    }
}
