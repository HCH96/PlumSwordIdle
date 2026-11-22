using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StartMenu;
using PlumSword.Core.StatUpgrade;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Flow
{
    public sealed class SaveReloadFlowTests
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

        [UnityTest]
        public IEnumerator SaveAndRelaunch_AfterStageThree_RestoresAndContinues()
        {
            var store = new MemorySaveStore();
            yield return GameFlow.Relaunch(store, StartEntry.Launch);
            var root = GameFlow.FindRoot();
            Assert.That(root.LoadResult.Status, Is.EqualTo(SaveLoadStatus.Empty), "The initial store must be empty.");

            yield return GameFlow.PowerUp(root, 200);
            root.Wallet.Add(root.StatUpgrades.GetCost(StatType.InnerPower));
            Assert.That(root.StatUpgrades.TryUpgrade(StatType.InnerPower), Is.True, "Inner power must upgrade successfully.");

            Time.timeScale = 20f;
            yield return GameFlow.WaitUntil(() => root.StageProgress.StageNumber == 3, 120f,
                () => DescribeTimeout(root, 3));

            // Freeze progression during the helper's startup frames, then save and deactivate
            // the old root in the same frame before any other game update can occur.
            Time.timeScale = 0f;
            var savedAt = DateTime.UtcNow;
            Assert.That(root.AutoSave.SaveNow(savedAt.Ticks), Is.True, "Immediate save at stage 3 must succeed.");
            var gold = root.Wallet.Balance;
            var attack = root.StatUpgrades.GetLevel(StatType.Attack);
            var health = root.StatUpgrades.GetLevel(StatType.Health);
            var innerPower = root.StatUpgrades.GetLevel(StatType.InnerPower);
            var realm = root.Realm.Current;
            var kills = root.Realm.Kills;
            var stage = root.StageProgress.StageNumber;
            var seenIntro = root.StageProgress.SeenIntroStage;
            var relaunch = GameFlow.Relaunch(store, StartEntry.Launch);
            Assert.That(relaunch.MoveNext(), Is.True, "Relaunch must deactivate the old root immediately.");
            yield return relaunch;

            root = GameFlow.FindRoot();
            Assert.That((DateTime.UtcNow - savedAt).TotalSeconds, Is.LessThan(10), "Reload must finish within the offline reward threshold.");
            Assert.That(root.LoadResult.Status, Is.EqualTo(SaveLoadStatus.Loaded), "The saved game must load.");
            Assert.That(root.Wallet.Balance, Is.EqualTo(gold), "Gold must survive reload.");
            Assert.That(attack, Is.EqualTo(200), "The saved attack level must be 200.");
            Assert.That(health, Is.EqualTo(200), "The saved health level must be 200.");
            Assert.That(innerPower, Is.EqualTo(1), "The saved inner power level must be 1.");
            Assert.That(root.StatUpgrades.GetLevel(StatType.Attack), Is.EqualTo(attack), "Attack level must survive reload.");
            Assert.That(root.StatUpgrades.GetLevel(StatType.Health), Is.EqualTo(health), "Health level must survive reload.");
            Assert.That(root.StatUpgrades.GetLevel(StatType.InnerPower), Is.EqualTo(innerPower), "Inner power level must survive reload.");
            Assert.That(root.Realm.Current, Is.EqualTo(realm), "Realm must survive reload.");
            Assert.That(root.Realm.Kills, Is.EqualTo(kills), "Realm kills must survive reload.");
            Assert.That(stage, Is.EqualTo(3), "The saved stage must be 3.");
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(stage), "Stage must survive reload.");
            Assert.That(root.StageProgress.SubstageNumber, Is.EqualTo(1), "Reload must start at the first substage.");
            Assert.That(root.StageProgress.SeenIntroStage, Is.EqualTo(seenIntro), "Seen intro stage must survive reload.");
            Assert.That(root.StageProgress.HasBegun, Is.True, "The restored stage must begin.");
            Assert.That(root.Battle.DiscipleHealth, Is.EqualTo(root.Battle.DiscipleMaxHealth), "Reload must restore full health.");
            Assert.That(root.OfflineReward, Is.Not.Null, "A loaded launch must calculate offline reward.");
            Assert.That(root.OfflineReward.HasReward, Is.False, "A short reload must not grant offline reward.");
            Assert.That(root.transform.Find("[OfflineRewardView]"), Is.Null, "A short reload must not show an offline reward view.");

            Time.timeScale = 20f;
            var resumedAt = Time.realtimeSinceStartup;
            // The first substage introduction pauses battle even when the theme intro was seen.
            yield return GameFlow.WaitUntil(() => root.StageProgress.Phase == StagePhase.Playing, 120f,
                () => "The restored substage introduction did not finish. " + DescribeTimeout(root, 4));
            Assert.That(root.Battle.Paused, Is.False, "Battle must resume after the substage introduction.");
            yield return GameFlow.WaitUntil(() => root.StageProgress.StageNumber == 4,
                Math.Max(0f, 120f - (Time.realtimeSinceStartup - resumedAt)),
                () => DescribeTimeout(root, 4));
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(4), "Progress must continue to stage 4 after reload.");
        }

        private static string DescribeTimeout(GameRoot root, int target)
        {
            return $"Stage {target} was not reached within 120 real seconds. " +
                   $"StageNumber={root.StageProgress.StageNumber}, SubstageNumber={root.StageProgress.SubstageNumber}, " +
                   $"Phase={root.StageProgress.Phase}, Paused={root.Battle.Paused}, " +
                   $"IsDiscipleDefeated={root.Battle.IsDiscipleDefeated}";
        }
    }
}
