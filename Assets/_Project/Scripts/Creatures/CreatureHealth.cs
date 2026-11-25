using System;
using Jupiter.Combat;
using UnityEngine;

namespace Jupiter.Creatures
{
    /// <summary>Unity wrapper around <see cref="CreatureHealthModel"/>: ticks torpor and exposes the rules to hitboxes.</summary>
    public class CreatureHealth : MonoBehaviour
    {
        [SerializeField] float maxHealth = 300f;
        [SerializeField] float torporMinSeconds = 20f;
        [SerializeField] float torporMaxSeconds = 40f;
        [SerializeField] int nodeCount = 6;

        public CreatureHealthModel Model { get; private set; }

        /// <summary>Raised when a node ruptures (index), so visuals can burst that node.</summary>
        public event Action<int> NodeRuptured;
        /// <summary>Raised on any hit (for stagger and venting effects).</summary>
        public event Action<DamageInfo> Hit;

        void Awake()
        {
            Model = new CreatureHealthModel(() => UnityEngine.Random.value, nodeCount, maxHealth)
            {
                TorporMinSeconds = torporMinSeconds,
                TorporMaxSeconds = torporMaxSeconds
            };
        }

        void Update() => Model.Tick(Time.deltaTime);

        public void ApplyBodyHit(DamageInfo info)
        {
            if (info.Type == DamageType.Explosive) Model.TakeExplosiveDamage(info.Amount);
            else Model.TakeBodyDamage(info.Amount);
            Hit?.Invoke(info);
        }

        public void ApplyNodeHit(int index, DamageInfo info)
        {
            bool wasRuptured = Model.IsNodeRuptured(index);
            Model.RuptureNode(index);
            if (!wasRuptured && Model.IsNodeRuptured(index)) NodeRuptured?.Invoke(index);
            Hit?.Invoke(info);
        }
    }
}
