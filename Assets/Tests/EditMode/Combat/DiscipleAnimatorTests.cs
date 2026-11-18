using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Combat
{
    public class DiscipleAnimatorTests
    {
        private const double Interval = 1.0;   // 17 fps

        [Test]
        public void New_StartsAtIdleFrameZero()
        {
            var animator = new DiscipleAnimator();
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
            Assert.AreEqual(0, animator.Frame);
        }

        [Test]
        public void Idle_LoopsAtTenFramesPerSecond()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.25, false, false, false, 0.0, Interval);
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
            Assert.AreEqual(2, animator.Frame);

            animator.Update(2.2, false, false, false, 0.0, Interval);   // 2.45 초 -> 24 프레임 + 0
            Assert.AreEqual(0, animator.Frame);
        }

        [Test]
        public void Moving_PlaysRunAtTwentyFramesPerSecondFromFrameZero()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.5, false, false, false, 0.0, Interval);
            animator.Update(0.0, true, false, false, 0.0, Interval);
            Assert.AreEqual(DiscipleMotion.Run, animator.Motion);
            Assert.AreEqual(0, animator.Frame);

            animator.Update(0.1, true, false, false, 0.0, Interval);   // 2 프레임
            Assert.AreEqual(2, animator.Frame);

            animator.Update(0.6, true, false, false, 0.0, Interval);
            // 지금까지 0.7 초 = 14 프레임.
            Assert.AreEqual(14, animator.Frame);
        }

        [Test]
        public void StopMoving_AfterGrace_ReturnsToIdleFromStart()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.3, true, false, false, 0.0, Interval);
            animator.Update(0.05, false, false, false, 0.0, Interval);
            Assert.AreEqual(DiscipleMotion.Run, animator.Motion, "유예 시간 안에서는 계속 달린다");

            animator.Update(0.05, false, false, false, 0.0, Interval);
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
            Assert.AreEqual(0, animator.Frame);   // 대기 0.05 초 = 0번 프레임
        }

        [Test]
        public void FrameWithoutBattleStep_KeepsRunCycleGoing()
        {
            // 60 fps 화면에서 0.02 초 칸은 6 프레임에 한 번 진행하지 않는다.
            var animator = new DiscipleAnimator();
            const double frame = 1.0 / 60.0;
            for (int i = 0; i < 30; i++)
            {
                animator.Update(frame, i % 6 != 5, false, false, 0.0, Interval);
                Assert.AreEqual(DiscipleMotion.Run, animator.Motion, $"프레임 {i}");
            }

            // 0.5 초 = 10 프레임째 (20 fps).
            Assert.AreEqual(10, animator.Frame);
        }

        [Test]
        public void RunningAfterKill_InterruptsRecoveryAfterSlashFrames()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.02, false, true, true, Interval, Interval);

            // 검기 프레임(7~9) 동안은 달려도 공격 그림을 유지한다.
            double t = 2.5 / 17.0;
            animator.Update(t, true, false, false, Interval - t, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame + 2, animator.Frame);

            // 그 뒤 달리고 있으면 달리기로 바뀌고 달리기 처음부터 센다 (1/17 초 = 20 fps 로 1번 프레임).
            animator.Update(1.0 / 17.0, true, false, false, Interval - t - 1.0 / 17.0, Interval);
            Assert.AreEqual(DiscipleMotion.Run, animator.Motion);
            Assert.AreEqual(1, animator.Frame);
        }

        [Test]
        public void AttackFramesPerSecond_IsFrameCountOverInterval()
        {
            Assert.AreEqual(17.0, DiscipleAnimator.AttackFramesPerSecond(1.0), 1e-9);
            Assert.AreEqual(34.0, DiscipleAnimator.AttackFramesPerSecond(0.5), 1e-9);
        }

        [TestCase(0.0)]
        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void AttackFramesPerSecond_InvalidInterval_Throws(double interval)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DiscipleAnimator.AttackFramesPerSecond(interval));
        }

        [Test]
        public void Attacked_ShowsHitFrameAtTheMomentOfDamage()
        {
            var animator = new DiscipleAnimator();
            // 이번 Tick 의 마지막 칸에 공격했다: 대기 = 간격.
            animator.Update(0.02, false, true, true, Interval, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame, animator.Frame);
        }

        [Test]
        public void Attacked_EarlierInTick_SkipsElapsedRecoveryFrames()
        {
            var animator = new DiscipleAnimator();
            // 공격 뒤 0.12 초가 이미 흘렀다 (17 fps 로 2 프레임).
            animator.Update(0.15, false, true, true, Interval - 0.12, Interval);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame + 2, animator.Frame);
        }

        [Test]
        public void Recovery_PlaysToLastFrameThenLeavesAttack()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.02, false, true, true, Interval, Interval);

            // 9.5 프레임 뒤: 마지막 프레임(16).
            double t = 9.5 / 17.0;
            animator.Update(t, false, false, true, Interval - t, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(DiscipleAnimator.AttackFrameCount - 1, animator.Frame);

            // 적이 없어지면(engaged=false) 마무리 뒤 대기로 돌아간다.
            animator.Update(1.0 / 17.0, false, false, false, Interval - t - 1.0 / 17.0, Interval);
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
        }

        [Test]
        public void Engaged_WindUpEndsExactlyWhenCooldownReachesZero()
        {
            var animator = new DiscipleAnimator();

            // 타격까지 8 프레임보다 많이 남았으면 아직 대기 동작.
            animator.Update(0.02, false, false, true, 8.0 / 17.0, Interval);
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);

            // 정확히 7 프레임 남음 -> 0번 프레임.
            animator.Update(0.02, false, false, true, 7.0 / 17.0, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(0, animator.Frame);

            // 3.5 프레임 남음 -> 3번 프레임.
            animator.Update(0.02, false, false, true, 3.5 / 17.0, Interval);
            Assert.AreEqual(3, animator.Frame);

            // 거의 0 -> 마지막 앞 동작(6번).
            animator.Update(0.02, false, false, true, 0.01, Interval);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame - 1, animator.Frame);

            // 대기가 0 이 된 칸에 공격 -> 타격 프레임.
            animator.Update(0.02, false, true, true, Interval, Interval);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame, animator.Frame);
        }

        [Test]
        public void ContinuousAttacks_LoopWithoutIdleGap()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.02, false, true, true, Interval, Interval);

            // 마무리 10 프레임이 끝나는 순간 대기는 7 프레임 남아 앞 동작 0번이 된다.
            double t = 10.0 / 17.0;
            animator.Update(t, false, false, true, Interval - t, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(0, animator.Frame);
        }

        [Test]
        public void AttackedWithoutWindUp_StartsAtHitFrame()
        {
            // Battle 은 달려와 멈춘 칸에 앞 동작 대기를 두므로 정상 진행에서는 생기지 않는다. 생기더라도 피해 순간은 타격 프레임이다.
            var animator = new DiscipleAnimator();
            animator.Update(0.5, true, false, false, 0.0, Interval);
            animator.Update(0.02, true, true, true, Interval, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame, animator.Frame);
        }

        [Test]
        public void NotEngaged_DoesNotWindUp()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.02, false, false, false, 0.1, Interval);
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
        }

        [Test]
        public void FasterInterval_PlaysAttackFaster()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.02, false, true, true, 0.5, 0.5);
            // 34 fps: 0.1 초 = 3.4 프레임.
            animator.Update(0.1, false, false, true, 0.4, 0.5);
            Assert.AreEqual(DiscipleAnimator.AttackHitFrame + 3, animator.Frame);
        }

        [Test]
        public void Reset_ReturnsToIdleFrameZero()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.02, false, true, true, Interval, Interval);
            animator.Reset();
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
            Assert.AreEqual(0, animator.Frame);

            animator.Update(0.02, false, false, false, 0.0, Interval);
            Assert.AreEqual(DiscipleMotion.Idle, animator.Motion);
        }

        [Test]
        public void Engaged_WhileStillMoving_StartsWindUpOnArrivalFrame()
        {
            var animator = new DiscipleAnimator();
            animator.Update(0.5, true, false, false, 0.0, Interval);
            // 이번 프레임에 달려와 멈췄고, Battle 이 대기를 7/17 초로 올렸다.
            animator.Update(1.0 / 60.0, true, false, true, 7.0 / 17.0, Interval);
            Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
            Assert.AreEqual(0, animator.Frame);
        }

        [Test]
        public void WindUpRatio_MatchesHitFrameOverFrameCount()
        {
            Assert.AreEqual((double)DiscipleAnimator.AttackHitFrame / DiscipleAnimator.AttackFrameCount,
                Battle.DiscipleAttackWindUpRatio, 1e-12);
        }

        [Test]
        public void BattleDiscipleAttackInterval_FixedAfterUpgrades()
        {
            var upgrades = new StatUpgrades(new Wallet(1000), 10, 100);
            var battle = new Battle(upgrades);
            Assert.AreEqual(1.0, battle.DiscipleAttackInterval, 1e-9);

            Assert.IsTrue(upgrades.TryUpgrade(StatType.Attack));
            Assert.IsTrue(upgrades.TryUpgrade(StatType.Health));
            Assert.AreEqual(Battle.DiscipleBaseAttackInterval, battle.DiscipleAttackInterval, 1e-9);
        }

        [Test]
        public void WithBattle_HitFrameCoincidesWithEnemyDamage()
        {
            var upgrades = new StatUpgrades(new Wallet(0), 10, 100);
            var battle = new Battle(upgrades);
            battle.AddEnemy(EnemyKind.Boss, 1, 3.0);   // 보스는 움직이지 않는다
            var animator = new DiscipleAnimator();

            int damagedAtHitFrame = 0;
            bool sawWindUpStart = false;
            int damageCount = 0;
            bool damagedThisFrame = false;
            battle.EnemyDamaged += _ => { damagedThisFrame = true; damageCount++; };

            const double frameSeconds = 1.0 / 60.0;
            for (int i = 0; i < 60 * 6; i++)
            {
                damagedThisFrame = false;
                double previousPosition = battle.DisciplePosition;
                battle.Tick(frameSeconds);

                bool moved = battle.DisciplePosition - previousPosition > DiscipleAnimator.MoveThreshold;
                bool attacked = damagedThisFrame;   // 화면 코드와 같이 EnemyDamaged 로 가린다
                bool engaged = battle.Enemies.Count > 0
                    && battle.Enemies[0].Position - battle.DisciplePosition <= Battle.DiscipleAttackRange + Battle.Epsilon;
                animator.Update(frameSeconds, moved, attacked, engaged, battle.DiscipleAttackCooldown, battle.DiscipleAttackInterval);

                if (damageCount == 0 && animator.Motion == DiscipleMotion.Attack && animator.Frame == 0) sawWindUpStart = true;
                if (damagedThisFrame)
                {
                    Assert.AreEqual(DiscipleMotion.Attack, animator.Motion);
                    Assert.AreEqual(DiscipleAnimator.AttackHitFrame, animator.Frame, $"프레임 {i}");
                    damagedAtHitFrame++;
                }
            }

            Assert.Greater(damageCount, 3);
            Assert.IsTrue(sawWindUpStart, "첫 공격 전에 앞 동작 0번 프레임이 보여야 한다");
            Assert.AreEqual(damageCount, damagedAtHitFrame);
        }
    }
}
