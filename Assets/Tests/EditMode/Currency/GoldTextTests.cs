using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.Currency;

namespace PlumSword.Core.Tests.Currency
{
    public class GoldTextTests
    {
        // FormatNumber

        [TestCase(0L, "0")]
        [TestCase(150L, "150")]
        [TestCase(999L, "999")]
        [TestCase(1000L, "1,000")]
        [TestCase(1234L, "1,234")]
        [TestCase(28800L, "28,800")]
        [TestCase(1234567L, "1,234,567")]
        public void FormatNumber_PositiveOrZero_InsertsCommaEveryThreeDigits(long amount, string expected)
        {
            Assert.That(GoldText.FormatNumber(amount), Is.EqualTo(expected));
        }

        [Test]
        public void FormatNumber_LongMaxValue_FormatsAllDigits()
        {
            Assert.That(GoldText.FormatNumber(long.MaxValue), Is.EqualTo("9,223,372,036,854,775,807"));
        }

        [Test]
        public void FormatNumber_Negative_PrefixesMinusWithoutException()
        {
            string result = null;
            Assert.DoesNotThrow(() => result = GoldText.FormatNumber(-1234));
            Assert.That(result, Is.EqualTo("-1,234"));
        }

        [Test]
        public void FormatNumber_CurrentCultureIsGerman_StillUsesComma()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(GoldText.FormatNumber(1234), Is.EqualTo("1,234"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // Format

        [TestCase(0L, "G 0")]
        [TestCase(1234L, "G 1,234")]
        [TestCase(28800L, "G 28,800")]
        public void Format_Amount_PrefixesGAndSpace(long amount, string expected)
        {
            Assert.That(GoldText.Format(amount), Is.EqualTo(expected));
        }
    }
}
