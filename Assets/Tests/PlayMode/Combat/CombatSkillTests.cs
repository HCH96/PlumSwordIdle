using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class CombatSkillTests
    {
        [UnityTest]
        public IEnumerator HealthBars_AnimateDamageAndResetImmediately()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100));
            var viewObject = new GameObject("[TestCombatView]");
            var cameraObject = new GameObject("[TestCamera]");
            try
            {
                viewObject.AddComponent<CombatView>().Initialize(battle, cameraObject.AddComponent<Camera>());
                battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
                var discipleFill = viewObject.transform.Find("[Disciple]/HealthBack/HealthFill");
                var enemyFill = viewObject.transform.Find("[Enemy]/HealthBack/HealthFill");
                float fullWidth = discipleFill.localScale.x;
                Assert.That(discipleFill.localScale.y, Is.EqualTo(0.125f));
                Assert.That(enemyFill.localScale.x, Is.EqualTo(fullWidth).Within(1e-5f));
                battle.Step();
                battle.SetPaused(true);
                yield return new WaitForSeconds(0.1f);
                Assert.That(discipleFill.localScale.x, Is.LessThan(fullWidth));
                Assert.That(discipleFill.localScale.x, Is.GreaterThan(fullWidth * 0.95f));
                Assert.That(enemyFill.localScale.x, Is.LessThan(fullWidth));
                Assert.That(enemyFill.localScale.x, Is.GreaterThan(fullWidth * 2f / 3f));
                yield return new WaitForSeconds(0.4f);
                Assert.That(discipleFill.localScale.x, Is.EqualTo(fullWidth * 0.95f).Within(1e-4f));
                Assert.That(enemyFill.localScale.x, Is.EqualTo(fullWidth * 2f / 3f).Within(1e-4f));
                battle.Reset();
                Assert.That(discipleFill.localScale.x, Is.EqualTo(fullWidth).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator DamageToBothSides_ShowsActualHealthLost_AndResetClearsNumbers()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100));
            var viewObject = new GameObject("[TestCombatView]");
            var cameraObject = new GameObject("[TestCamera]");
            try
            {
                viewObject.AddComponent<CombatView>().Initialize(battle, cameraObject.AddComponent<Camera>());
                battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
                battle.Step();
                var numbers = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in viewObject.transform)
                    if (child.name == "[DamageNumber]") numbers.Add(child);
                Assert.That(numbers.Count, Is.EqualTo(2));
                Assert.That(numbers[0].GetChild(0).name, Is.EqualTo("1"));
                Assert.That(numbers[0].GetChild(1).name, Is.EqualTo("0"));
                Assert.That(numbers[1].GetChild(0).name, Is.EqualTo("5"));
                battle.Reset();
                yield return null;
                Assert.That(viewObject.transform.Find("[DamageNumber]"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator FirstScene_SkillEnabledAndSlashInitiallyHidden()
        {
            yield return SceneManager.LoadSceneAsync(0);
            var game = GameObject.Find(GameBootstrap.RootName);
            Assert.That(game, Is.Not.Null);
            var root = game.GetComponent<GameRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Battle.SkillEnabled, Is.True);
            var slash = game.transform.Find("[CombatView]/[SkillSlash]");
            Assert.That(slash, Is.Not.Null);
            var renderer = slash.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(!renderer.enabled || !slash.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator SkillUsed_PlaysApprovedAnimation_PausesAndResetStopsIt()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100), null, true);
            var viewObject = new GameObject("[TestCombatView]");
            var cameraObject = new GameObject("[TestCamera]");
            try
            {
                viewObject.AddComponent<CombatView>().Initialize(battle, cameraObject.AddComponent<Camera>());
                var slash = viewObject.transform.Find("[SkillSlash]").GetComponent<SpriteRenderer>();
                var body = viewObject.transform.Find("[Disciple]/Body").GetComponent<SpriteRenderer>();
                Assert.That(Resources.Load<Texture2D>("Art/Characters/disciple_skill"), Is.Not.Null);
                battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
                battle.Step();
                Assert.That(slash.enabled, Is.False);
                Assert.That(body.sprite.name, Is.EqualTo("disciple_skill_00"));
                yield return new WaitForSeconds(0.2f);
                Assert.That(body.sprite.name, Does.StartWith("disciple_skill_"));
                Assert.That(body.sprite.name, Is.Not.EqualTo("disciple_skill_00"));
                battle.SetPaused(true);
                yield return null;
                var pausedFrame = body.sprite;
                yield return new WaitForSeconds(0.2f);
                Assert.That(body.sprite, Is.EqualTo(pausedFrame));
                battle.Reset();
                Assert.That(body.sprite.name, Does.StartWith("disciple_idle_"));
                Assert.That(slash.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

    }
}
