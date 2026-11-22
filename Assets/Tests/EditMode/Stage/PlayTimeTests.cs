using System;
using NUnit.Framework;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Stage
{
    public class PlayTimeTests
    {
        [Test]
        public void Add_ValidDeltas_AccumulatesSecondsAndMilliseconds()
        {
            var time = new PlayTime();
            Assert.That(time.Seconds, Is.EqualTo(0.0));
            time.Add(1.5);
            time.Add(1.5);
            time.Add(0.0);
            Assert.That(time.Seconds, Is.EqualTo(3.0));
            Assert.That(time.Milliseconds, Is.EqualTo(3000L));
        }

        [Test]
        public void Milliseconds_FractionalMilliseconds_RoundsDown()
        {
            Assert.That(new PlayTime(12.3456).Milliseconds, Is.EqualTo(12345L));
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Add_InvalidDelta_ThrowsAndPreservesTime(double delta)
        {
            var time = new PlayTime(12.5);
            Assert.Throws<ArgumentOutOfRangeException>(() => time.Add(delta));
            Assert.That(time.Seconds, Is.EqualTo(12.5));
        }

        [TestCase(-5.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Constructor_InvalidInitialSeconds_StartsAtZero(double seconds)
        {
            Assert.That(new PlayTime(seconds).Seconds, Is.EqualTo(0.0));
        }
    }
}
