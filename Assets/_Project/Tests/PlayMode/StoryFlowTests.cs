using System.Collections;
using Jupiter.Core;
using Jupiter.Creatures;
using Jupiter.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>
    /// The game plays as one story: finishing the Meridian (Chapter 2) leads on to the landing on Europa, and
    /// the choices made on the ship are still known on Europa.
    /// </summary>
    public class StoryFlowTests
    {
        [UnityTest]
        public IEnumerator FinishingTheMeridian_LandsOnEuropa_WithChoicesKept()
        {
            yield return SceneManager.LoadSceneAsync("L02_Meridian", LoadSceneMode.Single);
            yield return null;
            var session = GameSession.Current;
            session.AutosaveEnabled = false;
            session.NewGame();

            var transition = Object.FindAnyObjectByType<LevelTransition>();
            Assert.IsNotNull(transition, "the Meridian ends with a transition");
            Assert.AreEqual("L03_EuropaSurface", transition.NextScene);

            session.Story.SetFlag("told_mara_truth_early");
            session.Story.MarkCompleted("ch02_observation_radio"); // the last beat of Chapter 2
            transition.Begin();                                     // skip the pause before the fade

            for (float t = 0f; t < 10f && SceneManager.GetActiveScene().name != "L03_EuropaSurface"; t += Time.deltaTime)
                yield return null;
            Assert.AreEqual("L03_EuropaSurface", SceneManager.GetActiveScene().name);
            yield return null;

            Assert.AreSame(session, GameSession.Current, "one GameSession for the whole game");
            Assert.AreEqual(1, Object.FindObjectsByType<GameSession>().Length, "the new level's own copy stood down");
            Assert.IsTrue(GameSession.Current.Story.HasFlag("told_mara_truth_early"), "Chapter 2 choices reach Europa");
            Assert.AreEqual(3, Jupiter.Combat.SquadMember.All.Count, "the landing party is aboard the lander");
            Assert.AreEqual("Wait for the ramp", Object.FindAnyObjectByType<MissionManager>().CurrentText);
        }

        [UnityTest]
        public IEnumerator TheTransitionWaitsForTheLastConversation()
        {
            yield return SceneManager.LoadSceneAsync("L02_Meridian", LoadSceneMode.Single);
            yield return null;
            GameSession.Current.AutosaveEnabled = false;
            GameSession.Current.NewGame();
            var transition = Object.FindAnyObjectByType<LevelTransition>();
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(transition.Started, "nothing happens before the observation deck call");
            Assert.AreEqual("L02_Meridian", SceneManager.GetActiveScene().name);
        }
    }
}
