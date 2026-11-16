using System;

namespace PlumSword.Core.Tests.Stage
{
    /// <summary>
    /// 테스트용 난수. Next(minValue, maxValue) 가 항상 minValue 를 돌려준다.
    /// </summary>
    internal sealed class MinRandom : Random
    {
        public override int Next(int minValue, int maxValue)
        {
            return minValue;
        }
    }
}
