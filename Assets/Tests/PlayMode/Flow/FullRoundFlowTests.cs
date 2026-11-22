using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Realm;
using PlumSword.Core.Save;
using PlumSword.Core.StartMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Flow
{
    public sealed class FullRoundFlowTests
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
            Assert.That(GameFlow.FindRoot().StageProgress.StageNumber, Is.EqualTo(1), "정리 후 스테이지가 1이어야 합니다.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "정리 후 시간 배율이 1이어야 합니다.");
        }

        [UnityTest]
        public IEnumerator FreshLaunch_DefeatFinalBoss_ShowsEndingAndClearsSave()
        {
            var store = new MemorySaveStore();
            yield return GameFlow.Relaunch(store, StartEntry.Launch);
            var root = GameFlow.FindRoot();
            Assert.That(root.LoadResult.Status, Is.EqualTo(SaveLoadStatus.Empty), "빈 저장소는 Empty여야 합니다.");
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(1), "새 게임은 1스테이지에서 시작해야 합니다.");
            Assert.That(root.StageProgress.RoundNumber, Is.EqualTo(1), "새 게임은 1회차에서 시작해야 합니다.");

            var cleared = new List<int>();
            var failed = new List<int>();
            root.StageProgress.StageCleared += cleared.Add;
            root.StageProgress.StageFailed += failed.Add;
            yield return GameFlow.PowerUp(root, 200);
            Assert.That(root.StatUpgrades.Attack, Is.EqualTo(410), "공격력 200단계는 410이어야 합니다.");
            Assert.That(root.StatUpgrades.MaxHealth, Is.EqualTo(4100), "체력 200단계는 4100이어야 합니다.");
            var balanceAfterUpgrade = root.Wallet.Balance;

            Time.timeScale = 20f;
            yield return GameFlow.WaitUntil(() => root.StageProgress.StageNumber == 6, 180f,
                () => $"180초 안에 6스테이지에 도달하지 못했습니다. StageNumber={root.StageProgress.StageNumber}, " +
                      $"SubstageNumber={root.StageProgress.SubstageNumber}, Phase={root.StageProgress.Phase}, " +
                      $"IsDiscipleDefeated={root.Battle.IsDiscipleDefeated}, " +
                      $"StageCleared=[{string.Join(", ", cleared)}], StageFailed=[{string.Join(", ", failed)}]");
            for (var i = 0; i < 3; i++) yield return null;

            Assert.That(root.StageProgress.RoundNumber, Is.EqualTo(2), "천마 처치 후 2회차여야 합니다.");
            Assert.That(root.StageProgress.DisplayStageNumber, Is.EqualTo(1), "2회차의 표시 스테이지는 1이어야 합니다.");
            Assert.That(root.StageProgress.SubstageNumber, Is.EqualTo(1), "2회차는 첫 구간에서 시작해야 합니다.");
            Assert.That(cleared, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }), "1~5스테이지를 순서대로 완료해야 합니다.");
            Assert.That(failed, Is.Empty, "1회차에서 실패 이벤트가 없어야 합니다.");
            Assert.That(root.Realm.Current, Is.EqualTo(RealmLevel.SecondRate), "1회차 종료 경지는 이류여야 합니다.");
            Assert.That(root.Wallet.Balance - balanceAfterUpgrade, Is.InRange(974L, 1286L), "1회차 금화 보상이 예상 범위여야 합니다.");
            var saved = SaveCodec.Decode(store.Text);
            Assert.That(saved.Status, Is.EqualTo(SaveLoadStatus.Loaded), "자동 저장을 읽을 수 있어야 합니다.");
            Assert.That(saved.Data.StageNumber, Is.EqualTo(6), "자동 저장에 다음 회차 스테이지가 기록되어야 합니다.");
        }
    }
}
