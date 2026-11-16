using System;
using System.Linq;
using NUnit.Framework;
using PlumSword.Core.Stage;

namespace PlumSword.Core.Tests.Stage
{
    public class StageLayoutTests
    {
        private const double Tolerance = 1e-6;

        // 상수

        [Test]
        public void Constants_AreSpecValues()
        {
            Assert.That(StageLayout.NormalCount, Is.EqualTo(10));
            Assert.That(StageLayout.MaxGroupSize, Is.EqualTo(3));
            Assert.That(StageLayout.FirstGroupPosition, Is.EqualTo(6.0).Within(Tolerance));
            Assert.That(StageLayout.GroupSpacing, Is.EqualTo(3.0).Within(Tolerance));
            Assert.That(StageLayout.InGroupSpacing, Is.EqualTo(0.3).Within(Tolerance));
            Assert.That(StageLayout.BossDistance, Is.EqualTo(18.0).Within(Tolerance));
        }

        // RollGroups

        [Test]
        public void RollGroups_MaxRandom_ReturnsThreeThreeThreeOne()
        {
            var random = new MaxRandom();

            int[] groups = StageLayout.RollGroups(random);

            Assert.That(groups, Is.EqualTo(new[] { 3, 3, 3, 1 }));
            Assert.That(random.MaxValues, Is.EqualTo(new[] { 4, 4, 4, 2 }));
        }

        [Test]
        public void RollGroups_MinRandom_ReturnsTenOnes()
        {
            int[] groups = StageLayout.RollGroups(new MinRandom());

            Assert.That(groups, Is.EqualTo(Enumerable.Repeat(1, 10).ToArray()));
        }

        [Test]
        public void RollGroups_Seeds0To999_SumTenSizesOneToThreeLengthFourToTen()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                int[] groups = StageLayout.RollGroups(new Random(seed));

                Assert.That(groups.Sum(), Is.EqualTo(10), $"seed {seed}");
                Assert.That(groups, Has.All.InRange(1, 3), $"seed {seed}");
                Assert.That(groups.Length, Is.InRange(4, 10), $"seed {seed}");
            }
        }

        [Test]
        public void RollGroups_Null_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => StageLayout.RollGroups(null));
        }

        // Positions

        [Test]
        public void Positions_TwoOneThreeOneThree_ReturnsSpecExample()
        {
            double[] positions = StageLayout.Positions(new[] { 2, 1, 3, 1, 3 });

            AssertPositions(positions, 5.85, 6.15, 9.0, 11.7, 12.0, 12.3, 15.0, 17.7, 18.0, 18.3);
        }

        [Test]
        public void Positions_ThreeThreeThreeOne_ReturnsCenteredGroups()
        {
            double[] positions = StageLayout.Positions(new[] { 3, 3, 3, 1 });

            AssertPositions(positions, 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0);
        }

        [Test]
        public void Positions_TenOnes_ReturnsGroupCenters()
        {
            double[] positions = StageLayout.Positions(Enumerable.Repeat(1, 10).ToArray());

            AssertPositions(positions, 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, 30.0, 33.0);
        }

        [Test]
        public void Positions_Empty_ReturnsEmptyArray()
        {
            double[] positions = StageLayout.Positions(new int[0]);

            Assert.That(positions, Is.Not.Null);
            Assert.That(positions.Length, Is.EqualTo(0));
        }

        [Test]
        public void Positions_Null_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => StageLayout.Positions(null));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void Positions_SizeOutOfRange_ThrowsArgumentOutOfRange(int size)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StageLayout.Positions(new[] { size }));
        }

        private static void AssertPositions(double[] actual, params double[] expected)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(Tolerance), $"index {i}");
            }
        }
    }
}
