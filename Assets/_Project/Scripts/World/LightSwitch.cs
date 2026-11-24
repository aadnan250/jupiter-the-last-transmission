using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>Toggles a set of lights. A simple interactable for testing; reused for breaker panels later.</summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] Light[] lights;
        [SerializeField] bool isOn = true;

        public string Prompt => isOn ? "Lights off" : "Lights on";

        void Start() => Apply();

        public bool CanInteract(GameObject interactor) => lights != null && lights.Length > 0;

        public void Interact(GameObject interactor)
        {
            isOn = !isOn;
            Apply();
        }

        void Apply()
        {
            if (lights == null) return;
            foreach (var l in lights)
                if (l != null) l.enabled = isOn;
        }
    }
}
