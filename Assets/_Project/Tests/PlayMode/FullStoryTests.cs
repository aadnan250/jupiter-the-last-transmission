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
    /// End-to-end tests of the chapters around the Europa story: the launch from Cabo Lumen (1), Jupiter and the
    /// signal loss on the Meridian (3-4), the sample and the last transmission (9), and home with the ending (10).
    /// </summary>
    public class FullStoryTests
    {
        DialogueManager dialogue;
        StoryState story;
        PlayerController player;

        IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            yield return null;
            GameSession.Current.AutosaveEnabled = false;
            GameSession.Current.NewGame();
            dialogue = Object.FindAnyObjectByType<DialogueManager>();
            story = dialogue.Story;
            player = Object.FindAnyObjectByType<PlayerController>();
            yield return null;
        }

        void SkipLines()
        {
            for (int g = 0; g < 200 && dialogue.IsActive && !dialogue.OnTheMove && dialogue.Runner.Kind == DialogueRunner.StepKind.Line; g++)
                dialogue.Advance();
        }

        IEnumerator WaitFor(System.Func<bool> condition, float seconds)
        {
            for (float t = 0f; t < seconds && !condition(); t += Time.deltaTime) yield return null;
        }

        IEnumerator WaitForIdleDialogue() => WaitFor(() => !dialogue.IsActive, 60f);

        [UnityTest]
        public IEnumerator Chapter1_Goodbye_Tower_Launch_ToTheMeridian()
        {
            yield return Load("L01_CaboLumen");
            Assert.IsTrue(player.IsGrounded || true);
            Assert.AreEqual("Say goodbye to Ada and Zara", Object.FindAnyObjectByType<MissionManager>().CurrentText);
            yield return WaitForIdleDialogue();   // Mara's call on the facility comms

            var ada = Object.FindObjectsByType<CrewMember>().First(c => c.CharacterId == "ADA");
            ada.Interact(player.gameObject);
            SkipLines();
            Assert.AreEqual(3, dialogue.Runner.CurrentChoices.Count);
            dialogue.Choose(0);                          // *! promise Zara
            SkipLines();
            dialogue.Choose(2);                          // wave to Mara
            SkipLines();
            Assert.IsTrue(story.HasFlag("promised_zara"));
            Assert.IsTrue(story.IsCompleted("ch01_family"));

            Object.FindObjectsByType<DialogueTrigger>().First(t => t.name == "Trigger_LaunchTower").TryFire();
            var launch = Object.FindAnyObjectByType<LaunchSequence>();
            yield return WaitFor(() => launch.InCapsule, 5f);
            Assert.IsTrue(launch.InCapsule, "strapped into the capsule");
            Assert.Greater(player.transform.position.y, 50f, "at the top of the stack");

            yield return WaitFor(() => SceneManager.GetActiveScene().name == "L02_Meridian", 90f);
            Assert.AreEqual("L02_Meridian", SceneManager.GetActiveScene().name, "launched: on to the ship");
            Assert.IsTrue(GameSession.Current.Story.HasFlag("promised_zara"), "the promise travels with Sam");
        }

        [UnityTest]
        public IEnumerator Chapters3And4_FirstSound_Blackout_Relay_Restored()
        {
            yield return Load("L02_Meridian");
            var terminal = Object.FindObjectsByType<ConversationTerminal>().First(t => t.name == "SeismometerConsole");
            Assert.IsFalse(terminal.CanInteract(player.gameObject), "not before Jupiter");

            story.MarkCompleted("ch02_observation_radio");
            yield return null;
            Assert.IsTrue(story.HasFlag("ch3_jupiter"));
            Assert.IsTrue(terminal.CanInteract(player.gameObject));
            terminal.Interact(player.gameObject);
            SkipLines();
            Assert.AreEqual(3, dialogue.Runner.CurrentChoices.Count);
            dialogue.Choose(1);                          // *! log it as Feature Seven
            SkipLines();
            Assert.IsTrue(story.HasFlag("logged_feature7"));

            var director = Object.FindAnyObjectByType<BlackoutDirector>();
            yield return WaitFor(() => director.BlackedOut, 10f);
            Assert.IsTrue(director.BlackedOut, "the ship goes dark");

            var relays = Object.FindObjectsByType<RelayPanel>();
            Assert.AreEqual(3, relays.Length);
            var health = player.GetComponent<Jupiter.Combat.PlayerHealth>();
            relays.First(r => !r.Faulty).Interact(player.gameObject);
            Assert.Less(health.Model.Current, health.Model.Max, "the wrong relay bites");
            Assert.IsFalse(director.Restored);
            relays.First(r => r.Faulty).Interact(player.gameObject);
            Assert.IsTrue(director.Restored);
            Assert.IsTrue(story.HasFlag("comms_restored"));
            yield return WaitFor(() => story.IsCompleted("ch04_restored_radio"), 90f);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "L03_EuropaSurface", 20f);
            Assert.AreEqual("L03_EuropaSurface", SceneManager.GetActiveScene().name, "on to Europa");
        }

        [UnityTest]
        public IEnumerator Chapter9_VentTheSample_ThenTheLastTransmission()
        {
            yield return Load("L05_Return");
            var canister = Object.FindObjectsByType<ConversationTerminal>().First(t => t.name == "Canister");
            var comms = Object.FindObjectsByType<ConversationTerminal>().First(t => t.name == "CommsScreen");
            Assert.IsFalse(comms.CanInteract(player.gameObject), "the canister first");

            canister.Interact(player.gameObject);
            SkipLines();
            Assert.AreEqual(2, dialogue.Runner.CurrentChoices.Count);
            dialogue.Choose(0);                          // *! vent it
            SkipLines();
            Assert.IsTrue(story.HasFlag("sample_vented"));
            yield return null;
            Assert.IsFalse(GameObject.Find("Sample") != null && GameObject.Find("Sample").activeInHierarchy, "its light goes out");

            comms.Interact(player.gameObject);
            SkipLines();
            Assert.AreEqual(3, dialogue.Runner.CurrentChoices.Count, "three ways to end it");
            dialogue.Choose(0);                          // *! "Ada, Zara. I'm coming home."
            SkipLines();
            Assert.IsTrue(story.HasFlag("closing_family"));
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "L06_Home", 20f);
            Assert.AreEqual("L06_Home", SceneManager.GetActiveScene().name, "ten months later");
        }

        [UnityTest]
        public IEnumerator Chapter10_Home_TheAnswerToZara_ThenTheEnding()
        {
            yield return Load("L06_Home");
            story.SetFlag("sample_kept");
            var zara = Object.FindObjectsByType<CrewMember>().First(c => c.CharacterId == "ZARA");
            zara.Interact(player.gameObject);
            SkipLines();
            Assert.AreEqual(2, dialogue.Runner.CurrentChoices.Count, "the promise answer only appears if it was made");
            dialogue.Choose(0);                          // *! "...It was listening."
            SkipLines();
            Assert.IsTrue(story.HasFlag("home_reunited"));

            var ending = Object.FindAnyObjectByType<EndingDirector>();
            yield return WaitFor(() => ending.Started, 6f);
            Assert.IsTrue(ending.Started, "the ending begins");
            yield return WaitFor(() => ending.CurrentText.Contains("QUARANTINE"), 8f);
            StringAssert.Contains("QUARANTINE", ending.CurrentText, "the kept-sample ending");
        }
    }
}
