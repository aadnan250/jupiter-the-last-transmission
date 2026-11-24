using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jupiter.Narrative
{
    public class DialogueParseException : Exception
    {
        public int LineNumber { get; }

        public DialogueParseException(string source, int line, string message)
            : base($"{source}:{line}: {message}") => LineNumber = line;
    }

    /// <summary>
    /// Parses the project's plain-text dialogue format into a <see cref="DialogueConversation"/>.
    /// Writers can author branching conversations in any text editor; the format is documented in
    /// docs/DIALOGUE_FORMAT.md. Errors report the file and line number.
    ///
    /// <code>
    /// === start                         node header (first node is the entry point)
    /// MARA: Okafor.                     line spoken in person
    /// LUCA (radio): Copy that.          line heard over the radio
    /// [if flag met_mara] MARA: Again?   conditional line
    /// ~ trust MARA +1                   effect (trust / flag / unflag)
    /// -> next_node                      jump (or -> END)
    /// * "Reply text" -> target          choice (max 4 per node)
    /// *! "Big decision" -> target       significant choice (eerie sting)
    ///   ~ flag made_big_decision        indented effect belongs to the choice above
    /// </code>
    /// </summary>
    public static class DialogueParser
    {
        static readonly Regex ConditionRx = new Regex(@"^\[if\s+([^\]]+)\]\s*", RegexOptions.Compiled);
        static readonly Regex LineRx = new Regex(@"^([A-Z][A-Z0-9_]*)\s*(\((radio)\))?\s*:\s*(.+)$", RegexOptions.Compiled);
        static readonly Regex GotoRx = new Regex(@"^->\s*([A-Za-z0-9_]+)\s*$", RegexOptions.Compiled);
        static readonly Regex ChoiceTargetRx = new Regex(@"^(.*?)\s*->\s*([A-Za-z0-9_]+)\s*$", RegexOptions.Compiled);
        static readonly Regex TrustCondRx = new Regex(@"^trust\s+([A-Za-z0-9_]+)\s*(>=|<=|==|>|<)\s*(-?\d+)$", RegexOptions.Compiled);
        static readonly Regex FlagCondRx = new Regex(@"^(!?)flag\s+([A-Za-z0-9_]+)$", RegexOptions.Compiled);
        static readonly Regex TrustEffectRx = new Regex(@"^trust\s+([A-Za-z0-9_]+)\s+([+-]\d+)$", RegexOptions.Compiled);
        static readonly Regex FlagEffectRx = new Regex(@"^(flag|unflag)\s+([A-Za-z0-9_]+)$", RegexOptions.Compiled);
        static readonly Regex NodeRx = new Regex(@"^===\s*([A-Za-z0-9_]+)\s*$", RegexOptions.Compiled);

        public static DialogueConversation Parse(string text, string id)
        {
            var conv = new DialogueConversation { Id = id };
            DialogueNode node = null;
            string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                int ln = i + 1;
                string raw = lines[i];
                string t = raw.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;

                var nodeMatch = NodeRx.Match(t);
                if (nodeMatch.Success)
                {
                    string nodeId = nodeMatch.Groups[1].Value;
                    if (nodeId == DialogueConversation.EndTarget) throw Error(id, ln, "'END' is reserved and cannot be a node name");
                    if (conv.Nodes.ContainsKey(nodeId)) throw Error(id, ln, $"duplicate node '{nodeId}'");
                    node = new DialogueNode { Id = nodeId };
                    conv.Nodes.Add(nodeId, node);
                    if (conv.StartNode == null) conv.StartNode = nodeId;
                    continue;
                }

                if (node == null) throw Error(id, ln, "content before the first '=== node' header");

                bool indented = raw.Length > 0 && char.IsWhiteSpace(raw[0]);

                // Effect
                if (t.StartsWith("~"))
                {
                    var effect = ParseEffect(t.Substring(1).Trim(), id, ln);
                    if (node.Choices.Count > 0)
                    {
                        if (!indented) throw Error(id, ln, "effects after choices must be indented under a choice");
                        node.Choices[node.Choices.Count - 1].Effects.Add(effect);
                    }
                    else node.Body.Add(new DialogueItem { Kind = DialogueItem.ItemKind.Effect, Effect = effect });
                    continue;
                }

                // Choice
                if (t.StartsWith("*"))
                {
                    if (node.Choices.Count >= DialogueConversation.MaxChoices)
                        throw Error(id, ln, $"more than {DialogueConversation.MaxChoices} choices in node '{node.Id}'");
                    node.Choices.Add(ParseChoice(t, id, ln));
                    continue;
                }

                if (node.Choices.Count > 0)
                    throw Error(id, ln, "only choices and indented effects may follow the first choice in a node");

                // Optional conditions, then a goto or a spoken line.
                string rest = t;
                var conditions = ParseConditions(ref rest, id, ln);

                var gotoMatch = GotoRx.Match(rest);
                if (gotoMatch.Success)
                {
                    node.Body.Add(new DialogueItem
                    {
                        Kind = DialogueItem.ItemKind.Goto,
                        GotoTarget = gotoMatch.Groups[1].Value,
                        GotoConditions = conditions
                    });
                    continue;
                }

                var lineMatch = LineRx.Match(rest);
                if (!lineMatch.Success)
                    throw Error(id, ln, $"cannot understand '{t}'. Expected 'SPEAKER: text', '-> node', '* choice -> node' or '~ effect'");

                node.Body.Add(new DialogueItem
                {
                    Kind = DialogueItem.ItemKind.Line,
                    Line = new DialogueLine
                    {
                        Speaker = lineMatch.Groups[1].Value,
                        Radio = lineMatch.Groups[3].Success,
                        Text = lineMatch.Groups[4].Value.Trim(),
                        Conditions = conditions,
                        SourceLine = ln
                    }
                });
            }

            Validate(conv, id);
            return conv;
        }

        static DialogueChoice ParseChoice(string t, string id, int ln)
        {
            bool significant = t.StartsWith("*!");
            string rest = t.Substring(significant ? 2 : 1).Trim();
            var conditions = ParseConditions(ref rest, id, ln);

            var m = ChoiceTargetRx.Match(rest);
            if (!m.Success) throw Error(id, ln, "a choice needs a target: * \"text\" -> node");
            string text = m.Groups[1].Value.Trim();
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"') text = text.Substring(1, text.Length - 2);
            if (text.Length == 0) throw Error(id, ln, "choice text is empty");

            return new DialogueChoice
            {
                Text = text,
                Target = m.Groups[2].Value,
                Significant = significant,
                Conditions = conditions,
                SourceLine = ln
            };
        }

        static List<Condition> ParseConditions(ref string rest, string id, int ln)
        {
            var list = new List<Condition>();
            Match m;
            while ((m = ConditionRx.Match(rest)).Success)
            {
                string body = m.Groups[1].Value.Trim();
                var trust = TrustCondRx.Match(body);
                var flag = FlagCondRx.Match(body);
                if (trust.Success)
                    list.Add(new Condition { Kind = Condition.ConditionKind.Trust, Name = trust.Groups[1].Value, Operator = trust.Groups[2].Value, Value = int.Parse(trust.Groups[3].Value) });
                else if (flag.Success)
                    list.Add(new Condition { Kind = flag.Groups[1].Value == "!" ? Condition.ConditionKind.NotFlag : Condition.ConditionKind.Flag, Name = flag.Groups[2].Value });
                else
                    throw Error(id, ln, $"unknown condition '[if {body}]'. Use 'flag x', '!flag x' or 'trust NAME >= n'");
                rest = rest.Substring(m.Length);
            }
            return list;
        }

        static Effect ParseEffect(string body, string id, int ln)
        {
            var trust = TrustEffectRx.Match(body);
            if (trust.Success)
                return new Effect { Kind = Effect.EffectKind.Trust, Name = trust.Groups[1].Value, Delta = int.Parse(trust.Groups[2].Value) };
            var flag = FlagEffectRx.Match(body);
            if (flag.Success)
                return new Effect { Kind = flag.Groups[1].Value == "flag" ? Effect.EffectKind.SetFlag : Effect.EffectKind.ClearFlag, Name = flag.Groups[2].Value };
            throw Error(id, ln, $"unknown effect '~ {body}'. Use 'trust NAME +n', 'flag x' or 'unflag x'");
        }

        static void Validate(DialogueConversation conv, string id)
        {
            if (conv.StartNode == null) throw Error(id, 1, "no nodes found");

            foreach (var node in conv.Nodes.Values)
            {
                if (node.Body.Count == 0 && node.Choices.Count == 0)
                    throw Error(id, 1, $"node '{node.Id}' is empty");
                foreach (var item in node.Body)
                    if (item.Kind == DialogueItem.ItemKind.Goto) CheckTarget(conv, id, item.GotoTarget, node.Id);
                foreach (var c in node.Choices) CheckTarget(conv, id, c.Target, node.Id, c.SourceLine);
            }
        }

        static void CheckTarget(DialogueConversation conv, string id, string target, string from, int line = 1)
        {
            if (target != DialogueConversation.EndTarget && !conv.Nodes.ContainsKey(target))
                throw Error(id, line, $"node '{from}' jumps to unknown node '{target}'");
        }

        static DialogueParseException Error(string id, int line, string message) => new DialogueParseException(id, line, message);
    }
}
