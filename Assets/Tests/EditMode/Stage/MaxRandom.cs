using System;
using System.Collections.Generic;

namespace PlumSword.Core.Tests.Stage
{
    /// <summary>
    /// 테스트용 난수. Next(minValue, maxValue) 가 항상 maxValue - 1 을 돌려주고,
    /// 받은 maxValue 를 순서대로 MaxValues 에 기록한다.
    /// </summary>
    internal sealed class MaxRandom : Random
    {
        private readonly List<int> _maxValues = new List<int>();

        public IReadOnlyList<int> MaxValues => _maxValues;

        public override int Next(int minValue, int maxValue)
        {
            _maxValues.Add(maxValue);
            return maxValue - 1;
        }
    }
}
