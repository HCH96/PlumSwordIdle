using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Save;
using PlumSword.Game.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Save
{
    public sealed class GameRootSaveTests
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
        public void BatchBoot_LoadResultIsEmpty()
        {
            Assert.That(root.LoadResult.Status, Is.EqualTo(SaveLoadStatus.Empty));
            Assert.That(root.LoadResult.HasSaveData, Is.False);
        }

        [Test]
        public void BatchBoot_AutoSaveIsEnabled()
        {
            Assert.That(root.AutoSave, Is.Not.Null);
            Assert.That(root.AutoSave.Enabled, Is.True);
        }

        [Test]
        public void BatchBoot_FirstStageHasBegun()
        {
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(1));
            Assert.That(root.StageProgress.HasBegun, Is.True);
        }

        [Test]
        public void BatchBoot_InitialSaveHasFlushed()
        {
            Assert.That(root.AutoSave.IsDirty, Is.False);
            Assert.That(root.AutoSave.HasFailed, Is.False);
        }

        [Test]
        public void BatchBoot_HasSaveView()
        {
            var view = root.transform.Find("[SaveView]");
            Assert.That(view, Is.Not.Null);
            Assert.That(view.GetComponent<SaveView>(), Is.Not.Null);
        }

        [Test]
        public void BatchBoot_HasNoErrorViewOrPanel()
        {
            Assert.That(root.transform.Find("[SaveErrorView]"), Is.Null);
            Assert.That(root.UiCanvas.transform.Find("SaveErrorPanel"), Is.Null);
        }
    }
}
