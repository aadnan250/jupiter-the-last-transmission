using System.Collections.Generic;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// A crew member the player can talk to. Plays the first conversation in <see cref="conversations"/>
    /// that hasn't been completed yet; once all are done, plays their short idle conversation.
    /// Turns to face the player while talking, and owns the 3D AudioSource their voice comes from.
    /// </summary>
    public class CrewMember : MonoBehaviour, IInteractable
    {
        [SerializeField] string characterId = "MARA";
        [SerializeField] TextAsset[] conversations;
        [Tooltip("Played once every conversation is done. A one-line script, so it is voiced like everything else.")]
        [SerializeField] TextAsset idleConversation;
        [SerializeField] AudioSource voice;
        [SerializeField] DialogueManager dialogue;
        [SerializeField] float turnSpeed = 240f;

        static readonly Dictionary<TextAsset, DialogueConversation> Cache = new Dictionary<TextAsset, DialogueConversation>();
        static readonly Dictionary<string, AudioSource> Voices = new Dictionary<string, AudioSource>();

        Transform lookTarget;

        public string CharacterId => characterId;
        public AudioSource Voice => voice;
        public string Prompt => $"Talk to {Characters.DisplayName(characterId)}";

        /// <summary>The AudioSource a character speaks from, if they are in the current scene.</summary>
        public static bool TryGetVoice(string characterId, out AudioSource source) =>
            Voices.TryGetValue(characterId.ToUpperInvariant(), out source) && source != null;

        void Awake()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        }

        void OnEnable()
        {
            if (voice != null) Voices[characterId.ToUpperInvariant()] = voice;
        }

        void OnDisable()
        {
            if (Voices.TryGetValue(characterId.ToUpperInvariant(), out var v) && v == voice)
                Voices.Remove(characterId.ToUpperInvariant());
        }

        public bool CanInteract(GameObject interactor) => dialogue != null && !dialogue.IsActive;

        public void Interact(GameObject interactor)
        {
            if (dialogue == null) return;
            lookTarget = interactor != null ? interactor.transform : null;

            var asset = NextConversation();
            if (asset != null)
            {
                var conv = Load(asset);
                dialogue.Play(conv, () =>
                {
                    dialogue.Story.MarkCompleted(conv.Id);
                    lookTarget = null;
                });
            }
            else if (idleConversation != null)
            {
                dialogue.Play(Load(idleConversation), () => lookTarget = null);
            }
        }

        /// <summary>The next conversation this character has for the player, or null if all are done.</summary>
        public TextAsset NextConversation()
        {
            if (conversations == null) return null;
            foreach (var c in conversations)
                if (c != null && !dialogue.Story.IsCompleted(c.name)) return c;
            return null;
        }

        void Update()
        {
            if (lookTarget == null) return;
            Vector3 to = lookTarget.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), turnSpeed * Time.deltaTime);
        }

        public static DialogueConversation Load(TextAsset asset)
        {
            if (!Cache.TryGetValue(asset, out var conv))
            {
                conv = DialogueParser.Parse(asset.text, asset.name);
                Cache[asset] = conv;
            }
            return conv;
        }
    }
}
