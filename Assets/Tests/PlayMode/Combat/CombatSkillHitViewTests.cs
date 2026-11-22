using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Combat;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class CombatSkillHitViewTests
    {
        [Test]
        public void SkillFrames_AllUseFootPivot()
        {
            var sprites = DiscipleSprites.TryLoad();
            Assert.That(sprites, Is.Not.Null);
            try
            {
                Assert.That(sprites.HasSkill, Is.True);
                for (int i = 0; i < 32; i++)
                {
                    var sprite = sprites.GetSkill(i);
                    Assert.That(sprite.pivot.x, Is.EqualTo(58f).Within(0.001f), $"frame {i}");
                    Assert.That(sprite.pivot.y, Is.EqualTo(18f).Within(0.001f), $"frame {i}");
                }
            }
            finally { sprites.Dispose(); }
        }

        [Test]
        public void SkillHit_EnemyDamageUsesPlumDigits()
        {
            Assert.That(GetEnemyDamageTexture(true, EnemyKind.Boss, 2, 1.0), Is.EqualTo("damage_numbers_plum"));
        }

        [Test]
        public void BasicHit_EnemyDamageUsesIvoryDigits()
        {
            Assert.That(GetEnemyDamageTexture(false, EnemyKind.Normal, 1, 0.8), Is.EqualTo("damage_numbers"));
        }

        private static string GetEnemyDamageTexture(bool skill, EnemyKind kind, int level, double position)
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100), null, skill);
            var view = new GameObject("[TestCombatView]");
            var camera = new GameObject("[TestCamera]");
            try
            {
                view.AddComponent<CombatView>().Initialize(battle, camera.AddComponent<Camera>());
                battle.AddEnemy(kind, level, position);
                battle.Step();
                var number = view.transform.Find("[DamageNumber]");
                Assert.That(number, Is.Not.Null);
                Assert.That(number.childCount, Is.GreaterThan(0));
                return number.GetChild(0).GetComponent<SpriteRenderer>().sprite.texture.name;
            }
            finally { Object.DestroyImmediate(view); Object.DestroyImmediate(camera); }
        }

        [Test]
        public void SkillAnimation_StartsAtFrameTwo()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100), null, true);
            var view = new GameObject("[TestCombatView]");
            var camera = new GameObject("[TestCamera]");
            try
            {
                view.AddComponent<CombatView>().Initialize(battle, camera.AddComponent<Camera>());
                battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
                battle.Step();
                var body = view.transform.Find("[Disciple]/Body").GetComponent<SpriteRenderer>();
                Assert.That(body.sprite.name, Is.EqualTo("disciple_skill_02"));
            }
            finally { Object.DestroyImmediate(view); Object.DestroyImmediate(camera); }
        }

        [UnityTest]
        public IEnumerator SkillAnimation_EndsAfterOneAndQuarterSeconds()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(), 10, 100), null, true);
            var view = new GameObject("[TestCombatView]");
            var camera = new GameObject("[TestCamera]");
            try
            {
                view.AddComponent<CombatView>().Initialize(battle, camera.AddComponent<Camera>());
                battle.AddEnemy(EnemyKind.Boss, 2, 1.0);
                var body = view.transform.Find("[Disciple]/Body").GetComponent<SpriteRenderer>();
                battle.Step();
                Assert.That(body.sprite.name, Is.EqualTo("disciple_skill_02"));
                yield return new WaitForSeconds(1.4f);
                Assert.That(body.sprite.name, Does.Not.StartWith("disciple_skill_"));
            }
            finally { Object.DestroyImmediate(view); Object.DestroyImmediate(camera); }
        }
    }
}
