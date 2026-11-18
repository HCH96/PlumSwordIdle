using System;
using NUnit.Framework;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class RealmTextTests
    {
        [TestCase(RealmLevel.ThirdRate, "THIRD RATE", "third_rate")]
        [TestCase(RealmLevel.SecondRate, "SECOND RATE", "second_rate")]
        [TestCase(RealmLevel.FirstRate, "FIRST RATE", "first_rate")]
        [TestCase(RealmLevel.Peak, "PEAK", "peak")]
        [TestCase(RealmLevel.Transcendent, "TRANSCENDENT", "transcendent")]
        public void Text_ValidLevel_ReturnsNameAndAssetId(RealmLevel level, string name, string id)
        {
            Assert.That(RealmText.Name(level), Is.EqualTo(name));
            Assert.That(RealmText.AssetId(level), Is.EqualTo(id));
        }

        [TestCase(RealmLevel.ThirdRate, "REALM: THIRD RATE")]
        [TestCase(RealmLevel.SecondRate, "REALM: SECOND RATE")]
        public void Label_ValidLevel_ReturnsLabel(RealmLevel level, string expected) =>
            Assert.That(RealmText.Label(level), Is.EqualTo(expected));

        [Test]
        public void Text_InvalidLevel_ThrowsEvenAtMax()
        {
            var invalid = (RealmLevel)99;
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmText.Name(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmText.Label(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmText.AssetId(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmText.ButtonLabel(invalid, 500, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmText.ButtonLabel(invalid, 0, true));
        }

        [TestCase(500L, false, "G 500")]
        [TestCase(1000L, false, "G 1,000")]
        [TestCase(0L, true, "MAX")]
        [TestCase(2000L, true, "MAX")]
        [TestCase(-5L, false, "G -5")]
        public void Cost_AmountAndMax_ReturnsExpectedText(long cost, bool isMax, string expected) =>
            Assert.That(RealmText.Cost(cost, isMax), Is.EqualTo(expected));

        [Test]
        public void ButtonLabel_CurrentLevelAndCost_ReturnsThreeLines()
        {
            Assert.That(RealmText.ButtonTitle, Is.EqualTo("BREAKTHROUGH"));
            Assert.That(RealmText.ButtonLabel(RealmLevel.SecondRate, 500, false), Is.EqualTo("BREAKTHROUGH\nSECOND RATE\nG 500"));
            Assert.That(RealmText.ButtonLabel(RealmLevel.Transcendent, 0, true), Is.EqualTo("BREAKTHROUGH\nTRANSCENDENT\nMAX"));
        }
    }
}
