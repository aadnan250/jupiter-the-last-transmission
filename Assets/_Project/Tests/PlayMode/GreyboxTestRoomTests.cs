using System.Collections;
using Jupiter.Player;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// Smoke tests that load the real grey-box scene and check the player rig works end to end.
    /// </summary>
    public class GreyboxTestRoomTests
    {
        const string SceneName = "GreyboxTestRoom";

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator Player_SpawnsAndLandsOnFloor()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player, "Scene has no PlayerController");

            yield return new WaitForSeconds(1f);

            Assert.IsTrue(player.IsGrounded, "Player should be standing on the floor after one second");
            Assert.AreEqual(0f, player.transform.position.y, 0.1f, "Player should rest at floor height");
            Assert.AreEqual("Earth", player.Environment.displayName);
        }

        [UnityTest]
        public IEnumerator EuropaZone_SwitchesProfileOnEnterAndRestoresOnExit()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            var zone = Object.FindAnyObjectByType<EnvironmentZone>();
            Assert.IsNotNull(zone, "Scene has no EnvironmentZone");
            var cc = player.GetComponent<CharacterController>();

            // Stand just inside the zone's north edge (zone spans z -36.5 .. -13.5).
            yield return Teleport(player, cc, new Vector3(0f, 0.05f, -14f));
            Assert.AreEqual("Europa", player.Environment.displayName);
            Assert.IsTrue(player.Environment.isVacuum);

            // Walk out through the doorway, as a player would. Physics only reports trigger exits for real movement.
            for (int i = 0; i < 10; i++)
            {
                cc.Move(new Vector3(0f, 0f, 0.2f));
                yield return new WaitForFixedUpdate();
            }
            yield return null;
            Assert.AreEqual("Earth", player.Environment.displayName);
        }

        [UnityTest]
        public IEnumerator Interaction_DetectsDoorInFrontOfPlayer()
        {
            var interaction = Object.FindAnyObjectByType<PlayerInteraction>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var cc = player.GetComponent<CharacterController>();

            // Stand 1.5 m in front of the door, facing it (door is at z = -12.25, player faces -Z).
            yield return Teleport(player, cc, new Vector3(0f, 0.05f, -10.6f));
            yield return null;

            Assert.IsInstanceOf<SlidingDoor>(interaction.Current, "Crosshair should be on the sliding door");
            Assert.AreEqual("Open", interaction.Current.Prompt);
        }

        static IEnumerator Teleport(PlayerController player, CharacterController cc, Vector3 position)
        {
            // CharacterController overrides transform writes unless it is disabled during the move.
            cc.enabled = false;
            player.transform.position = position;
            cc.enabled = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }
    }
}
