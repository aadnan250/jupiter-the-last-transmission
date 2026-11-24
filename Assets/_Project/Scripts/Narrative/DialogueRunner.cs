using System;
using System.Collections.Generic;

namespace Jupiter.Narrative
{
    /// <summary>
    /// Walks a <see cref="DialogueConversation"/> one step at a time against a <see cref="StoryState"/>.
    /// Pure logic with no Unity dependencies: the MonoBehaviour <c>DialogueManager</c> drives it and draws the UI.
    ///
    /// Each step is one of: a line to show, a set of choices (1-4), or the end.
    /// Conditions hide lines, jumps and choices; effects change the story state as they are reached.
    /// </summary>
    public class DialogueRunner
    {
        public enum StepKind { Line, Choices, End }

        /// <summary>Speaker id used when the player's chosen reply is spoken aloud.</summary>
        public const string PlayerSpeaker = "SAM";

        const int MaxStepsPerAdvance = 1000; // guards against "-> a" / "-> b" loops with no lines

        readonly DialogueConversation conversation;
        readonly StoryState state;
        readonly List<DialogueChoice> visibleChoices = new List<DialogueChoice>();

        DialogueNode node;
        int index;
        string pendingTarget;

        public StepKind Kind { get; private set; } = StepKind.End;
        public DialogueLine CurrentLine { get; private set; }
        public IReadOnlyList<DialogueChoice> CurrentChoices => visibleChoices;
        public DialogueConversation Conversation => conversation;

        public DialogueRunner(DialogueConversation conversation, StoryState state)
        {
            this.conversation = conversation ?? throw new ArgumentNullException(nameof(conversation));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Start()
        {
            Jump(conversation.StartNode);
            Continue();
        }

        /// <summary>Moves past the current line.</summary>
        public void Advance()
        {
            if (Kind != StepKind.Line) throw new InvalidOperationException($"Advance() called while {Kind}");
            if (pendingTarget != null)
            {
                // The player's echoed reply has been shown; now go where the choice leads.
                string target = pendingTarget;
                pendingTarget = null;
                if (!Jump(target)) { Finish(); return; }
            }
            Continue();
        }

        /// <summary>Picks one of <see cref="CurrentChoices"/>. Returns the choice so callers can react to significance.</summary>
        public DialogueChoice Choose(int choiceIndex)
        {
            if (Kind != StepKind.Choices) throw new InvalidOperationException($"Choose() called while {Kind}");
            if (choiceIndex < 0 || choiceIndex >= visibleChoices.Count) throw new ArgumentOutOfRangeException(nameof(choiceIndex));

            var choice = visibleChoices[choiceIndex];
            foreach (var e in choice.Effects) e.Apply(state);

            if (!choice.IsAction)
            {
                // Show the player's reply as a spoken line before continuing.
                CurrentLine = new DialogueLine { Speaker = PlayerSpeaker, Text = choice.Text, SourceLine = choice.SourceLine };
                Kind = StepKind.Line;
                pendingTarget = choice.Target;
                visibleChoices.Clear();
                return choice;
            }

            visibleChoices.Clear();
            if (!Jump(choice.Target)) Finish();
            else Continue();
            return choice;
        }

        void Continue()
        {
            for (int guard = 0; guard < MaxStepsPerAdvance; guard++)
            {
                if (index < node.Body.Count)
                {
                    var item = node.Body[index++];
                    switch (item.Kind)
                    {
                        case DialogueItem.ItemKind.Effect:
                            item.Effect.Apply(state);
                            continue;
                        case DialogueItem.ItemKind.Goto:
                            if (!Condition.All(item.GotoConditions, state)) continue;
                            if (!Jump(item.GotoTarget)) { Finish(); return; }
                            continue;
                        default:
                            if (!Condition.All(item.Line.Conditions, state)) continue;
                            CurrentLine = item.Line;
                            Kind = StepKind.Line;
                            return;
                    }
                }

                // End of the node body: offer choices if any are visible.
                visibleChoices.Clear();
                foreach (var c in node.Choices)
                    if (Condition.All(c.Conditions, state)) visibleChoices.Add(c);

                if (visibleChoices.Count > 0) { Kind = StepKind.Choices; CurrentLine = null; }
                else Finish();
                return;
            }
            throw new InvalidOperationException($"Dialogue '{conversation.Id}' looped without showing a line (node '{node.Id}')");
        }

        /// <summary>Moves to a node. Returns false for END.</summary>
        bool Jump(string target)
        {
            if (target == DialogueConversation.EndTarget) return false;
            node = conversation.Nodes[target];
            index = 0;
            return true;
        }

        void Finish()
        {
            Kind = StepKind.End;
            CurrentLine = null;
            visibleChoices.Clear();
        }
    }
}
