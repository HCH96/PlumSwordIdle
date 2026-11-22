using System;
using NUnit.Framework;
using PlumSword.Core.Balance;

namespace PlumSword.Core.Tests.Balance
{
    public class BalanceTableTests
    {
        [TearDown] public void ResetCurrent() => BalanceTable.Current = BalanceTable.Default;

        private static void Curve(GrowthCurve curve, double b, double linear, double growth)
        {
            Assert.That(curve.Base, Is.EqualTo(b)); Assert.That(curve.Linear, Is.EqualTo(linear)); Assert.That(curve.Growth, Is.EqualTo(growth));
        }

        [Test]
        public void Default_AllProperties_MatchDefaults()
        {
            var t = BalanceTable.Default;
            Curve(t.AttackCost, 20, 5, 1); Curve(t.HealthCost, 20, 5, 1); Curve(t.InnerPowerCost, 20, 5, 1);
            Assert.That(t.AttackPerLevel, Is.EqualTo(2)); Assert.That(t.HealthPerLevel, Is.EqualTo(20));
            Assert.That(t.DiscipleBaseAttack, Is.EqualTo(10)); Assert.That(t.DiscipleBaseMaxHealth, Is.EqualTo(100));
            Assert.That(t.DiscipleAttackInterval, Is.EqualTo(1)); Assert.That(t.SkillBasePercent, Is.EqualTo(200));
            Assert.That(t.SkillPercentPerInnerLevel, Is.EqualTo(10)); Assert.That(t.SkillCooldownSeconds, Is.EqualTo(5)); Assert.That(t.SubstageStep, Is.Zero);
            Curve(t.NormalHealth, 30, 10, 1); Curve(t.NormalAttack, 5, 2, 1); Curve(t.BossHealth, 100, 30, 1); Curve(t.BossAttack, 10, 4, 1);
            Assert.That(t.NormalAttackInterval, Is.EqualTo(2)); Assert.That(t.BossAttackInterval, Is.EqualTo(2));
            Assert.That(t.FinalBossHealth, Is.Zero); Assert.That(t.FinalBossAttack, Is.Zero);
            Curve(t.NormalGold, 10, 2, 1); Curve(t.BossGold, 50, 10, 1);
            Assert.That(t.RealmKillsRequired, Is.EqualTo(new[] { 30, 50, 70, 100 }));
            Assert.That(t.RealmBonusPercent, Is.EqualTo(new[] { 0, 10, 20, 30, 40 }));
            Assert.DoesNotThrow(() => t.Validate());
        }

        [Test]
        public void Default_SeparateCalls_AreIndependent()
        {
            BalanceTable.Current = BalanceTable.Default;
            var a = BalanceTable.Default; var b = BalanceTable.Default;
            Assert.That(a, Is.Not.SameAs(b)); Assert.That(a.RealmKillsRequired, Is.Not.SameAs(b.RealmKillsRequired));
            Assert.That(a.RealmBonusPercent, Is.Not.SameAs(b.RealmBonusPercent));
            a.AttackPerLevel = 99; a.RealmKillsRequired[0] = 1; a.RealmBonusPercent[0] = 1;
            Assert.That(b.AttackPerLevel, Is.EqualTo(2)); Assert.That(BalanceTable.Current.AttackPerLevel, Is.EqualTo(2));
            Assert.That(b.RealmKillsRequired[0], Is.EqualTo(30)); Assert.That(b.RealmBonusPercent[0], Is.Zero);
        }

        [Test]
        public void Current_Null_ThrowsAndPreservesReference()
        {
            var original = BalanceTable.Current;
            Assert.Throws<ArgumentNullException>(() => BalanceTable.Current = null);
            Assert.That(BalanceTable.Current, Is.SameAs(original));
        }

        [Test]
        public void Current_InvalidTables_ThrowAndPreserveReference()
        {
            var original = BalanceTable.Current;
            var a = BalanceTable.Default; a.RealmBonusPercent = new int[4];
            var b = BalanceTable.Default; b.DiscipleAttackInterval = 0;
            var c = BalanceTable.Default; c.NormalHealth = null;
            foreach (var table in new[] { a, b, c })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => BalanceTable.Current = table);
                Assert.That(BalanceTable.Current, Is.SameAs(original));
            }
        }

        [Test]
        public void Current_ValidTable_StoresSameInstance()
        {
            var t = BalanceTable.Default; BalanceTable.Current = t;
            Assert.That(BalanceTable.Current, Is.SameAs(t));
        }

        [Test]
        public void SubstageIndex_SeventhSteps_CalculatesIndex()
        {
            var t = BalanceTable.Default; t.SubstageStep = 1.0 / 7;
            Assert.That(t.SubstageIndex(1, 1), Is.EqualTo(0));
            Assert.That(t.SubstageIndex(1, 7), Is.EqualTo(6.0 / 7).Within(1e-9));
            Assert.That(t.SubstageIndex(2, 1), Is.EqualTo(1));
        }

        [TestCase(1, 0)] [TestCase(1, 8)] [TestCase(0, 1)]
        public void SubstageIndex_InvalidInput_Throws(int stage, int substage) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => BalanceTable.Default.SubstageIndex(stage, substage));

        [Test]
        public void Validate_InvalidProperties_Throws()
        {
            Action<BalanceTable>[] changes = {
                t => t.AttackCost = null, t => t.HealthCost = null, t => t.InnerPowerCost = null,
                t => t.NormalHealth = null, t => t.NormalAttack = null, t => t.BossHealth = null, t => t.BossAttack = null,
                t => t.NormalGold = null, t => t.BossGold = null,
                t => t.AttackPerLevel = -1, t => t.HealthPerLevel = -1, t => t.DiscipleBaseAttack = 0, t => t.DiscipleBaseMaxHealth = 0,
                t => t.DiscipleAttackInterval = double.NaN, t => t.SkillCooldownSeconds = double.PositiveInfinity,
                t => t.NormalAttackInterval = -1, t => t.BossAttackInterval = 0,
                t => t.SkillBasePercent = -1, t => t.SkillPercentPerInnerLevel = -1,
                t => t.SubstageStep = -1, t => t.SubstageStep = double.NaN, t => t.SubstageStep = double.PositiveInfinity,
                t => t.FinalBossHealth = -1, t => t.FinalBossAttack = -1,
                t => t.RealmKillsRequired = null, t => t.RealmKillsRequired = new int[3], t => t.RealmKillsRequired[0] = 0,
                t => t.RealmBonusPercent = null, t => t.RealmBonusPercent = new int[4], t => t.RealmBonusPercent[0] = -1
            };
            foreach (var change in changes)
            {
                var table = BalanceTable.Default; change(table);
                Assert.Throws<ArgumentOutOfRangeException>(() => table.Validate());
            }
        }
    }
}
