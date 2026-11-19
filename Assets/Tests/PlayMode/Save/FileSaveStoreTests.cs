using System;
using System.IO;
using NUnit.Framework;
using PlumSword.Core.Save;
using PlumSword.Game.Save;

namespace PlumSword.Game.Tests.Save
{
    public sealed class FileSaveStoreTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "PlumSwordSaveTest_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
            catch (Exception) { }
        }

        [Test]
        public void Constructor_NullOrEmptyDirectory_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => new FileSaveStore(null));
            Assert.Throws<ArgumentException>(() => new FileSaveStore(""));
        }

        [Test]
        public void Load_MissingDirectory_ReturnsEmpty()
        {
            var result = new FileSaveStore(directory).Load();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Empty));
            Assert.That(result.Data, Is.Null);
            Assert.That(Directory.Exists(directory), Is.False);
        }

        [Test]
        public void TryWrite_NewFile_WritesUtf8WithoutBomAndRemovesTemporaryFile()
        {
            var store = new FileSaveStore(directory);
            Assert.That(store.TryWrite("abc"), Is.True);
            string path = Path.Combine(directory, "save.dat");
            Assert.That(File.ReadAllText(path), Is.EqualTo("abc"));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 0x61, 0x62, 0x63 }));
            Assert.That(File.Exists(Path.Combine(directory, "save.tmp")), Is.False);
        }

        [Test]
        public void Load_InvalidThenReplacedSave_ReturnsCorruptThenLoaded()
        {
            var store = new FileSaveStore(directory);
            Assert.That(store.TryWrite("abc"), Is.True);
            Assert.That(store.Load().Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            var data = new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000L);
            Assert.That(store.TryWrite(SaveCodec.Encode(data)), Is.True);
            var result = store.Load();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.Gold, Is.EqualTo(1234));
            Assert.That(result.Data.AttackLevel, Is.EqualTo(3));
            Assert.That(result.Data.HealthLevel, Is.EqualTo(2));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(4));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(2));
            Assert.That(result.Data.StageNumber, Is.EqualTo(7));
            Assert.That(result.Data.SeenIntroStage, Is.EqualTo(5));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
            Assert.That(File.Exists(Path.Combine(directory, "save.tmp")), Is.False);
        }

        [Test]
        public void TryWrite_TemporaryPathIsDirectory_PreservesExistingFile()
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "save.dat");
            File.WriteAllText(path, "old");
            Directory.CreateDirectory(Path.Combine(directory, "save.tmp"));
            Assert.That(new FileSaveStore(directory).TryWrite("new"), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo("old"));
        }

        [Test]
        public void TryWrite_NullText_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FileSaveStore(directory).TryWrite(null));
        }
    }
}
