using System.Collections;
using NUnit.Framework;
using PlumSword.Core.StartMenu;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.StartMenu
{
    public sealed class GameRootStartMenuTests
    {
        private GameRoot root;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            for (var i = 0; i < 3; i++) yield return null;
            var game = GameObject.Find(GameBootstrap.RootName);
            Assert.That(game, Is.Not.Null);
            root = game.GetComponent<GameRoot>();
            Assert.That(root, Is.Not.Null);
        }

        [Test]
        public void BatchBoot_UsesLaunchAndSkipsStartScreen()
        {
            Assert.That(root.Entry, Is.EqualTo(StartEntry.Launch));
            Assert.That(root.IsResumeRestart, Is.False);
            Assert.That(root.IsStartScreenShowing, Is.False);
            Assert.That(root.transform.Find("[StartScreenView]"), Is.Null);
            Assert.That(root.UiCanvas.transform.Find("StartScreenPanel"), Is.Null);
        }

        [Test]
        public void BatchBoot_InitializesSettingsAndBackKey()
        {
            var settings = root.transform.Find("[SettingsView]");
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.GetComponent<SettingsView>(), Is.SameAs(root.Settings));
            Assert.That(root.Settings.IsOpen, Is.False);
            Assert.That(root.UiCanvas.transform.Find("GameSettingsButton"), Is.Not.Null);
            Assert.That(root.Volume.Bgm, Is.EqualTo(80));
            Assert.That(root.Volume.Sfx, Is.EqualTo(80));
            Assert.That(root.LeaveGame, Is.Not.Null);
            Assert.That(root.LeaveGame.HasFailed, Is.False);
            var watcher = root.transform.Find("[BackKeyWatcher]");
            Assert.That(watcher, Is.Not.Null);
            Assert.That(watcher.GetComponent<BackKeyWatcher>(), Is.Not.Null);
        }

        [Test]
        public void BatchBoot_StartsStageAndAutoSave()
        {
            Assert.That(root.StageProgress.HasBegun, Is.True);
            Assert.That(root.AutoSave.Enabled, Is.True);
        }

        [Test]
        public void InGameSettings_OpenAndCloseWithoutChangingBattlePause()
        {
            var paused = root.Battle.Paused;
            var panel = root.UiCanvas.transform.Find("SettingsPanel");
            Assert.That(panel, Is.Not.Null);
            try
            {
                root.Settings.Open(SettingsMode.InGame);
                Assert.That(root.Settings.IsOpen, Is.True);
                Assert.That(root.Settings.Mode, Is.EqualTo(SettingsMode.InGame));
                Assert.That(panel.gameObject.activeSelf, Is.True);
                Assert.That(panel.GetSiblingIndex(), Is.EqualTo(root.UiCanvas.transform.childCount - 1));
                Assert.That(root.Battle.Paused, Is.EqualTo(paused));
            }
            finally
            {
                root.Settings.Close();
            }
            Assert.That(root.Settings.IsOpen, Is.False);
            Assert.That(panel.gameObject.activeSelf, Is.False);
        }
    }
}
