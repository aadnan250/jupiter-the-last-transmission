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

        [Header("Voices")]
        [Tooltip("2D source for Sam's own lines.")]
        [SerializeField] AudioSource playerVoice;
        [Tooltip("2D source with band-pass and distortion filters for radio lines.")]
        [SerializeField] AudioSource radioVoice;

        AudioSource speaking;

        /// <summary>The clip playing for the current line, if any (tests and lip sync read this).</summary>
        public AudioClip CurrentVoiceClip => speaking != null && speaking.isPlaying ? speaking.clip : null;

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

        /// <summary>
        /// True while a conversation plays "on the move": radio chatter during a chase. The player keeps full
        /// control, lines advance on their own, and the conversation must not offer replies.
        /// </summary>
        public bool OnTheMove { get; private set; }

        float lineEndsAt;

        public bool Play(DialogueConversation conversation, Action finished = null) => Begin(conversation, finished, false);

        /// <summary>Plays a reply-free conversation without taking control away (see <see cref="OnTheMove"/>).</summary>
        public bool PlayOnTheMove(DialogueConversation conversation, Action finished = null) => Begin(conversation, finished, true);

        bool Begin(DialogueConversation conversation, Action finished, bool onTheMove)
        {
            if (IsActive || conversation == null) return false;

            runner = new DialogueRunner(conversation, Story);
            onFinished = finished;
            startedFrame = Time.frameCount;
            OnTheMove = onTheMove;
            if (input != null && !onTheMove) input.SetDialogueMode(true);

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
            if (IsActive && OnTheMove)
            {
                if (runner.Kind == DialogueRunner.StepKind.Line && Time.time >= lineEndsAt)
                {
                    if (ui != null && ui.IsTyping) ui.CompleteTyping();
                    runner.Advance();
                    Render();
                }
                return;
            }
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
                    PlayVoice(line);
                    if (ui != null) ui.ShowLine(Characters.DisplayName(line.Speaker), Characters.SubtitleColor(line.Speaker), line.Text, line.Radio);
                    lineEndsAt = Time.time + DialogueTiming.LineSeconds(speaking != null ? speaking.clip.length : 0f, line.Text.Length);
                    break;
                case DialogueRunner.StepKind.Choices when OnTheMove:
                    // Nobody can pick a reply while running: on-the-move scripts must not have any.
                    Debug.LogWarning($"[DialogueManager] '{runner.Conversation.Id}' reached replies while on the move; ending it.");
                    Finish();
                    break;
                case DialogueRunner.StepKind.Choices:
                    StopVoice();
                    if (ui != null) ui.ShowChoices(runner.CurrentChoices);
                    break;
                default:
                    Finish();
                    break;
            }
        }

        /// <summary>
        /// Plays the line's voice from the right place: radio lines through the radio filter, Sam's lines from
        /// the player, and crew lines from the crew member's own 3D source (which also drives their lip sync).
        /// </summary>
        void PlayVoice(DialogueLine line)
        {
            StopVoice();
            var clip = VoiceLibrary.Find(runner.Conversation.Id, line.SourceLine);
            if (clip == null) return;

            AudioSource source;
            if (line.Radio) source = radioVoice;
            else if (line.Speaker == DialogueRunner.PlayerSpeaker) source = playerVoice;
            else if (!CrewMember.TryGetVoice(line.Speaker, out source)) source = radioVoice;

            if (source == null) return;
            source.clip = clip;
            source.Play();
            speaking = source;
        }

        void StopVoice()
        {
            if (speaking != null) speaking.Stop();
            speaking = null;
        }

        void Finish()
        {
            StopVoice();
            string id = runner.Conversation.Id;
            runner = null;
            if (ui != null) ui.Hide();
            if (input != null && !OnTheMove) input.SetDialogueMode(false);
            OnTheMove = false;

            var callback = onFinished;
            onFinished = null;
            callback?.Invoke();
            ConversationEnded?.Invoke(id);

            if (GameSession.Current != null && GameSession.Current.AutosaveEnabled)
                GameSession.Current.SaveGame(); // autosave after every conversation
        }
    }
}
