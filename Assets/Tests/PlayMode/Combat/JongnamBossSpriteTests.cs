using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class JongnamBossSpriteTests
    {
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
        }

        [Test]
        public void BossSheets_KeepOriginalSizeAndPixelFiltering()
        {
            foreach (var item in new[] { ("idle", 96, 100), ("walk", 96, 104), ("attack", 100, 100), ("skill", 100, 100) })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/jongnam_boss_" + item.Item1);
                Assert.That(texture, Is.Not.Null, item.Item1);
                Assert.That(texture.width, Is.EqualTo(item.Item2 * 24), item.Item1);
                Assert.That(texture.height, Is.EqualTo(item.Item3), item.Item1);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(texture.mipmapCount, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator StageOne_BossAnimatesNormalAndSkillWithoutChangingDamage()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var battle = new Battle(new StatUpgrades(new Wallet(), 1, 1000));
            var enemy = battle.AddEnemy(EnemyKind.Boss, 1, 0.8);
            var obj = new GameObject("[BossTest]");
            obj.transform.SetParent(game.transform, false);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<PlumSword.Game.Combat.CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                var body = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                Assert.That(body.sprite.name, Does.StartWith("jongnam_boss_idle"));
                Assert.That(body.transform.localScale, Is.EqualTo(new Vector3(1.5f, 1.5f, 1f)));
                Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(32));
                bool attack = false, skill = false;
                float until = Time.time + 5.7f;
                while (Time.time < until)
                {
                    attack |= body.sprite.name.StartsWith("jongnam_boss_attack");
                    skill |= body.sprite.name.StartsWith("jongnam_boss_skill");
                    yield return null;
                }
                Assert.That(attack, Is.True, "일반 공격이 보여야 한다");
                Assert.That(skill, Is.True, "세 번째 공격에 스킬 동작이 보여야 한다");
                Assert.That(battle.DiscipleHealth, Is.EqualTo(970), "스킬 모션도 기존 공격 피해를 유지한다");
                Assert.That(enemy.Position, Is.EqualTo(0.8).Within(1e-9));

                battle.SetPaused(true);
                var paused = body.sprite;
                yield return new WaitForSeconds(0.3f);
                Assert.That(body.sprite, Is.SameAs(paused));
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [UnityTest]
        public IEnumerator JongnamBoss_ApproachesAndShowsProjectileUntilImpact()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var battle = new Battle(new StatUpgrades(new Wallet(), 1, 1000));
            var boss = battle.AddEnemy(EnemyKind.Boss, 1, 2.8);
            var obj = new GameObject("[BossMovementTest]");
            obj.transform.SetParent(game.transform, false);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<PlumSword.Game.Combat.CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                yield return new WaitForSeconds(0.1f);
                var body = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                Assert.That(boss.Position, Is.LessThan(2.8));
                Assert.That(body.sprite.name, Does.StartWith("jongnam_boss_walk"));
                for (int i = 0; i < 400 && battle.BossProjectiles.Count == 0; i++) battle.Step();
                Assert.That(battle.BossProjectiles.Count, Is.EqualTo(1));
                battle.SetPaused(true);
                yield return null;
                var projectile = obj.transform.Find("[JongnamSnowProjectile]");
                Assert.That(projectile, Is.Not.Null);
                var firstX = projectile.localPosition.x;
                battle.SetPaused(false);
                yield return new WaitForSeconds(0.06f);
                Assert.That(projectile.localPosition.x, Is.LessThan(firstX));
                yield return new WaitForSeconds(0.3f);
                Assert.That(battle.BossProjectiles, Is.Empty);
                Assert.That(obj.transform.Find("[JongnamSnowProjectile]"), Is.Null);
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
