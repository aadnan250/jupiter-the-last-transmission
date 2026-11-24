using System.Collections;
using Jupiter.Audio;
using Jupiter.Player;
using Jupiter.UI;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>Smoke tests for the Meridian grey-box level (Chapter 2 layout).</summary>
    public class MeridianLevelTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("L02_Meridian", LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator Player_StandsInSpinGravity()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual("MeridianRing", player.Environment.displayName);
            Assert.AreEqual(5.886f, player.Environment.gravity, 1e-3f, "0.6 g spin gravity");
        }

        [UnityTest]
        public IEnumerator RoomDoors_OpenAndLockedDoorsStayShut()
        {
            var doors = Object.FindObjectsByType<SplitDoor>();
            Assert.AreEqual(12, doors.Length, "10 room doors + cargo hold + airlock hatch");

            int locked = 0;
            foreach (var door in doors)
            {
                if (door.IsLocked)
                {
                    locked++;
                    Assert.IsFalse(door.Open(), $"{door.name} is locked and must refuse to open");
                    Assert.IsFalse(door.IsOpen);
                    Assert.IsTrue(door.CanInteract(null), "locked doors tell the player they are locked");
                }
                else
                {
                    Assert.IsTrue(door.Open(), $"{door.name} should open");
                    Assert.IsTrue(door.IsOpen);
                    Assert.IsFalse(door.CanInteract(null), "unlocked doors open by themselves and show no prompt");
                }
            }
            Assert.AreEqual(2, locked);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Doors_SlideOpenWhenThePlayerApproachesAndCloseBehind()
        {
            var door = System.Array.Find(Object.FindObjectsByType<SplitDoor>(), d => d.name == "Door_Galley");
            var player = Object.FindAnyObjectByType<PlayerController>();
            var cc = player.GetComponent<CharacterController>();
            yield return null;
            Assert.IsFalse(door.IsOpen, "closed while the player is far away");

            cc.enabled = false;
            player.transform.position = new Vector3(-0.2f, 0.05f, 15f);
            cc.enabled = true;
            yield return null;
            yield return null;
            Assert.IsTrue(door.IsOpen, "opens when the player is near");

            cc.enabled = false;
            player.transform.position = new Vector3(0f, 0.05f, 3f);
            cc.enabled = true;
            yield return null;
            yield return null;
            Assert.IsFalse(door.IsOpen, "closes again once the player leaves");
        }

        [UnityTest]
        public IEnumerator Readable_OpensPanelBlocksInputAndCloses()
        {
            var ui = Object.FindAnyObjectByType<ReadableUI>();
            var input = Object.FindAnyObjectByType<InputReader>();
            var readable = Object.FindAnyObjectByType<Readable>();
            Assert.IsNotNull(ui);
            Assert.IsNotNull(readable);

            readable.Interact(null);
            yield return null;
            Assert.IsTrue(ui.IsOpen);
            Assert.AreSame(readable, ui.Current);
            Assert.IsTrue(input.GameplayBlocked, "movement must be blocked while reading");

            readable.Interact(null);
            yield return null;
            Assert.IsFalse(ui.IsOpen);
            Assert.IsFalse(input.GameplayBlocked);
        }

        [UnityTest]
        public IEnumerator EveryReadableHasText()
        {
            var readables = Object.FindObjectsByType<Readable>();
            Assert.GreaterOrEqual(readables.Length, 7);
            foreach (var r in readables)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Title), r.name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Body), r.name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryRoomToneHasALoopingClip()
        {
            yield return null; // let Start() run
            var tones = Object.FindObjectsByType<RoomTone>();
            Assert.GreaterOrEqual(tones.Length, 10);
            foreach (var t in tones)
            {
                Assert.IsNotNull(t.Source.clip, $"{t.name} has no clip");
                Assert.IsTrue(t.Source.loop, $"{t.name} must loop");
            }
        }

        [UnityTest]
        public IEnumerator WindowBarrier_StopsPlayerWalkingIntoSpace()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = new Vector3(0f, 0.05f, 48f);
            cc.enabled = true;
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 30; i++)
            {
                cc.Move(new Vector3(0f, 0f, 0.2f));
                yield return null;
            }
            Assert.Less(player.transform.position.z, 50f, "player must not pass the observation window");
        }
    }
}
