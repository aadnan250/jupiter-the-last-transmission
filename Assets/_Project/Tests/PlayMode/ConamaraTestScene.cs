using System.Collections;
using System.Linq;
using Jupiter.Combat;
using Jupiter.Core;
using Jupiter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jupiter.Tests
{
    /// <summary>
    /// Loads Conamara Base for tests with a fresh story and autosave off. <see cref="LoadAtContact"/> skips the
    /// Chapter 6 investigation and the Chapter 7 eruption: it sets the blackout and contact flags (so the Listener
    /// exists) and arms Sam with the bolt driver.
    /// </summary>
    public static class ConamaraTestScene
    {
        public const string Scene = "L04_ConamaraBase";
        public const string ContactFlag = "ch7_contact";
        public const string BlackoutFlag = "eruption_blackout";

        public static IEnumerator LoadFresh()
        {
            yield return SceneManager.LoadSceneAsync(Scene, LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(GameSession.Current, "Conamara Base needs a GameSession");
            GameSession.Current.AutosaveEnabled = false;
            GameSession.Current.NewGame();
            yield return null;
        }

        public static IEnumerator LoadAtContact()
        {
            yield return LoadFresh();
            // The state just after the eruption: lights out, Ashford and Hana gone, the Listener in the base.
            GameSession.Current.Story.SetFlag(BlackoutFlag);
            GameSession.Current.Story.SetFlag(ContactFlag);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var driver = Object.FindObjectsByType<Pickup>().First(p => p.ItemKind == Pickup.Kind.BoltDriver);
            driver.Interact(player.gameObject);
            yield return null;
        }
    }
}
