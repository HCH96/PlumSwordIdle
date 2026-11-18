using System;
using NUnit.Framework;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class RealmAuraTests
    {
        [TestCase(RealmLevel.ThirdRate, false, 0, 0, 0, 0)]
        [TestCase(RealmLevel.SecondRate, true, 0.55, 0.85, 1.0, 0.35)]
        [TestCase(RealmLevel.FirstRate, true, 0.35, 0.55, 1.0, 0.40)]
        [TestCase(RealmLevel.Peak, true, 0.70, 0.40, 1.0, 0.45)]
        [TestCase(RealmLevel.Transcendent, true, 1.0, 0.55, 0.75, 0.50)]
        public void Aura_ValidLevel_ReturnsPresenceAndColor(RealmLevel level, bool hasAura, double r, double g, double b, double a)
        {
            Assert.That(RealmAura.HasAura(level), Is.EqualTo(hasAura));
            var color = RealmAura.Color(level);
            Assert.That(color.R, Is.EqualTo(r).Within(1e-9));
            Assert.That(color.G, Is.EqualTo(g).Within(1e-9));
            Assert.That(color.B, Is.EqualTo(b).Within(1e-9));
            Assert.That(color.A, Is.EqualTo(a).Within(1e-9));
        }

        [Test]
        public void Aura_InvalidLevel_Throws()
        {
            var invalid = (RealmLevel)99;
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmAura.HasAura(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmAura.Color(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmAura.PulseAlpha(invalid, 0));
        }

        [TestCase(0, 0.2625)]
        [TestCase(0.4, 0.35)]
        [TestCase(1.2, 0.175)]
        [TestCase(1.6, 0.2625)]
        [TestCase(-0.4, 0.175)]
        public void PulseAlpha_ElapsedTime_ReturnsExpectedAlpha(double time, double expected) =>
            Assert.That(RealmAura.PulseAlpha(RealmLevel.SecondRate, time), Is.EqualTo(expected).Within(1e-9));

        [Test]
        public void PulseAlpha_ThirdRate_ReturnsZero() =>
            Assert.That(RealmAura.PulseAlpha(RealmLevel.ThirdRate, 0.4), Is.EqualTo(0).Within(1e-9));

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void PulseAlpha_NonFiniteTime_Throws(double time) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmAura.PulseAlpha(RealmLevel.SecondRate, time));

        [TestCase(0, 1.5)]
        [TestCase(0.2, 1.25)]
        [TestCase(0.4, 1.0)]
        [TestCase(1.0, 1.0)]
        [TestCase(-1, 1.5)]
        [TestCase(double.PositiveInfinity, 1.0)]
        [TestCase(double.NegativeInfinity, 1.5)]
        public void BurstScale_ElapsedTime_ReturnsExpectedScale(double time, double expected) =>
            Assert.That(RealmAura.BurstScale(time), Is.EqualTo(expected).Within(1e-9));

        [Test]
        public void BurstScale_NaN_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => RealmAura.BurstScale(double.NaN));

        [Test]
        public void AuraColor_Constructor_DoesNotValidateValues()
        {
            var color = new AuraColor(-1, 2, double.NaN, double.PositiveInfinity);
            Assert.That(color.R, Is.EqualTo(-1).Within(1e-9));
            Assert.That(color.G, Is.EqualTo(2).Within(1e-9));
            Assert.That(double.IsNaN(color.B), Is.True);
            Assert.That(color.A, Is.EqualTo(double.PositiveInfinity));
        }
    }
}
