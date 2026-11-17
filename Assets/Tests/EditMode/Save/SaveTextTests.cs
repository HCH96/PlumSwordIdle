using NUnit.Framework;
using PlumSword.Core.Save;

namespace PlumSword.Core.Tests.Save
{
    public class SaveTextTests
    {
        [Test]
        public void TextConstants_MatchSpecifiedStrings()
        {
            Assert.That(SaveText.SaveFailed, Is.EqualTo("SAVE FAILED"));
            Assert.That(SaveText.CorruptTitle, Is.EqualTo("SAVE DATA IS CORRUPTED"));
            Assert.That(SaveText.CorruptMessage, Is.EqualTo("START A NEW GAME? THE OLD SAVE WILL BE REPLACED."));
            Assert.That(SaveText.NewGameButton, Is.EqualTo("NEW GAME"));
        }
    }
}
