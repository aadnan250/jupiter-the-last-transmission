using Jupiter.Player;
using Jupiter.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jupiter.UI
{
    /// <summary>
    /// Full-screen reading panel for <see cref="Readable"/> objects. While open, player movement and look
    /// are blocked; pressing Interact again (or Escape) closes it.
    /// </summary>
    public class ReadableUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] InputReader input;

        Readable current;

        public bool IsOpen => current != null;
        public Readable Current => current;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable() => Readable.Used += Toggle;

        void OnDisable()
        {
            Readable.Used -= Toggle;
            Close();
        }

        void Update()
        {
            if (current != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        void Toggle(Readable readable)
        {
            if (current == readable) { Close(); return; }

            current = readable;
            if (titleText != null) titleText.text = readable.Title;
            if (bodyText != null) bodyText.text = readable.Body;
            if (panel != null) panel.SetActive(true);
            if (input != null) input.GameplayBlocked = true;
        }

        public void Close()
        {
            current = null;
            if (panel != null) panel.SetActive(false);
            if (input != null) input.GameplayBlocked = false;
        }
    }
}
