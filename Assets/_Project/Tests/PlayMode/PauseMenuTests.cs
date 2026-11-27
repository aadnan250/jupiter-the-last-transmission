using System.Collections;
using Jupiter.Player;
using Jupiter.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Jupiter.Tests
{
    /// <summary>The pause menu on the real player rig: time stops and input is off; settings reach the camera.</summary>
    public class PauseMenuTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("GreyboxTestRoom", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pause_StopsTime_AndResume_RestoresIt()
        {
            var menu = Object.FindAnyObjectByType<PauseMenu>();
            Assert.IsNotNull(menu, "the player rig carries a pause menu");
            var input = menu.GetComponent<InputReader>();

            menu.Pause();
            Assert.IsTrue(menu.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.IsFalse(input.enabled, "no walking about while paused");
            yield return null;

            menu.Resume();
            Assert.IsFalse(menu.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(input.enabled);
        }

        [UnityTest]
        public IEnumerator Settings_ReachTheCamera()
        {
            var menu = Object.FindAnyObjectByType<PauseMenu>();
            var cam = Object.FindAnyObjectByType<CameraController>();
            bool invert = menu.Settings.InvertY, bob = menu.Settings.HeadBob;
            try
            {
                menu.Settings.InvertY = true;
                menu.Settings.HeadBob = false;
                menu.Apply();
                Assert.IsTrue(cam.InvertY);
                Assert.IsFalse(cam.HeadBob);
            }
            finally
            {
                menu.Settings.InvertY = invert;
                menu.Settings.HeadBob = bob;
                menu.Apply();
            }
            yield return null;
        }
    }
}
