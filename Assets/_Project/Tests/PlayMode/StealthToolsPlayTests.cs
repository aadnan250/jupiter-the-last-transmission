using System.Collections;
using System.Linq;
using Jupiter.Combat;
using Jupiter.Creatures;
using Jupiter.Player;
using Jupiter.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// End-to-end tests of the stealth tools in Conamara Base: lockers hide Sam from sight, thrown objects and
    /// decoys draw the Listener, and flares lure and dazzle it.
    /// </summary>
    public class StealthToolsPlayTests
    {
        PlayerController player;
        CreatureAI creature;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("L04_ConamaraBase", LoadSceneMode.Single);
            yield return null;
            player = Object.FindAnyObjectByType<PlayerController>();
            creature = Object.FindAnyObjectByType<CreatureAI>();
            // Let loose props settle before anything is thrown.
            yield return new WaitForSeconds(0.5f);
        }

        void WarpCreature(Vector3 near)
        {
            Assert.IsTrue(NavMesh.SamplePosition(near, out var hit, 2f, NavMesh.AllAreas), $"NavMesh near {near}");
            creature.Agent.Warp(hit.position);
        }

        [Test]
        public void Level_HasLockersThrowablesAndTheFlarePistol()
        {
            Assert.GreaterOrEqual(Object.FindObjectsByType<HidingSpot>().Length, 5, "lockers on every route");
            var throwables = Object.FindObjectsByType<Throwable>();
            Assert.GreaterOrEqual(throwables.Count(t => !t.IsDecoy), 3, "cans and wrenches");
            Assert.GreaterOrEqual(throwables.Count(t => t.IsDecoy), 1, "a decoy thumper");
            Assert.IsTrue(Object.FindObjectsByType<Pickup>().Any(p => p.ItemKind == Pickup.Kind.FlarePistol), "Mara's flare pistol");
            Assert.IsNotNull(player.GetComponent<PlayerConcealment>());
            Assert.IsNotNull(player.GetComponent<PlayerThrower>());
            Assert.IsNotNull(player.GetComponent<FlarePistol>());
        }

        [UnityTest]
        public IEnumerator Locker_HidesSamFromSight_AndLeavingRevealsHim()
        {
            var spot = Object.FindObjectsByType<HidingSpot>().First(s => s.name == "Locker_Drill");
            var concealment = player.GetComponent<PlayerConcealment>();
            var input = player.GetComponent<InputReader>();

            Assert.AreEqual("Hide", spot.Prompt);
            Assert.IsTrue(spot.CanInteract(player.gameObject));
            spot.Interact(player.gameObject);
            Assert.IsTrue(concealment.IsHidden);
            Assert.IsTrue(input.GameplayBlocked, "no moving or firing from inside a locker");
            Assert.AreEqual("Leave", spot.Prompt);
            Assert.Less(Vector3.Distance(player.transform.position, spot.Inside.position), 0.2f);

            // The Listener stands a metre outside the door: well inside its 4 m sight range.
            WarpCreature(spot.Exit.position + spot.transform.forward * 1f);
            yield return new WaitForSeconds(1.5f);
            CollectionAssert.DoesNotContain(new[] { CreatureState.Hunting, CreatureState.Attacking }, creature.State,
                "it cannot see Sam in the locker");
            Assert.AreEqual(100f, player.GetComponent<PlayerHealth>().Model.Current, "and so cannot hurt him");

            spot.Interact(player.gameObject);
            Assert.IsFalse(concealment.IsHidden);
            Assert.IsFalse(input.GameplayBlocked);
            yield return new WaitForSeconds(0.3f);
            CollectionAssert.Contains(new[] { CreatureState.Listening, CreatureState.Hunting, CreatureState.Attacking }, creature.State,
                "stepping out in front of it is a mistake");
        }

        [UnityTest]
        public IEnumerator ThrownCan_LandingNearTheListenerWakesIt()
        {
            var can = Object.FindObjectsByType<Throwable>().First(t => !t.IsDecoy);
            var thrower = player.GetComponent<PlayerThrower>();
            Assert.IsTrue(thrower.Hold(can));
            Assert.AreSame(can, thrower.Held);
            Assert.IsFalse(can.gameObject.activeSelf, "in Sam's hand, not in the world");

            // Drop it hard about 2.7 m from the dormant Listener; Sam is far away in the airlock.
            Assert.AreEqual(CreatureState.Dormant, creature.State);
            can.Launch(new Vector3(-13.5f, 1.5f, 54f), Vector3.down * 7f, player.GetComponent<CharacterController>());
            yield return new WaitForSeconds(0.6f);

            Assert.GreaterOrEqual(can.ImpactNoises, 1, "the landing made a noise");
            Assert.AreNotEqual(CreatureState.Dormant, creature.State, "and the Listener heard it");
            Assert.IsTrue(creature.Suspicion.LastKnownPosition.HasValue);
            Assert.Less(Vector3.Distance(creature.Suspicion.LastKnownPosition.Value, can.transform.position), 1.5f,
                "it heard the can, not Sam");
        }

        [UnityTest]
        public IEnumerator DecoyThumper_PulsesThroughTheFloorAfterLanding()
        {
            var decoy = Object.FindObjectsByType<Throwable>().First(t => t.IsDecoy);
            int thumps = 0;
            System.Action<VibrationEvent> count = e => { if (e.Source == decoy.gameObject && Mathf.Approximately(e.Radius, StealthRules.DecoyRadius)) thumps++; };
            VibrationSystem.Emitted += count;
            try
            {
                player.GetComponent<PlayerThrower>().Hold(decoy);
                decoy.Launch(new Vector3(-13.5f, 1.5f, 54f), Vector3.down * 7f, null);
                yield return new WaitForSeconds(3.5f);
            }
            finally { VibrationSystem.Emitted -= count; }

            Assert.IsTrue(decoy.Decoy.Armed);
            Assert.GreaterOrEqual(thumps, 3, "thumps at 0, 1.5 and 3 s");
            Assert.AreNotEqual(CreatureState.Dormant, creature.State);
            Assert.IsFalse(decoy.CanInteract(player.gameObject), "a running decoy cannot be picked up");
        }

        [UnityTest]
        public IEnumerator FlarePistol_NeedsThePistolAndAFlare()
        {
            var pistol = player.GetComponent<FlarePistol>();
            var inv = player.GetComponent<PlayerInventory>();
            Assert.IsFalse(pistol.Fire(), "no pistol yet");

            var pickup = Object.FindObjectsByType<Pickup>().First(p => p.ItemKind == Pickup.Kind.FlarePistol);
            pickup.Interact(player.gameObject);
            Assert.IsTrue(inv.HasFlarePistol);
            Assert.AreEqual(3, inv.Model.Count(PlayerInventory.Flare));

            for (int i = 0; i < 3; i++) Assert.IsTrue(pistol.Fire());
            Assert.IsFalse(pistol.Fire(), "out of flares");
            Assert.AreEqual(3, pistol.FlaresFired);
            yield return null;
            Assert.GreaterOrEqual(BurningFlare.Active.Count, 3);
        }

        [UnityTest]
        public IEnumerator BurningFlare_LuresTheListenerThenBlindsIt()
        {
            var flare = BurningFlare.Spawn(new Vector3(-3f, 0.2f, 55f), Vector3.zero, null, 30f);
            yield return null;
            Assert.AreEqual(CreatureState.Investigating, creature.State, "drawn by heat and light, 8 m away");

            float t = 0f;
            while (creature.State != CreatureState.Blinded && t < 12f) { t += Time.deltaTime; yield return null; }
            Assert.AreEqual(CreatureState.Blinded, creature.State, "dazzled when it reaches the flare");
            Assert.AreEqual(0f, creature.Suspicion.Value, 1e-4f, "it forgets everything");

            yield return new WaitForSeconds(StealthRules.FlareBlindSeconds + 0.5f);
            Assert.AreNotEqual(CreatureState.Blinded, creature.State, "it recovers");
            Assert.AreNotEqual(CreatureState.Investigating, creature.State, "and is not fooled by the same flare twice");
            Assert.IsTrue(flare.Burning);
        }
    }
}
