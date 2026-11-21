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
        public void FinalDisplayStage_IsFive()
        {
            Assert.That(StageLayout.FinalDisplayStage, Is.EqualTo(5));
        }

        [Test]
        public void Constants_AreSpecValues()
        {
            Assert.That(StageLayout.NormalCount, Is.EqualTo(10));
            Assert.That(StageLayout.MaxGroupSize, Is.EqualTo(3));
            Assert.That(StageLayout.FirstGroupPosition, Is.EqualTo(6.0).Within(Tolerance));
            Assert.That(StageLayout.GroupSpacing, Is.EqualTo(18.0).Within(Tolerance));
            Assert.That(StageLayout.InGroupSpacing, Is.EqualTo(2.0).Within(Tolerance));
            Assert.That(StageLayout.BossDistance, Is.EqualTo(24.0).Within(Tolerance));
            Assert.That(StageLayout.EncounterDistance, Is.EqualTo(24.0).Within(Tolerance));
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

            AssertPositions(positions, 5.0, 7.0, 24.0, 40.0, 42.0, 44.0, 60.0, 76.0, 78.0, 80.0);
        }

        [Test]
        public void Positions_ThreeThreeThreeOne_ReturnsCenteredGroups()
        {
            double[] positions = StageLayout.Positions(new[] { 3, 3, 3, 1 });

            AssertPositions(positions, 4.0, 6.0, 8.0, 22.0, 24.0, 26.0, 40.0, 42.0, 44.0, 60.0);
        }

        [Test]
        public void Positions_TenOnes_ReturnsGroupCenters()
        {
            double[] positions = StageLayout.Positions(Enumerable.Repeat(1, 10).ToArray());

            AssertPositions(positions, 6.0, 24.0, 42.0, 60.0, 78.0, 96.0, 114.0, 132.0, 150.0, 168.0);
        }

        [Test]
        public void Positions_AdjacentFullGroups_NextGroupMayAppearOnWideScreen()
        {
            double[] positions = StageLayout.Positions(new[] { 3, 3 });
            // 20:9에서는 화면 폭 25유닛, 카메라는 제자보다 오른쪽 4유닛이다.
            // 가장 오른쪽 상대와 붙은 제자의 위치를 보수적으로 그 상대 위치로 잡는다.
            double screenRight = positions[2] + 4.0 + 12.5;
            // 적 표시 오프셋 -1.85, 가장 넓은 공격 칸의 절반(96px × 1.5 / 32 / 2).
            double nextGroupLeft = positions[3] - 1.85 - (96.0 * 1.5 / 32.0 / 2.0);
            Assert.That(nextGroupLeft, Is.LessThan(screenRight));
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
