using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    /// <summary>010_battle_upgrade_reentry: 체력 강화 반영과 알림 안에서 전투를 다시 부를 때.</summary>
    public class BattleUpgradeTests
    {
        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;

        private int _enemyAddedCount;
        private int _enemyDamagedCount;
        private int _discipleDamagedCount;
        private int _enemyDefeatedCount;
        private int _discipleDefeatedCount;
        private int _resetCompletedCount;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet(1000);
            Create(10, 100);
        }

        // Battle 을 만들고 바로 구독해 알림 횟수를 센다.
        private void Create(long attack, long maxHealth)
        {
            _upgrades = new StatUpgrades(_wallet, attack, maxHealth);
            _battle = new Battle(_upgrades);

            _enemyAddedCount = 0;
            _enemyDamagedCount = 0;
            _discipleDamagedCount = 0;
            _enemyDefeatedCount = 0;
            _discipleDefeatedCount = 0;
            _resetCompletedCount = 0;

            _battle.EnemyAdded += _ => _enemyAddedCount++;
            _battle.EnemyDamaged += _ => _enemyDamagedCount++;
            _battle.DiscipleDamaged += () => _discipleDamagedCount++;
            _battle.EnemyDefeated += _ => _enemyDefeatedCount++;
            _battle.DiscipleDefeated += () => _discipleDefeatedCount++;
            _battle.ResetCompleted += () => _resetCompletedCount++;
        }

        private void Steps(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _battle.Step();
            }
        }

        // ---- 체력 강화 반영 ----

        [Test]
        public void HealthUpgradeWhileDamaged_RaisesCurrentHealthByIncrease()
        {
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            Steps(1);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(90));

            Assert.That(_upgrades.TryUpgrade(StatType.Health), Is.True);

            Assert.That(_battle.DiscipleHealth, Is.EqualTo(110));
            Assert.That(_battle.DiscipleMaxHealth, Is.EqualTo(120));
        }

        [Test]
        public void AttackUpgrade_DoesNotChangeCurrentHealth()
        {
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            Steps(1);
            _upgrades.TryUpgrade(StatType.Health);

            Assert.That(_upgrades.TryUpgrade(StatType.Attack), Is.True);

            Assert.That(_battle.DiscipleHealth, Is.EqualTo(110));
            Assert.That(_battle.DiscipleMaxHealth, Is.EqualTo(120));
        }

        [Test]
        public void HealthUpgradeTwice_RaisesCurrentHealthTwice()
        {
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            Steps(1);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(90));

            _upgrades.TryUpgrade(StatType.Health);
            _upgrades.TryUpgrade(StatType.Health);

            Assert.That(_battle.DiscipleHealth, Is.EqualTo(130));
            Assert.That(_battle.DiscipleMaxHealth, Is.EqualTo(140));
        }

        [Test]
        public void HealthUpgradeAfterReset_UsesMaxHealthRememberedAtReset()
        {
            _upgrades.TryUpgrade(StatType.Health);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(120));

            _battle.Reset();
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(120));

            _upgrades.TryUpgrade(StatType.Health);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(140));
        }

        [Test]
        public void HealthUpgradeWhilePaused_RaisesCurrentHealthWithoutNotifications()
        {
            _battle.SetPaused(true);

            _upgrades.TryUpgrade(StatType.Health);

            Assert.That(_battle.DiscipleHealth, Is.EqualTo(120));
            Assert.That(_battle.Paused, Is.True);
            Assert.That(_enemyAddedCount + _enemyDamagedCount + _discipleDamagedCount
                        + _enemyDefeatedCount + _discipleDefeatedCount + _resetCompletedCount, Is.EqualTo(0));
        }

        [Test]
        public void HealthUpgradeWhileDefeated_KeepsHealthZeroAndResetFillsNewMax()
        {
            Create(10, 5);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Steps(1);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(0));
            Assert.That(_battle.IsDiscipleDefeated, Is.True);

            _upgrades.TryUpgrade(StatType.Health);

            Assert.That(_battle.DiscipleHealth, Is.EqualTo(0));
            Assert.That(_battle.DiscipleMaxHealth, Is.EqualTo(25));

            _battle.Reset();
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(25));
        }

        // ---- 알림 안에서 전투를 다시 부를 때 ----

        [Test]
        public void AddEnemyInsideEnemyDefeated_AddsBossWithoutException()
        {
            Create(30, 100);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.EnemyDefeated += _ => _battle.AddEnemy(EnemyKind.Boss, 1, _battle.DisciplePosition + 5.0);

            Assert.DoesNotThrow(() => Steps(1));

            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Enemy boss = _battle.Enemies[0];
            Assert.That(boss.Kind, Is.EqualTo(EnemyKind.Boss));
            Assert.That(boss.Health, Is.EqualTo(100));
            Assert.That(boss.Index, Is.EqualTo(1));
            Assert.That(_enemyAddedCount, Is.EqualTo(2));
        }

        [Test]
        public void EnemyAddedInsideEnemyDefeated_ActsFromNextStep()
        {
            Create(30, 100);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy boss = null;
            _battle.EnemyDefeated += _ =>
            {
                if (boss == null)
                {
                    boss = _battle.AddEnemy(EnemyKind.Boss, 1, _battle.DisciplePosition + 1.0);
                }
            };

            Steps(1);
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.Health, Is.EqualTo(100));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(95));

            Steps(1);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(85));
            Assert.That(boss.Health, Is.EqualTo(100));

            Steps(48);
            Assert.That(boss.Health, Is.EqualTo(100));

            Steps(1);
            Assert.That(boss.Health, Is.EqualTo(70));
        }

        [Test]
        public void ResetInsideEnemyDefeated_StopsRemainingNotifications()
        {
            Create(30, 5);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.EnemyDefeated += _ => _battle.Reset();

            Assert.DoesNotThrow(() => Steps(1));

            Assert.That(_discipleDefeatedCount, Is.EqualTo(0));
            Assert.That(_resetCompletedCount, Is.EqualTo(1));
            Assert.That(_battle.IsDiscipleDefeated, Is.False);
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(5));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(0));
        }

        [Test]
        public void ResetInsideDiscipleDamaged_StopsRemainingAttacksAndKeepsResetState()
        {
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.DiscipleDamaged += () => _battle.Reset();

            Assert.DoesNotThrow(() => Steps(1));

            Assert.That(_discipleDamagedCount, Is.EqualTo(1));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(100));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(0));
            Assert.That(_battle.DiscipleAttackCooldown, Is.EqualTo(0.0));
            Assert.That(_enemyDefeatedCount, Is.EqualTo(0));
            Assert.That(_discipleDefeatedCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetInsideEnemyDamaged_KeepsDiscipleAttackCooldownZero()
        {
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.EnemyDamaged += _ => _battle.Reset();

            Assert.DoesNotThrow(() => Steps(1));

            Assert.That(_battle.DiscipleAttackCooldown, Is.EqualTo(0.0));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(100));
            Assert.That(_battle.Enemies.Count, Is.EqualTo(0));
            Assert.That(_discipleDamagedCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetInsideNotificationDuringTick_StopsTick()
        {
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            bool resetDone = false;
            _battle.DiscipleDamaged += () =>
            {
                if (!resetDone)
                {
                    resetDone = true;
                    _battle.Reset();
                }
            };

            Assert.DoesNotThrow(() => _battle.Tick(0.2));

            // 첫 칸에서 Reset 되어 모아 둔 시간이 0이 되므로 더 진행하지 않는다.
            Assert.That(_battle.DisciplePosition, Is.EqualTo(0.0));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(100));
        }

        [Test]
        public void SetPausedInsideEnemyDamaged_FinishesCurrentStepThenStops()
        {
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy enemy = _battle.Enemies[0];
            _battle.EnemyDamaged += _ => _battle.SetPaused(true);

            Steps(1);

            Assert.That(_battle.Paused, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(20));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(95));
            Assert.That(_discipleDamagedCount, Is.EqualTo(1));

            Steps(5);

            Assert.That(_battle.DiscipleHealth, Is.EqualTo(95));
            Assert.That(enemy.Health, Is.EqualTo(20));
        }

        [Test]
        public void SetPausedInsideNotificationDuringTick_FinishesThatStepOnly()
        {
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy enemy = _battle.Enemies[0];
            _battle.EnemyDamaged += _ => _battle.SetPaused(true);

            Assert.DoesNotThrow(() => _battle.Tick(0.2));

            // 첫 칸은 끝까지 처리되고 (적의 공격, 제자의 다음 대기 설정) 그 뒤 칸은 진행하지 않는다.
            Assert.That(_battle.Paused, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(20));
            Assert.That(_battle.DiscipleHealth, Is.EqualTo(95));
            Assert.That(_battle.DiscipleAttackCooldown, Is.EqualTo(1.0).Within(1e-6));
            Assert.That(enemy.AttackCooldown, Is.EqualTo(2.0).Within(1e-6));
            Assert.That(_discipleDamagedCount, Is.EqualTo(1));
        }

        [Test]
        public void AddEnemyInsideResetCompleted_AddsEnemyWithIndexZero()
        {
            _battle.ResetCompleted += () => _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);

            Assert.DoesNotThrow(() => _battle.Reset());

            Assert.That(_battle.Enemies.Count, Is.EqualTo(1));
            Assert.That(_battle.Enemies[0].Index, Is.EqualTo(0));
        }

        [Test]
        public void AddEnemyTenTimesInsideEnemyAddedChain_DoesNotThrow()
        {
            _battle.EnemyAdded += _ =>
            {
                if (_battle.Enemies.Count < 10)
                {
                    _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);
                }
            };

            Assert.DoesNotThrow(() => _battle.AddEnemy(EnemyKind.Normal, 1, 5.0));

            Assert.That(_battle.Enemies.Count, Is.EqualTo(10));
            Assert.That(_enemyAddedCount, Is.EqualTo(10));
        }
    }
}
