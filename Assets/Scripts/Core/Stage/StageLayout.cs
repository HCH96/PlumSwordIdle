using System;
using System.Collections.Generic;

namespace PlumSword.Core.Stage
{
    /// <summary>
    /// 한 스테이지의 일반 몬스터를 무리로 나누고 좌표로 바꾼다. 상태가 없는 정적 메서드만 가진다.
    /// NormalCount 를 뺀 수치는 임시값이다 (docs/spec/stage.md "수치").
    /// </summary>
    public static class StageLayout
    {
        public const int NormalCount = 10;
        public const int MaxGroupSize = 3;
        public const double FirstGroupPosition = 6.0;
        // 새 무리는 화면 밖에서 접근한다. FirstGroupPosition은 기존 Positions API 호환값이다.
        public const double EncounterDistance = 24.0;
        public const double GroupSpacing = 18.0;
        public const double InGroupSpacing = 2.0;
        public const double BossDistance = 24.0;
        public const int NormalSubstageCount = 6;
        public const int MinSubstageEnemies = 2;
        /// <summary>일반 구간 없이 최종 보스만 나오는 화면상 큰 스테이지 번호.</summary>
        public const int FinalDisplayStage = 5;

        /// <summary>작은 스테이지 하나의 일반 상대 수와 2유닛 간격을 정한다.</summary>
        public static double[] SubstagePositions(Random random, double center)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            int count = random.Next(MinSubstageEnemies, MaxGroupSize + 1);
            var positions = new double[count];
            for (int i = 0; i < count; i++)
                positions[i] = center + (i - (count - 1) / 2.0) * InGroupSpacing;
            return positions;
        }

        /// <summary>
        /// 무리별 마리 수를 정한다. 남은 수가 0 이 될 때까지
        /// random.Next(1, Math.Min(MaxGroupSize, 남은 수) + 1) 을 무리 하나에 한 번씩 부른다.
        /// 합은 정확히 NormalCount 다. random 이 null 이면 ArgumentNullException.
        /// </summary>
        public static int[] RollGroups(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var groups = new List<int>();
            int remaining = NormalCount;
            while (remaining > 0)
            {
                int size = random.Next(1, Math.Min(MaxGroupSize, remaining) + 1);
                groups.Add(size);
                remaining -= size;
            }

            return groups.ToArray();
        }

        /// <summary>
        /// 무리 구성을 좌표로 바꾼다. g 번째 무리 중심 = FirstGroupPosition + GroupSpacing * g,
        /// n 마리 무리의 i 번째 = 중심 + (i - (n - 1) / 2.0) * InGroupSpacing.
        /// 무리 순서대로, 무리 안에서는 작은 좌표부터 돌려준다.
        /// groups 가 null 이면 ArgumentNullException, 원소가 1~MaxGroupSize 밖이면 ArgumentOutOfRangeException.
        /// </summary>
        public static double[] Positions(IReadOnlyList<int> groups)
        {
            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            int total = 0;
            for (int g = 0; g < groups.Count; g++)
            {
                int n = groups[g];
                if (n < 1 || n > MaxGroupSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(groups), n, "each group size must be between 1 and MaxGroupSize.");
                }

                total += n;
            }

            var result = new double[total];
            int index = 0;
            for (int g = 0; g < groups.Count; g++)
            {
                int n = groups[g];
                double center = FirstGroupPosition + GroupSpacing * g;
                for (int i = 0; i < n; i++)
                {
                    result[index++] = center + (i - (n - 1) / 2.0) * InGroupSpacing;
                }
            }

            return result;
        }
    }
}
