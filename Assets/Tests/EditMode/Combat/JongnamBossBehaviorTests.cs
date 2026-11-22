using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public sealed class JongnamBossBehaviorTests
    {
        private static Battle NewBattle() => new Battle(new StatUpgrades(new Wallet(), 1, 1000));

        [Test]
        public void JongnamBoss_ApproachesAtNormalSpeed_WhileOtherBossStaysStill()
        {
            var battle = NewBattle();
            var jongnam = battle.AddEnemy(EnemyKind.Boss, 6, 3);
            var other = battle.AddEnemy(EnemyKind.Boss, 2, 3);
            battle.Step();
            Assert.That(jongnam.Position, Is.EqualTo(2.98).Within(1e-9));
            Assert.That(other.Position, Is.EqualTo(3).Within(1e-9));
            Assert.That(jongnam.IsJongnamBoss, Is.True);
            Assert.That(other.IsJongnamBoss, Is.False);
        }

        [Test]
        public void ThirdAttack_LaunchesProjectile_DamageOccursOnlyOnImpact()
        {
            var battle = NewBattle();
            var boss = battle.AddEnemy(EnemyKind.Boss, 1, 1);
            for (int i = 0; i < 201; i++) battle.Step();
            Assert.That(boss.AttackCount, Is.EqualTo(3));
            Assert.That(battle.DiscipleHealth, Is.EqualTo(992));
            Assert.That(battle.BossProjectiles.Count, Is.EqualTo(1));
            for (int i = 0; i < 8; i++) battle.Step();
            Assert.That(battle.DiscipleHealth, Is.EqualTo(992));
            battle.Step();
            Assert.That(battle.DiscipleHealth, Is.EqualTo(988));
            Assert.That(battle.BossProjectiles, Is.Empty);
        }

        [Test]
        public void PausedAndReset_ProjectileDoesNotMoveAndIsCleared()
        {
            var battle = NewBattle();
            battle.AddEnemy(EnemyKind.Boss, 1, 1);
            for (int i = 0; i < 201; i++) battle.Step();
            double position = battle.BossProjectiles[0].Position;
            battle.SetPaused(true);
            battle.Step();
            Assert.That(battle.BossProjectiles[0].Position, Is.EqualTo(position));
            battle.Reset();
            Assert.That(battle.BossProjectiles, Is.Empty);
        }
    }
}
