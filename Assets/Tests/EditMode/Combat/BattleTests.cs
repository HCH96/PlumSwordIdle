using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public class BattleTests
    {
        private const double Tolerance = 1e-6;

        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;

        private List<Enemy> _added;
        private int _enemyDefeatedCount;
        private int _resetCompletedCount;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet(1000);
            _upgrades = new StatUpgrades(_wallet, 10, 100, 1.0);
            _battle = new Battle(_upgrades);

            _added = new List<Enemy>();
            _enemyDefeatedCount = 0;
            _resetCompletedCount = 0;
            _battle.EnemyAdded += enemy => _added.Add(enemy);
            _battle.EnemyDefeated += _ => _enemyDefeatedCount++;
            _battle.ResetCompleted += () => _resetCompletedCount++;
        }

        private void Steps(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _battle.Step();
            }
        }

        // ---- 시작 상태, 상수, 생성자 ----

        [Test]
        public void New_InitialState_IsAtStart()
        {
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(100L, _battle.DiscipleHealth);
            Assert.AreEqual(100L, _battle.DiscipleMaxHealth);
            Assert.AreEqual(0.0, _battle.DiscipleAttackCooldown, Tolerance);
            Assert.IsFalse(_battle.IsDiscipleDefeated);
            Assert.IsFalse(_battle.Paused);
            Assert.AreEqual(0, _battle.Enemies.Count);
        }

        [Test]
        public void New_NoNotifications_AreSent()
        {
            Assert.AreEqual(0, _added.Count);
            Assert.AreEqual(0, _resetCompletedCount);
        }

        [Test]
        public void Constants_Values_MatchSpec()
        {
            Assert.AreEqual(0.02, Battle.StepSeconds, 1e-12);
            Assert.AreEqual(0.25, Battle.MaxTickSeconds, 1e-12);
            Assert.AreEqual(1e-9, Battle.Epsilon, 1e-15);
            Assert.AreEqual(10L, Battle.DiscipleBaseAttack);
            Assert.AreEqual(100L, Battle.DiscipleBaseMaxHealth);
            Assert.AreEqual(1.0, Battle.DiscipleBaseAttackInterval, 1e-12);
            Assert.AreEqual(2.0, Battle.DiscipleMoveSpeed, 1e-12);
            Assert.AreEqual(1.0, Battle.DiscipleAttackRange, 1e-12);
        }

        [Test]
        public void New_NullUpgrades_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Battle(null));
        }

        [Test]
        public void DiscipleMaxHealth_AfterHealthUpgrade_ReadsUpgradesDirectly()
        {
            Assert.IsTrue(_upgrades.TryUpgrade(StatType.Health));
            Assert.AreEqual(_upgrades.MaxHealth, _battle.DiscipleMaxHealth);
        }

        // ---- 이동 (적 없음) ----

        [Test]
        public void Step_NoEnemies_DiscipleAdvances()
        {
            Steps(1);
            Assert.AreEqual(0.04, _battle.DisciplePosition, Tolerance);

            Steps(49);
            Assert.AreEqual(2.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(0.0, _battle.DiscipleAttackCooldown, Tolerance);
        }

        // ---- 적 추가 ----

        [Test]
        public void AddEnemy_NormalThenBoss_CreatesEnemiesWithStats()
        {
            Enemy normal = _battle.AddEnemy(EnemyKind.Normal, 2, 5.0);

            Assert.AreEqual(EnemyKind.Normal, normal.Kind);
            Assert.AreEqual(0, normal.Index);
            Assert.AreEqual(5.0, normal.Position, Tolerance);
            Assert.AreEqual(40L, normal.MaxHealth);
            Assert.AreEqual(40L, normal.Health);
            Assert.AreEqual(7L, normal.Attack);
            Assert.AreEqual(2.0, normal.AttackInterval, Tolerance);
            Assert.AreEqual(0.0, normal.AttackCooldown, Tolerance);
            Assert.IsFalse(normal.IsDead);
            Assert.AreEqual(1, _added.Count);
            Assert.AreSame(normal, _added[0]);
            Assert.AreEqual(1, _battle.Enemies.Count);
            Assert.AreSame(normal, _battle.Enemies[0]);

            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 1, 8.0);

            Assert.AreEqual(1, boss.Index);
            Assert.AreEqual(100L, boss.MaxHealth);
            Assert.AreEqual(10L, boss.Attack);
            Assert.AreSame(boss, _battle.Enemies[1]);
            Assert.AreEqual(2, _added.Count);
        }

        [Test]
        public void AddEnemy_InvalidArguments_ThrowsAndChangesNothing()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy(EnemyKind.Normal, 0, 5.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy((EnemyKind)99, 1, 5.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy(EnemyKind.Normal, 1, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy(EnemyKind.Normal, 1, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy(EnemyKind.Normal, 1, -0.1));

            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.AreEqual(0, _added.Count);

            Enemy enemy = null;
            Assert.DoesNotThrow(() => enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 0.0));
            Assert.AreEqual(0, enemy.Index);
        }

        [Test]
        public void AddEnemy_NegativeInfinityPosition_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy(EnemyKind.Normal, 1, double.NegativeInfinity));
            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.AreEqual(0, _added.Count);
        }

        [Test]
        public void AddEnemy_BehindAdvancedDisciple_ThrowsButSamePositionIsAllowed()
        {
            Steps(50);
            Assert.AreEqual(2.0, _battle.DisciplePosition, Tolerance);

            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.AddEnemy(EnemyKind.Normal, 1, 1.9));
            Assert.DoesNotThrow(() => _battle.AddEnemy(EnemyKind.Normal, 1, 2.0));
        }

        // ---- 이동 (적 있음) ----

        [Test]
        public void Step_NormalOutOfReactRange_EnemyWaits()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 10.0);
            Steps(1);

            Assert.AreEqual(10.0, enemy.Position, Tolerance);
            Assert.AreEqual(0.04, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Step_DiscipleWithinReactRange_NormalApproaches()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);

            Steps(49);
            Assert.AreEqual(5.0, enemy.Position, Tolerance);
            Assert.AreEqual(1.96, _battle.DisciplePosition, Tolerance);

            Steps(1);
            Assert.AreEqual(4.98, enemy.Position, Tolerance);
            Assert.AreEqual(2.0, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Step_Approaching_DiscipleStopsAtOneAndNormalAtPointEight()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);

            Steps(82);
            Assert.AreEqual(3.28, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(4.34, enemy.Position, Tolerance);

            Steps(1);
            Assert.AreEqual(3.32, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(4.32, enemy.Position, Tolerance);

            Steps(10);
            Assert.AreEqual(4.12, enemy.Position, Tolerance);

            Steps(7);
            Assert.AreEqual(3.32, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(4.12, enemy.Position, Tolerance);
        }

        [Test]
        public void Step_Boss_DoesNotMove()
        {
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 1, 5.0);

            Steps(100);
            Assert.AreEqual(4.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(5.0, boss.Position, Tolerance);

            Steps(50);
            Assert.AreEqual(4.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(5.0, boss.Position, Tolerance);
        }

        [Test]
        public void Step_TwoBosses_DiscipleStopsBeforeNearest()
        {
            _battle.AddEnemy(EnemyKind.Boss, 1, 3.0);
            _battle.AddEnemy(EnemyKind.Boss, 1, 2.0);

            Steps(100);
            Assert.AreEqual(1.0, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Step_WithEnemies_HealthDoesNotChange()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);

            Steps(200);
            Assert.AreEqual(100L, _battle.DiscipleHealth);
            Assert.AreEqual(30L, enemy.Health);
            Assert.IsFalse(enemy.IsDead);
            Assert.IsFalse(_battle.IsDiscipleDefeated);
            Assert.AreEqual(0, _enemyDefeatedCount);
        }

        // ---- 초기화 ----

        [Test]
        public void Reset_AfterSteps_RestoresStartState()
        {
            _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);
            Steps(10);
            Assert.AreEqual(0.4, _battle.DisciplePosition, Tolerance);

            _battle.Reset();

            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(100L, _battle.DiscipleHealth);
            Assert.AreEqual(0.0, _battle.DiscipleAttackCooldown, Tolerance);
            Assert.IsFalse(_battle.IsDiscipleDefeated);
            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.AreEqual(1, _resetCompletedCount);
            Assert.AreEqual(0, _enemyDefeatedCount);

            Enemy next = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);
            Assert.AreEqual(0, next.Index);

            _battle.Reset();
            Assert.AreEqual(2, _resetCompletedCount);
        }

        [Test]
        public void Reset_AfterPartialTick_ClearsAccumulatedTime()
        {
            _battle.Tick(0.01);
            _battle.Reset();
            _battle.Tick(0.01);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);

            _battle.Tick(0.01);
            Assert.AreEqual(0.04, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Reset_NoSubscribers_DoesNotThrow()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 10, 100, 1.0));
            Assert.DoesNotThrow(() =>
            {
                battle.AddEnemy(EnemyKind.Normal, 1, 5.0);
                battle.Tick(0.1);
                battle.Reset();
            });
        }

        // ---- 멈춤 ----

        [Test]
        public void SetPaused_True_StepAndTickDoNothingAndTimeIsDiscarded()
        {
            _battle.SetPaused(true);
            Assert.IsTrue(_battle.Paused);

            Steps(10);
            _battle.Tick(0.1);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);

            _battle.SetPaused(false);
            Assert.IsFalse(_battle.Paused);
            Steps(1);
            Assert.AreEqual(0.04, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void SetPaused_WhilePaused_AddEnemyAndResetStillWork()
        {
            _battle.SetPaused(true);

            _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);
            Assert.AreEqual(1, _battle.Enemies.Count);
            Assert.AreEqual(1, _added.Count);

            _battle.Reset();
            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.IsTrue(_battle.Paused);
            Assert.AreEqual(1, _resetCompletedCount);

            Assert.DoesNotThrow(() => _battle.SetPaused(true));
            Assert.IsTrue(_battle.Paused);
        }

        // ---- 시간 ----

        [Test]
        public void Tick_PointOne_RunsFiveSteps()
        {
            _battle.Tick(0.1);
            Assert.AreEqual(0.2, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Tick_SmallerThanStep_Accumulates()
        {
            _battle.Tick(0.01);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);

            _battle.Tick(0.01);
            Assert.AreEqual(0.04, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Tick_Remainder_CarriesOver()
        {
            _battle.Tick(0.05);
            Assert.AreEqual(0.08, _battle.DisciplePosition, Tolerance);

            _battle.Tick(0.01);
            Assert.AreEqual(0.12, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Tick_LargerThanMax_IsClampedToTwelveSteps()
        {
            _battle.Tick(1.0);
            Assert.AreEqual(0.48, _battle.DisciplePosition, Tolerance);

            // 남은 0.01 초에 0.01 초를 더하면 한 칸 더 간다.
            _battle.Tick(0.01);
            Assert.AreEqual(0.52, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Tick_Zero_DoesNothing()
        {
            _battle.Tick(0);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Tick_InvalidValues_ThrowAndChangeNothing()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.Tick(-0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.Tick(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => _battle.Tick(double.NegativeInfinity));
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);

            // 예외가 난 Tick 은 시간을 모으지 않는다.
            _battle.Tick(0.01);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void Tick_PausedWithRemainder_KeepsPositionWhilePaused()
        {
            _battle.Tick(0.05);
            _battle.SetPaused(true);
            _battle.Tick(0.05);
            Assert.AreEqual(0.08, _battle.DisciplePosition, Tolerance);
        }
    }
}
