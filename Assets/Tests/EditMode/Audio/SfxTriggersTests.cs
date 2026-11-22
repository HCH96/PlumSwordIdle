using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Audio;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Core.Tests.Stage;

namespace PlumSword.Core.Tests.Audio
{
    public class SfxTriggersTests
    {
        private SfxQueue queue;
        private Wallet wallet;
        private RealmProgress realm;
        private StatUpgrades upgrades;
        private Battle battle;
        private StageProgress stage;
        private SfxTriggers triggers;
        private int time;

        private void Create(long gold = 0, long attack = 1000, long health = 100000, bool skill = false, int startStage = 1)
        {
            queue = new SfxQueue(0);
            wallet = new Wallet(gold);
            realm = new RealmProgress(wallet);
            upgrades = new StatUpgrades(wallet, attack, health, realm: realm);
            battle = new Battle(upgrades, realm, skill);
            stage = new StageProgress(wallet, upgrades, battle, new MinRandom(), startStage);
            triggers = new SfxTriggers(queue, battle, stage, upgrades, realm);
            time = 0;
        }

        private IReadOnlyList<SfxId> Drain() { return queue.Drain(time++); }

        private IReadOnlyList<SfxId> Advance()
        {
            if (stage.Phase == StagePhase.FadingOut || stage.Phase == StagePhase.FadingIn)
                stage.Tick(StageProgress.FadeSeconds * 2);
            else if (stage.IsSubstageIntroShowing) stage.Tick(StageProgress.SubstageIntroSeconds);
            else if (stage.IsIntroShowing) stage.Tick(StageProgress.IntroSeconds);
            else battle.Step();
            return Drain();
        }

        private IReadOnlyList<SfxId> Until(Func<bool> condition)
        {
            for (int i = 0; i < 20000; i++)
            {
                var sounds = Advance();
                if (condition()) return sounds;
            }
            Assert.Fail("Expected event did not occur within 20000 steps.");
            return null;
        }

        [Test]
        public void Constructor_NullArguments_ThrowsWithoutSubscribing()
        {
            Create();
            triggers.Dispose();
            Assert.Throws<ArgumentNullException>(() => new SfxTriggers(null, battle, stage, upgrades, realm));
            Assert.Throws<ArgumentNullException>(() => new SfxTriggers(queue, null, stage, upgrades, realm));
            Assert.Throws<ArgumentNullException>(() => new SfxTriggers(queue, battle, null, upgrades, realm));
            Assert.Throws<ArgumentNullException>(() => new SfxTriggers(queue, battle, stage, null, realm));
            Assert.Throws<ArgumentNullException>(() => new SfxTriggers(queue, battle, stage, upgrades, null));
            battle.AddEnemy(EnemyKind.Boss, 1, battle.DisciplePosition + 0.5);
            Assert.That(Drain(), Is.Empty);
        }

        [TestCase(EnemyKind.Boss, true)]
        [TestCase(EnemyKind.Normal, false)]
        public void EnemyAdded_OnlyBossRequestsAppear(EnemyKind kind, bool boss)
        {
            Create();
            battle.AddEnemy(kind, 1, battle.DisciplePosition + 0.5);
            Assert.That(Drain(), Is.EqualTo(boss ? new[] { SfxId.BossAppear } : new SfxId[0]));
        }

        [Test]
        public void NormalAttack_RequestsSwingAndHit()
        {
            Create(attack: 1);
            bool damaged = false;
            battle.EnemyDamaged += e => damaged = true;
            battle.AddEnemy(EnemyKind.Normal, 1, battle.DisciplePosition + 0.5);
            Drain();
            Assert.That(Until(() => damaged), Is.EqualTo(new[] { SfxId.SwordSwing, SfxId.AttackHit }));
        }

        [Test]
        public void SkillAttack_ReplacesSwingAndHit()
        {
            Create(attack: 1, skill: true);
            bool used = false;
            battle.SkillUsed += () => used = true;
            battle.AddEnemy(EnemyKind.Normal, 1, battle.DisciplePosition + 0.5);
            Drain();
            var sounds = Until(() => used);
            Assert.That(sounds, Has.Member(SfxId.PlumSwordSkill));
            Assert.That(sounds, Has.No.Member(SfxId.SwordSwing));
            Assert.That(sounds, Has.No.Member(SfxId.AttackHit));
        }

        [Test]
        public void SkillFirstHit_RequestsSkillWithoutSwingOrHit()
        {
            Create(attack: 10, skill: true);
            battle.AddEnemy(EnemyKind.Boss, 2, battle.DisciplePosition + 1.0);
            battle.Step();
            var sounds = Drain();
            Assert.That(sounds, Has.Member(SfxId.PlumSwordSkill));
            Assert.That(sounds, Has.No.Member(SfxId.SwordSwing));
            Assert.That(sounds, Has.No.Member(SfxId.AttackHit));
        }

        [Test]
        public void SkillRemainingSteps_RequestNoSwingOrHit()
        {
            Create(attack: 10, skill: true);
            battle.AddEnemy(EnemyKind.Boss, 2, battle.DisciplePosition + 1.0);
            battle.Step();
            Drain();
            int hits = 0;
            battle.EnemyDamaged += enemy => hits++;
            for (int i = 0; i < 62; i++)
            {
                battle.Step();
                var sounds = Drain();
                Assert.That(sounds, Has.No.Member(SfxId.SwordSwing), "step " + (i + 2));
                Assert.That(sounds, Has.No.Member(SfxId.AttackHit), "step " + (i + 2));
            }
            Assert.That(hits, Is.EqualTo(12));
        }

        [Test]
        public void AttackAfterSkill_RequestsSwingAndHit()
        {
            Create(attack: 10, skill: true);
            battle.AddEnemy(EnemyKind.Boss, 2, battle.DisciplePosition + 1.0);
            for (int i = 0; i < 63; i++)
            {
                battle.Step();
                Drain();
            }
            battle.Step();
            var sounds = Drain();
            Assert.That(sounds, Has.Member(SfxId.SwordSwing));
            Assert.That(sounds, Has.Member(SfxId.AttackHit));
        }

        [TestCase(EnemyKind.Normal, true)]
        [TestCase(EnemyKind.Boss, false)]
        public void EnemyDefeated_OnlyNormalRequestsDefeat(EnemyKind kind, bool normal)
        {
            Create();
            var enemy = battle.AddEnemy(kind, 1, battle.DisciplePosition + 0.5);
            Drain();
            var sounds = Until(() => enemy.IsDead);
            if (normal) Assert.That(sounds, Has.Member(SfxId.EnemyDefeated));
            else Assert.That(sounds, Has.No.Member(SfxId.EnemyDefeated));
        }

        [Test]
        public void NormalGroupCleared_RequestsTransitionAndDefeat()
        {
            Create();
            stage.Begin(); Drain();
            var sounds = Until(() => stage.Phase == StagePhase.FadingOut);
            Assert.That(sounds, Has.Member(SfxId.StageTransition));
            Assert.That(sounds, Has.Member(SfxId.EnemyDefeated));
        }

        [Test]
        public void StageBossCleared_RequestsBossDefeatWithoutTransition()
        {
            Create();
            bool cleared = false;
            stage.StageCleared += n => cleared = true;
            stage.Begin(); Drain();
            var sounds = Until(() => cleared);
            Assert.That(sounds, Has.Member(SfxId.BossDefeated));
            Assert.That(sounds, Has.No.Member(SfxId.StageTransition));
        }

        [TestCase(1)]
        [TestCase(5)]
        public void DiscipleDefeated_RequestsPlayerDefeatWithoutTransition(int startStage)
        {
            Create(attack: 1, health: 1, startStage: startStage);
            bool failed = false;
            stage.StageFailed += n => failed = true;
            stage.Begin(); Drain();
            var sounds = Until(() => failed);
            Assert.That(sounds, Has.Member(SfxId.PlayerDefeated));
            Assert.That(sounds, Has.No.Member(SfxId.StageTransition));
        }

        [Test]
        public void BossAndDiscipleDefeatedTogether_RequestsOnlyPlayerOutcome()
        {
            Create(attack: 100, health: 10);
            stage.Begin(); Drain();
            stage.Tick(StageProgress.SubstageIntroSeconds); Drain();
            battle.AddEnemy(EnemyKind.Boss, 1, 1.0); Drain();
            battle.Step();
            var sounds = Drain();
            Assert.That(battle.IsDiscipleDefeated, Is.True);
            Assert.That(sounds, Has.Member(SfxId.PlayerDefeated));
            Assert.That(sounds, Has.No.Member(SfxId.BossDefeated));
        }

        [TestCase(1000000, true)]
        [TestCase(0, false)]
        public void Upgrade_OnlySuccessRequestsSound(long gold, bool success)
        {
            Create(gold: gold);
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.EqualTo(success));
            Assert.That(Drain(), Is.EqualTo(success ? new[] { SfxId.UpgradeSuccess } : new SfxId[0]));
        }

        [Test]
        public void Breakthrough_RequestsSound()
        {
            Create();
            int required = realm.KillsRequired;
            for (int i = 0; i < required; i++) realm.RecordKill();
            Assert.That(Drain(), Has.Member(SfxId.RealmBreakthrough));
        }

        [Test]
        public void Dispose_TwiceUnsubscribesWithoutThrowing()
        {
            Create(gold: 1000000);
            triggers.Dispose();
            Assert.DoesNotThrow(() => triggers.Dispose());
            battle.AddEnemy(EnemyKind.Boss, 1, battle.DisciplePosition + 0.5);
            Assert.That(upgrades.TryUpgrade(StatType.Attack), Is.True);
            Assert.That(Drain(), Is.Empty);
        }
    }
}
