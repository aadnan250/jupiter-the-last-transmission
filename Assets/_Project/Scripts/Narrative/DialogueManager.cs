using System;
using Jupiter.Core;
using Jupiter.Player;
using Jupiter.UI;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Plays conversations: drives a <see cref="DialogueRunner"/>, shows lines and up to four replies on the
    /// <see cref="DialogueUI"/>, switches input to the Dialogue map, plays radio squelch for radio lines,
    /// and announces significant choices so <see cref="ConsequenceFeedback"/> can react.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] InputReader input;
        [SerializeField] DialogueUI ui;

        [Header("Radio")]
        [SerializeField] AudioSource radioSource;
        [SerializeField] AudioClip radioSquelch;

        DialogueRunner runner;
        Action onFinished;
        int startedFrame;
        StoryState fallbackState;

        public bool IsActive => runner != null;
        public DialogueRunner Runner => runner;

        /// <summary>Raised when the player picks a reply marked as significant (*! in the script).</summary>
        public event Action<DialogueChoice> SignificantChoiceMade;
        public event Action<string> ConversationEnded;

        /// <summary>The shared story state, or a local one in scenes without a <see cref="GameSession"/>.</summary>
        public StoryState Story => GameSession.Current != null ? GameSession.Current.Story : (fallbackState ??= new StoryState());

        public bool Play(DialogueConversation conversation, Action finished = null)
        {
            if (IsActive || conversation == null) return false;

            runner = new DialogueRunner(conversation, Story);
            onFinished = finished;
            startedFrame = Time.frameCount;
            if (input != null) input.SetDialogueMode(true);

            runner.Start();
            Render();
            return true;
        }

        /// <summary>Picks a reply by index. Public so tests and UI buttons can drive the conversation.</summary>
        public void Choose(int index)
        {
            if (!IsActive || runner.Kind != DialogueRunner.StepKind.Choices || index >= runner.CurrentChoices.Count) return;
            var choice = runner.Choose(index);
            if (choice.Significant) SignificantChoiceMade?.Invoke(choice);
            Render();
        }

        /// <summary>Moves to the next line (finishes the typewriter first if it is still running).</summary>
        public void Advance()
        {
            if (!IsActive || runner.Kind != DialogueRunner.StepKind.Line) return;
            if (ui != null && ui.IsTyping) { ui.CompleteTyping(); return; }
            runner.Advance();
            Render();
        }

        void Update()
        {
            if (!IsActive || input == null) return;
            if (Time.frameCount == startedFrame) return; // the press that opened the conversation must not skip its first line

            if (runner.Kind == DialogueRunner.StepKind.Line && input.AdvancePressedThisFrame) Advance();
            else if (runner.Kind == DialogueRunner.StepKind.Choices)
                for (int i = 0; i < DialogueConversation.MaxChoices; i++)
                    if (input.ChoicePressedThisFrame(i)) { Choose(i); break; }
        }

        void Render()
        {
            switch (runner.Kind)
            {
                case DialogueRunner.StepKind.Line:
                    var line = runner.CurrentLine;
                    if (line.Radio && radioSource != null && radioSquelch != null) radioSource.PlayOneShot(radioSquelch, 0.6f);
                    if (ui != null) ui.ShowLine(Characters.DisplayName(line.Speaker), Characters.SubtitleColor(line.Speaker), line.Text, line.Radio);
                    break;
                case DialogueRunner.StepKind.Choices:
                    if (ui != null) ui.ShowChoices(runner.CurrentChoices);
                    break;
                default:
                    Finish();
                    break;
            }
        }

        void Finish()
        {
            string id = runner.Conversation.Id;
            runner = null;
            if (ui != null) ui.Hide();
            if (input != null) input.SetDialogueMode(false);

            var callback = onFinished;
            onFinished = null;
            callback?.Invoke();
            ConversationEnded?.Invoke(id);

            if (GameSession.Current != null && GameSession.Current.AutosaveEnabled)
                GameSession.Current.SaveGame(); // autosave after every conversation
        }
    }
}
