using System;
using NUnit.Framework;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class RealmRulesTests
    {
        [TestCase(RealmLevel.ThirdRate, 0, 30L)]
        [TestCase(RealmLevel.SecondRate, 10, 50L)]
        [TestCase(RealmLevel.FirstRate, 20, 70L)]
        [TestCase(RealmLevel.Peak, 30, 100L)]
        [TestCase(RealmLevel.Transcendent, 40, 0L)]
        public void Rules_ValidLevel_ReturnExpectedValues(RealmLevel level, int bonus, long cost)
        {
            Assert.That(RealmRules.BonusPercent(level), Is.EqualTo(bonus));
            Assert.That(RealmRules.KillsRequired(level), Is.EqualTo(cost));
        }

        [Test]
        public void Rules_InvalidLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmRules.BonusPercent((RealmLevel)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmRules.KillsRequired((RealmLevel)99));
        }

        [Test]
        public void LevelCount_IsFive() => Assert.That(RealmRules.LevelCount, Is.EqualTo(5));
    }
}
