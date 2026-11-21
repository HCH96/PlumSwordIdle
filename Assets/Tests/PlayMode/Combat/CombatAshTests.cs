using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class CombatAshTests
    {
        [UnityTest]
        public IEnumerator StageFour_AshAppearsOnlyForItsThemeAndDoesNotBlockInput()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var root = GameObject.Find(GameBootstrap.RootName).GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            var ash = root.UiCanvas.transform.Find("[CombatAsh]");
            Assert.That(ash, Is.Not.Null);
            Assert.That(ash.childCount, Is.EqualTo(20));
            for (int i = 1; i <= 5; i++)
            {
                var texture = Resources.Load<Texture2D>("Art/Effects/Stage4Ash/ash_0" + i);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(1536));
                Assert.That(texture.height, Is.EqualTo(1024));
            }

            try
            {
                var wallet = new Wallet();
                var upgrades = new StatUpgrades(wallet, 1000, 10000);
                var battle = new Battle(upgrades);
                foreach (int stage in new[] { 4, 5, 9, 1 })
                {
                    property.GetSetMethod(true).Invoke(root, new object[]
                    {
                        new StageProgress(wallet, upgrades, battle, new System.Random(1), stage)
                    });
                    yield return null;
                    int visible = 0;
                    foreach (Transform child in ash)
                    {
                        var image = child.GetComponent<Image>();
                        Assert.That(image.raycastTarget, Is.False);
                        if (!image.enabled) continue;
                        visible++;
                        Assert.That(image.sprite.name, Does.StartWith("stage4_ash_"));
                        var size = ((RectTransform)child).sizeDelta;
                        Assert.That(Mathf.Max(size.x, size.y), Is.InRange(3.84f, 7.71f));
                    }
                    if (stage == 4 || stage == 9) Assert.That(visible, Is.GreaterThan(0));
                    else Assert.That(visible, Is.Zero);
                }
            }
            finally { property.GetSetMethod(true).Invoke(root, new object[] { original }); }
        }
    }
}
