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
        public IEnumerator SkillUsed_ShowsSlashAtAttackPosition_FadesAndResetHidesIt()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100), null, true);
            var viewObject = new GameObject("[TestCombatView]");
            var cameraObject = new GameObject("[TestCamera]");
            try
            {
                var view = viewObject.AddComponent<CombatView>();
                view.Initialize(battle, cameraObject.AddComponent<Camera>());
                var slash = viewObject.transform.Find("[SkillSlash]");
                var renderer = slash.GetComponent<SpriteRenderer>();
                Assert.That(renderer.enabled, Is.False);
                Assert.That(slash.localScale, Is.EqualTo(new Vector3(2f, 0.4f, 1f)));
                Assert.That(renderer.sortingOrder, Is.EqualTo(20000));
                battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
                battle.Step();
                Assert.That(renderer.enabled, Is.True);
                Assert.That(slash.localPosition.x, Is.EqualTo((float)battle.DisciplePosition - 1.25f).Within(1e-5f));
                Assert.That(slash.localPosition.y, Is.EqualTo(-0.75f).Within(1e-5f));
                Assert.That(renderer.color.a, Is.EqualTo(0.6f).Within(1e-5f));
                var position = slash.localPosition;
                yield return new WaitForSeconds(0.1f);
                Assert.That(slash.localPosition, Is.EqualTo(position));
                Assert.That(renderer.color.a, Is.LessThan(0.6f));
                yield return new WaitForSeconds(0.3f);
                Assert.That(renderer.enabled, Is.False);
                battle.Reset();
                battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
                battle.Step();
                Assert.That(renderer.enabled, Is.True);
                battle.Reset();
                Assert.That(renderer.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
