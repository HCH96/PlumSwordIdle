using System.Globalization;
using System.Threading;
using NUnit.Framework;
using PlumSword.Core.StartMenu;

namespace PlumSword.Core.Tests.StartMenu
{
    public class StartMenuTextTests
    {
        [Test]
        public void TitleAndStartButtons_Constants_MatchLabels()
        {
            Assert.That(StartMenuText.Title, Is.EqualTo("PLUM BLOSSOM SWORD"));
            Assert.That(StartMenuText.NewGameButton, Is.EqualTo("START"));
            Assert.That(StartMenuText.ContinueButton, Is.EqualTo("CONTINUE"));
        }

        [Test]
        public void SettingsLabels_Constants_MatchLabels()
        {
            Assert.That(StartMenuText.SettingsButton, Is.EqualTo("SETTINGS"));
            Assert.That(StartMenuText.SettingsTitle, Is.EqualTo("SETTINGS"));
        }

        [Test]
        public void ActionButtons_Constants_MatchLabels()
        {
            Assert.That(StartMenuText.ToTitleButton, Is.EqualTo("TO TITLE"));
            Assert.That(StartMenuText.QuitButton, Is.EqualTo("QUIT GAME"));
            Assert.That(StartMenuText.CloseButton, Is.EqualTo("CLOSE"));
        }

        [Test]
        public void LeaveFailedMessage_Constant_MatchesLabel()
        {
            Assert.That(StartMenuText.LeaveFailedMessage, Is.EqualTo("SAVE FAILED. TRY AGAIN."));
        }

        [TestCase(StartButtonKind.Continue, "CONTINUE")]
        [TestCase(StartButtonKind.NewGame, "START")]
        [TestCase((StartButtonKind)99, "START")]
        public void StartButtonLabel_Kind_ReturnsExpected(StartButtonKind kind, string expected)
        {
            Assert.That(StartMenuText.StartButtonLabel(kind), Is.EqualTo(expected));
        }

        [TestCase(80, "BGM 80")]
        [TestCase(100, "BGM 100")]
        public void BgmLine_Volume_ReturnsExpected(int volume, string expected)
        {
            Assert.That(StartMenuText.BgmLine(volume), Is.EqualTo(expected));
        }

        [TestCase(0, "SFX 0")]
        [TestCase(-5, "SFX -5")]
        public void SfxLine_Volume_ReturnsExpected(int volume, string expected)
        {
            Assert.That(StartMenuText.SfxLine(volume), Is.EqualTo(expected));
        }

        [Test]
        public void VolumeLines_CustomCulture_UseInvariantUnclampedNumbers()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            try
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Assert.That(StartMenuText.BgmLine(int.MinValue), Is.EqualTo("BGM -2147483648"));
                Assert.That(StartMenuText.SfxLine(int.MaxValue), Is.EqualTo("SFX 2147483647"));
                Assert.That(StartMenuText.SfxLine(-5), Is.EqualTo("SFX -5"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
