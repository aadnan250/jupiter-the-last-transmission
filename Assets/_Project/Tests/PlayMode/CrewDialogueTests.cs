using System.Collections;
using System.Linq;
using Jupiter.Core;
using Jupiter.Narrative;
using Jupiter.Player;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// End-to-end tests of talking to the crew on the Meridian: choices change the story,
    /// significant choices trigger the consequence feedback, and later content reacts to earlier decisions.
    /// </summary>
    public class CrewDialogueTests
    {
        DialogueManager dialogue;
        StoryState story;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("L02_Meridian", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(GameSession.Current, "Meridian needs a GameSession");
            GameSession.Current.AutosaveEnabled = false;
            GameSession.Current.NewGame();
            dialogue = Object.FindAnyObjectByType<DialogueManager>();
            story = dialogue.Story;
        }

        static CrewMember Crew(string id) =>
            Object.FindObjectsByType<CrewMember>().First(c => c.CharacterId == id);

        /// <summary>Advances through lines until the runner reaches choices or ends.</summary>
        void SkipLines()
        {
            for (int guard = 0; guard < 200 && dialogue.IsActive && dialogue.Runner.Kind == DialogueRunner.StepKind.Line; guard++)
                dialogue.Advance();
        }

        [UnityTest]
        public IEnumerator TalkingToMara_ChoicesChangeTrustFlagsAndTriggerConsequence()
        {
            var input = Object.FindAnyObjectByType<InputReader>();
            var feedback = Object.FindAnyObjectByType<ConsequenceFeedback>();
            var player = Object.FindAnyObjectByType<PlayerController>().gameObject;

            Crew("MARA").Interact(player);
            Assert.IsTrue(dialogue.IsActive);
            Assert.IsTrue(input.DialogueMode, "walking is disabled while talking");

            SkipLines();
            Assert.AreEqual(4, dialogue.Runner.CurrentChoices.Count, "Mara's opening offers four replies");
            dialogue.Choose(3);                     // look at the music player
            SkipLines();
            dialogue.Choose(0);                     // offer to fix it
            SkipLines();
            Assert.AreEqual(0, feedback.TimesTriggered);
            dialogue.Choose(0);                     // *! tell her the truth about Tomas
            Assert.AreEqual(1, feedback.TimesTriggered, "significant choice plays the eerie sting");
            SkipLines();

            Assert.IsFalse(dialogue.IsActive);
            Assert.IsFalse(input.DialogueMode);
            Assert.IsTrue(story.IsCompleted("ch02_workshop_mara"));
            Assert.IsTrue(story.HasFlag("offered_fix_player"));
            Assert.IsTrue(story.HasFlag("told_mara_truth_early"));
            Assert.AreEqual(3, story.GetTrust("MARA"), "+1 for the offer, +2 for the truth");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ashford_DirectiveSevenOptionOnlyAfterReadingTheRadarSummary()
        {
            var player = Object.FindAnyObjectByType<PlayerController>().gameObject;
            var ashford = Crew("ASHFORD");

            ashford.Interact(player);
            SkipLines();
            Assert.AreEqual(3, dialogue.Runner.CurrentChoices.Count, "hidden until the summary is read");
            dialogue.Choose(2);
            SkipLines();
            Assert.IsFalse(dialogue.IsActive);

            // Fresh story, read the document first.
            GameSession.Current.NewGame();
            var radar = Object.FindObjectsByType<Readable>().First(r => r.SetsFlag == "read_directive7");
            radar.Interact(player);
            radar.Interact(player); // close the reading panel
            Assert.IsTrue(story.HasFlag("read_directive7"));

            ashford.Interact(player);
            SkipLines();
            Assert.AreEqual(4, dialogue.Runner.CurrentChoices.Count);
            Assert.IsTrue(dialogue.Runner.CurrentChoices[3].Significant);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObservationRadio_ReflectsEarlierDecision()
        {
            story.MarkCompleted("ch02_galley_luca_hana");
            story.MarkCompleted("ch02_workshop_mara");
            story.MarkCompleted("ch02_command_ashford");
            story.SetFlag("told_mara_truth_early");

            var player = Object.FindAnyObjectByType<PlayerController>();
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = new Vector3(0f, 0.05f, 37f);
            cc.enabled = true;
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 15 && !dialogue.IsActive; i++)
            {
                cc.Move(new Vector3(0f, 0f, 0.25f));
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(dialogue.IsActive, "radio call plays on entering the observation deck");
            Assert.AreEqual("MARA", dialogue.Runner.CurrentLine.Speaker);
            Assert.IsTrue(dialogue.Runner.CurrentLine.Radio);
            Assert.IsTrue(story.HasFlag("reached_observation"));
        }

        [UnityTest]
        public IEnumerator Objectives_TrackCrewConversationsThenObservationDeck()
        {
            var mission = Object.FindAnyObjectByType<MissionManager>();
            yield return null;
            Assert.AreEqual("Check in with the crew (0/3)", mission.CurrentText);

            story.MarkCompleted("ch02_galley_luca_hana");
            Assert.AreEqual("Check in with the crew (1/3)", mission.CurrentText);
            story.MarkCompleted("ch02_workshop_mara");
            story.MarkCompleted("ch02_command_ashford");
            Assert.AreEqual("Go to the observation deck", mission.CurrentText);
            story.SetFlag("reached_observation");
            Assert.AreEqual("Month 11, Jupiter: bring Tiamat's seismometers online (science lab)", mission.CurrentText, "the ship chapters carry on into Jupiter");
        }

        [UnityTest]
        public IEnumerator CrewAreRealisticRiggedCharactersWithLipSync()
        {
            foreach (var crew in Object.FindObjectsByType<CrewMember>())
            {
                var renderers = crew.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.GreaterOrEqual(renderers.Length, 5, $"{crew.CharacterId} should be a full character (body, eyes, hair, clothes...)");
                foreach (var r in renderers)
                    foreach (var m in r.sharedMaterials)
                        Assert.IsNotNull(m != null ? m.GetTexture("_BaseMap") : null, $"{crew.CharacterId}/{r.name} is untextured");

                var lip = crew.GetComponentInChildren<LipSync>();
                Assert.IsNotNull(lip, $"{crew.CharacterId} has no lip sync");
                Assert.IsNotNull(crew.GetComponentInChildren<CharacterIdleMotion>(), $"{crew.CharacterId} has no idle motion");
                Assert.IsNotNull(crew.Voice, $"{crew.CharacterId} has no voice source");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrewLinesPlayTheirVoiceFromTheCharacter()
        {
            var player = Object.FindAnyObjectByType<PlayerController>().gameObject;
            var mara = Crew("MARA");
            mara.Interact(player);
            yield return null;

            Assert.AreEqual("MARA", dialogue.Runner.CurrentLine.Speaker);
            Assert.IsNotNull(mara.Voice.clip, "Mara's first line should load a voice clip");
            StringAssert.StartsWith("ch02_workshop_mara_L", mara.Voice.clip.name);
            SkipLines();
        }

        [UnityTest]
        public IEnumerator FinishedCrewMemberFallsBackToIdleLine()
        {
            story.MarkCompleted("ch02_workshop_mara");
            var player = Object.FindAnyObjectByType<PlayerController>().gameObject;
            Crew("MARA").Interact(player);
            Assert.IsTrue(dialogue.IsActive);
            Assert.AreEqual("I'm working, Okafor.", dialogue.Runner.CurrentLine.Text);
            SkipLines();
            Assert.IsFalse(dialogue.IsActive);
            yield return null;
        }
    }
}
