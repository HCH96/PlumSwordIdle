using System;
using NUnit.Framework;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class RealmRulesTests
    {
        [TestCase(RealmLevel.ThirdRate, 0, 200L)]
        [TestCase(RealmLevel.SecondRate, 10, 500L)]
        [TestCase(RealmLevel.FirstRate, 20, 1000L)]
        [TestCase(RealmLevel.Peak, 30, 2000L)]
        [TestCase(RealmLevel.Transcendent, 40, 0L)]
        public void Rules_ValidLevel_ReturnExpectedValues(RealmLevel level, int bonus, long cost)
        {
            Assert.That(RealmRules.BonusPercent(level), Is.EqualTo(bonus));
            Assert.That(RealmRules.BreakthroughCost(level), Is.EqualTo(cost));
        }

        [Test]
        public void Rules_InvalidLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmRules.BonusPercent((RealmLevel)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmRules.BreakthroughCost((RealmLevel)99));
        }

        [Test]
        public void LevelCount_IsFive() => Assert.That(RealmRules.LevelCount, Is.EqualTo(5));
    }
}
