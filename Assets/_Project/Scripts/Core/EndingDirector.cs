using Jupiter.Narrative;
using UnityEngine;

namespace Jupiter.Core
{
    /// <summary>
    /// The end of the game (Chapter 10.3). After the reunion (<see cref="triggerFlag"/>) the screen fades. If the
    /// sample was vented: the names of the crew, one at a time. If it was kept: a quarantine monitor in the corner of
    /// the frame, pulsing at 0.4 Hz, then the names. Then the credits, then THE END. Text is driven by
    /// <see cref="EndingScript"/> so the order is fixed and tested.
    /// </summary>
    public class EndingDirector : MonoBehaviour
    {
        [SerializeField] string triggerFlag = "home_reunited";
        [SerializeField] string keptFlag = "sample_kept";
        [SerializeField] float delaySeconds = 3f;
        [SerializeField] AudioSource music;
        [SerializeField] DialogueManager dialogue;

        ScreenFade fade;
        float t = -1f, waited;
        EndingScript script;

        public bool Started => t >= 0f;
        public bool Finished => script != null && t >= script.Duration;
        public string CurrentText => fade != null ? fade.Text : string.Empty;

        void Start()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
            fade = gameObject.AddComponent<ScreenFade>();
        }

        void Update()
        {
            if (dialogue == null) return;
            if (!Started)
            {
                if (!dialogue.Story.HasFlag(triggerFlag) || dialogue.IsActive) return;
                if ((waited += Time.deltaTime) < delaySeconds) return;
                Begin();
            }
            t += Time.deltaTime;
            var frame = script.At(t);
            fade.Alpha = frame.Black;
            fade.Text = frame.Text;
            fade.TextAlpha = frame.TextAlpha;
            fade.FontSize = frame.Size;
        }

        /// <summary>Starts the ending now. Public for tests.</summary>
        public void Begin()
        {
            if (Started) return;
            t = 0f;
            script = new EndingScript(dialogue.Story.HasFlag(keptFlag));
            var input = FindAnyObjectByType<Player.InputReader>();
            if (input != null) input.GameplayBlocked = true;
            if (music != null) music.Play();
        }
    }
}
