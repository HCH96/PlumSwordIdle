using System;
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
    public sealed class StageHudTests
    {
        [TestCase(2, "2-1")]
        [TestCase(3, "3-1")]
        [TestCase(4, "4-1")]
        [TestCase(7, "2-1")]
        public void AllThemes_ShareTheStageOneProgressHud(int startStage, string expected)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), startStage);
            var canvas = new GameObject("[HudCanvas]", typeof(Canvas));
            var root = new GameObject("[HudView]");
            try
            {
                root.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                var preview = canvas.transform.Find("SubstagePreview");
                Assert.That(preview.gameObject.activeSelf, Is.True);
                var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
                var label = preview.Find("SubstageNumber").GetComponent(textType);
                Assert.That(textType.GetProperty("text").GetValue(label), Is.EqualTo(expected));
                Assert.That(preview.Find("Marker1").GetComponent<Image>().sprite.name, Is.EqualTo("current"));
                Assert.That(canvas.transform.Find("StageHeader").gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(9, 4)]
        [TestCase(7, 2)]
        public void ProgressHud_ChangesFromProgressToBossHealth(int startStage, int displayStage)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 30, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), startStage);
            var canvasObject = new GameObject("[StageHudTestCanvas]", typeof(Canvas));
            var viewObject = new GameObject("[StageHudTestView]");
            try
            {
                var view = viewObject.AddComponent<StageView>();
                view.Initialize(stage, canvasObject.GetComponent<Canvas>());
                stage.Begin();
                var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
                Assert.That(canvasObject.transform.Find("StageCount"), Is.Null);
                var intro = canvasObject.transform.Find("SubstageIntroBanner");
                Assert.That(intro, Is.Not.Null);
                Assert.That(intro.gameObject.activeSelf, Is.True);
                Assert.That(intro.Find("Plaque").GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(intro.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(textType.GetProperty("text").GetValue(intro.Find("SubstageNumber").GetComponent(textType)), Is.EqualTo(displayStage + "-1"));
                var bossName = canvasObject.transform.Find("BossName").gameObject;
                var fill = canvasObject.transform.Find("StageBar/Fill").GetComponent<Image>();
                var frame = canvasObject.transform.Find("JongnamStageHeader").GetComponent<Image>();
                var title = canvasObject.transform.Find("JongnamStageTitle").GetComponent<Image>();
                Assert.That(frame.sprite, Is.Not.Null);
                var preview = canvasObject.transform.Find("SubstagePreview");
                Assert.That(preview, Is.Not.Null);
                Assert.That(preview.gameObject.activeSelf, Is.True);
                Assert.That(preview.Find("Marker1").GetComponent<Image>().sprite.name, Is.EqualTo("current"));
                Assert.That(preview.Find("Marker2").GetComponent<Image>().sprite.name, Is.EqualTo("empty"));
                Assert.That(preview.Find("BossMarker").GetComponent<Image>().sprite.name, Is.EqualTo("boss"));
                Assert.That(textType.GetProperty("text").GetValue(preview.Find("SubstageNumber").GetComponent(textType)), Is.EqualTo(displayStage + "-1"));
                Assert.That(frame.gameObject.activeSelf, Is.False);
                Assert.That(title.sprite, Is.Not.Null);
                Assert.That(title.gameObject.activeSelf, Is.False);
                Assert.That(title.raycastTarget, Is.False);
                Assert.That(((RectTransform)frame.transform).sizeDelta.x, Is.EqualTo(260f));
                Assert.That(((RectTransform)title.transform).anchoredPosition.x, Is.EqualTo(0f));
                Assert.That(((RectTransform)fill.transform.parent).anchoredPosition.x, Is.EqualTo(0f));
                Assert.That(((RectTransform)fill.transform.parent).sizeDelta.y,
                    Is.EqualTo(12.2f * 260f / 380f).Within(0.001f));
                Assert.That(canvasObject.transform.Find("StageLabel").gameObject.activeSelf, Is.False);
                Assert.That(canvasObject.transform.Find("StageHeader").gameObject.activeSelf, Is.False);
                Assert.That(frame.raycastTarget, Is.False);
                Assert.That(bossName.activeSelf, Is.False);
                Assert.That(((RectTransform)fill.transform).anchorMax.x, Is.EqualTo(0f));

                for (int n = 1; n <= 6; n++)
                {
                    stage.Tick(StageProgress.SubstageIntroSeconds);
                    Assert.That(intro.gameObject.activeSelf, Is.False);
                    for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                    Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                    stage.Tick(StageProgress.FadeSeconds * 2);
                    Assert.That(stage.SubstageNumber, Is.EqualTo(n + 1));
                    Assert.That(intro.gameObject.activeSelf, Is.True);
                    Assert.That(textType.GetProperty("text").GetValue(intro.Find("SubstageNumber").GetComponent(textType)), Is.EqualTo(displayStage + "-" + (n + 1)));
                    Assert.That(textType.GetProperty("text").GetValue(preview.Find("SubstageNumber").GetComponent(textType)), Is.EqualTo(displayStage + "-" + (n + 1)));
                    Assert.That(preview.Find("Marker" + n).GetComponent<Image>().sprite.name, Is.EqualTo("completed"));
                    for (int marker = 1; marker <= 6; marker++)
                        Assert.That(((RectTransform)preview.Find("Marker" + marker)).anchoredPosition.y, Is.EqualTo(-96f));
                }
                Assert.That(stage.IsBossActive, Is.True);
                for (int marker = 1; marker <= 6; marker++)
                    Assert.That(preview.Find("Marker" + marker).gameObject.activeSelf, Is.False);
                Assert.That(preview.Find("BossMarker").gameObject.activeSelf, Is.False);
                Assert.That(preview.gameObject.activeSelf, Is.True);
                Assert.That(frame.gameObject.activeSelf, Is.False);
                Assert.That(fill.transform.parent.gameObject.activeSelf, Is.True);
                Assert.That(title.gameObject.activeSelf, Is.False);
                Assert.That(bossName.activeSelf, Is.False);
                Assert.That(textType.GetProperty("text").GetValue(bossName.GetComponent(textType)), Is.EqualTo(StageText.ThemedBossName(displayStage)));
                Assert.That(((RectTransform)fill.transform).anchorMax.x, Is.EqualTo(1f));
                Canvas.ForceUpdateCanvases();
                var fillRect = (RectTransform)fill.transform;
                var track = (RectTransform)fill.transform.parent;
                Assert.That(fillRect.rect.width, Is.EqualTo(track.rect.width).Within(0.001f));
                Assert.That(fillRect.rect.height, Is.EqualTo(track.rect.height).Within(0.001f));
                Assert.That(fill.color.r, Is.GreaterThan(fill.color.g));
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Boss.Health == stage.Boss.MaxHealth; i++) battle.Step();
                Assert.That(stage.Boss.Health, Is.GreaterThan(0).And.LessThan(stage.Boss.MaxHealth));
                Assert.That(((RectTransform)fill.transform).anchorMax.x, Is.EqualTo((float)stage.BarRatio).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
