using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.OfflineReward;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StartMenu;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Flow
{
    public sealed class OfflineRewardFlowTests
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
        public IEnumerator OneHourOffline_ClaimOnceAndRelaunch_ContinuesWithoutAnotherReward()
        {
            long savedAt = DateTime.UtcNow.Ticks - TimeSpan.FromHours(1).Ticks;
            var store = new MemorySaveStore();
            Assert.That(store.TryWrite(SaveCodec.Encode(new SaveData(100, 0, 0, 0, 0, 2, 2, savedAt, 0))), Is.True, "초기 저장 쓰기");
            yield return GameFlow.Relaunch(store, StartEntry.Launch);
            var root = GameFlow.FindRoot();

            Assert.That(root.OfflineReward.HasReward, Is.True, "수령 전 보상 존재");
            Assert.That(root.OfflineReward.KillCount, Is.EqualTo(360), "수령 전 처치 수");
            Assert.That(root.OfflineReward.Gold, Is.EqualTo(4320), "수령 전 보상 금화");
            Assert.That(root.Wallet.Balance, Is.EqualTo(100), "수령 전 보유 금화");
            Assert.That(root.Battle.Paused, Is.True, "수령 전 전투 정지");
            Assert.That(root.StageProgress.HasBegun, Is.False, "수령 전 스테이지 미시작");
            Assert.That(root.AutoSave.Enabled, Is.False, "수령 전 자동 저장 비활성화");
            var buttonObject = root.UiCanvas.transform.Find("OfflineRewardOverlay/OfflineRewardWindow/ClaimButton");
            Assert.That(buttonObject, Is.Not.Null, "수령 버튼 존재");
            var button = buttonObject.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, "수령 버튼 컴포넌트 존재");
            long beforeClaim = DateTime.UtcNow.Ticks;
            button.onClick.Invoke();
            for (var i = 0; i < 3; i++) yield return null;

            Assert.That(root.Wallet.Balance, Is.EqualTo(4420), "수령 후 보유 금화");
            Assert.That(root.OfflineRewardClaim.State, Is.EqualTo(OfflineRewardClaimState.Done), "수령 완료 상태");
            Assert.That(root.OfflineRewardClaim.HasPaid, Is.True, "지급 여부");
            var saved = SaveCodec.Decode(store.Text).Data;
            Assert.That(saved.Gold, Is.EqualTo(4420), "수령 후 저장 금화");
            Assert.That(saved.SavedAtUtcTicks, Is.GreaterThanOrEqualTo(beforeClaim), "수령 후 저장 시각");
            Assert.That(root.UiCanvas.transform.Find("OfflineRewardOverlay"), Is.Null, "수령 후 창 닫힘");
            Assert.That(root.StageProgress.HasBegun, Is.True, "수령 후 스테이지 시작");
            Assert.That(root.StageProgress.StageNumber, Is.EqualTo(2), "수령 후 스테이지 번호");
            Assert.That(root.StageProgress.Phase, Is.EqualTo(StagePhase.SubstageIntro), "수령 후 구간 소개");
            yield return GameFlow.WaitUntil(
                () => root.StageProgress.Phase == StagePhase.Playing,
                10f,
                () => "10초 안에 구간 소개가 끝나지 않았습니다. 현재 단계: " + root.StageProgress.Phase);
            Assert.That(root.Battle.Paused, Is.False, "구간 소개 후 전투 재개");

            var balanceBeforeRetry = root.Wallet.Balance;
            Assert.That(root.OfflineRewardClaim.TryClaim(DateTime.UtcNow.Ticks), Is.True, "중복 수령 호출 결과");
            Assert.That(root.Wallet.Balance, Is.EqualTo(balanceBeforeRetry), "중복 수령 시 금화 유지");

            var savedGoldBeforeRelaunch = SaveCodec.Decode(store.Text).Data.Gold;
            yield return GameFlow.Relaunch(store, StartEntry.Launch);
            root = GameFlow.FindRoot();
            Assert.That(root.OfflineReward.HasReward, Is.False, "재실행 후 보상 없음");
            Assert.That(root.transform.Find("[OfflineRewardView]"), Is.Null, "재실행 후 보상 뷰 없음");
            Assert.That(root.Wallet.Balance, Is.EqualTo(savedGoldBeforeRelaunch), "재실행 후 저장 금화 복원");
            Assert.That(root.Wallet.Balance, Is.GreaterThanOrEqualTo(4420), "재실행 후 수령 금화 유지");
        }
    }
}
