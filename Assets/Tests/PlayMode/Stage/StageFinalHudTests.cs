using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class StageFinalHudTests
    {
        private GameObject root;
        private GameObject canvas;
        private Battle battle;
        private StageProgress stage;

        private void Begin(int startStage)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), startStage);
            root = new GameObject("[FinalHudView]");
            canvas = new GameObject("[FinalHudCanvas]", typeof(Canvas));
            root.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
            stage.Begin();
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
        }

        private static string Label(Transform item)
        {
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            return (string)type.GetProperty("text").GetValue(item.GetComponent(type));
        }

        private static void AssertMarkers(Transform preview, bool visible)
        {
            for (int i = 1; i <= 6; i++)
                Assert.That(preview.Find("Marker" + i).gameObject.activeSelf, Is.EqualTo(visible));
            Assert.That(preview.Find("BossMarker").gameObject.activeSelf, Is.EqualTo(visible));
        }

        [Test]
        public void FinalStage_BeginsWithFinalBossHud()
        {
            Begin(5);
            var preview = canvas.transform.Find("SubstagePreview");
            Assert.That(preview.gameObject.activeSelf, Is.True);
            Assert.That(Label(preview.Find("SubstageNumber")), Is.EqualTo("FINAL"));
            Assert.That(Label(canvas.transform.Find("SubstageIntroBanner/SubstageNumber")), Is.EqualTo("FINAL"));
            AssertMarkers(preview, false);
            var bossName = canvas.transform.Find("BossName");
            Assert.That(bossName.gameObject.activeSelf, Is.True);
            Assert.That(Label(bossName), Is.EqualTo("HEAVENLY DEMON"));
            Assert.That(((RectTransform)canvas.transform.Find("StageBar/Fill")).anchorMax.x, Is.EqualTo(1f));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(9, 4)]
        public void NormalAndBossStages_UpdateMarkersAndBossHud(int startStage, int displayStage)
        {
            Begin(startStage);
            var preview = canvas.transform.Find("SubstagePreview");
            AssertMarkers(preview, true);
            for (int n = 0; n < 6; n++)
            {
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                stage.Tick(StageProgress.FadeSeconds * 2);
            }
            Assert.That(stage.IsBossActive, Is.True);
            Assert.That(preview.gameObject.activeSelf, Is.True);
            AssertMarkers(preview, false);
            Assert.That(Label(preview.Find("SubstageNumber")), Is.EqualTo(displayStage + "-7"));
            Assert.That(canvas.transform.Find("BossName").gameObject.activeSelf, Is.True);
            Assert.That(canvas.transform.Find("StageBar").gameObject.activeSelf, Is.True);
        }

        private void DefeatFinalBoss()
        {
            stage.Tick(StageProgress.SubstageIntroSeconds);
            for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
        }

        [UnityTest]
        public IEnumerator FinalBoss_WithDownSheet_WaitsBeforeFade()
        {
            var sheet = Resources.Load<Texture2D>("Art/Enemies/tianmo_final_boss_down");
            Assert.That(sheet, Is.Not.Null);
            Assert.That(sheet.width, Is.EqualTo(2600));
            Assert.That(sheet.height, Is.EqualTo(104));
            Begin(5);
            DefeatFinalBoss();
            yield return new WaitForSeconds(1.5f);
            Assert.That(stage.FadeAlpha, Is.EqualTo(0).Within(0.001));
            yield return new WaitForSeconds(1f);
            Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator FinalBoss_WithoutDownSheet_FadesImmediately()
        {
            var field = typeof(StageView).GetField("FinalBossDownPath", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null);
            var original = field.GetValue(null);
            try
            {
                field.SetValue(null, "Art/Enemies/missing_final_boss_down");
                Begin(5);
                DefeatFinalBoss();
                yield return new WaitForSeconds(0.5f);
                Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
            }
            finally { field.SetValue(null, original); }
        }
    }
}
