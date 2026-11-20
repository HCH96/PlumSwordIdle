using System;
using NUnit.Framework;
using PlumSword.Core.Realm;

namespace PlumSword.Core.Tests.Realm
{
    public class BreakthroughEffectTests
    {
        private const double Tolerance = 1e-9;
        private static readonly RealmLevel Invalid = (RealmLevel)99;

        [TestCase(RealmLevel.SecondRate, false, false, false, false, 12, 0.25, 0, 1.0, 0, 0, 2, 0)]
        [TestCase(RealmLevel.FirstRate, true, false, false, false, 18, 0.35, 1, 1.5, 1.5, 0, 2, 2)]
        [TestCase(RealmLevel.Peak, true, true, false, false, 24, 0.45, 2, 2.0, 2.0, 5.0, 3, 3)]
        [TestCase(RealmLevel.Transcendent, true, true, true, true, 36, 0.60, 3, 3.0, 2.5, 7.0, 4, 5)]
        public void Style_AuraLevels_ReturnTableValues(RealmLevel realm, bool hasRing, bool hasPillar,
            bool hasShockwave, bool hasFlash, int burstPetals, double dimAlpha, int shakePixels,
            double lightPeak, double ringRadius, double pillarHeight, int auraWisps, int auraPetals)
        {
            var style = BreakthroughEffect.Style(realm);
            Assert.That(style.HasRing, Is.EqualTo(hasRing));
            Assert.That(style.HasPillar, Is.EqualTo(hasPillar));
            Assert.That(style.HasShockwave, Is.EqualTo(hasShockwave));
            Assert.That(style.HasFlash, Is.EqualTo(hasFlash));
            Assert.That(style.BurstPetals, Is.EqualTo(burstPetals));
            Assert.That(style.DimAlpha, Is.EqualTo(dimAlpha).Within(Tolerance));
            Assert.That(style.ShakePixels, Is.EqualTo(shakePixels));
            Assert.That(style.LightPeak, Is.EqualTo(lightPeak).Within(Tolerance));
            Assert.That(style.RingRadius, Is.EqualTo(ringRadius).Within(Tolerance));
            Assert.That(style.PillarHeight, Is.EqualTo(pillarHeight).Within(Tolerance));
            Assert.That(style.AuraWisps, Is.EqualTo(auraWisps));
            Assert.That(style.AuraPetals, Is.EqualTo(auraPetals));
        }

        [Test]
        public void Style_ThirdRate_IsAllFalseAndZero()
        {
            var style = BreakthroughEffect.Style(RealmLevel.ThirdRate);
            Assert.That(style.HasRing, Is.False);
            Assert.That(style.HasPillar, Is.False);
            Assert.That(style.HasShockwave, Is.False);
            Assert.That(style.HasFlash, Is.False);
            Assert.That(style.BurstPetals, Is.EqualTo(0));
            Assert.That(style.DimAlpha, Is.EqualTo(0));
            Assert.That(style.ShakePixels, Is.EqualTo(0));
            Assert.That(style.LightPeak, Is.EqualTo(0));
            Assert.That(style.RingRadius, Is.EqualTo(0));
            Assert.That(style.PillarHeight, Is.EqualTo(0));
            Assert.That(style.AuraWisps, Is.EqualTo(0));
            Assert.That(style.AuraPetals, Is.EqualTo(0));
        }

        [Test]
        public void Style_InvalidLevel_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.Style(Invalid));

        [TestCase(1.79, true)]
        [TestCase(1.8, false)]
        [TestCase(-1, true)]
        [TestCase(double.PositiveInfinity, false)]
        public void IsPlaying_Time_ReturnsExpected(double seconds, bool expected) =>
            Assert.That(BreakthroughEffect.IsPlaying(seconds), Is.EqualTo(expected));

        [TestCase(0.49, false)]
        [TestCase(0.5, true)]
        public void ShowsNewAura_Time_ReturnsExpected(double seconds, bool expected) =>
            Assert.That(BreakthroughEffect.ShowsNewAura(seconds), Is.EqualTo(expected));

        [TestCase(-1, 0)]
        [TestCase(0.25, 0.125)]
        [TestCase(1.0, 0.25)]
        [TestCase(1.55, 0.125)]
        [TestCase(1.8, 0)]
        [TestCase(double.PositiveInfinity, 0)]
        public void DimAlpha_SecondRate_ReturnsExpected(double seconds, double expected) =>
            Assert.That(BreakthroughEffect.DimAlpha(RealmLevel.SecondRate, seconds),
                Is.EqualTo(expected).Within(Tolerance));

        [Test]
        public void DimAlpha_TranscendentHold_ReturnsPeak() =>
            Assert.That(BreakthroughEffect.DimAlpha(RealmLevel.Transcendent, 1.0), Is.EqualTo(0.6).Within(Tolerance));

        [TestCase(-1, 0)]
        [TestCase(1.5, 0)]
        [TestCase(2.75, 0.5)]
        [TestCase(4, 1)]
        [TestCase(10, 1)]
        public void DimMask_Distance_ReturnsExpected(double distance, double expected) =>
            Assert.That(BreakthroughEffect.DimMask(distance), Is.EqualTo(expected).Within(Tolerance));

        [Test]
        public void DimMask_NaN_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.DimMask(double.NaN));

        [TestCase(0, 0)]
        [TestCase(0.25, 0.6)]
        [TestCase(0.5, 3.0)]
        [TestCase(0.9, 1.5)]
        [TestCase(1.3, 0)]
        public void LightIntensity_Transcendent_ReturnsExpected(double seconds, double expected) =>
            Assert.That(BreakthroughEffect.LightIntensity(RealmLevel.Transcendent, seconds),
                Is.EqualTo(expected).Within(Tolerance));

        [Test]
        public void LightIntensity_SecondRateAtBurst_ReturnsPeak() =>
            Assert.That(BreakthroughEffect.LightIntensity(RealmLevel.SecondRate, 0.5), Is.EqualTo(1.0).Within(Tolerance));

        [Test]
        public void BloomIntensity_Transcendent_IsHalfOfLight() =>
            Assert.That(BreakthroughEffect.BloomIntensity(RealmLevel.Transcendent, 0.9), Is.EqualTo(0.75).Within(Tolerance));

        [TestCase(0.4, 0)]
        [TestCase(0.5, 0.8)]
        [TestCase(0.575, 0.4)]
        [TestCase(0.65, 0)]
        public void FlashAlpha_Transcendent_ReturnsExpected(double seconds, double expected) =>
            Assert.That(BreakthroughEffect.FlashAlpha(RealmLevel.Transcendent, seconds),
                Is.EqualTo(expected).Within(Tolerance));

        [Test]
        public void FlashAlpha_PeakWithoutFlash_IsZero() =>
            Assert.That(BreakthroughEffect.FlashAlpha(RealmLevel.Peak, 0.5), Is.EqualTo(0));

        [TestCase(RealmLevel.FirstRate, 0.6, 1)]
        [TestCase(RealmLevel.Transcendent, 0.79, 3)]
        [TestCase(RealmLevel.Transcendent, 0.8, 0)]
        [TestCase(RealmLevel.Transcendent, 0.4, 0)]
        [TestCase(RealmLevel.SecondRate, 0.6, 0)]
        public void ShakePixels_Time_ReturnsExpected(RealmLevel realm, double seconds, int expected) =>
            Assert.That(BreakthroughEffect.ShakePixels(realm, seconds), Is.EqualTo(expected));

        [Test]
        public void TimeFunctions_NaN_Throw()
        {
            var realm = RealmLevel.Transcendent;
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.IsPlaying(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.ShowsNewAura(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.DimAlpha(realm, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.LightIntensity(realm, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.BloomIntensity(realm, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.FlashAlpha(realm, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.ShakePixels(realm, double.NaN));
        }

        [Test]
        public void RealmFunctions_InvalidLevel_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.DimAlpha(Invalid, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.LightIntensity(Invalid, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.BloomIntensity(Invalid, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.FlashAlpha(Invalid, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BreakthroughEffect.ShakePixels(Invalid, 0));
        }

        [Test]
        public void EffectPoint_Constructor_StoresValuesWithoutValidation()
        {
            var point = new EffectPoint(1, 2, 3, 4);
            Assert.That(point.X, Is.EqualTo(1));
            Assert.That(point.Y, Is.EqualTo(2));
            Assert.That(point.Scale, Is.EqualTo(3));
            Assert.That(point.Alpha, Is.EqualTo(4));

            Assert.DoesNotThrow(() => new EffectPoint(-1, double.NaN, -2, 5));
            var odd = new EffectPoint(-1, double.NaN, -2, 5);
            Assert.That(odd.X, Is.EqualTo(-1));
            Assert.That(double.IsNaN(odd.Y), Is.True);
            Assert.That(odd.Scale, Is.EqualTo(-2));
            Assert.That(odd.Alpha, Is.EqualTo(5));
        }
    }
}
