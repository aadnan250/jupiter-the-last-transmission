using System.Linq;
using Jupiter.Narrative;
using NUnit.Framework;

namespace Jupiter.Tests
{
    /// <summary>Unit tests for the dialogue script parser, the runner and the story state.</summary>
    public class DialogueTests
    {
        const string Sample = @"
# comment
=== start
MARA: Okafor.
[if flag met] MARA: Again?
LUCA (radio): Copy.
~ flag started
* ""Hello."" -> friendly
  ~ trust MARA +2
*! ""(Say nothing.)"" -> silent
  ~ flag stayed_silent
* [if trust MARA >= 3] ""Old friend."" -> END
  ~ trust MARA +1

=== friendly
MARA: Hi.
-> END

=== silent
[if flag stayed_silent] -> after
MARA: Unreachable.

=== after
MARA: Right.
";

        static DialogueConversation Parse(string text = Sample) => DialogueParser.Parse(text, "sample");

        // ---------- parser ----------

        [Test]
        public void Parser_ReadsNodesLinesChoicesAndEffects()
        {
            var c = Parse();
            Assert.AreEqual("start", c.StartNode);
            Assert.AreEqual(4, c.Nodes.Count);

            var start = c.Nodes["start"];
            Assert.AreEqual(4, start.Body.Count, "3 lines + 1 effect");
            Assert.AreEqual(3, start.Choices.Count);
            Assert.AreEqual("Hello.", start.Choices[0].Text, "surrounding quotes are stripped");
            Assert.AreEqual(2, start.Choices[0].Effects[0].Delta);
            Assert.IsTrue(start.Choices[1].Significant);
            Assert.IsTrue(start.Choices[1].IsAction);
            Assert.AreEqual(Condition.ConditionKind.Trust, start.Choices[2].Conditions[0].Kind);
        }

        [Test]
        public void Parser_DetectsRadioLines()
        {
            var lines = Parse().Nodes["start"].Body.Where(i => i.Kind == DialogueItem.ItemKind.Line).Select(i => i.Line).ToList();
            Assert.IsFalse(lines[0].Radio);
            Assert.IsTrue(lines[2].Radio);
            Assert.AreEqual("LUCA", lines[2].Speaker);
        }

        [Test]
        public void Parser_RejectsMoreThanFourChoices()
        {
            const string text = "=== a\nMARA: x\n* 1 -> END\n* 2 -> END\n* 3 -> END\n* 4 -> END\n* 5 -> END\n";
            var ex = Assert.Throws<DialogueParseException>(() => DialogueParser.Parse(text, "t"));
            Assert.AreEqual(7, ex.LineNumber);
        }

        [Test]
        public void Parser_RejectsUnknownTarget()
        {
            var ex = Assert.Throws<DialogueParseException>(() => DialogueParser.Parse("=== a\nMARA: x\n-> nowhere\n", "t"));
            StringAssert.Contains("nowhere", ex.Message);
        }

        [Test]
        public void Parser_RejectsUnindentedEffectAfterChoices()
        {
            const string text = "=== a\nMARA: x\n* go -> END\n~ flag bad\n";
            Assert.Throws<DialogueParseException>(() => DialogueParser.Parse(text, "t"));
        }

        [Test]
        public void Parser_RejectsGarbageWithLineNumber()
        {
            var ex = Assert.Throws<DialogueParseException>(() => DialogueParser.Parse("=== a\nMARA: fine\nthis is not valid\n", "t"));
            Assert.AreEqual(3, ex.LineNumber);
        }

        // ---------- runner ----------

        [Test]
        public void Runner_SkipsLinesWhoseConditionsFail()
        {
            var state = new StoryState();
            var r = new DialogueRunner(Parse(), state);
            r.Start();
            Assert.AreEqual("Okafor.", r.CurrentLine.Text);
            r.Advance();
            Assert.AreEqual("Copy.", r.CurrentLine.Text, "the [if flag met] line is hidden");
        }

        [Test]
        public void Runner_ShowsConditionalLineWhenFlagSet()
        {
            var state = new StoryState();
            state.SetFlag("met");
            var r = new DialogueRunner(Parse(), state);
            r.Start();
            r.Advance();
            Assert.AreEqual("Again?", r.CurrentLine.Text);
        }

        [Test]
        public void Runner_AppliesBodyEffectsAndHidesLockedChoices()
        {
            var state = new StoryState();
            var r = new DialogueRunner(Parse(), state);
            r.Start(); r.Advance();          // Okafor. -> Copy.
            r.Advance();                     // -> choices
            Assert.AreEqual(DialogueRunner.StepKind.Choices, r.Kind);
            Assert.IsTrue(state.HasFlag("started"));
            Assert.AreEqual(2, r.CurrentChoices.Count, "trust-gated choice hidden at trust 0");
        }

        [Test]
        public void Runner_SpokenChoiceIsEchoedThenFollowsTarget()
        {
            var state = new StoryState();
            var r = new DialogueRunner(Parse(), state);
            r.Start(); r.Advance(); r.Advance();
            var chosen = r.Choose(0);

            Assert.IsFalse(chosen.Significant);
            Assert.AreEqual(2, state.GetTrust("MARA"));
            Assert.AreEqual(DialogueRunner.PlayerSpeaker, r.CurrentLine.Speaker);
            Assert.AreEqual("Hello.", r.CurrentLine.Text);
            r.Advance();
            Assert.AreEqual("Hi.", r.CurrentLine.Text);
            r.Advance();
            Assert.AreEqual(DialogueRunner.StepKind.End, r.Kind);
        }

        [Test]
        public void Runner_ActionChoiceIsNotSpokenAndConditionalGotoFollowsFlag()
        {
            var state = new StoryState();
            var r = new DialogueRunner(Parse(), state);
            r.Start(); r.Advance(); r.Advance();
            var chosen = r.Choose(1);

            Assert.IsTrue(chosen.Significant);
            Assert.IsTrue(state.HasFlag("stayed_silent"));
            Assert.AreEqual("Right.", r.CurrentLine.Text, "action skips the echo and the conditional jump skips 'Unreachable'");
        }

        [Test]
        public void Runner_TrustGatedChoiceAppearsWhenEarned()
        {
            var state = new StoryState();
            state.AddTrust("mara", 3);
            var r = new DialogueRunner(Parse(), state);
            r.Start(); r.Advance(); r.Advance();
            Assert.AreEqual(3, r.CurrentChoices.Count);
            r.Choose(2);
            r.Advance(); // past the echo
            Assert.AreEqual(DialogueRunner.StepKind.End, r.Kind);
            Assert.AreEqual(4, state.GetTrust("MARA"));
        }

        [Test]
        public void Runner_DetectsInfiniteLoop()
        {
            var c = DialogueParser.Parse("=== a\n-> b\n=== b\n-> a\n", "loop");
            var r = new DialogueRunner(c, new StoryState());
            Assert.Throws<System.InvalidOperationException>(() => r.Start());
        }

        // ---------- story state ----------

        [Test]
        public void StoryState_ClampsTrustAndIgnoresCase()
        {
            var s = new StoryState();
            s.AddTrust("Mara", 99);
            Assert.AreEqual(StoryState.MaxTrust, s.GetTrust("MARA"));
            s.AddTrust("mara", -99);
            Assert.AreEqual(StoryState.MinTrust, s.GetTrust("Mara"));
        }

        [Test]
        public void StoryState_RaisesChangedOnlyOnRealChanges()
        {
            var s = new StoryState();
            int changes = 0;
            s.Changed += () => changes++;
            s.SetFlag("a");
            s.SetFlag("A"); // same flag
            s.MarkCompleted("conv");
            s.MarkCompleted("conv");
            Assert.AreEqual(2, changes);
        }
    }
}
