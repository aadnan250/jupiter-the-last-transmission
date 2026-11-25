using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Jupiter.Tests
{
    /// <summary>
    /// The game should fill the screen: builds start in borderless full screen, and the editor's Game view plays
    /// maximized. The second relies on a non-public Unity setting, so this test fails loudly if an upgrade renames it.
    /// </summary>
    public class FullScreenSetupTests
    {
        [Test]
        public void Builds_StartBorderlessFullScreenAtNativeResolution()
        {
            Assert.AreEqual(FullScreenMode.FullScreenWindow, PlayerSettings.fullScreenMode);
            Assert.IsTrue(PlayerSettings.defaultIsNativeResolution);
        }

        [Test]
        public void GameView_HasAPlayMaximizedSetting()
        {
            var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            Assert.IsNotNull(gameView, "UnityEditor.GameView exists");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var behaviour = gameView.GetProperty("enterPlayModeBehavior", flags);
            var maximize = gameView.GetProperty("maximizeOnPlay", flags);
            bool viaBehaviour = behaviour != null && behaviour.CanWrite && behaviour.PropertyType.IsEnum
                                && Array.IndexOf(Enum.GetNames(behaviour.PropertyType), "PlayMaximized") >= 0;
            bool viaFlag = maximize != null && maximize.CanWrite;
            Assert.IsTrue(viaBehaviour || viaFlag, "FullScreenSetup needs enterPlayModeBehavior (PlayMaximized) or maximizeOnPlay");
        }
    }
}
