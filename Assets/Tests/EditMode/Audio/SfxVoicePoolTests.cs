using System;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class SfxVoicePoolTests
    {
        [Test]
        public void Constructor_ValidatesCountAndDefaultsToEight()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfxVoicePool(0));
            Assert.AreEqual(8, new SfxVoicePool().Count);
        }

        [Test]
        public void Acquire_UsesFreeThenOldestVoice()
        {
            var pool = new SfxVoicePool(2);
            Assert.AreEqual(0, pool.Acquire(0, 1));
            Assert.AreEqual(1, pool.Acquire(0.1, 1));
            Assert.AreEqual(0, pool.Acquire(0.2, 1));
            Assert.AreEqual(1, pool.Acquire(0.3, 1));
            Assert.AreEqual(0, pool.Acquire(2, 1));
        }

        [Test]
        public void Acquire_TiedStartsChooseLowestIndex()
        {
            var pool = new SfxVoicePool();
            for (int i = 0; i < 8; i++) Assert.AreEqual(i, pool.Acquire(0, 1));
            Assert.AreEqual(0, pool.Acquire(0.5, 1));
        }

        [Test]
        public void InvalidArguments_ThrowWithoutChangingState()
        {
            var pool = new SfxVoicePool(2);
            Assert.AreEqual(0, pool.Acquire(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Acquire(double.NaN, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Acquire(double.PositiveInfinity, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Acquire(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Acquire(0, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => pool.Acquire(0, double.NaN));
            Assert.AreEqual(1, pool.Acquire(0, 1));
            Assert.AreEqual(0, pool.Acquire(1, 1));
        }

        [Test]
        public void EmptyVoices_AreAvailableAtNegativeTimeAndZeroLengthEndsImmediately()
        {
            var pool = new SfxVoicePool(2);
            Assert.AreEqual(0, pool.Acquire(-2, 0));
            Assert.AreEqual(0, pool.Acquire(-2, 1));
            Assert.AreEqual(1, pool.Acquire(-2, 1));
        }
    }
}
