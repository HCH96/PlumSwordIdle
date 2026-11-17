using System;
using NUnit.Framework;
using PlumSword.Core.Save;

namespace PlumSword.Core.Tests.Save
{
    public class SaveLoadResultTests
    {
        [Test]
        public void Constructor_LoadedWithoutData_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SaveLoadResult(SaveLoadStatus.Loaded, null));
        }

        [Test]
        public void Constructor_EmptyWithData_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SaveLoadResult(SaveLoadStatus.Empty, new SaveData(0, 0, 0, 0, 1, 0, 0)));
        }

        [Test]
        public void Constructor_CorruptWithoutData_HasNoSaveData()
        {
            var result = new SaveLoadResult(SaveLoadStatus.Corrupt, null);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [Test]
        public void Constructor_LoadedWithData_ReturnsSameData()
        {
            var data = new SaveData(0, 0, 0, 0, 1, 0, 0);
            var result = new SaveLoadResult(SaveLoadStatus.Loaded, data);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data, Is.SameAs(data));
            Assert.That(result.HasSaveData, Is.True);
        }

        [Test]
        public void StatusValues_MatchSaveFormat()
        {
            Assert.That((int)SaveLoadStatus.Empty, Is.EqualTo(0));
            Assert.That((int)SaveLoadStatus.Loaded, Is.EqualTo(1));
            Assert.That((int)SaveLoadStatus.Corrupt, Is.EqualTo(2));
            Assert.That((int)SaveLoadStatus.UnsupportedVersion, Is.EqualTo(3));
        }
    }
}
