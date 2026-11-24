using Jupiter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>
    /// Shows the crosshair and a prompt such as "[E] Talk to Mara" (or the PlayStation Square button)
    /// when the player looks at an interactable. Event-driven: reacts to target and input-device changes.
    /// Also keeps the controls hint at the bottom of the screen in sync with the active device.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] PlayerInteraction interaction;
        [SerializeField] Text promptText;
        [SerializeField] Graphic crosshair;
        [SerializeField] Text controlsHelpText;
        [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] Color activeColor = new Color(1f, 1f, 1f, 0.95f);

        void OnEnable()
        {
            if (interaction != null) interaction.TargetChanged += Show;
            InputDeviceWatcher.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (interaction != null) interaction.TargetChanged -= Show;
            InputDeviceWatcher.Changed -= Refresh;
        }

        void Refresh()
        {
            Show(interaction != null ? interaction.Current : null);
            if (controlsHelpText != null) controlsHelpText.text = ButtonGlyphs.ControlsHelp;
        }

        void Show(IInteractable target)
        {
            bool active = target != null;
            if (promptText != null)
            {
                promptText.enabled = active;
                promptText.text = active ? $"[{ButtonGlyphs.Interact}]  {target.Prompt}" : string.Empty;
            }
            if (crosshair != null) crosshair.color = active ? activeColor : idleColor;
        }
    }
}
