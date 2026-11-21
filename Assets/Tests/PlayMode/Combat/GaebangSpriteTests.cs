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
    public sealed class GaebangSpriteTests
    {
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StageTwoAndNextRound_UseBothSpritesAndPlayDown()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            try
            {
                foreach (int number in new[] { 2, 7 })
                {
                    var wallet = new Wallet();
                    var upgrades = new StatUpgrades(wallet, 10000, 10000);
                    var battle = new Battle(upgrades);
                    var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), number, number);
                    property.GetSetMethod(true).Invoke(root, new object[] { stage });
                    battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    battle.AddEnemy(EnemyKind.Boss, number, 0.8);
                    var obj = new GameObject("[GaebangTest]");
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
                        Assert.That(bodies.Keys, Is.EquivalentTo(new[] { "staff", "unarmed" }));
                        var bossBody = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                        Assert.That(bossBody.sprite.name, Does.StartWith("gaebang_boss_idle"));
                        for (int i = 0; i < 100 && battle.Enemies.Count > 0; i++) battle.Step();
                        Assert.That(battle.Enemies, Is.Empty);
                        battle.SetPaused(true);
                        Assert.That(bodies["staff"].sprite.name, Is.EqualTo("gaebang_staff_down_00"));
                        Assert.That(bodies["unarmed"].sprite.name, Is.EqualTo("gaebang_unarmed_down_00"));
                        Assert.That(bossBody.sprite.name, Is.EqualTo("gaebang_boss_down_00"));
                        yield return new WaitForSeconds(1.22f);
                        Assert.That(bodies["staff"].sprite.name, Is.EqualTo("gaebang_staff_down_15"));
                        Assert.That(bodies["unarmed"].sprite.name, Is.EqualTo("gaebang_unarmed_down_15"));
                        Assert.That(bossBody.sprite.name, Does.StartWith("gaebang_boss_down_"));
                        yield return new WaitForSeconds(0.7f);
                        Assert.That(bossBody.sprite.name, Is.EqualTo("gaebang_boss_down_24"));
                    }
                    finally { Object.DestroyImmediate(obj); }
                }
            }
            finally { property.GetSetMethod(true).Invoke(root, new object[] { original }); }
        }

        [Test]
        public void BossSheets_KeepApprovedFramesAndPixelFiltering()
        {
            foreach (var item in new[] { ("idle", 9), ("walk", 25), ("attack", 25), ("down", 25), ("skill", 33) })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/gaebang_boss_" + item.Item1);
                Assert.That(texture, Is.Not.Null, item.Item1);
                Assert.That(texture.width, Is.EqualTo(96 * item.Item2), item.Item1);
                Assert.That(texture.height, Is.EqualTo(96), item.Item1);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.mipmapCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator StageTwoBoss_ThirdAttackUsesSkillAndDealsNormalMeleeDamage()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 1, 1000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 2, 2);
            var obj = new GameObject("[GaebangBossSkillTest]");
            obj.transform.SetParent(game.transform, false);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.SetParent(obj.transform, false);
            try
            {
                property.GetSetMethod(true).Invoke(root, new object[] { stage });
                var boss = battle.AddEnemy(EnemyKind.Boss, 2, 0.8);
                obj.AddComponent<CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                var body = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                bool skillBeforeHit = false;
                float deadline = Time.time + 7f;
                while (boss.AttackCount < 3 && Time.time < deadline)
                {
                    if (boss.AttackCount == 2 && body.sprite.name.StartsWith("gaebang_boss_skill_"))
                        skillBeforeHit = true;
                    yield return null;
                }
                Assert.That(boss.AttackCount, Is.EqualTo(3));
                Assert.That(skillBeforeHit, Is.True, "기술 준비 동작이 세 번째 피해보다 먼저 보여야 한다");
                Assert.That(body.sprite.name, Does.StartWith("gaebang_boss_skill_"));
                int frame = int.Parse(body.sprite.name.Substring(body.sprite.name.LastIndexOf('_') + 1));
                Assert.That(frame, Is.GreaterThanOrEqualTo(19));
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

        [UnityTest]
        public IEnumerator LastNormalOpponent_StageTwoWaitsForDownBeforeFade()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 2, 2);
            var obj = new GameObject("[GaebangFadeTest]");
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
                yield return new WaitForSeconds(0.9f);
                Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [UnityTest]
        public IEnumerator StageTwoBoss_WaitsForApprovedDownBeforeFade()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 2, 2);
            var obj = new GameObject("[GaebangBossFadeTest]");
            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                if (stage.Phase == StagePhase.Intro) stage.Tick(StageProgress.IntroSeconds);
                stage.Tick(StageProgress.SubstageIntroSeconds);
                typeof(StageProgress).GetProperty("SubstageNumber").GetSetMethod(true)
                    .Invoke(stage, new object[] { StageLayout.NormalSubstageCount + 1 });
                battle.AddEnemy(EnemyKind.Boss, 2, 0.8);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                yield return new WaitForSeconds(1.3f);
                Assert.That(stage.FadeAlpha, Is.EqualTo(0).Within(0.001));
                yield return new WaitForSeconds(0.8f);
                Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
