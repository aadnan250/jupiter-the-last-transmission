using System;

namespace Jupiter.Combat
{
    /// <summary>Player health: 100 HP, no regeneration, healed only by med-patches. Pure C#.</summary>
    public class HealthModel
    {
        public float Max { get; }
        public float LowThreshold { get; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public bool IsLow => !IsDead && Current <= LowThreshold;

        public event Action<float, float> Changed; // (previous, current)
        public event Action Died;

        public HealthModel(float max = 100f, float lowThreshold = 30f)
        {
            Max = max;
            LowThreshold = lowThreshold;
            Current = max;
        }

        public void Damage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            float previous = Current;
            Current = Math.Max(0f, Current - amount);
            Changed?.Invoke(previous, Current);
            if (IsDead) Died?.Invoke();
        }

        /// <summary>Returns how much was actually healed (0 when dead or full).</summary>
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f || Current >= Max) return 0f;
            float previous = Current;
            Current = Math.Min(Max, Current + amount);
            Changed?.Invoke(previous, Current);
            return Current - previous;
        }
    }

    /// <summary>
    /// Deliberately small inventory (design doc §4): one weapon, one tool, four consumable slots.
    /// Consumables stack up to <see cref="MaxStack"/> per slot. Pure C#.
    /// </summary>
    public class InventoryModel
    {
        public const int ConsumableSlots = 4;
        public const int MaxStack = 3;

        public string Weapon { get; private set; }
        public string Tool { get; private set; }

        readonly string[] items = new string[ConsumableSlots];
        readonly int[] counts = new int[ConsumableSlots];

        public event Action Changed;

        public void EquipWeapon(string id) { Weapon = id; Changed?.Invoke(); }
        public void EquipTool(string id) { Tool = id; Changed?.Invoke(); }

        public string SlotItem(int slot) => items[slot];
        public int SlotCount(int slot) => counts[slot];

        public int Count(string item)
        {
            int n = 0;
            for (int i = 0; i < ConsumableSlots; i++) if (items[i] == item) n += counts[i];
            return n;
        }

        /// <summary>Adds up to <paramref name="amount"/>, filling existing stacks first. Returns how many fit.</summary>
        public int Add(string item, int amount)
        {
            if (string.IsNullOrEmpty(item) || amount <= 0) return 0;
            int added = 0;
            for (int i = 0; i < ConsumableSlots && added < amount; i++)
                if (items[i] == item) { int take = Math.Min(MaxStack - counts[i], amount - added); counts[i] += take; added += take; }
            for (int i = 0; i < ConsumableSlots && added < amount; i++)
                if (items[i] == null) { int take = Math.Min(MaxStack, amount - added); items[i] = item; counts[i] = take; added += take; }
            if (added > 0) Changed?.Invoke();
            return added;
        }

        /// <summary>Uses one of an item. Empty slots are freed.</summary>
        public bool Consume(string item)
        {
            for (int i = ConsumableSlots - 1; i >= 0; i--)
            {
                if (items[i] != item) continue;
                if (--counts[i] == 0) items[i] = null;
                Changed?.Invoke();
                return true;
            }
            return false;
        }
    }

    public enum WeaponState { Ready, Cooling, Reloading, Empty }

    /// <summary>
    /// The bolt driver: a pneumatic construction tool, not a soldier's weapon. 6-bolt magazine, slow 2.2 s reload,
    /// a pause between shots, limited reserve. Time is passed in, so the state machine is deterministic and testable.
    /// </summary>
    public class WeaponModel
    {
        public int MagazineSize { get; }
        public int MaxReserve { get; }
        public float ReloadSeconds { get; }
        public float ShotInterval { get; }

        public int Loaded { get; private set; }
        public int Reserve { get; private set; }

        float cooldown, reloadRemaining;

        public event Action Changed;

        public WeaponModel(int magazine = 6, int reserve = 6, int maxReserve = 30, float reloadSeconds = 2.2f, float shotInterval = 0.6f)
        {
            MagazineSize = magazine;
            Loaded = magazine;
            MaxReserve = maxReserve;
            Reserve = Math.Min(reserve, maxReserve);
            ReloadSeconds = reloadSeconds;
            ShotInterval = shotInterval;
        }

        public WeaponState State =>
            reloadRemaining > 0f ? WeaponState.Reloading :
            Loaded == 0 ? WeaponState.Empty :
            cooldown > 0f ? WeaponState.Cooling : WeaponState.Ready;

        public bool TryFire()
        {
            if (State != WeaponState.Ready) return false;
            Loaded--;
            cooldown = ShotInterval;
            Changed?.Invoke();
            return true;
        }

        public bool TryReload()
        {
            if (reloadRemaining > 0f || Loaded >= MagazineSize || Reserve <= 0) return false;
            reloadRemaining = ReloadSeconds;
            Changed?.Invoke();
            return true;
        }

        public int AddAmmo(int bolts)
        {
            int added = Math.Max(0, Math.Min(bolts, MaxReserve - Reserve));
            Reserve += added;
            if (added > 0) Changed?.Invoke();
            return added;
        }

        public void Tick(float deltaTime)
        {
            if (cooldown > 0f) cooldown = Math.Max(0f, cooldown - deltaTime);
            if (reloadRemaining <= 0f) return;
            reloadRemaining -= deltaTime;
            if (reloadRemaining > 0f) return;
            reloadRemaining = 0f;
            int moved = Math.Min(MagazineSize - Loaded, Reserve);
            Loaded += moved;
            Reserve -= moved;
            Changed?.Invoke();
        }
    }
}
