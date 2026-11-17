using System;
using NUnit.Framework;
using PlumSword.Core.Save;

namespace PlumSword.Core.Tests.Save
{
    public class MemorySaveStoreTests
    {
        [Test]
        public void Write_Text_IsStored()
        {
            var store = new MemorySaveStore();
            Assert.That(store.Text, Is.Null);
            Assert.That(store.TryWrite("abc"), Is.True);
            Assert.That(store.Text, Is.EqualTo("abc"));
        }

        [Test]
        public void Write_Null_ThrowsArgumentNull()
        {
            var store = new MemorySaveStore();
            Assert.Throws<ArgumentNullException>(() => store.TryWrite(null));
            Assert.That(store.Text, Is.Null);
        }
    }
}
