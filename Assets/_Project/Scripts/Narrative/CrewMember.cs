using System.Collections.Generic;
using Jupiter.Player;
using UnityEngine;

namespace Jupiter.Narrative
{
    /// <summary>
    /// A crew member the player can talk to. Plays the first conversation in <see cref="conversations"/>
    /// that hasn't been completed yet; once all are done, says a short idle line.
    /// Turns to face the player while talking. Completing a conversation is recorded in the story state.
    /// </summary>
    public class CrewMember : MonoBehaviour, IInteractable
    {
        [SerializeField] string characterId = "MARA";
        [SerializeField] TextAsset[] conversations;
        [SerializeField, TextArea] string idleLine = "Not now, Okafor.";
        [SerializeField] DialogueManager dialogue;
        [SerializeField] float turnSpeed = 240f;

        static readonly Dictionary<TextAsset, DialogueConversation> Cache = new Dictionary<TextAsset, DialogueConversation>();

        Transform lookTarget;

        public string CharacterId => characterId;
        public string Prompt => $"Talk to {Characters.DisplayName(characterId)}";

        void Awake()
        {
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
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
            else
            {
                dialogue.Play(IdleConversation(), () => lookTarget = null);
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

        DialogueConversation IdleConversation()
        {
            var conv = new DialogueConversation { Id = $"{characterId.ToLowerInvariant()}_idle", StartNode = "idle" };
            var node = new DialogueNode { Id = "idle" };
            node.Body.Add(new DialogueItem { Kind = DialogueItem.ItemKind.Line, Line = new DialogueLine { Speaker = characterId, Text = idleLine } });
            conv.Nodes.Add("idle", node);
            return conv;
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
