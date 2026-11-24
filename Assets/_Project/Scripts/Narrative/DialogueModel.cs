using System.Collections.Generic;

namespace Jupiter.Narrative
{
    /// <summary>A test against <see cref="StoryState"/>: a flag, a missing flag, or a trust comparison.</summary>
    public class Condition
    {
        public enum ConditionKind { Flag, NotFlag, Trust }

        public ConditionKind Kind;
        public string Name;
        public string Operator;
        public int Value;

        public bool Evaluate(StoryState state)
        {
            switch (Kind)
            {
                case ConditionKind.Flag: return state.HasFlag(Name);
                case ConditionKind.NotFlag: return !state.HasFlag(Name);
                default:
                    int t = state.GetTrust(Name);
                    switch (Operator)
                    {
                        case ">=": return t >= Value;
                        case "<=": return t <= Value;
                        case ">": return t > Value;
                        case "<": return t < Value;
                        default: return t == Value; // "=="
                    }
            }
        }

        public static bool All(List<Condition> conditions, StoryState state)
        {
            if (conditions == null) return true;
            foreach (var c in conditions)
                if (!c.Evaluate(state)) return false;
            return true;
        }
    }

    /// <summary>A change to <see cref="StoryState"/>: adjust trust, set or clear a flag.</summary>
    public class Effect
    {
        public enum EffectKind { Trust, SetFlag, ClearFlag }

        public EffectKind Kind;
        public string Name;
        public int Delta;

        public void Apply(StoryState state)
        {
            switch (Kind)
            {
                case EffectKind.Trust: state.AddTrust(Name, Delta); break;
                case EffectKind.SetFlag: state.SetFlag(Name); break;
                default: state.ClearFlag(Name); break;
            }
        }
    }

    public class DialogueLine
    {
        public string Speaker;   // character id, e.g. "MARA"
        public string Text;
        public bool Radio;
        public List<Condition> Conditions = new List<Condition>();
        public int SourceLine;
    }

    public class DialogueChoice
    {
        public string Text;
        public string Target;
        /// <summary>A choice with lasting consequences. Triggers the eerie feedback sting.</summary>
        public bool Significant;
        public List<Condition> Conditions = new List<Condition>();
        public List<Effect> Effects = new List<Effect>();
        public int SourceLine;

        /// <summary>Choices written in parentheses are actions, e.g. "(Say nothing.)", and are not spoken aloud.</summary>
        public bool IsAction => Text.StartsWith("(");
    }

    public class DialogueItem
    {
        public enum ItemKind { Line, Effect, Goto }

        public ItemKind Kind;
        public DialogueLine Line;
        public Effect Effect;
        public string GotoTarget;
        public List<Condition> GotoConditions = new List<Condition>();
    }

    public class DialogueNode
    {
        public string Id;
        public List<DialogueItem> Body = new List<DialogueItem>();
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
    }

    public class DialogueConversation
    {
        public const string EndTarget = "END";
        public const int MaxChoices = 4;

        public string Id;
        public string StartNode;
        public Dictionary<string, DialogueNode> Nodes = new Dictionary<string, DialogueNode>();
    }
}
