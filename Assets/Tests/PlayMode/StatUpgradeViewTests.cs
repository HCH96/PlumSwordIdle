using System;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.StatUpgrade;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Tests
{
    public sealed class StatUpgradeViewTests
    {
        [Test]
        public void Artwork_UpgradeAndMax_UpdateLabelsAndAvailability()
        {
            var canvas = new GameObject("TestCanvas", typeof(Canvas));
            var viewObject = new GameObject("TestUpgradeView", typeof(StatUpgradeView));
            try
            {
                var wallet = new Wallet(1000);
                var upgrades = new StatUpgrades(wallet, 10, 100, 0, int.MaxValue);
                viewObject.GetComponent<StatUpgradeView>().Initialize(upgrades, wallet, canvas.GetComponent<Canvas>());
                var attack = canvas.transform.Find("StatButton_Attack");
                var health = canvas.transform.Find("StatButton_Health");
                Assert.That(attack.Find("Visual").GetComponent<Image>().sprite.name, Is.EqualTo("button"));
                Assert.That(attack.Find("Visual/Icon").GetComponent<Image>().sprite.name, Is.EqualTo("attack"));
                Assert.That(ReadText(attack.Find("Visual/Cost")), Is.EqualTo("20"));
                attack.GetComponent<Button>().onClick.Invoke();
                Assert.That(ReadText(attack.Find("Visual/Level")), Is.EqualTo("Lv.1"));
                Assert.That(ReadText(attack.Find("Visual/Cost")), Is.EqualTo("25"));
                Assert.That(wallet.Balance, Is.EqualTo(980));
                Assert.That(ReadText(health.Find("Visual/Cost")), Is.EqualTo("MAX"));
                Assert.That(health.Find("Visual/Coin").gameObject.activeSelf, Is.False);
                Assert.That(health.GetComponent<Button>().interactable, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void ButtonEffect_HoverPressAndPurchase_OnlyAnimatesWhenAvailable()
        {
            var canvas = new GameObject("TestCanvas", typeof(Canvas));
            var viewObject = new GameObject("TestUpgradeView", typeof(StatUpgradeView));
            try
            {
                var wallet = new Wallet(20);
                var upgrades = new StatUpgrades(wallet, 10, 100);
                viewObject.GetComponent<StatUpgradeView>().Initialize(upgrades, wallet, canvas.GetComponent<Canvas>());
                var attack = canvas.transform.Find("StatButton_Attack");
                var visual = attack.Find("Visual");
                var effect = attack.GetComponent<StatUpgradeButtonEffect>();
                effect.OnPointerEnter(null); effect.SendMessage("Update");
                Assert.That(visual.localScale.x, Is.GreaterThan(1));
                effect.OnPointerDown(null); effect.SendMessage("Update");
                Assert.That(visual.localScale.x, Is.LessThan(1));
                effect.OnPointerUp(null);
                attack.GetComponent<Button>().onClick.Invoke();
                Assert.That(attack.Find("SuccessPetal0").GetComponent<Image>().enabled, Is.True);
                Assert.That(upgrades.GetLevel(StatType.Attack), Is.EqualTo(1));
                var health = canvas.transform.Find("StatButton_Health");
                health.GetComponent<StatUpgradeButtonEffect>().OnPointerEnter(null);
                health.GetComponent<StatUpgradeButtonEffect>().SendMessage("Update");
                health.GetComponent<Button>().onClick.Invoke();
                Assert.That(health.Find("Visual").localScale.x, Is.EqualTo(1));
                Assert.That(health.Find("SuccessPetal0").GetComponent<Image>().enabled, Is.False);
                Assert.That(upgrades.GetLevel(StatType.Health), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        private static string ReadText(Transform item)
        {
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            return (string)type.GetProperty("text").GetValue(item.GetComponent(type));
        }
    }
}
