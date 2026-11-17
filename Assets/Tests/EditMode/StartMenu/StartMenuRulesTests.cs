using NUnit.Framework;
using PlumSword.Core.Save;
using PlumSword.Core.StartMenu;

namespace PlumSword.Core.Tests.StartMenu
{
    public class StartMenuRulesTests
    {
        [TestCase(SaveLoadStatus.Loaded, StartButtonKind.Continue)]
        [TestCase(SaveLoadStatus.Empty, StartButtonKind.NewGame)]
        [TestCase(SaveLoadStatus.UnsupportedVersion, StartButtonKind.NewGame)]
        [TestCase(SaveLoadStatus.Corrupt, StartButtonKind.NewGame)]
        [TestCase((SaveLoadStatus)99, StartButtonKind.NewGame)]
        public void StartButton_Status_ReturnsExpectedButton(SaveLoadStatus status, StartButtonKind expected)
        {
            Assert.That(StartMenuRules.StartButton(status), Is.EqualTo(expected));
        }

        [TestCase(StartEntry.Launch, false, true)]
        [TestCase(StartEntry.Launch, true, false)]
        [TestCase(StartEntry.ResumeInGame, false, false)]
        [TestCase(StartEntry.ResumeInGame, true, false)]
        [TestCase(StartEntry.ResumeOnTitle, false, true)]
        [TestCase(StartEntry.ResumeOnTitle, true, true)]
        [TestCase(StartEntry.TitleReturn, false, true)]
        [TestCase(StartEntry.TitleReturn, true, true)]
        [TestCase((StartEntry)99, false, true)]
        [TestCase((StartEntry)99, true, true)]
        public void ShowsStartScreen_EntryAndBatchMode_ReturnsExpected(StartEntry entry, bool batch, bool expected)
        {
            Assert.That(StartMenuRules.ShowsStartScreen(entry, batch), Is.EqualTo(expected));
        }

        [TestCase(StartEntry.Launch, true)]
        [TestCase(StartEntry.ResumeInGame, true)]
        [TestCase(StartEntry.ResumeOnTitle, true)]
        [TestCase(StartEntry.TitleReturn, false)]
        [TestCase((StartEntry)99, true)]
        public void CalculatesOfflineReward_Entry_ReturnsExpected(StartEntry entry, bool expected)
        {
            Assert.That(StartMenuRules.CalculatesOfflineReward(entry), Is.EqualTo(expected));
        }

        [TestCase(SaveLoadStatus.Loaded, false, true)]
        [TestCase(SaveLoadStatus.Loaded, true, false)]
        [TestCase(SaveLoadStatus.Empty, false, false)]
        [TestCase(SaveLoadStatus.Empty, true, false)]
        [TestCase(SaveLoadStatus.UnsupportedVersion, false, false)]
        [TestCase(SaveLoadStatus.Corrupt, false, false)]
        [TestCase((SaveLoadStatus)99, false, false)]
        public void AutoSaveStartsEnabled_StatusAndReward_ReturnsExpected(SaveLoadStatus status, bool reward, bool expected)
        {
            Assert.That(StartMenuRules.AutoSaveStartsEnabled(status, reward), Is.EqualTo(expected));
        }

        [TestCase(true, StartEntry.ResumeOnTitle)]
        [TestCase(false, StartEntry.ResumeInGame)]
        public void ResumeEntry_ScreenState_ReturnsExpected(bool showing, StartEntry expected)
        {
            Assert.That(StartMenuRules.ResumeEntry(showing), Is.EqualTo(expected));
        }

        [TestCase(true, false, false, BackKeyAction.CloseSettings)]
        [TestCase(true, false, true, BackKeyAction.CloseSettings)]
        [TestCase(true, true, false, BackKeyAction.CloseSettings)]
        [TestCase(true, true, true, BackKeyAction.CloseSettings)]
        [TestCase(false, false, true, BackKeyAction.OpenSettings)]
        [TestCase(false, false, false, BackKeyAction.None)]
        [TestCase(false, true, false, BackKeyAction.None)]
        [TestCase(false, true, true, BackKeyAction.None)]
        public void BackKey_State_ReturnsExpected(bool settingsOpen, bool showing, bool begun, BackKeyAction expected)
        {
            Assert.That(StartMenuRules.BackKey(settingsOpen, showing, begun), Is.EqualTo(expected));
        }

        [Test]
        public void EnumValues_MatchPublicContract()
        {
            Assert.That((int)StartEntry.Launch, Is.EqualTo(0));
            Assert.That((int)StartEntry.ResumeInGame, Is.EqualTo(1));
            Assert.That((int)StartEntry.ResumeOnTitle, Is.EqualTo(2));
            Assert.That((int)StartEntry.TitleReturn, Is.EqualTo(3));
            Assert.That((int)StartButtonKind.NewGame, Is.EqualTo(0));
            Assert.That((int)StartButtonKind.Continue, Is.EqualTo(1));
            Assert.That((int)SettingsMode.Title, Is.EqualTo(0));
            Assert.That((int)SettingsMode.InGame, Is.EqualTo(1));
            Assert.That((int)BackKeyAction.None, Is.EqualTo(0));
            Assert.That((int)BackKeyAction.OpenSettings, Is.EqualTo(1));
            Assert.That((int)BackKeyAction.CloseSettings, Is.EqualTo(2));
        }
    }
}
