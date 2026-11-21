using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Combat;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class TianmoSpriteTests
    {
        [Test]
        public void Sheets_LoadWithExpectedDimensionsAndFiltering()
        {
            foreach (string actor in new[] { "swordsman", "guard" })
            foreach (var item in new[] { ("idle", 17), ("walk", 25), ("attack", 25), ("down", 17) })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/tianmo_" + actor + "_" + item.Item1);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(64 * item.Item2));
                Assert.That(texture.height, Is.EqualTo(64));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.mipmapCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator StageFourAndNextRound_UseBothSpritesAndPlayDown()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            try
            {
                foreach (int number in new[] { 5, 10 })
                {
                    var wallet = new Wallet();
                    var upgrades = new StatUpgrades(wallet, 10000, 10000);
                    var battle = new Battle(upgrades);
                    var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), number, number);
                    property.GetSetMethod(true).Invoke(root, new object[] { stage });
                    battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    var obj = new GameObject("[TianmoTest]");
                    obj.transform.SetParent(game.transform, false);
                    var camera = new GameObject("Camera", typeof(Camera));
                    camera.transform.SetParent(obj.transform, false);
                    try
                    {
                        obj.AddComponent<CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                        var bodies = new Dictionary<string, SpriteRenderer>();
                        foreach (Transform child in obj.transform)
                        {
                            if (child.name != "[Enemy]") continue;
                            var body = child.Find("Body").GetComponent<SpriteRenderer>();
                            bodies.Add(body.sprite.name.Split('_')[1], body);
                            Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(32f));
                        }
                        Assert.That(bodies.Keys, Is.EquivalentTo(new[] { "swordsman", "guard" }));
                        for (int i = 0; i < 100 && battle.Enemies.Count > 0; i++) battle.Step();
                        Assert.That(battle.Enemies, Is.Empty);
                        battle.SetPaused(true);
                        Assert.That(bodies["swordsman"].sprite.name, Is.EqualTo("tianmo_swordsman_down_00"));
                        Assert.That(bodies["guard"].sprite.name, Is.EqualTo("tianmo_guard_down_00"));
                        yield return new WaitForSeconds(1.3f);
                        Assert.That(bodies["swordsman"].sprite.name, Is.EqualTo("tianmo_swordsman_down_16"));
                        Assert.That(bodies["guard"].sprite.name, Is.EqualTo("tianmo_guard_down_16"));
                    }
                    finally { Object.DestroyImmediate(obj); }
                }
            }
            finally { property.GetSetMethod(true).Invoke(root, new object[] { original }); }
        }

        [UnityTest]
        public IEnumerator LastNormalOpponent_StageFourWaitsForDownBeforeFade()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 5, 5);
            var obj = new GameObject("[TianmoFadeTest]");
            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                yield return new WaitForSeconds(0.5f);
                Assert.That(stage.FadeAlpha, Is.EqualTo(0).Within(0.001));
                yield return new WaitForSeconds(1.0f);
                Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
