using System.Collections;
using System.Linq;
using Jupiter.Combat;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using Jupiter.UI;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// End-to-end tests of Chapter 6 "The Discovery" in Conamara Base: the Listener is absent until contact,
    /// Tomas's message and the choice about Mara, Ashford's code-locked terminal, the sealed orders conversation,
    /// the palm-print scare, and the objectives that walk the player through it.
    /// </summary>
    public class ChapterSixTests
    {
        DialogueManager dialogue;
        StoryState story;
        GameObject player;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return ConamaraTestScene.LoadFresh();
            dialogue = Object.FindAnyObjectByType<DialogueManager>();
            story = dialogue.Story;
            player = Object.FindAnyObjectByType<PlayerController>().gameObject;
        }

        void SkipLines()
        {
            for (int guard = 0; guard < 200 && dialogue.IsActive && dialogue.Runner.Kind == DialogueRunner.StepKind.Line; guard++)
                dialogue.Advance();
        }

        static T Named<T>(string name) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include).First(c => c.name == name);

        static KeypadTerminal Terminal => Object.FindAnyObjectByType<KeypadTerminal>();
        static CrewMember Mara => Object.FindObjectsByType<CrewMember>().First(c => c.CharacterId == "MARA");
        static MissionManager Mission => Object.FindAnyObjectByType<MissionManager>();

        [UnityTest]
        public IEnumerator ChapterStarts_UnarmedWithNoListener_AndContactBringsItIn()
        {
            Assert.IsNull(Object.FindAnyObjectByType<CreatureAI>(), "nothing has come up the bore yet");
            Assert.IsFalse(player.GetComponent<PlayerInventory>().HasBoltDriver, "Chapter 6 is an investigation");
            Assert.AreEqual("Check the mail cache in comms", Mission.CurrentText);

            story.SetFlag(ConamaraTestScene.ContactFlag);
            yield return null;
            var creature = Object.FindAnyObjectByType<CreatureAI>();
            Assert.IsNotNull(creature, "the story gate lets it in at contact");
            Assert.IsTrue(creature.Agent.isOnNavMesh);
        }

        [UnityTest]
        public IEnumerator MailCache_ShowingMaraTheMessage_SetsTheFlagAndRaisesTrust()
        {
            var mail = Named<ConversationTerminal>("MailCache");
            Assert.IsTrue(mail.CanInteract(player));
            mail.Interact(player);
            Assert.IsTrue(dialogue.IsActive);

            SkipLines();
            dialogue.Choose(0);                      // play it privately
            SkipLines();
            Assert.AreEqual(3, dialogue.Runner.CurrentChoices.Count);
            dialogue.Choose(0);                      // *! show Mara
            SkipLines();

            Assert.IsFalse(dialogue.IsActive);
            Assert.IsTrue(story.IsCompleted("ch06_mail_cache"));
            Assert.IsTrue(story.HasFlag("heard_tomas_message"));
            Assert.IsTrue(story.HasFlag("showed_mara_message"));
            Assert.AreEqual(2, story.GetTrust("MARA"));
            Assert.IsFalse(mail.CanInteract(player), "the message plays once");
            Assert.AreEqual("Find out what Ashford is hiding (0/2)", Mission.CurrentText);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MailCache_IfSamToldMaraOnTheShip_SheAlreadyKnows()
        {
            story.SetFlag("told_mara_truth_early");
            Named<ConversationTerminal>("MailCache").Interact(player);
            SkipLines();
            Assert.AreEqual(2, dialogue.Runner.CurrentChoices.Count, "the 'already knew' branch");
            dialogue.Choose(1);
            SkipLines();
            Assert.IsTrue(story.HasFlag("heard_tomas_message"));
            Assert.IsTrue(story.HasFlag("apologised_to_mara"));
            Assert.IsFalse(story.HasFlag("showed_mara_message"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AshfordsTerminal_RejectsWrongCode_OpensOnHisBirthdayBackwards()
        {
            var terminal = Terminal;
            var ui = Object.FindAnyObjectByType<KeypadUI>();
            var input = player.GetComponent<InputReader>();
            Assert.AreEqual("Enter code", terminal.Prompt);

            terminal.Interact(player);
            Assert.IsTrue(terminal.IsOpen);
            Assert.IsTrue(ui.IsOpen, "the keypad panel opens");
            Assert.IsTrue(input.GameplayBlocked);

            foreach (char c in "1704") terminal.Press(c - '0');   // the birthday forwards is wrong
            Assert.IsFalse(terminal.Model.Unlocked);
            Assert.AreEqual(1, terminal.Model.FailedAttempts);

            var results = "4071".Select(c => terminal.Press(c - '0')).ToArray();
            Assert.AreEqual(KeypadResult.Accepted, results.Last());
            Assert.IsTrue(terminal.Model.Unlocked);
            Assert.IsTrue(story.HasFlag("unlocked_ashford_terminal"));
            Assert.IsTrue(story.HasFlag("read_sealed_orders"), "the orders open straight away");
            Assert.IsFalse(ui.IsOpen);
            var reader = Object.FindAnyObjectByType<ReadableUI>();
            Assert.IsTrue(reader.IsOpen);
            StringAssert.Contains("DIRECTIVE 7", reader.Current.Title);
            reader.Close();
            Assert.AreEqual("Read", terminal.Prompt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SealedOrders_MaraOnlyTalksAboutThemAfterTheyAreRead_AndFoundTheMessageIfKept()
        {
            Assert.IsNull(Mara.NextConversation(), "nothing to discuss before the orders");
            story.SetFlag("kept_tomas_message");
            story.SetFlag("read_sealed_orders");
            Assert.AreEqual("ch06_sealed_orders_mara", Mara.NextConversation().name);

            Mara.Interact(player);
            SkipLines();
            Assert.AreEqual(2, dialogue.Runner.CurrentChoices.Count, "she found the message herself");
            dialogue.Choose(0);
            SkipLines();
            Assert.AreEqual(4, dialogue.Runner.CurrentChoices.Count, "what to do about Ashford");
            dialogue.Choose(0);                     // *! confront him
            SkipLines();

            Assert.IsTrue(story.HasFlag("mara_found_message"));
            Assert.IsTrue(story.HasFlag("confront_ashford"));
            Assert.IsTrue(story.IsCompleted("ch06_sealed_orders_mara"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PalmPrints_OnlyAfterTheOrders_PlayOnceAndStayVisible()
        {
            var scare = Object.FindAnyObjectByType<ScareEvent>();
            var prints = Named<Transform>("PalmPrints").gameObject;
            Assert.IsFalse(prints.activeSelf, "not there before");
            Assert.IsFalse(scare.Fire(), "not armed before the sealed orders conversation");

            story.MarkCompleted("ch06_sealed_orders_mara");
            Assert.IsTrue(scare.Fire());
            Assert.IsTrue(prints.activeSelf);
            Assert.IsTrue(story.HasFlag("saw_palm_prints"));

            var silhouette = Named<Transform>("Silhouette").gameObject;
            bool seen = false;
            float t = 0f;
            while (scare.Playing && t < 4f) { seen |= silhouette.activeSelf; t += Time.deltaTime; yield return null; }
            Assert.IsTrue(seen, "the silhouette flashed");
            Assert.IsFalse(silhouette.activeSelf, "and is gone");
            Assert.IsFalse(scare.Fire(), "once only");
        }

        [UnityTest]
        public IEnumerator Objectives_WalkThroughTheChapter()
        {
            story.MarkCompleted("ch06_mail_cache");
            Assert.AreEqual("Find out what Ashford is hiding (0/2)", Mission.CurrentText);
            story.SetFlag("read_tiamat_log");
            story.SetFlag("read_sealed_orders");
            Assert.AreEqual("Talk to Mara", Mission.CurrentText);
            story.MarkCompleted("ch06_sealed_orders_mara");
            Assert.AreEqual("Go down to the drill chamber", Mission.CurrentText);
            story.SetFlag(ConamaraTestScene.ContactFlag);
            Assert.AreEqual("Chapter 6 complete", Mission.CurrentText);
            yield return null;
        }

        [Test]
        public void Documents_HoldTheClueTheLogAndTheBoltDriver()
        {
            var medical = Named<Readable>("Readable_CrewMedical");
            StringAssert.Contains("DOB 17 04 2131", medical.Body, "Ashford's birthday: the code is 4071");
            StringAssert.Contains("FEATURE 7", Named<Readable>("Readable_TiamatLog").Body);
            Assert.IsTrue(Object.FindObjectsByType<Pickup>().Any(p => p.ItemKind == Pickup.Kind.BoltDriver));
        }
    }
}
