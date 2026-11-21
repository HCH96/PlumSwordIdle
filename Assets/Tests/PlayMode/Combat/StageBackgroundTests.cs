using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class StageBackgroundTests
    {
        [UnityTest]
        public IEnumerator StageChanges_SelectBackgroundAndFallback()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            var obj = new GameObject("[BackgroundTest]");
            var camera = new GameObject("[BackgroundTestCamera]");
            try
            {
                obj.transform.SetParent(game.transform, false);
                var wallet = new Wallet();
                var upgrades = new StatUpgrades(wallet, 1000, 10000);
                var battle = new Battle(upgrades);
                var view = obj.AddComponent<CombatView>();
                view.Initialize(battle, camera.AddComponent<Camera>());
                foreach (int stageNumber in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1 })
                {
                    var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), stageNumber);
                    property.GetSetMethod(true).Invoke(root, new object[] { stage });
                    view.SendMessage("Update");
                    var tile = obj.transform.Find("[StageBackground]");
                    Assert.That(tile.gameObject.activeSelf, Is.True);
                    Assert.That(obj.transform.Find("[Ground]").gameObject.activeSelf, Is.False);
                    string[] names = { "stage1_preview", "stage2_gaebang", "stage3_namgung", "stage4_magyo", "stage5_tianmo" };
                    Assert.That(tile.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo(names[(stageNumber - 1) % 5]));
                    if (stageNumber == 4 || stageNumber == 5)
                    {
                        var tiles = new System.Collections.Generic.List<SpriteRenderer>();
                        foreach (Transform child in obj.transform)
                            if (child.name == "[StageBackground]") tiles.Add(child.GetComponent<SpriteRenderer>());
                        Assert.That(tiles.Count, Is.EqualTo(3));
                        Assert.That(tiles[0].flipX, Is.True);
                        Assert.That(tiles[1].flipX, Is.False);
                        Assert.That(tiles[2].flipX, Is.True);
                        float width = tiles[1].bounds.size.x;
                        Assert.That(tiles[1].bounds.size.y, Is.EqualTo(18f).Within(0.01f));
                        Assert.That(tiles[1].transform.localPosition.y, Is.EqualTo(2.9f).Within(0.01f));
                        Assert.That(tiles[1].transform.localPosition.x - tiles[0].transform.localPosition.x, Is.EqualTo(width).Within(0.01f));
                        Assert.That(tiles[2].transform.localPosition.x - tiles[1].transform.localPosition.x, Is.EqualTo(width).Within(0.01f));
                    }
                }
                var backgrounds = (Sprite[])typeof(CombatView).GetField("_stageBackgrounds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
                backgrounds[4] = null;
                var missing = new StageProgress(wallet, upgrades, battle, new System.Random(1), 5);
                property.GetSetMethod(true).Invoke(root, new object[] { missing });
                view.SendMessage("Update");
                Assert.That(obj.transform.Find("[StageBackground]").gameObject.activeSelf, Is.False);
                Assert.That(obj.transform.Find("[Ground]").gameObject.activeSelf, Is.True);
            }
            finally
            {
                property.GetSetMethod(true).Invoke(root, new object[] { original });
                Object.DestroyImmediate(obj);
                Object.DestroyImmediate(camera);
            }
        }
    }
}
