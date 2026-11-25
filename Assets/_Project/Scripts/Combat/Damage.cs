using UnityEngine;

namespace Jupiter.Combat
{
    public enum DamageType { Bolt, Explosive, Melee }

    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public Vector3 Point;
        public GameObject Source;

        public DamageInfo(float amount, DamageType type, Vector3 point, GameObject source = null)
        {
            Amount = amount;
            Type = type;
            Point = point;
            Source = source;
        }
    }

    /// <summary>Anything that can be hurt: the player, creature body parts, creature pressure nodes.</summary>
    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
    }
}
