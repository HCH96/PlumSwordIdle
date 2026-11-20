using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.OfflineReward;
using PlumSword.Core.Realm;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.OfflineReward;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.OfflineReward
{
    public sealed class OfflineRewardViewTests
    {
        private sealed class Store : ISaveStore
        {
            public bool Succeeds;
            public bool TryWrite(string text) => Succeeds;
        }

        [UnityTest]
        public IEnumerator Artwork_HoverAndFailedClaim_RetryPaysOnce()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            var viewObject = new GameObject("TestOfflineRewardView");
            var wallet = new Wallet(100);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), 1, 1);
            var store = new Store();
            var save = new AutoSave(store, wallet, upgrades, new RealmProgress(wallet), stage, enabled: false);
            var reward = OfflineRewardCalculator.Calculate(0, TimeSpan.FromHours(9).Ticks, 1);
            var claim = new OfflineRewardClaim(wallet, save, reward);
            int callbacks = 0;
            try
            {
                viewObject.AddComponent<OfflineRewardView>().Initialize(claim, canvasObject.GetComponent<Canvas>(), () => callbacks++);
                var window = canvasObject.transform.Find("OfflineRewardOverlay/OfflineRewardWindow");
                Assert.That(window.GetComponent<Image>().sprite.name, Is.EqualTo("window"));
                Assert.That(window.Find("RewardPlaque").GetComponent<Image>().sprite.name, Is.EqualTo("reward_plaque"));
                Assert.That(window.Find("GoldGroup/Coin").GetComponent<Image>().sprite.name, Is.EqualTo("coin"));
                Assert.That(window.Find("Applied").gameObject.activeSelf, Is.True);
                Assert.That(window.Find("Maximum").gameObject.activeSelf, Is.True);
                Assert.That(window.Find("Title").GetComponent<Image>().sprite.name, Is.EqualTo("title_label"));
                Assert.That(window.Find("ElapsedLabel").GetComponent<Image>().sprite.name, Is.EqualTo("elapsed_label"));
                Assert.That(window.Find("AppliedLabel").GetComponent<Image>().sprite.name, Is.EqualTo("applied_label"));
                Assert.That(window.Find("AppliedBackdrop"), Is.Null);
                var elapsedLabel = window.Find("ElapsedLabel").GetComponent<Image>();
                var appliedLabel = window.Find("AppliedLabel").GetComponent<Image>();
                Assert.That(elapsedLabel.preserveAspect && appliedLabel.preserveAspect, Is.True);
                Assert.That(elapsedLabel.rectTransform.sizeDelta, Is.EqualTo(appliedLabel.rectTransform.sizeDelta));
                Assert.That(elapsedLabel.rectTransform.anchoredPosition.x, Is.EqualTo(appliedLabel.rectTransform.anchoredPosition.x));
                var button = window.Find("ClaimButton");
                var effect = button.GetComponent<SettingsControlEffect>();
                var art = (RectTransform)button.Find("InteractionArt");
                Assert.That(art.GetComponent<Image>().sprite.name, Is.EqualTo("claim_button"));
                Assert.That(art.Find("Text").GetComponent<Image>().sprite.name, Is.EqualTo("claim_label"));
                effect.OnPointerEnter(null);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(art.localScale.x, Is.GreaterThan(1f));
                effect.OnPointerDown(null);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(art.localScale.x, Is.LessThan(1f));
                effect.OnPointerUp(null);
                button.GetComponent<Button>().onClick.Invoke();
                Assert.That(window.Find("SaveFailed").gameObject.activeSelf, Is.True);
                Assert.That(art.Find("Text").GetComponent<Image>().sprite.name, Is.EqualTo("retry_label"));
                Assert.That(window.Find("SaveFailed").GetComponent<Image>().sprite.name, Is.EqualTo("save_failed_label"));
                Assert.That(wallet.Balance, Is.EqualTo(100 + reward.Gold));
                store.Succeeds = true;
                button.GetComponent<Button>().onClick.Invoke();
                Assert.That(wallet.Balance, Is.EqualTo(100 + reward.Gold));
                Assert.That(callbacks, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void OrdinaryReward_ShowsBothTimesAndGoldWithoutLetter()
        {
            var canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            var viewObject = new GameObject("TestOfflineRewardView");
            try
            {
                var wallet = new Wallet(0);
                var upgrades = new StatUpgrades(wallet, 10, 100);
                var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), 1, 1);
                var save = new AutoSave(new Store(), wallet, upgrades, new RealmProgress(wallet), stage, enabled: false);
                var reward = OfflineRewardCalculator.Calculate(0, TimeSpan.FromSeconds(25).Ticks, 1);
                viewObject.AddComponent<OfflineRewardView>().Initialize(new OfflineRewardClaim(wallet, save, reward), canvasObject.GetComponent<Canvas>(), () => { });
                var window = canvasObject.transform.Find("OfflineRewardOverlay/OfflineRewardWindow");
                var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
                var text = textType.GetProperty("text");
                Assert.That(text.GetValue(window.Find("Elapsed").GetComponent(textType)), Is.EqualTo("0:00:25"));
                Assert.That(text.GetValue(window.Find("Applied").GetComponent(textType)), Is.EqualTo("0:00:20"));
                var gold = window.Find("GoldGroup/Gold");
                Assert.That(gold.childCount, Is.EqualTo(2));
                Assert.That(gold.GetChild(0).GetComponent<Image>().sprite.rect.x, Is.EqualTo(403));
                Assert.That(gold.GetChild(1).GetComponent<Image>().sprite.rect.x, Is.EqualTo(33));
                var group = (RectTransform)window.Find("GoldGroup");
                var coin = (RectTransform)group.Find("Coin");
                var last = (RectTransform)gold.GetChild(1);
                Assert.That(coin.anchoredPosition.x - coin.rect.width / 2, Is.EqualTo(-(last.anchoredPosition.x + last.rect.width / 2)).Within(.01f));
                Assert.That(window.Find("Applied").gameObject.activeSelf, Is.True);
                Assert.That(window.Find("Maximum").gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
