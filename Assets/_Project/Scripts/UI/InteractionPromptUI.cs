using Jupiter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>
    /// Shows the crosshair and a one-word prompt when the player looks at an interactable.
    /// Event-driven: listens to <see cref="PlayerInteraction.TargetChanged"/> instead of polling.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] PlayerInteraction interaction;
        [SerializeField] Text promptText;
        [SerializeField] Graphic crosshair;
        [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] Color activeColor = new Color(1f, 1f, 1f, 0.95f);

        void OnEnable()
        {
            if (interaction != null) interaction.TargetChanged += Show;
            Show(interaction != null ? interaction.Current : null);
        }

        void OnDisable()
        {
            if (interaction != null) interaction.TargetChanged -= Show;
        }

        void Show(IInteractable target)
        {
            bool active = target != null;
            if (promptText != null)
            {
                promptText.enabled = active;
                promptText.text = active ? target.Prompt : string.Empty;
            }
            if (crosshair != null) crosshair.color = active ? activeColor : idleColor;
        }
    }
}
