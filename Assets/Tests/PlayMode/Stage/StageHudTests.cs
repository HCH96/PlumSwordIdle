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
        [Test]
        public void ProgressHud_ChangesFromProgressToBossHealth()
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000, 1.0);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1));
            var canvasObject = new GameObject("[StageHudTestCanvas]", typeof(Canvas));
            var viewObject = new GameObject("[StageHudTestView]");
            try
            {
                var view = viewObject.AddComponent<StageView>();
                view.Initialize(stage, canvasObject.GetComponent<Canvas>());
                stage.Begin();
                Assert.That(canvasObject.transform.Find("StageCount"), Is.Null);
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
                var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
                Assert.That(textType.GetProperty("text").GetValue(preview.Find("SubstageNumber").GetComponent(textType)), Is.EqualTo("1-1"));
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
                    for (int i = 0; i < 5000 && stage.SubstageNumber == n; i++) battle.Step();
                    Assert.That(stage.SubstageNumber, Is.EqualTo(n + 1));
                    Assert.That(textType.GetProperty("text").GetValue(preview.Find("SubstageNumber").GetComponent(textType)), Is.EqualTo("1-" + (n + 1)));
                    Assert.That(preview.Find("Marker" + n).GetComponent<Image>().sprite.name, Is.EqualTo("completed"));
                    for (int marker = 1; marker <= 6; marker++)
                        Assert.That(((RectTransform)preview.Find("Marker" + marker)).anchoredPosition.y, Is.EqualTo(-96f));
                }
                Assert.That(stage.IsBossActive, Is.True);
                Assert.That(preview.gameObject.activeSelf, Is.True);
                Assert.That(frame.gameObject.activeSelf, Is.False);
                Assert.That(fill.transform.parent.gameObject.activeSelf, Is.True);
                Assert.That(title.gameObject.activeSelf, Is.False);
                Assert.That(bossName.activeSelf, Is.True);
                Assert.That(((RectTransform)fill.transform).anchorMax.x, Is.EqualTo(1f));
                Canvas.ForceUpdateCanvases();
                var fillRect = (RectTransform)fill.transform;
                var track = (RectTransform)fill.transform.parent;
                Assert.That(fillRect.rect.width, Is.EqualTo(track.rect.width).Within(0.001f));
                Assert.That(fillRect.rect.height, Is.EqualTo(track.rect.height).Within(0.001f));
                Assert.That(fill.color.r, Is.GreaterThan(fill.color.g));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
