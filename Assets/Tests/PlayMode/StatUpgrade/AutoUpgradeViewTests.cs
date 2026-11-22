using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Tests.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.StatUpgrade
{
    public sealed class AutoUpgradeViewTests
    {
        private GameObject canvasObject;
        private GameObject viewObject;
        private AutoUpgrade autoUpgrade;

        private void CreateView()
        {
            var wallet = new Wallet();
            autoUpgrade = new AutoUpgrade(new StatUpgrades(wallet, 10, 100), wallet, false);
            canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            viewObject = new GameObject("TestAutoUpgradeView");
            viewObject.AddComponent<PlumSword.Game.StatUpgrade.AutoUpgradeView>()
                .Initialize(autoUpgrade, canvasObject.GetComponent<Canvas>());
        }

        [TearDown]
        public void TearDown()
        {
            if (viewObject != null) UnityEngine.Object.DestroyImmediate(viewObject);
            if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
            autoUpgrade?.Dispose();
        }

        [Test]
        public void Initialize_Disabled_ShowsButtonAndLabel()
        {
            CreateView();
            var button = canvasObject.transform.Find("AutoUpgradeButton");
            Assert.That(button, Is.Not.Null);
            var rect = (RectTransform)button;
            Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(132, 86)));
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(96, 26)));
            Assert.That(ReadLabel(button), Is.EqualTo("자동 강화 꺼짐"));
        }

        [Test]
        public void Click_Disabled_EnablesAndUpdatesLabel()
        {
            CreateView();
            var button = canvasObject.transform.Find("AutoUpgradeButton");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(autoUpgrade.Enabled, Is.True);
            Assert.That(ReadLabel(button), Is.EqualTo("자동 강화 켜짐"));
        }

        private static string ReadLabel(Transform button)
        {
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            return (string)type.GetProperty("text").GetValue(button.Find("Label").GetComponent(type));
        }

        [UnityTest]
        public IEnumerator FirstScene_GameRoot_StartsWithAutoUpgradeEnabled()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return GameFlow.ResetToFreshLaunch();
            var root = GameObject.Find(GameBootstrap.RootName).GetComponent<GameRoot>();
            Assert.That(root.AutoUpgrade, Is.Not.Null);
            Assert.That(root.AutoUpgrade.Enabled, Is.True);
        }
    }
}
