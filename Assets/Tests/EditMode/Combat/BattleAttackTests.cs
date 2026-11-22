using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    /// <summary>009_battle_attack: 공격, 피해, 쓰러짐 판정과 알림.</summary>
    public class BattleAttackTests
    {
        private const double Tolerance = 1e-6;

        private Wallet _wallet;
        private StatUpgrades _upgrades;
        private Battle _battle;

        private List<string> _log;
        private List<Enemy> _added;
        private List<Enemy> _damaged;
        private List<Enemy> _defeated;
        private List<bool> _discipleDefeatedFlagAtEnemyDefeated;
        private int _discipleDamagedCount;
        private int _discipleDefeatedCount;
        private int _resetCompletedCount;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet(1000);
            Create(10, 100);
        }

        // Battle 을 만들고 바로 구독한다. 기본 설정과 다른 강화 값이 필요한 테스트는 처음에 다시 부른다.
        private void Create(long attack, long maxHealth)
        {
            _upgrades = new StatUpgrades(_wallet, attack, maxHealth);
            _battle = new Battle(_upgrades);

            _log = new List<string>();
            _added = new List<Enemy>();
            _damaged = new List<Enemy>();
            _defeated = new List<Enemy>();
            _discipleDefeatedFlagAtEnemyDefeated = new List<bool>();
            _discipleDamagedCount = 0;
            _discipleDefeatedCount = 0;
            _resetCompletedCount = 0;

            _battle.EnemyAdded += enemy =>
            {
                _log.Add("EnemyAdded");
                _added.Add(enemy);
            };
            _battle.EnemyDamaged += enemy =>
            {
                _log.Add("EnemyDamaged");
                _damaged.Add(enemy);
            };
            _battle.DiscipleDamaged += () =>
            {
                _log.Add("DiscipleDamaged");
                _discipleDamagedCount++;
            };
            _battle.EnemyDefeated += enemy =>
            {
                _log.Add("EnemyDefeated");
                _defeated.Add(enemy);
                _discipleDefeatedFlagAtEnemyDefeated.Add(_battle.IsDiscipleDefeated);
            };
            _battle.DiscipleDefeated += () =>
            {
                _log.Add("DiscipleDefeated");
                _discipleDefeatedCount++;
            };
            _battle.ResetCompleted += () =>
            {
                _log.Add("ResetCompleted");
                _resetCompletedCount++;
            };
        }

        private void Steps(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _battle.Step();
            }
        }

        // ---- 제자의 공격 ----

        [Test]
        public void DiscipleReachesRange_AttacksAfterWindUp()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);

            // 29칸째에 달려와 멈추고 앞 동작 대기를 시작한다.
            Steps(29);
            Assert.AreEqual(30L, enemy.Health);
            Assert.AreEqual(0, _damaged.Count);
            Assert.AreEqual(3.74, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(4.72, enemy.Position, Tolerance);
            Assert.AreEqual(7.0 / 17.0, _battle.DiscipleAttackCooldown, Tolerance);

            // 앞 동작 0.4118초 = 21칸 뒤(50칸째)에 첫 피해.
            Steps(20);
            Assert.AreEqual(30L, enemy.Health);
            Assert.AreEqual(0, _damaged.Count);

            Steps(1);
            Assert.AreEqual(20L, enemy.Health);
            Assert.AreEqual(1, _damaged.Count);
            Assert.AreSame(enemy, _damaged[0]);
            Assert.AreEqual(3.74, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(4.54, enemy.Position, Tolerance);
            Assert.AreEqual(1.0, _battle.DiscipleAttackCooldown, Tolerance);
        }

        [Test]
        public void WindUp_ScalesWithAttackInterval()
        {
            Create(10, 100);
            _battle.AddEnemy(EnemyKind.Boss, 2, 5.0);

            Steps(31);    // 31칸째에 4.0 에 도착
            Assert.AreEqual(4.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(Battle.DiscipleBaseAttackInterval * 7.0 / 17.0, _battle.DiscipleAttackCooldown, Tolerance);
        }

        [Test]
        public void EnemyAlreadyInRangeAtStart_AttacksWithoutWindUp()
        {
            // 제자가 전진하지 않은 채로 범위 안에 적이 있으면 지금처럼 바로 공격한다.
            Enemy enemy = _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);

            Steps(1);
            Assert.AreEqual(90L, enemy.Health);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
        }

        // ---- 적의 공격 ----

        [Test]
        public void NormalReachesRange_AttacksImmediately()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);

            Steps(37);
            Assert.AreEqual(100L, _battle.DiscipleHealth);
            Assert.AreEqual(0, _discipleDamagedCount);

            Steps(1);
            Assert.AreEqual(95L, _battle.DiscipleHealth);
            Assert.AreEqual(1, _discipleDamagedCount);
            Assert.AreEqual(4.54, enemy.Position, Tolerance);
            Assert.AreEqual(2.0, enemy.AttackCooldown, Tolerance);
        }

        // ---- 처치 ----

        [Test]
        public void NormalHealthReachesZero_IsDefeatedAndDiscipleNotHealed()
        {
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0);

            // 앞 동작 때문에 피해는 50·100·150칸째에 들어간다.
            Steps(100);
            Assert.AreEqual(10L, enemy.Health);

            Steps(49);
            Assert.AreEqual(10L, enemy.Health);
            Assert.AreEqual(0, _defeated.Count);
            Assert.IsFalse(enemy.IsDead);

            Steps(1);
            Assert.AreEqual(0L, enemy.Health);
            Assert.AreEqual(1, _defeated.Count);
            Assert.AreSame(enemy, _defeated[0]);
            Assert.IsTrue(enemy.IsDead);
            Assert.AreEqual(0, _battle.Enemies.Count);
            // 적은 38칸째와 138칸째에 공격했다.
            Assert.AreEqual(90L, _battle.DiscipleHealth);

            Steps(1);
            Assert.AreEqual(3.8696, _battle.DisciplePosition, Tolerance);
        }

        // ---- 여러 적 ----

        [Test]
        public void ThreeNormalsInRange_AllHitAndNotificationsInOrder()
        {
            Enemy e0 = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy e1 = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy e2 = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _log.Clear();

            Steps(1);

            Assert.AreEqual(20L, e0.Health);
            Assert.AreEqual(20L, e1.Health);
            Assert.AreEqual(20L, e2.Health);
            Assert.AreEqual(85L, _battle.DiscipleHealth);

            Assert.AreEqual(3, _damaged.Count);
            Assert.AreEqual(0, _damaged[0].Index);
            Assert.AreEqual(1, _damaged[1].Index);
            Assert.AreEqual(2, _damaged[2].Index);
            Assert.AreEqual(3, _discipleDamagedCount);

            CollectionAssert.AreEqual(
                new[]
                {
                    "EnemyDamaged", "EnemyDamaged", "EnemyDamaged",
                    "DiscipleDamaged", "DiscipleDamaged", "DiscipleDamaged",
                },
                _log);
        }

        [Test]
        public void ThreeNormalsDefeatedSameStep_StillAttackThatStep()
        {
            Enemy e0 = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy e1 = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy e2 = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);

            Steps(51);
            Assert.AreEqual(10L, e0.Health);
            Assert.AreEqual(10L, e1.Health);
            Assert.AreEqual(10L, e2.Health);
            Assert.AreEqual(85L, _battle.DiscipleHealth);

            Steps(50);
            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.AreEqual(3, _defeated.Count);
            Assert.AreEqual(0, _defeated[0].Index);
            Assert.AreEqual(1, _defeated[1].Index);
            Assert.AreEqual(2, _defeated[2].Index);
            Assert.AreEqual(6, _discipleDamagedCount);
            Assert.AreEqual(70L, _battle.DiscipleHealth);
            Assert.IsFalse(_battle.IsDiscipleDefeated);
        }

        [Test]
        public void EnemyOutOfRange_IsNotHit()
        {
            Enemy normal = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 1, 1.5);

            Steps(1);

            Assert.AreEqual(20L, normal.Health);
            Assert.AreEqual(100L, boss.Health);
            Assert.AreEqual(1, _damaged.Count);
            Assert.AreSame(normal, _damaged[0]);
        }

        // ---- 보스 ----

        [Test]
        public void BossInRange_TradesHitsUntilDefeated()
        {
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);

            Steps(1);
            Assert.AreEqual(90L, boss.Health);
            Assert.AreEqual(96L, _battle.DiscipleHealth);

            Steps(449);
            Assert.AreEqual(10L, boss.Health);
            Assert.AreEqual(0, _defeated.Count);

            Steps(1);
            Assert.AreEqual(0L, boss.Health);
            Assert.AreEqual(1, _defeated.Count);
            Assert.AreEqual(80L, _battle.DiscipleHealth);
        }

        [Test]
        public void DiscipleWalksToBoss_BossAttacksOnArrivalDiscipleAfterWindUp()
        {
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 2, 5.0);

            // 31칸째에 4.0 에 도착한다.
            Steps(30);
            Assert.AreEqual(130L, boss.Health);
            Assert.AreEqual(100L, _battle.DiscipleHealth);

            Steps(1);
            Assert.AreEqual(130L, boss.Health);
            Assert.AreEqual(92L, _battle.DiscipleHealth);
            Assert.AreEqual(4.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(5.0, boss.Position, Tolerance);

            Steps(20);
            Assert.AreEqual(130L, boss.Health);

            Steps(1);
            Assert.AreEqual(120L, boss.Health);
        }

        // ---- 대기 시간 ----

        [Test]
        public void CooldownRunsWhileMoving_WaitsRemainingTime()
        {
            Create(30, 100);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 2, 2.0);

            Steps(1);
            Assert.AreEqual(1, _battle.Enemies.Count);
            Assert.AreEqual(95L, _battle.DiscipleHealth);

            Steps(25);
            Assert.AreEqual(1.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(130L, boss.Health);
            Assert.AreEqual(0.5, _battle.DiscipleAttackCooldown, Tolerance);
            Assert.AreEqual(87L, _battle.DiscipleHealth);

            Steps(24);
            Assert.AreEqual(130L, boss.Health);

            Steps(1);
            Assert.AreEqual(100L, boss.Health);
        }

        [Test]
        public void CooldownFinishedBeforeArrival_StillWindsUpOnArrival()
        {
            Create(30, 100);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 2, 8.0);

            // 56칸째에 도착하고, 앞 동작 21칸 뒤인 77칸째에 피해.
            Steps(76);
            Assert.AreEqual(130L, boss.Health);

            Steps(1);
            Assert.AreEqual(100L, boss.Health);
        }

        // ---- 강화 반영 ----

        [Test]
        public void AttackUpgradeMidBattle_NextHitUsesNewAttack()
        {
            Enemy boss = _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);

            Steps(1);
            Assert.IsTrue(_upgrades.TryUpgrade(StatType.Attack));

            Steps(50);
            Assert.AreEqual(78L, boss.Health);
        }

        // ---- 동시 쓰러짐 ----

        [Test]
        public void DiscipleAndNormalDefeatedSameStep_EnemyDefeatedFirstWithDiscipleAlreadyDefeated()
        {
            Create(30, 5);
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);

            int enemiesCountAtEnemyDefeated = -1;
            bool isDeadAtEnemyDefeated = false;
            _battle.EnemyDefeated += e =>
            {
                enemiesCountAtEnemyDefeated = _battle.Enemies.Count;
                isDeadAtEnemyDefeated = e.IsDead;
            };
            _log.Clear();

            Steps(1);

            CollectionAssert.AreEqual(
                new[] { "EnemyDamaged", "DiscipleDamaged", "EnemyDefeated", "DiscipleDefeated" },
                _log);
            Assert.AreEqual(1, _discipleDefeatedFlagAtEnemyDefeated.Count);
            Assert.IsTrue(_discipleDefeatedFlagAtEnemyDefeated[0]);
            Assert.AreEqual(0, enemiesCountAtEnemyDefeated);
            Assert.IsTrue(isDeadAtEnemyDefeated);

            Assert.AreEqual(0L, _battle.DiscipleHealth);
            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.IsTrue(enemy.IsDead);
        }

        [Test]
        public void DiscipleAndBossDefeatedSameStep_DiscipleDefeatedFlagIsTrueAtEnemyDefeated()
        {
            Create(100, 4);
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);

            Steps(1);

            Assert.AreEqual(1, _defeated.Count);
            Assert.IsTrue(_discipleDefeatedFlagAtEnemyDefeated[0]);
            Assert.AreEqual(1, _discipleDefeatedCount);
        }

        // ---- 패배 ----

        [Test]
        public void DiscipleDefeated_StepAndTickDoNothing()
        {
            Create(10, 5);
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);

            Steps(1);
            Assert.AreEqual(0L, _battle.DiscipleHealth);
            Assert.IsTrue(_battle.IsDiscipleDefeated);
            Assert.AreEqual(1, _discipleDefeatedCount);
            Assert.AreEqual(20L, enemy.Health);
            Assert.AreEqual(1, _battle.Enemies.Count);

            int logCount = _log.Count;
            Steps(100);
            _battle.Tick(0.1);

            Assert.AreEqual(20L, enemy.Health);
            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(logCount, _log.Count);
        }

        [Test]
        public void DiscipleDefeated_AddEnemyWorksAndResetRestores()
        {
            Create(10, 5);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            Steps(1);
            Assert.IsTrue(_battle.IsDiscipleDefeated);

            int addedBefore = _added.Count;
            Enemy extra = null;
            Assert.DoesNotThrow(() => extra = _battle.AddEnemy(EnemyKind.Normal, 1, 5.0));
            Assert.AreEqual(addedBefore + 1, _added.Count);
            Assert.AreSame(extra, _added[_added.Count - 1]);
            Assert.AreEqual(2, _battle.Enemies.Count);

            _battle.Reset();
            Assert.IsFalse(_battle.IsDiscipleDefeated);
            Assert.AreEqual(5L, _battle.DiscipleHealth);
            Assert.AreEqual(0, _battle.Enemies.Count);

            Steps(1);
            Assert.AreEqual(0.1296, _battle.DisciplePosition, Tolerance);
        }

        [Test]
        public void OverkillOnDisciple_HealthStaysZeroAndDefeatedOnce()
        {
            Create(10, 12);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);

            Steps(1);

            Assert.AreEqual(0L, _battle.DiscipleHealth);
            Assert.AreEqual(3, _discipleDamagedCount);
            Assert.AreEqual(1, _discipleDefeatedCount);
        }

        [Test]
        public void OverkillOnEnemy_HealthStaysZero()
        {
            Create(1000, 100);
            Enemy enemy = _battle.AddEnemy(EnemyKind.Normal, 1, 0.8);

            Steps(1);

            Assert.AreEqual(0L, enemy.Health);
            Assert.IsTrue(enemy.IsDead);
        }

        // ---- 초기화 ----

        [Test]
        public void ResetAfterCombat_RestoresStartStateWithoutEnemyDefeated()
        {
            _battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
            Steps(1);
            Assert.AreEqual(96L, _battle.DiscipleHealth);
            Assert.AreEqual(1.0, _battle.DiscipleAttackCooldown, Tolerance);

            _battle.Reset();

            Assert.AreEqual(0.0, _battle.DisciplePosition, Tolerance);
            Assert.AreEqual(100L, _battle.DiscipleHealth);
            Assert.AreEqual(0.0, _battle.DiscipleAttackCooldown, Tolerance);
            Assert.IsFalse(_battle.IsDiscipleDefeated);
            Assert.AreEqual(0, _battle.Enemies.Count);
            Assert.AreEqual(1, _resetCompletedCount);
            Assert.AreEqual(0, _defeated.Count);
        }

        // ---- 구독자 없음 ----

        [Test]
        public void NoSubscribers_CombatToDefeat_DoesNotThrow()
        {
            var battle = new Battle(new StatUpgrades(new Wallet(1000), 30, 5));
            Assert.DoesNotThrow(() =>
            {
                battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
                battle.AddEnemy(EnemyKind.Boss, 1, 1.0);
                battle.Step();
            });
            Assert.IsTrue(battle.IsDiscipleDefeated);
        }
    }
}
