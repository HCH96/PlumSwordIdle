using System.Reflection;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class BossHealthFrameTests
    {
        private GameObject _canvas, _root;
        private StageProgress _stage;
        private Battle _battle;

        private void StartStage(int number)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            _battle = new Battle(upgrades);
            _stage = new StageProgress(wallet, upgrades, _battle, new System.Random(1), number);
            _canvas = new GameObject("[BossFrameCanvas]", typeof(Canvas));
            _root = new GameObject("[BossFrameView]");
            _root.AddComponent<StageView>().Initialize(_stage, _canvas.GetComponent<Canvas>());
            _stage.Begin();
        }

        [TearDown]
        public void Cleanup()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_canvas != null) Object.DestroyImmediate(_canvas);
        }

        private void AdvanceToBoss()
        {
            for (int n = 0; n < 6 && !_stage.IsBossActive; n++)
            {
                _stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && _stage.Phase == StagePhase.Playing; i++) _battle.Step();
                Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                _stage.Tick(StageProgress.FadeSeconds * 2);
            }
            Assert.That(_stage.IsBossActive, Is.True);
        }

        private RectTransform Rect(string name) => (RectTransform)_canvas.transform.Find(name);

        private static void CheckRect(RectTransform rect, float x, float y, float width, float height)
        {
            Assert.That(rect.anchoredPosition.x, Is.EqualTo(x).Within(0.1f));
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(y).Within(0.1f));
            Assert.That(rect.sizeDelta.x, Is.EqualTo(width).Within(0.1f));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(height).Within(0.1f));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void BossStage_ShowsMatchingFrame(int number)
        {
            StartStage(number);
            AdvanceToBoss();
            var frame = Rect("BossHealthFrame");
            var image = frame.GetComponent<Image>();
            Assert.That(frame.gameObject.activeSelf, Is.True);
            Assert.That(image.sprite.name, Is.EqualTo("boss_health_frame_" + number));
            Assert.That(image.raycastTarget, Is.False);
            CheckRect(frame, 0, -126, 300, 100);
            if (number == 1)
            {
                CheckRect(Rect("StageBar"), 13.7f, -127.4f, 215.6f, 8.3f);
                Assert.That(Rect("StageBar").GetSiblingIndex(), Is.LessThan(frame.GetSiblingIndex()));
                Assert.That(frame.GetSiblingIndex(), Is.LessThan(Rect("BossName").GetSiblingIndex()));
            }
            if (number == 5)
            {
                Assert.That(Rect("StageBar").sizeDelta.x, Is.EqualTo(193.1f).Within(0.1f));
                Assert.That(Rect("StageBar").sizeDelta.y, Is.EqualTo(11.0f).Within(0.1f));
            }
        }

        [Test]
        public void NormalStage_HidesFrameAndBar()
        {
            StartStage(1);
            Assert.That(Rect("BossHealthFrame").gameObject.activeSelf, Is.False);
            Assert.That(Rect("StageBar").gameObject.activeSelf, Is.False);
            Assert.That(Rect("StageBar").anchoredPosition.x, Is.EqualTo(0f));
            Assert.That(Rect("StageBar").anchoredPosition.y, Is.EqualTo(-76f * 260f / 380f).Within(0.001f));
            Assert.That(Rect("StageBar").sizeDelta.x, Is.EqualTo(264f * 260f / 380f).Within(0.001f));
            Assert.That(Rect("StageBar").sizeDelta.y, Is.EqualTo(12.2f * 260f / 380f).Within(0.001f));
        }

        [Test]
        public void BossDefeated_NextStageHidesFrame()
        {
            StartStage(1);
            AdvanceToBoss();
            _stage.Tick(StageProgress.SubstageIntroSeconds);
            for (int i = 0; i < 5000 && _stage.Phase == StagePhase.Playing; i++) _battle.Step();
            Assert.That(_stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            _stage.Tick(StageProgress.FadeSeconds);
            Assert.That(_stage.DisplayStageNumber, Is.EqualTo(2));
            Assert.That(_stage.SubstageNumber, Is.EqualTo(1));
            Assert.That(Rect("BossHealthFrame").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MissingFrame_UsesFallbackBar()
        {
            var field = typeof(StageView).GetField("BossHealthFramePath", BindingFlags.NonPublic | BindingFlags.Static);
            var original = field.GetValue(null);
            try
            {
                field.SetValue(null, "missing/boss_health_frame_");
                StartStage(1);
                AdvanceToBoss();
                Assert.That(Rect("BossHealthFrame").gameObject.activeSelf, Is.False);
                CheckRect(Rect("StageBar"), 0, -126, 180, 7);
            }
            finally { field.SetValue(null, original); }
        }
    }
}
