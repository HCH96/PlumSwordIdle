using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Balance;
using PlumSword.Core.Save;
using PlumSword.Core.StartMenu;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Flow
{
    public sealed class BalanceStartStatsFlowTests
    {
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            for (var i = 0; i < 3; i++) yield return null;
            yield return GameFlow.Relaunch(new MemorySaveStore(), StartEntry.Launch);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            BalanceTable.Current = BalanceTable.Default;
            yield return GameFlow.ResetToFreshLaunch();
        }

        [UnityTest]
        public IEnumerator Relaunch_WithChangedBalance_UsesChangedStartStats()
        {
            var table = BalanceTable.Default;
            table.DiscipleBaseAttack = 15;
            table.DiscipleBaseMaxHealth = 150;
            BalanceTable.Current = table;
            yield return GameFlow.Relaunch(new MemorySaveStore(), StartEntry.Launch);
            var root = GameFlow.FindRoot();
            Assert.That(root.StatUpgrades.Attack, Is.EqualTo(15));
            Assert.That(root.StatUpgrades.MaxHealth, Is.EqualTo(150));
        }

        [UnityTest]
        public IEnumerator Relaunch_WithDefaultBalance_UsesDefaultStartStats()
        {
            BalanceTable.Current = BalanceTable.Default;
            yield return GameFlow.Relaunch(new MemorySaveStore(), StartEntry.Launch);
            var root = GameFlow.FindRoot();
            Assert.That(root.StatUpgrades.Attack, Is.EqualTo(10));
            Assert.That(root.StatUpgrades.MaxHealth, Is.EqualTo(100));
        }
    }
}
