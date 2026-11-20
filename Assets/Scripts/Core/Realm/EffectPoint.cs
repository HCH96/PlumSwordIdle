namespace PlumSword.Core.Realm
{
    // 위치(오오라 가운데 기준, 유닛)·크기 배율·알파 한 점. 생성자는 값을 검사하지 않는다.
    public readonly struct EffectPoint
    {
        public EffectPoint(double x, double y, double scale, double alpha)
        {
            X = x;
            Y = y;
            Scale = scale;
            Alpha = alpha;
        }

        public double X { get; }
        public double Y { get; }
        public double Scale { get; }
        public double Alpha { get; }
    }
}
