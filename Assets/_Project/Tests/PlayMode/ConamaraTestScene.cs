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
    /// Chapter 6 investigation: it sets the contact flag (so the Listener exists) and arms Sam with the bolt driver.
    /// </summary>
    public static class ConamaraTestScene
    {
        public const string Scene = "L04_ConamaraBase";
        public const string ContactFlag = "ch7_contact";

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
            GameSession.Current.Story.SetFlag(ContactFlag);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var driver = Object.FindObjectsByType<Pickup>().First(p => p.ItemKind == Pickup.Kind.BoltDriver);
            driver.Interact(player.gameObject);
            yield return null;
        }
    }
}
