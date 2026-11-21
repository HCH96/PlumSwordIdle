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
    public sealed class NamgungSpriteTests
    {
        [UnityTest]
        public IEnumerator StageThreeAndNextRound_UseBothSpritesAndPlayDown()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            try
            {
                foreach (int number in new[] { 3, 8 })
                {
                    var wallet = new Wallet();
                    var upgrades = new StatUpgrades(wallet, 10000, 10000);
                    var battle = new Battle(upgrades);
                    var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), number, number);
                    property.GetSetMethod(true).Invoke(root, new object[] { stage });
                    battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    battle.AddEnemy(EnemyKind.Boss, number, 0.8);
                    var obj = new GameObject("[NamgungTest]");
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
                            Assert.That(body.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                        }
                        Assert.That(bodies.Keys, Is.EquivalentTo(new[] { "male", "female" }));
                        var bossBody = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                        Assert.That(bossBody.sprite.name, Does.StartWith("namgung_boss_idle"));
                        for (int i = 0; i < 100 && battle.Enemies.Count > 0; i++) battle.Step();
                        Assert.That(battle.Enemies, Is.Empty);
                        battle.SetPaused(true);
                        Assert.That(bodies["male"].sprite.name, Is.EqualTo("namgung_male_down_00"));
                        Assert.That(bodies["female"].sprite.name, Is.EqualTo("namgung_female_down_00"));
                        Assert.That(bossBody.sprite.name, Is.EqualTo("namgung_boss_down_00"));
                        yield return new WaitForSeconds(1.3f);
                        Assert.That(bodies["male"].sprite.name, Is.EqualTo("namgung_male_down_16"));
                        Assert.That(bodies["female"].sprite.name, Is.EqualTo("namgung_female_down_16"));
                        Assert.That(bossBody.sprite.name, Is.EqualTo("namgung_boss_down_16"));
                    }
                    finally { Object.DestroyImmediate(obj); }
                }
            }
            finally { property.GetSetMethod(true).Invoke(root, new object[] { original }); }
        }

        [UnityTest]
        public IEnumerator StageThreeBoss_ThirdAttackPlaysSkillAndDealsOneNormalHit()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 1, 1000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 3, 3);
            var obj = new GameObject("[NamgungBossSkillTest]");
            obj.transform.SetParent(game.transform, false);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.SetParent(obj.transform, false);
            try
            {
                property.GetSetMethod(true).Invoke(root, new object[] { stage });
                var boss = battle.AddEnemy(EnemyKind.Boss, 3, 0.8);
                obj.AddComponent<CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                var body = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                bool windupSeen = false;
                float deadline = Time.time + 7f;
                while (boss.AttackCount < 3 && Time.time < deadline)
                {
                    if (boss.AttackCount == 2 && body.sprite.name.StartsWith("namgung_boss_skill_"))
                        windupSeen = true;
                    yield return null;
                }
                Assert.That(boss.AttackCount, Is.EqualTo(3));
                Assert.That(windupSeen, Is.True);
                Assert.That(body.sprite.name, Does.StartWith("namgung_boss_skill_"));
                Assert.That(body.sprite.pivot.x, Is.EqualTo(209f).Within(0.01));
                Assert.That(body.sprite.pivot.y, Is.EqualTo(15f).Within(0.01));
                Assert.That(battle.DiscipleHealth, Is.EqualTo(1000 - boss.Attack * 3));
                Assert.That(battle.BossProjectiles, Is.Empty);
                battle.SetPaused(true);
                var paused = body.sprite;
                yield return new WaitForSeconds(0.15f);
                Assert.That(body.sprite, Is.SameAs(paused));
            }
            finally
            {
                property.GetSetMethod(true).Invoke(root, new object[] { original });
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void BossSheets_KeepApprovedDimensions()
        {
            foreach (var item in new[] { ("idle", 17), ("walk", 25), ("attack", 25), ("down", 17) })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/namgung_boss_" + item.Item1);
                Assert.That(texture, Is.Not.Null, item.Item1);
                Assert.That(texture.width, Is.EqualTo(100 * item.Item2), item.Item1);
                Assert.That(texture.height, Is.EqualTo(100), item.Item1);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
            var skill = Resources.Load<Texture2D>("Art/Enemies/namgung_boss_skill");
            Assert.That(skill, Is.Not.Null);
            Assert.That(skill.width, Is.EqualTo(246 * 5));
            Assert.That(skill.height, Is.EqualTo(138 * 5));
            Assert.That(skill.filterMode, Is.EqualTo(FilterMode.Point));
        }

        [Test]
        public void Sheets_HaveExpectedDimensionsAndPixelFiltering()
        {
            foreach (string actor in new[] { "male", "female" })
            foreach (var item in new[] { ("idle", 17), ("walk", 25), ("attack", 25), ("down", 17) })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/namgung_" + actor + "_" + item.Item1);
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(76 * item.Item2));
                Assert.That(texture.height, Is.EqualTo(76));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.mipmapCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator LastNormalOpponent_StageThreeWaitsForDownBeforeFade()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 3, 3);
            var obj = new GameObject("[NamgungFadeTest]");
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

        [UnityTest]
        public IEnumerator StageThreeBoss_WaitsForDownBeforeFade()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 3, 3);
            var obj = new GameObject("[NamgungBossFadeTest]");
            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                stage.Tick(StageProgress.SubstageIntroSeconds);
                typeof(StageProgress).GetProperty("SubstageNumber").GetSetMethod(true)
                    .Invoke(stage, new object[] { StageLayout.NormalSubstageCount + 1 });
                battle.AddEnemy(EnemyKind.Boss, 3, 0.8);
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
