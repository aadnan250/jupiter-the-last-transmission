using Jupiter.Combat;
using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>
    /// A collider on a creature. Body parts take ordinary damage; a pressure node (glowing cyan sphere)
    /// ruptures on any hit. Rupturing three nodes is the only reliable kill (story bible §8.1).
    /// </summary>
    public class CreatureHitbox : MonoBehaviour, IDamageable
    {
        [SerializeField] CreatureHealth owner;
        [SerializeField] bool isNode;
        [SerializeField] int nodeIndex;

        public bool IsNode => isNode;
        public int NodeIndex => nodeIndex;
        public CreatureHealth Owner => owner;

        void Awake()
        {
            if (owner == null) owner = GetComponentInParent<CreatureHealth>();
        }

        public void TakeDamage(DamageInfo info)
        {
            if (owner == null) return;
            if (isNode) owner.ApplyNodeHit(nodeIndex, info);
            else owner.ApplyBodyHit(info);
        }
    }
}
