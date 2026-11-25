using System;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.World
{
    /// <summary>
    /// A code-locked terminal, such as Ashford's. Interact to open the keypad (<see cref="UI.KeypadUI"/> listens to
    /// <see cref="Opened"/>), enter the code, and the terminal unlocks, sets a story flag and shows its contents:
    /// a child <see cref="Readable"/>. Once open it stays open, including after a save and load.
    /// </summary>
    public class KeypadTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] string code = "0000";
        [Tooltip("Story flag set when the code is accepted. Also restores the unlocked state on load.")]
        [SerializeField] string unlockFlag;
        [Tooltip("What the terminal shows once unlocked.")]
        [SerializeField] Readable contents;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip keyClip;
        [SerializeField] AudioClip acceptClip;
        [SerializeField] AudioClip rejectClip;

        KeypadModel model;
        GameObject user;

        /// <summary>Fired when a player starts entering a code, and when they stop.</summary>
        public static event Action<KeypadTerminal> Opened;
        public static event Action<KeypadTerminal> Closed;

        public KeypadModel Model => model ??= new KeypadModel(code);
        public bool IsOpen => user != null;
        public Readable Contents => contents;

        public string Prompt => Model.Unlocked ? "Read" : IsOpen ? "Close keypad" : "Enter code";

        void Start()
        {
            var story = Story();
            if (story != null && !string.IsNullOrEmpty(unlockFlag) && story.HasFlag(unlockFlag)) Model.ForceUnlock();
        }

        static Narrative.StoryState Story()
        {
            if (Core.GameSession.Current != null) return Core.GameSession.Current.Story;
            var dialogue = FindAnyObjectByType<Narrative.DialogueManager>();
            return dialogue != null ? dialogue.Story : null;
        }

        public bool CanInteract(GameObject interactor) => interactor != null;

        public void Interact(GameObject interactor)
        {
            if (Model.Unlocked) { ShowContents(interactor); return; }
            if (IsOpen) Close();
            else Open(interactor);
        }

        public void Open(GameObject interactor)
        {
            if (IsOpen || Model.Unlocked) return;
            user = interactor;
            Model.Clear();
            Opened?.Invoke(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            user = null;
            Model.Clear();
            Closed?.Invoke(this);
        }

        /// <summary>Enters one digit. On the right code the terminal unlocks and shows its contents.</summary>
        public KeypadResult Press(int digit)
        {
            var result = Model.Press(digit);
            switch (result)
            {
                case KeypadResult.Pending: Play(keyClip, 0.5f); break;
                case KeypadResult.Rejected: Play(rejectClip, 0.8f); break;
                case KeypadResult.Accepted:
                    Play(acceptClip, 0.8f);
                    var story = Story();
                    if (story != null && !string.IsNullOrEmpty(unlockFlag)) story.SetFlag(unlockFlag);
                    var who = user;
                    Close();
                    ShowContents(who);
                    break;
            }
            return result;
        }

        void ShowContents(GameObject interactor)
        {
            if (contents != null) contents.Interact(interactor);
        }

        void Play(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume);
        }

        void OnDisable() => Close();
    }
}
