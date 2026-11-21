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
                foreach (int stageNumber in new[] { 1, 2, 3, 4, 7, 8, 1 })
                {
                    var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), stageNumber);
                    property.GetSetMethod(true).Invoke(root, new object[] { stage });
                    view.SendMessage("Update");
                    var tile = obj.transform.Find("[StageBackground]");
                    bool hasArt = stageNumber != 4;
                    Assert.That(tile.gameObject.activeSelf, Is.EqualTo(hasArt));
                    Assert.That(obj.transform.Find("[Ground]").gameObject.activeSelf, Is.EqualTo(!hasArt));
                    if (hasArt)
                    {
                        string expected = stageNumber == 1 ? "stage1_preview"
                            : stageNumber == 2 || stageNumber == 7 ? "stage2_gaebang"
                            : "stage3_namgung";
                        Assert.That(tile.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo(expected));
                    }
                }
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
