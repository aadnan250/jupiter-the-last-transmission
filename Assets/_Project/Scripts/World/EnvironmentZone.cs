using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// Trigger volume that switches the player's <see cref="EnvironmentProfile"/> while they are inside it,
    /// e.g. stepping out of the airlock onto Europa. Restores the player's default profile on exit.
    /// Requires a Collider with "Is Trigger" enabled.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnvironmentZone : MonoBehaviour
    {
        [SerializeField] EnvironmentProfile profile;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && profile != null) player.SetEnvironment(profile);
        }

        void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.Environment == profile) player.SetEnvironment(player.DefaultEnvironment);
        }
    }
}
