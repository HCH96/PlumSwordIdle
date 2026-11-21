using System;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class SfxQueueTests
    {
        [Test]
        public void DuplicateRequests_DrainOnceAndClear()
        {
            var queue = new SfxQueue();
            queue.Request(SfxId.EnemyDefeated); queue.Request(SfxId.EnemyDefeated); queue.Request(SfxId.EnemyDefeated);
            Assert.That(queue.Drain(1.0), Is.EqualTo(new[] { SfxId.EnemyDefeated }));
            Assert.That(queue.Drain(1.01), Is.Empty);
            Assert.IsFalse(queue.HasPending);
        }

        [Test]
        public void Interval_SuppressesOnlyRecentIdWithoutUpdatingTime()
        {
            var queue = new SfxQueue();
            queue.Request(SfxId.EnemyDefeated); queue.Drain(1.0);
            queue.Request(SfxId.EnemyDefeated); queue.Request(SfxId.AttackHit);
            Assert.That(queue.Drain(1.04), Is.EqualTo(new[] { SfxId.AttackHit }));
            queue.Request(SfxId.EnemyDefeated);
            Assert.That(queue.Drain(1.06), Is.EqualTo(new[] { SfxId.EnemyDefeated }));
        }

        [Test]
        public void Skill_ReplacesAttackSounds()
        {
            var queue = new SfxQueue();
            queue.Request(SfxId.SwordSwing); queue.Request(SfxId.AttackHit); queue.Request(SfxId.PlumSwordSkill);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.PlumSwordSkill }));
            queue.Request(SfxId.SwordSwing); queue.Request(SfxId.AttackHit);
            Assert.That(queue.Drain(2), Is.EqualTo(new[] { SfxId.SwordSwing, SfxId.AttackHit }));
        }

        [TestCase(SfxId.UiConfirm, SfxId.UpgradeSuccess)]
        [TestCase(SfxId.UpgradeUnavailable, SfxId.UpgradeSuccess)]
        [TestCase(SfxId.UiConfirm, SfxId.WindowOpenClose)]
        [TestCase(SfxId.UiConfirm, SfxId.OfflineReward)]
        public void SpecificSound_ReplacesButtonSound(SfxId button, SfxId specific)
        {
            var queue = new SfxQueue();
            queue.Request(button); queue.Request(specific);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { specific }));
        }

        [Test]
        public void ButtonAlone_IsReturned()
        {
            var queue = new SfxQueue(); queue.Request(SfxId.UiConfirm);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.UiConfirm }));
        }

        [Test]
        public void Requests_ReturnInIdOrder()
        {
            var queue = new SfxQueue(); queue.Request(SfxId.BossDefeated); queue.Request(SfxId.AttackHit);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.AttackHit, SfxId.BossDefeated }));
        }

        [Test]
        public void DisabledSound_IsFiltered()
        {
            var queue = new SfxQueue(0.05, id => id != SfxId.AttackHit);
            queue.Request(SfxId.AttackHit); queue.Request(SfxId.SwordSwing);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.SwordSwing }));
        }

        [Test]
        public void InvalidArguments_ThrowAndPreservePending()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfxQueue(-0.01));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfxQueue(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfxQueue(double.PositiveInfinity));
            var queue = new SfxQueue();
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.Request((SfxId)99));
            queue.Request(SfxId.UiConfirm);
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.Drain(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => queue.Drain(double.NegativeInfinity));
            Assert.IsTrue(queue.HasPending);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.UiConfirm }));
        }

        [Test]
        public void ZeroInterval_AllowsSameTime()
        {
            var queue = new SfxQueue(0); queue.Request(SfxId.UiConfirm);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.UiConfirm }));
            queue.Request(SfxId.UiConfirm);
            Assert.That(queue.Drain(1), Is.EqualTo(new[] { SfxId.UiConfirm }));
        }

        [Test]
        public void TimeMovesBackwards_RequestIsReturned()
        {
            var queue = new SfxQueue(); queue.Request(SfxId.UiConfirm); queue.Drain(5);
            queue.Request(SfxId.UiConfirm);
            Assert.That(queue.Drain(4), Is.EqualTo(new[] { SfxId.UiConfirm }));
        }

        [Test]
        public void DisabledSpecificSound_StillReplacesButtonBeforeFiltering()
        {
            var queue = new SfxQueue(0, id => id != SfxId.UpgradeSuccess);
            queue.Request(SfxId.UiConfirm); queue.Request(SfxId.UpgradeSuccess);
            Assert.That(queue.Drain(1), Is.Empty);
        }
    }
}
