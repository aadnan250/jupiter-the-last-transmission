using System.Collections.Generic;
using System.IO;
using Jupiter.Core;
using Jupiter.Narrative;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jupiter.Tests
{
    /// <summary>
    /// Checks the game's actual content and configuration, not just the code:
    /// every dialogue file parses, every reply has a consequence, controls cover keyboard and gamepad,
    /// and saves round-trip.
    /// </summary>
    public class ContentValidationTests
    {
        const string DialogueRoot = "Assets/_Project/Narrative/Dialogue";

        static IEnumerable<TextAsset> AllDialogue()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { DialogueRoot }))
                yield return AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }

        [Test]
        public void AllDialogueFilesParse()
        {
            int count = 0;
            foreach (var asset in AllDialogue())
            {
                Assert.DoesNotThrow(() => DialogueParser.Parse(asset.text, asset.name), asset.name);
                count++;
            }
            Assert.GreaterOrEqual(count, 4, "expected the Chapter 2 conversations");
        }

        /// <summary>Design rule: every decision the player makes must change the story state.</summary>
        [Test]
        public void EveryReplyHasAConsequence()
        {
            foreach (var asset in AllDialogue())
            {
                var conv = DialogueParser.Parse(asset.text, asset.name);
                foreach (var node in conv.Nodes.Values)
                    foreach (var choice in node.Choices)
                        Assert.IsNotEmpty(choice.Effects, $"{asset.name}:{choice.SourceLine} '{choice.Text}' has no effect");
            }
        }

        [Test]
        public void EverySpeakerIsAKnownCharacter()
        {
            foreach (var asset in AllDialogue())
            {
                var conv = DialogueParser.Parse(asset.text, asset.name);
                foreach (var node in conv.Nodes.Values)
                    foreach (var item in node.Body)
                        if (item.Kind == DialogueItem.ItemKind.Line)
                            Assert.IsTrue(Characters.IsKnown(item.Line.Speaker), $"{asset.name}:{item.Line.SourceLine} unknown speaker {item.Line.Speaker}");
            }
        }

        [Test]
        public void EveryConversationOffersAtLeastOneSignificantChoiceOrIsARadioCall()
        {
            foreach (var asset in AllDialogue())
            {
                var conv = DialogueParser.Parse(asset.text, asset.name);
                bool significant = false;
                foreach (var node in conv.Nodes.Values)
                    foreach (var c in node.Choices)
                        significant |= c.Significant;
                if (!asset.name.Contains("radio"))
                    Assert.IsTrue(significant, $"{asset.name} has no significant (*!) choice");
            }
        }

        [Test]
        public void ControlsCoverKeyboardAndGamepad()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/JupiterControls.inputactions");
            Assert.IsNotNull(actions);

            string[] needBoth = { "Player/Move", "Player/Interact", "Player/Jump", "Player/Crouch", "Player/Run",
                                  "Dialogue/Advance", "Dialogue/Choice1", "Dialogue/Choice2", "Dialogue/Choice3", "Dialogue/Choice4" };
            foreach (string path in needBoth)
            {
                var action = actions.FindAction(path, true);
                bool keyboard = false, gamepad = false;
                foreach (var b in action.bindings)
                {
                    keyboard |= b.path.StartsWith("<Keyboard>") || b.path.StartsWith("<Mouse>") || (b.isComposite && b.path == "2DVector");
                    gamepad |= b.path.StartsWith("<Gamepad>");
                }
                Assert.IsTrue(keyboard, $"{path} has no keyboard/mouse binding");
                Assert.IsTrue(gamepad, $"{path} has no gamepad (PS5/Xbox) binding");
            }

            // The four reply buttons map to the four face buttons: Triangle, Circle, Cross, Square.
            Assert.AreEqual("<Gamepad>/buttonNorth", GamepadPath(actions, "Dialogue/Choice1"));
            Assert.AreEqual("<Gamepad>/buttonEast", GamepadPath(actions, "Dialogue/Choice2"));
            Assert.AreEqual("<Gamepad>/buttonSouth", GamepadPath(actions, "Dialogue/Choice3"));
            Assert.AreEqual("<Gamepad>/buttonWest", GamepadPath(actions, "Dialogue/Choice4"));
        }

        static string GamepadPath(InputActionAsset actions, string action)
        {
            foreach (var b in actions.FindAction(action, true).bindings)
                if (b.path.StartsWith("<Gamepad>")) return b.path;
            return null;
        }

        [Test]
        public void SaveData_RoundTripsThroughDisk()
        {
            var story = new StoryState();
            story.AddTrust("MARA", 3);
            story.AddTrust("ASHFORD", -2);
            story.SetFlag("told_mara_truth_early");
            story.MarkCompleted("ch02_workshop_mara");

            string path = Path.Combine(Path.GetTempPath(), $"jupiter_save_test_{System.Guid.NewGuid():N}.json");
            try
            {
                SaveSystem.Write(SaveData.Capture(story, "L02_Meridian", new Vector3(1, 2, 3), 90f), path);
                var loaded = SaveSystem.Read(path);
                Assert.IsNotNull(loaded);
                Assert.AreEqual("L02_Meridian", loaded.scene);
                Assert.AreEqual(new Vector3(1, 2, 3), loaded.Position);
                Assert.AreEqual(90f, loaded.yaw);

                var restored = new StoryState();
                loaded.RestoreInto(restored);
                Assert.AreEqual(3, restored.GetTrust("mara"));
                Assert.AreEqual(-2, restored.GetTrust("ashford"));
                Assert.IsTrue(restored.HasFlag("told_mara_truth_early"));
                Assert.IsTrue(restored.IsCompleted("ch02_workshop_mara"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void SaveSystem_RejectsMissingCorruptAndFutureSaves()
        {
            string path = Path.Combine(Path.GetTempPath(), $"jupiter_save_test_{System.Guid.NewGuid():N}.json");
            try
            {
                Assert.IsNull(SaveSystem.Read(path), "missing");
                File.WriteAllText(path, "{ not json");
                Assert.IsNull(SaveSystem.Read(path), "corrupt");
                File.WriteAllText(path, $"{{\"version\": {SaveData.CurrentVersion + 1}, \"scene\": \"x\"}}");
                Assert.IsNull(SaveSystem.Read(path), "future version");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
