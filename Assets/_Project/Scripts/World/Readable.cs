using System;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// A document, note or screen the player can read: environmental storytelling.
    /// Raises a static event rather than referencing the UI directly, so readables can be placed
    /// in any scene without wiring. <see cref="UI.ReadableUI"/> listens and shows the text.
    /// </summary>
    public class Readable : MonoBehaviour, IInteractable
    {
        [SerializeField] string title = "Note";
        [SerializeField, TextArea(4, 14)] string body = "";

        /// <summary>Fired when any readable is used. The UI toggles open/closed.</summary>
        public static event Action<Readable> Used;

        public string Title => title;
        public string Body => body;
        public string Prompt => "Read";

        public bool CanInteract(GameObject interactor) => !string.IsNullOrEmpty(body);

        public void Interact(GameObject interactor) => Used?.Invoke(this);

        /// <summary>Sets content from code (used by the editor scene builders).</summary>
        public void SetContent(string newTitle, string newBody)
        {
            title = newTitle;
            body = newBody;
        }
    }
}
