using System;
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
using Object = UnityEngine.Object;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class TianmoFinalBossSpriteTests
    {
        private sealed class Setup : IDisposable
        {
            public readonly Battle Battle;
            public readonly Enemy Enemy;
            public readonly SpriteRenderer Body;
            private readonly GameRoot _root;
            private readonly StageProgress _original;
            private readonly PropertyInfo _property;
            private readonly GameObject _view;

            public Setup(int number, EnemyKind kind, int attack = 1)
            {
                var game = GameObject.Find(GameBootstrap.RootName);
                _root = game.GetComponent<GameRoot>();
                _property = typeof(GameRoot).GetProperty("StageProgress");
                _original = _root.StageProgress;
                var wallet = new Wallet();
                var upgrades = new StatUpgrades(wallet, attack, 10000);
                Battle = new Battle(upgrades);
                var stage = new StageProgress(wallet, upgrades, Battle, new System.Random(1), number, number);
                _property.GetSetMethod(true).Invoke(_root, new object[] { stage });
                Enemy = Battle.AddEnemy(kind, number, 0.8);
                _view = new GameObject("[FinalBossTest]");
                _view.transform.SetParent(game.transform, false);
                var camera = new GameObject("Camera", typeof(Camera));
                camera.transform.SetParent(_view.transform, false);
                _view.AddComponent<CombatView>().Initialize(Battle, camera.GetComponent<Camera>());
                Body = _view.transform.Find(kind == EnemyKind.Boss ? "[Boss]/Body" : "[Enemy]/Body").GetComponent<SpriteRenderer>();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_view);
                _property.GetSetMethod(true).Invoke(_root, new object[] { _original });
            }
        }

        [Test]
        public void Sheets_HaveExpectedDimensionsAndImportSettings()
        {
            foreach (string name in new[] { "idle", "walk", "attack", "down", "skill" })
            {
                var texture = Resources.Load<Texture2D>("Art/Enemies/tianmo_final_boss_" + name);
                Assert.That(texture, Is.Not.Null, name);
                Assert.That(texture.width, Is.EqualTo(name == "skill" ? 1300 : 2600), name);
                Assert.That(texture.height, Is.EqualTo(name == "skill" ? 400 : 104), name);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), name);
                Assert.That(texture.mipmapCount, Is.EqualTo(1), name);
            }
        }

        [UnityTest]
        public IEnumerator StageFourNormals_UseSwordsmanThenGuard()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            foreach (int number in new[] { 4, 9 })
            {
                using (var setup = new Setup(number, EnemyKind.Normal))
                {
                    Assert.That(setup.Enemy.Index, Is.EqualTo(0));
                    Assert.That(setup.Body.sprite.name, Does.StartWith("tianmo_swordsman_"));
                    var next = setup.Battle.AddEnemy(EnemyKind.Normal, number, 0.8);
                    Assert.That(next.Index, Is.EqualTo(1));
                    var enemies = setup.Body.transform.parent.parent.GetComponentsInChildren<SpriteRenderer>();
                    bool guardSeen = false;
                    foreach (var renderer in enemies)
                        if (renderer.name == "Body" && renderer.sprite != null && renderer.sprite.name.StartsWith("tianmo_guard_")) guardSeen = true;
                    Assert.That(guardSeen, Is.True);
                }
            }
        }

        [UnityTest]
        public IEnumerator FinalStage_UsesBossSpritesAndNormalFallback()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            using (var normal = new Setup(5, EnemyKind.Normal))
                Assert.That(normal.Body.sprite.name, Does.Not.StartWith("tianmo_"));
            foreach (int number in new[] { 5, 10 })
            {
                using (var boss = new Setup(number, EnemyKind.Boss))
                {
                    Assert.That(boss.Body.sprite.name, Does.StartWith("tianmo_final_boss_idle_00"));
                    Assert.That(boss.Body.sprite.pixelsPerUnit, Is.EqualTo(32f));
                    Assert.That(Mathf.Abs(boss.Body.transform.localScale.x), Is.EqualTo(1.5f));
                }
            }
        }

        [UnityTest]
        public IEnumerator ThirdAttack_UsesSkillAndDealsDamageOnce()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            using (var setup = new Setup(5, EnemyKind.Boss))
            {
                int hits = 0;
                setup.Battle.DiscipleDamaged += () => hits++;
                float deadline = Time.time + 7f;
                while (setup.Enemy.AttackCount < 2 && Time.time < deadline) yield return null;
                Assert.That(setup.Enemy.AttackCount, Is.EqualTo(2));
                long before = setup.Battle.DiscipleHealth;
                int hitsBefore = hits;
                bool skillSeen = false;
                while (setup.Enemy.AttackCount < 3 && Time.time < deadline)
                {
                    skillSeen |= setup.Body.sprite.name.StartsWith("tianmo_final_boss_skill_");
                    yield return null;
                }
                Assert.That(setup.Enemy.AttackCount, Is.EqualTo(3));
                Assert.That(skillSeen, Is.True);
                Assert.That(setup.Body.sprite.name, Does.StartWith("tianmo_final_boss_skill_"));
                Assert.That(setup.Battle.DiscipleHealth, Is.EqualTo(before - setup.Enemy.Attack));
                Assert.That(hits - hitsBefore, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator DefeatedBoss_PlaysAllDownFramesWhilePaused()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            using (var setup = new Setup(5, EnemyKind.Boss, 10000))
            {
                for (int i = 0; i < 100 && setup.Battle.Enemies.Count > 0; i++) setup.Battle.Step();
                Assert.That(setup.Battle.Enemies, Is.Empty);
                setup.Battle.SetPaused(true);
                Assert.That(setup.Body.sprite.name, Is.EqualTo("tianmo_final_boss_down_00"));
                yield return new WaitForSeconds(1.9f);
                Assert.That(setup.Body.sprite.name, Is.EqualTo("tianmo_final_boss_down_24"));
            }
        }

        [UnityTest]
        public IEnumerator MissingSheets_UseAttackOrBossFallback()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            var type = Type.GetType("PlumSword.Game.Combat.JongnamSprites, PlumSword.Game");
            Assert.That(type, Is.Not.Null);
            var skill = type.GetField("TianmoFinalBossSkillPath", BindingFlags.NonPublic | BindingFlags.Static);
            var prefix = type.GetField("TianmoFinalBossPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(skill, Is.Not.Null);
            Assert.That(prefix, Is.Not.Null);
            var originalSkill = skill.GetValue(null);
            var originalPrefix = prefix.GetValue(null);
            try
            {
                skill.SetValue(null, "Art/Enemies/missing_final_skill");
                using (var setup = new Setup(5, EnemyKind.Boss))
                {
                    float deadline = Time.time + 7f;
                    while (setup.Enemy.AttackCount < 3 && Time.time < deadline) yield return null;
                    Assert.That(setup.Enemy.AttackCount, Is.EqualTo(3));
                    Assert.That(setup.Body.sprite.name, Does.StartWith("tianmo_final_boss_attack_"));
                }
                prefix.SetValue(null, "Art/Enemies/missing_final_");
                using (var setup = new Setup(5, EnemyKind.Boss))
                    Assert.That(setup.Body.sprite.name, Does.Not.StartWith("tianmo_"));
            }
            finally
            {
                skill.SetValue(null, originalSkill);
                prefix.SetValue(null, originalPrefix);
            }
        }
    }
}
