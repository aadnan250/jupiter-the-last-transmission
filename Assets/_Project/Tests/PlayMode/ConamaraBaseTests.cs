using System.Collections;
using System.Linq;
using Jupiter.Combat;
using Jupiter.Creatures;
using Jupiter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// End-to-end tests of the Conamara Base slice: the level and NavMesh load, the Listener reacts to noise,
    /// torpor recovers, nodes kill, and the bolt driver and pickups work.
    /// </summary>
    public class ConamaraBaseTests
    {
        PlayerController player;
        CreatureAI creature;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return ConamaraTestScene.LoadAtContact();
            player = Object.FindAnyObjectByType<PlayerController>();
            creature = Object.FindAnyObjectByType<CreatureAI>();
        }

        void Teleport(Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = position;
            cc.enabled = true;
        }

        [UnityTest]
        public IEnumerator Level_LoadsWithPlayerCreatureAndNavMesh()
        {
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual("EuropaPressurised", player.Environment.displayName);
            Assert.AreEqual(1.315f, player.Environment.gravity, 1e-3f, "Europa gravity");
            Assert.IsFalse(player.Environment.isVacuum, "the base is pressurised");

            Assert.IsNotNull(creature);
            Assert.AreEqual(CreatureState.Dormant, creature.State);
            Assert.IsTrue(creature.Agent.isOnNavMesh, "the Listener stands on the baked NavMesh");
            Assert.IsTrue(player.GetComponent<PlayerInventory>().HasBoltDriver, "Sam is armed in this level");
            Assert.GreaterOrEqual(Object.FindObjectsByType<RimePatch>().Length, 3);
        }

        [UnityTest]
        public IEnumerator Gunshot_WakesTheListenerAndItMovesTowardTheSource()
        {
            // Stand at the drill chamber entrance, about 14 m from the dormant Listener.
            Teleport(new Vector3(-7f, 0.05f, 44.5f));
            yield return null;
            float startDistance = Vector3.Distance(creature.transform.position, player.transform.position);

            var driver = player.GetComponent<BoltDriver>();
            Assert.IsTrue(driver.Fire(), "the bolt driver fires");
            yield return null;
            Assert.AreNotEqual(CreatureState.Dormant, creature.State, "a 25 m gunshot is heard");

            yield return new WaitForSeconds(4f);
            float endDistance = Vector3.Distance(creature.transform.position, player.transform.position);
            CollectionAssert.Contains(new[] { CreatureState.Investigating, CreatureState.Hunting, CreatureState.Attacking }, creature.State);
            Assert.Less(endDistance, startDistance - 2f, "it walks toward where it heard the shot");
            var rig = creature.GetComponent<StalkerRig>();
            if (rig != null)
            {
                var expected = creature.State == CreatureState.Investigating ? StalkerPose.Stalk
                             : creature.State == CreatureState.Hunting ? StalkerPose.Run : StalkerPose.Idle;
                Assert.AreEqual(expected, rig.Pose, "it moves the way it hunts: a slow stalk, then the run");
            }
        }

        [UnityTest]
        public IEnumerator QuietFootstepsFarAwayAreNotHeard()
        {
            VibrationSystem.Emit(new VibrationEvent(new Vector3(0f, 0f, 3f), 3f, VibrationMedium.Ground, false, player.gameObject));
            yield return null;
            Assert.AreEqual(CreatureState.Dormant, creature.State);
            Assert.AreEqual(0f, creature.Suspicion.Value, 1e-5f);
        }

        [UnityTest]
        public IEnumerator BodyDamage_CausesTorpor_ThenItGetsBackUp()
        {
            var health = creature.Health;
            health.Model.TorporMinSeconds = 0.5f;
            health.Model.TorporMaxSeconds = 0.5f;

            var body = creature.GetComponentsInChildren<CreatureHitbox>().First(h => !h.IsNode);
            for (int i = 0; i < 10 && health.Model.Condition == CreatureCondition.Active; i++)
                body.TakeDamage(new DamageInfo(45f, DamageType.Bolt, body.transform.position));
            yield return null;
            Assert.AreEqual(CreatureCondition.Torpor, health.Model.Condition);
            Assert.AreEqual(CreatureState.Torpor, creature.State, "it collapses and looks dead");

            yield return new WaitForSeconds(1f);
            Assert.AreEqual(CreatureCondition.Active, health.Model.Condition);
            CollectionAssert.Contains(new[] { CreatureState.Recovering, CreatureState.Hunting, CreatureState.Attacking }, creature.State,
                "it gets back up, angry");
        }

        [UnityTest]
        public IEnumerator ThreeNodeHits_KillTheListener()
        {
            var nodes = creature.GetComponentsInChildren<CreatureHitbox>().Where(h => h.IsNode).ToArray();
            Assert.AreEqual(6, nodes.Length, "six pressure nodes");
            for (int i = 0; i < 3; i++) nodes[i].TakeDamage(new DamageInfo(45f, DamageType.Bolt, nodes[i].transform.position));
            yield return null;
            Assert.AreEqual(CreatureCondition.Dead, creature.Health.Model.Condition);
            Assert.AreEqual(CreatureState.Dead, creature.State);
            Assert.IsFalse(creature.Agent.enabled);
        }

        [UnityTest]
        public IEnumerator BoltDriver_RaycastHitsANodeInFront()
        {
            // The nodes run down the Listener's back: stand 3 m behind it, aim the camera at node 0 and fire.
            var node = creature.GetComponentsInChildren<CreatureHitbox>().First(h => h.IsNode && h.NodeIndex == 0);
            var cam = player.GetComponentInChildren<Camera>().transform;
            Teleport(node.transform.position - creature.transform.forward * 3f - Vector3.up * 1.62f);
            yield return null;
            cam.rotation = Quaternion.LookRotation(node.transform.position - cam.position);
            Assert.IsTrue(player.GetComponent<BoltDriver>().Fire());
            yield return null;
            Assert.IsTrue(creature.Health.Model.IsNodeRuptured(0), "the bolt ruptured the node it was aimed at");
        }

        [UnityTest]
        public IEnumerator Pickups_AddAmmoAndMedPatches()
        {
            var driver = player.GetComponent<BoltDriver>();
            var inv = player.GetComponent<PlayerInventory>();
            int reserve = driver.Model.Reserve;
            int patches = inv.Model.Count(PlayerInventory.MedPatch);

            foreach (var p in Object.FindObjectsByType<Pickup>().Take(10).ToArray())
                p.Interact(player.gameObject);
            yield return null;

            Assert.Greater(driver.Model.Reserve, reserve);
            Assert.Greater(inv.Model.Count(PlayerInventory.MedPatch), patches);

            var health = player.GetComponent<PlayerHealth>();
            health.TakeDamage(new DamageInfo(50f, DamageType.Melee, Vector3.zero));
            Assert.IsTrue(inv.UseMedPatch());
            Assert.AreEqual(90f, health.Model.Current, 1e-3f);
        }

        [UnityTest]
        public IEnumerator SafeRoomDoor_BlocksTheCreaturesNavMesh()
        {
            // A path from the corridor into the safe room must not exist while the door is closed.
            var from = new Vector3(0f, 0f, 30f);
            var to = new Vector3(8f, 0f, 37f);
            NavMesh.SamplePosition(from, out var a, 2f, NavMesh.AllAreas);
            NavMesh.SamplePosition(to, out var b, 2f, NavMesh.AllAreas);
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
            Assert.IsFalse(found && path.status == NavMeshPathStatus.PathComplete, "the Listener cannot path into the safe room");
            yield return null;
        }
    }
}
