using System.Collections;
using System.Linq;
using Jupiter.Creatures;
using Jupiter.Narrative;
using Jupiter.Player;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// End-to-end tests of Chapter 7 "Contact" in Conamara Base: Mara follows Sam down, the confrontation ends with the
    /// drill seizing, the eruption plays (blackout, Hana and Ashford gone, the Listener out, radio on the move),
    /// Ashford's pressure door holds and then gives way, and the safe room conversation.
    /// </summary>
    public class ChapterSevenTests
    {
        DialogueManager dialogue;
        StoryState story;
        InputReader input;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return ConamaraTestScene.LoadFresh();
            dialogue = Object.FindAnyObjectByType<DialogueManager>();
            story = dialogue.Story;
            input = Object.FindAnyObjectByType<InputReader>();
            story.MarkCompleted("ch06_sealed_orders_mara"); // Chapter 6 done
        }

        void SkipLines()
        {
            for (int guard = 0; guard < 200 && dialogue.IsActive && dialogue.Runner.Kind == DialogueRunner.StepKind.Line; guard++)
                dialogue.Advance();
        }

        static GameObject Find(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t => t.name == name).gameObject;

        static T Named<T>(string name) where T : Component => Find(name).GetComponent<T>();

        IEnumerator WaitFor(System.Func<bool> condition, float seconds)
        {
            for (float t = 0f; t < seconds && !condition(); t += Time.deltaTime) yield return null;
        }

        [UnityTest]
        public IEnumerator WalkingIntoTheDrillChamber_MaraFollows_AndTheConfrontationStarts()
        {
            Assert.IsTrue(Find("Crew_ASHFORD").activeSelf);
            Assert.IsTrue(Find("Crew_HANA").activeSelf);
            Assert.IsTrue(Find("Crew_MARA").activeSelf, "Mara starts in comms");
            Assert.IsFalse(Find("Crew_MARA_Drill").activeSelf);

            Assert.IsTrue(Named<DialogueTrigger>("Trigger_DrillChamber").TryFire());
            yield return null;
            Assert.IsTrue(dialogue.IsActive);
            Assert.AreEqual("ch07_confrontation", dialogue.Runner.Conversation.Id);
            Assert.IsFalse(Find("Crew_MARA").activeSelf);
            Assert.IsTrue(Find("Crew_MARA_Drill").activeSelf, "she came down with Sam");
        }

        [UnityTest]
        public IEnumerator Confrontation_EndsWithTheDrillSeizing_ThenTheBoreErupts()
        {
            story.SetFlag("confront_ashford");
            Named<DialogueTrigger>("Trigger_DrillChamber").TryFire();
            SkipLines();
            Assert.AreEqual(4, dialogue.Runner.CurrentChoices.Count);
            dialogue.Choose(1);                       // *! "Hana, stop the drill."
            SkipLines();
            Assert.IsFalse(dialogue.IsActive);
            Assert.IsTrue(story.HasFlag("ordered_drill_stop"));
            Assert.IsTrue(story.HasFlag("drill_seized"));

            var eruption = Object.FindAnyObjectByType<EruptionSequence>();
            Assert.IsTrue(eruption.Playing, "the drill seizing starts the eruption");
            var lamp = Find("Light_DrillLamp").GetComponent<Light>();
            float before = lamp.intensity;

            yield return WaitFor(() => eruption.Finished, 6f);
            Assert.IsTrue(eruption.Finished);
            Assert.IsTrue(story.HasFlag("eruption_blackout"));
            Assert.IsTrue(story.HasFlag(ConamaraTestScene.ContactFlag));
            Assert.Less(lamp.intensity, before * 0.5f, "the lights failed");
            Assert.Greater(lamp.color.r, lamp.color.b, "to emergency red");
            Assert.IsTrue(Find("EruptionRime").activeSelf, "Rime across the floor");
            Assert.IsFalse(Find("Crew_HANA").activeSelf, "Hana is gone");
            Assert.IsFalse(Find("Crew_ASHFORD").activeSelf);
            Assert.IsFalse(Find("Crew_MARA_Drill").activeSelf);
            Assert.IsTrue(Find("Crew_MARA_Safe").activeSelf, "Mara ran for the safe room");

            var creature = Object.FindAnyObjectByType<CreatureAI>();
            Assert.IsNotNull(creature, "the Listener is out");
            Assert.AreNotEqual(CreatureState.Dormant, creature.State, "and listening for Sam");

            Assert.IsTrue(dialogue.IsActive && dialogue.OnTheMove, "Mara's radio plays on the move");
            Assert.IsFalse(input.DialogueMode, "the player keeps control while it plays");
            yield return WaitFor(() => !dialogue.IsActive, 20f);
            Assert.IsTrue(story.IsCompleted("ch07_blackout_radio"), "the radio advances on its own");
        }

        [UnityTest]
        public IEnumerator PressureDoor_HoldsTheListenerBack_ThenGivesWay()
        {
            var hold = Object.FindAnyObjectByType<PressureDoorHold>();
            Assert.IsFalse(hold.TryClose(), "nothing to hold before the eruption");

            story.SetFlag("eruption_blackout");
            story.SetFlag(ConamaraTestScene.ContactFlag);
            hold.HoldSeconds = 4f;
            Assert.IsTrue(hold.TryClose());
            Assert.IsFalse(hold.Door.IsOpen);
            Assert.IsTrue(hold.Door.IsLocked);
            Assert.IsTrue(story.HasFlag("left_drill_chamber"));
            Assert.IsTrue(dialogue.IsActive && dialogue.OnTheMove, "Ashford's radio, on the move");

            yield return new WaitForSeconds(2.5f);
            NavMesh.SamplePosition(new Vector3(-7f, 0f, 47f), out var inside, 2f, NavMesh.AllAreas);
            NavMesh.SamplePosition(new Vector3(-7f, 0f, 41.5f), out var outside, 2f, NavMesh.AllAreas);
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(inside.position, outside.position, NavMesh.AllAreas, path);
            Assert.IsFalse(found && path.status == NavMeshPathStatus.PathComplete, "the shut door blocks the creature");

            yield return WaitFor(() => hold.Broken, 4f);
            Assert.IsTrue(hold.Broken, "it gives way");
            Assert.IsTrue(hold.Door.IsOpen);
            Assert.IsTrue(story.HasFlag("drill_door_broken"));
            Assert.IsFalse(hold.TryClose(), "once only");
        }

        [UnityTest]
        public IEnumerator SafeRoom_MaraIsWaiting_AndTheChoiceIsRecorded()
        {
            var safe = Named<DialogueTrigger>("Trigger_SafeRoom");
            Assert.IsFalse(safe.TryFire(), "only after the eruption");

            story.SetFlag("eruption_blackout");
            yield return null;
            Assert.IsTrue(Find("Crew_MARA_Safe").activeSelf);
            Assert.IsTrue(safe.TryFire());
            Assert.IsTrue(input.DialogueMode, "the safe room is safe enough to stop and talk");
            SkipLines();
            Assert.AreEqual(4, dialogue.Runner.CurrentChoices.Count);
            dialogue.Choose(1);                       // *! get to the pad
            SkipLines();
            Assert.IsTrue(story.HasFlag("leave_ashford"));
            Assert.IsTrue(story.HasFlag("reached_safe_room"));
            Assert.IsTrue(story.IsCompleted("ch07_safe_room"));
        }

        [UnityTest]
        public IEnumerator Objectives_CarryOnFromChapterSix()
        {
            var mission = Object.FindAnyObjectByType<MissionManager>();
            story.MarkCompleted("ch06_mail_cache");
            story.SetFlag("read_tiamat_log");
            story.SetFlag("read_sealed_orders");
            story.SetFlag("reached_drill_chamber");
            Assert.AreEqual("Get out of the drill chamber", mission.CurrentText);
            story.SetFlag("left_drill_chamber");
            Assert.AreEqual("Get to the safe room", mission.CurrentText);
            story.SetFlag("reached_safe_room");
            Assert.AreEqual("Rest. For now.", mission.CurrentText);
            yield return null;
        }
    }
}
