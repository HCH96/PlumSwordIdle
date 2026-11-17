using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Game.Currency;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests
{
    public sealed class CurrencyViewTests
    {
        [UnityTest]
        public IEnumerator Artwork_BalanceChanges_UpdatesAndExpandsForLargeAmounts()
        {
            var canvas = new GameObject("TestCanvas", typeof(Canvas));
            var view = new GameObject("TestCurrency", typeof(CurrencyView));
            try
            {
                var wallet = new Wallet(21950);
                view.GetComponent<CurrencyView>().Initialize(wallet, canvas.GetComponent<Canvas>());
                var plaque = (RectTransform)canvas.transform.Find("GoldPlaque");
                Assert.That(plaque.GetComponent<Image>().sprite.name, Is.EqualTo("currency_plaque"));
                Assert.That(plaque.Find("Coin").GetComponent<Image>().sprite.name, Is.EqualTo("coin"));
                Assert.That(plaque.GetComponent<Image>().raycastTarget, Is.False);
                var label = plaque.Find("GoldLabel");
                Assert.That(ReadText(label), Is.EqualTo("21,950"));
                var initialWidth = plaque.sizeDelta.x;
                wallet.Add(long.MaxValue);
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(ReadText(label), Is.EqualTo("9,223,372,036,854,775,807"));
                Assert.That(plaque.sizeDelta.x, Is.GreaterThan(initialWidth));
                Assert.That(plaque.sizeDelta.x, Is.LessThanOrEqualTo(200));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view);
                UnityEngine.Object.DestroyImmediate(canvas);
            }
        }

        [UnityTest]
        public IEnumerator Balance_GainsCountUpAndSpendingImmediatelyCorrectsDisplay()
        {
            var canvas = new GameObject("TestCanvas", typeof(Canvas));
            var view = new GameObject("TestCurrency", typeof(CurrencyView));
            try
            {
                var wallet = new Wallet(0);
                view.GetComponent<CurrencyView>().Initialize(wallet, canvas.GetComponent<Canvas>());
                var label = canvas.transform.Find("GoldPlaque/GoldLabel");
                wallet.Add(10);
                Assert.That(ReadText(label), Is.EqualTo("0"));
                yield return new WaitForSecondsRealtime(.15f);
                var middle = int.Parse(ReadText(label));
                Assert.That(middle, Is.GreaterThan(0).And.LessThan(10));
                wallet.Add(10);
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(ReadText(label), Is.EqualTo("20"));
                wallet.Add(10);
                yield return new WaitForSecondsRealtime(.1f);
                wallet.TrySpend(15);
                Assert.That(ReadText(label), Is.EqualTo("15"));
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(ReadText(label), Is.EqualTo("15"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(view);
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
