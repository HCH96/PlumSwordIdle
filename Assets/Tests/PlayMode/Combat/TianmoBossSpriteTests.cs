using System.Collections;
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
    public sealed class TianmoBossSpriteTests
    {
        [Test]
        public void BossSheets_HaveApprovedFramesAndPointFiltering()
        {
            foreach (var item in new[] { ("idle", 17), ("walk", 25), ("attack", 25), ("down", 17), ("skill", 37) })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/tianmo_boss_" + item.Item1);
                Assert.That(texture, Is.Not.Null, item.Item1);
                Assert.That(texture.width, Is.EqualTo(104 * item.Item2), item.Item1);
                Assert.That(texture.height, Is.EqualTo(104), item.Item1);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), item.Item1);
            }
        }

        [UnityTest]
        public IEnumerator StageFourAndNextRound_UseBossAndThirdAttackSkill()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            var property = typeof(GameRoot).GetProperty("StageProgress");
            var original = root.StageProgress;
            try
            {
                foreach (int number in new[] { 4, 9 })
                {
                    var wallet = new Wallet();
                    var upgrades = new StatUpgrades(wallet, 1, 1000);
                    var battle = new Battle(upgrades);
                    var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), number, number);
                    property.GetSetMethod(true).Invoke(root, new object[] { stage });
                    var boss = battle.AddEnemy(EnemyKind.Boss, number, 0.8);
                    var obj = new GameObject("[TianmoBossTest]");
                    obj.transform.SetParent(game.transform, false);
                    var camera = new GameObject("Camera", typeof(Camera));
                    camera.transform.SetParent(obj.transform, false);
                    try
                    {
                        obj.AddComponent<CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                        var body = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                        Assert.That(body.sprite.name, Does.StartWith("tianmo_boss_idle_"));
                        Assert.That(body.sprite.pivot.x, Is.EqualTo(52f).Within(0.01));
                        Assert.That(body.sprite.pivot.y, Is.EqualTo(18f).Within(0.01));
                        bool windupSeen = false;
                        float deadline = Time.time + 7f;
                        while (boss.AttackCount < 3 && Time.time < deadline)
                        {
                            if (boss.AttackCount == 2 && body.sprite.name.StartsWith("tianmo_boss_skill_")) windupSeen = true;
                            yield return null;
                        }
                        Assert.That(boss.AttackCount, Is.EqualTo(3));
                        Assert.That(windupSeen, Is.True);
                        Assert.That(body.sprite.name, Does.StartWith("tianmo_boss_skill_"));
                        Assert.That(battle.DiscipleHealth, Is.EqualTo(1000 - boss.Attack * 3));
                        battle.SetPaused(true);
                        var paused = body.sprite;
                        yield return new WaitForSeconds(0.15f);
                        Assert.That(body.sprite, Is.SameAs(paused));
                    }
                    finally { Object.DestroyImmediate(obj); }
                }
            }
            finally { property.GetSetMethod(true).Invoke(root, new object[] { original }); }
        }

        [UnityTest]
        public IEnumerator StageFourBoss_WaitsForDownBeforeFade()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), 4, 4);
            var obj = new GameObject("[TianmoBossFadeTest]");
            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                stage.Tick(StageProgress.SubstageIntroSeconds);
                typeof(StageProgress).GetProperty("SubstageNumber").GetSetMethod(true)
                    .Invoke(stage, new object[] { StageLayout.NormalSubstageCount + 1 });
                battle.AddEnemy(EnemyKind.Boss, 4, 0.8);
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
