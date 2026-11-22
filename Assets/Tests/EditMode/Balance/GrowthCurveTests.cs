using System;
using NUnit.Framework;
using PlumSword.Core.Balance;

namespace PlumSword.Core.Tests.Balance
{
    public class GrowthCurveTests
    {
        [Test] public void Evaluate_Linear_Returns35() => Assert.That(new GrowthCurve(20, 5, 1).Evaluate(3), Is.EqualTo  (35));
        [Test] public void Evaluate_Exponential_FloorsResult() => Assert.That(new GrowthCurve(10, 0, 1.15).Evaluate(3), Is.EqualTo(15));
        [Test] public void Evaluate_Overflow_Clamps() => Assert.That(new GrowthCurve(1, 0, 2).Evaluate(2000), Is.EqualTo(long.MaxValue));
        [Test] public void Evaluate_ZeroCurveWithOverflow_ReturnsZero() => Assert.That(new GrowthCurve(0, 0, 2).Evaluate(2000), Is.Zero);

        [TestCase(-1.0)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void Evaluate_InvalidInput_Throws(double n) => Assert.Throws<ArgumentOutOfRangeException>(() => new GrowthCurve(1, 0, 1).Evaluate(n));

        [TestCase(-1, 0, 1)] [TestCase(0, -1, 1)] [TestCase(0, 0, 0.9)]
        [TestCase(double.NaN, 0, 1)] [TestCase(0, double.PositiveInfinity, 1)] [TestCase(0, 0, double.NaN)]
        [TestCase(double.PositiveInfinity, 0, 1)] [TestCase(0, double.NaN, 1)] [TestCase(0, 0, double.PositiveInfinity)]
        public void Constructor_InvalidParameter_Throws(double b, double linear, double growth) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new GrowthCurve(b, linear, growth));
    }
}
