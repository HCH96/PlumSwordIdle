using System;
using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.StatUpgrade
{
    public class StatUpgradeTextTests
    {
        // Name

        [TestCase(StatType.Attack, "ATK")]
        [TestCase(StatType.Health, "HP")]
        [TestCase(StatType.AttackSpeed, "SPD")]
        public void Name_DefinedStat_ReturnsEnglishAbbreviation(StatType stat, string expected)
        {
            Assert.That(StatUpgradeText.Name(stat), Is.EqualTo(expected));
        }

        [Test]
        public void Name_UndefinedStat_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StatUpgradeText.Name((StatType)99));
        }

        // Level

        [TestCase(0, "Lv.0")]
        [TestCase(3, "Lv.3")]
        [TestCase(10, "Lv.10")]
        [TestCase(1234, "Lv.1234")]
        public void Level_NonNegative_PrefixesLvWithoutComma(int level, string expected)
        {
            Assert.That(StatUpgradeText.Level(level), Is.EqualTo(expected));
        }

        [Test]
        public void Level_Negative_FormatsAsIsWithoutException()
        {
            string result = null;
            Assert.DoesNotThrow(() => result = StatUpgradeText.Level(-1));
            Assert.That(result, Is.EqualTo("Lv.-1"));
        }

        [Test]
        public void Level_CurrentCultureIsGerman_StillHasNoSeparator()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.That(StatUpgradeText.Level(1234), Is.EqualTo("Lv.1234"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        // Cost

        [TestCase(20L, "G 20")]
        [TestCase(1234L, "G 1,234")]
        public void Cost_NotMaxLevel_PrefixesGAndSpaceWithComma(long cost, string expected)
        {
            Assert.That(StatUpgradeText.Cost(cost, false), Is.EqualTo(expected));
        }

        [Test]
        public void Cost_MaxLevel_ReturnsMax()
        {
            Assert.That(StatUpgradeText.Cost(70, true), Is.EqualTo("MAX"));
        }

        [Test]
        public void Cost_NegativeNotMaxLevel_FormatsAsIsWithoutException()
        {
            string result = null;
            Assert.DoesNotThrow(() => result = StatUpgradeText.Cost(-5, false));
            Assert.That(result, Is.EqualTo("G -5"));
        }

        // ButtonLabel

        [TestCase(StatType.Attack, 3, 35L, false, "ATK\nLv.3\nG 35")]
        [TestCase(StatType.Health, 0, 20L, false, "HP\nLv.0\nG 20")]
        [TestCase(StatType.AttackSpeed, 10, 70L, true, "SPD\nLv.10\nMAX")]
        public void ButtonLabel_Values_JoinsNameLevelCostWithNewline(
            StatType stat, int level, long cost, bool isMaxLevel, string expected)
        {
            Assert.That(StatUpgradeText.ButtonLabel(stat, level, cost, isMaxLevel), Is.EqualTo(expected));
        }

        [Test]
        public void ButtonLabel_UndefinedStat_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => StatUpgradeText.ButtonLabel((StatType)99, 0, 20, false));
        }
    }
}
