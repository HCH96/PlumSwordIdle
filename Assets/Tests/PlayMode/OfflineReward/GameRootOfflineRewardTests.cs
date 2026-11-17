using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.OfflineReward
{
    public sealed class GameRootOfflineRewardTests
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
        public void BatchBoot_HasNoOfflineRewardOrRestart()
        {
            Assert.That(root.OfflineReward, Is.Null);
            Assert.That(root.OfflineRewardClaim, Is.Null);
            Assert.That(root.IsResumeRestart, Is.False);
        }

        [Test]
        public void BatchBoot_AutomaticSaveAndStageAreStarted()
        {
            Assert.That(root.AutoSave.Enabled, Is.True);
            Assert.That(root.StageProgress.HasBegun, Is.True);
        }

        [Test]
        public void BatchBoot_HasNoOfflineObjects()
        {
            Assert.That(root.transform.Find("[OfflineRewardView]"), Is.Null);
            Assert.That(root.transform.Find("[OfflineResumeWatcher]"), Is.Null);
        }
    }
}
