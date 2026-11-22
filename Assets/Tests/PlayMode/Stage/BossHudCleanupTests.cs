using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Stage;
using UnityEngine;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class BossHudCleanupTests
    {
        private GameObject canvas, root;
        private StageProgress stage;
        private Battle battle;

        private void Begin(int number)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), number);
            canvas = new GameObject("[BossHudCanvas]", typeof(Canvas));
            root = new GameObject("[BossHudView]");
            root.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
            stage.Begin();
        }

        private void AdvanceToBoss()
        {
            for (int n = 0; n < 6; n++)
            {
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                stage.Tick(StageProgress.FadeSeconds * 2);
            }
            Assert.That(stage.IsBossActive, Is.True);
        }

        private void AssertTrackVisible(bool visible)
        {
            var preview = canvas.transform.Find("SubstagePreview");
            Assert.That(preview, Is.Not.Null);
            foreach (var name in new[] { "Track", "TrackHighlight", "Progress" })
                Assert.That(preview.Find(name).gameObject.activeSelf, Is.EqualTo(visible), name);
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        [Test]
        public void NormalStage_ShowsTrack()
        {
            Begin(1);
            var preview = canvas.transform.Find("SubstagePreview");
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Find("Track").gameObject.activeSelf, Is.True);
            Assert.That(preview.Find("TrackHighlight").gameObject.activeSelf, Is.True);
            Assert.That(preview.Find("Progress").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void BossStage_HidesTrack()
        {
            Begin(1);
            AdvanceToBoss();
            var preview = canvas.transform.Find("SubstagePreview");
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Find("Track").gameObject.activeSelf, Is.False);
            Assert.That(preview.Find("TrackHighlight").gameObject.activeSelf, Is.False);
            Assert.That(preview.Find("Progress").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void BossDefeated_NextNormalStageShowsTrackAgain()
        {
            Begin(1);
            AdvanceToBoss();
            stage.Tick(StageProgress.SubstageIntroSeconds);
            for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            stage.Tick(StageProgress.FadeSeconds);
            Assert.That(stage.IsBossActive, Is.False);
            AssertTrackVisible(true);
        }

        [Test]
        public void BossStage_HidesBossNameText()
        {
            Begin(1);
            AdvanceToBoss();
            Assert.That(canvas.transform.Find("BossName").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ThirdStageBoss_FrameIsBelowProgressHeader()
        {
            Begin(3);
            AdvanceToBoss();
            var frame = (RectTransform)canvas.transform.Find("BossHealthFrame");
            Assert.That(frame.anchoredPosition.x, Is.EqualTo(0f).Within(0.1f));
            Assert.That(frame.anchoredPosition.y, Is.EqualTo(-112f).Within(0.1f));
        }
    }
}
