using System;
using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Stage
{
    public class EndingTextTests
    {
        [TestCase(754.9, "플레이 시간 12:34")]
        [TestCase(0.0, "플레이 시간 00:00")]
        [TestCase(3725.0, "플레이 시간 62:05")]
        [TestCase(420.0, "플레이 시간 07:00")]
        public void PlayTime_ValidSeconds_FormatsMinutesAndSeconds(double seconds, string expected)
        {
            Assert.That(EndingText.PlayTime(seconds), Is.EqualTo(expected));
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void PlayTime_InvalidSeconds_Throws(double seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EndingText.PlayTime(seconds));
        }

        [Test]
        public void Text_RegressionCountsAndConstants_ReturnExpectedStrings()
        {
            Assert.That(EndingText.Regressions(3), Is.EqualTo("회귀 3회"));
            Assert.That(EndingText.Regressions(0), Is.EqualTo("회귀 0회"));
            Assert.That(EndingText.Title, Is.EqualTo("천마 토벌"));
            Assert.That(EndingText.ReturnButton, Is.EqualTo("처음으로"));
        }

        [Test]
        public void Text_CustomCulture_UsesInvariantNumbers()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                culture.NumberFormat.NegativeSign = "~";
                CultureInfo.CurrentCulture = culture;
                Assert.That(EndingText.PlayTime(3725), Is.EqualTo("플레이 시간 62:05"));
                Assert.That(EndingText.Regressions(-3), Is.EqualTo("회귀 -3회"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
