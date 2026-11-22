using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Realm;
using PlumSword.Core.Save;
using PlumSword.Core.StartMenu;
using PlumSword.Core.StatUpgrade;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Flow
{
    public sealed class GameFlowTests
    {
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            for (var i = 0; i < 3; i++) yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return GameFlow.ResetToFreshLaunch();
        }

        private static MemorySaveStore StageThreeStore()
        {
            var store = new MemorySaveStore();
            store.TryWrite(SaveCodec.Encode(new SaveData(500, 3, 2, 1, 1, 3, 3, DateTime.UtcNow.Ticks, 5)));
            return store;
        }

        [UnityTest]
        public IEnumerator Relaunch_WithSavedGame_RestoresAndFlushesState()
        {
            var oldRoot = GameFlow.FindRoot();
            var oldObject = oldRoot.gameObject;
            var store = StageThreeStore();
            yield return GameFlow.Relaunch(store, StartEntry.Launch);
            var root = GameFlow.FindRoot();
            Assert.That(root.LoadResult.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(root.Wallet.Balance, Is.EqualTo(500));
            Assert.That(root.StatUpgrades.GetLevel(StatType.Attack), Is.EqualTo(3));
            Assert.That(root.StatUpgrades.GetLevel(StatType.Health), Is.EqualTo(2));
            Assert.That(root.StatUpgrades.GetLevel(StatType.InnerPower), Is.EqualTo(1));
            Assert.That(root.Realm.Current, Is.EqualTo(RealmLevel.SecondRate));
            Assert.That(root.Realm.Kills, Is.EqualTo(5));
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(3));
            Assert.That(root.StageProgress.HasBegun, Is.True);
            Assert.That(root.OfflineReward.HasReward, Is.False);
            Assert.That(SaveCodec.Decode(store.Text).Data.StageNumber, Is.EqualTo(3));
            Assert.That(UnityEngine.Object.FindObjectsByType<GameRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(oldObject == null, Is.True);
        }

        [UnityTest]
        public IEnumerator ResetToFreshLaunch_AfterSavedGame_StartsFresh()
        {
            yield return GameFlow.Relaunch(StageThreeStore(), StartEntry.Launch);
            yield return GameFlow.ResetToFreshLaunch();
            var root = GameFlow.FindRoot();
            Assert.That(root.Entry, Is.EqualTo(StartEntry.Launch));
            Assert.That(root.LoadResult.Status, Is.EqualTo(SaveLoadStatus.Empty));
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(1));
            Assert.That(GameRoot.BatchSaveStore, Is.Null);
        }

        [UnityTest]
        public IEnumerator ResetToFreshLaunch_AfterSceneReload_KeepsRootCameraAlive()
        {
            yield return GameFlow.ResetToFreshLaunch();
            var root = GameFlow.FindRoot();
            var camera = root.GetComponentInChildren<Camera>();
            Assert.That(camera, Is.Not.Null);
            yield return SceneManager.LoadSceneAsync(0);
            for (var i = 0; i < 3; i++) yield return null;
            Assert.That(GameFlow.FindRoot(), Is.EqualTo(root));
            Assert.That(camera != null, Is.True);
        }

        [UnityTest]
        public IEnumerator PowerUp_TenLevels_UpgradesWithoutSpendingStartingGold()
        {
            yield return GameFlow.ResetToFreshLaunch();
            var root = GameFlow.FindRoot();
            var balance = root.Wallet.Balance;
            yield return GameFlow.PowerUp(root, 10);
            Assert.That(root.StatUpgrades.GetLevel(StatType.Attack), Is.EqualTo(10));
            Assert.That(root.StatUpgrades.GetLevel(StatType.Health), Is.EqualTo(10));
            Assert.That(root.StatUpgrades.GetLevel(StatType.InnerPower), Is.EqualTo(0));
            Assert.That(root.Wallet.Balance, Is.EqualTo(balance));
        }
    }
}
