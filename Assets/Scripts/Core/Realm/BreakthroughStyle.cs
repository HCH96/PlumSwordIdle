namespace PlumSword.Core.Realm
{
    // 경지별 돌파 연출 구성. 생성자는 값을 검사하지 않는다.
    public readonly struct BreakthroughStyle
    {
        public BreakthroughStyle(bool hasRing, bool hasPillar, bool hasShockwave, bool hasFlash,
            int burstPetals, double dimAlpha, int shakePixels, double lightPeak,
            double ringRadius, double pillarHeight, int auraWisps, int auraPetals)
        {
            HasRing = hasRing;
            HasPillar = hasPillar;
            HasShockwave = hasShockwave;
            HasFlash = hasFlash;
            BurstPetals = burstPetals;
            DimAlpha = dimAlpha;
            ShakePixels = shakePixels;
            LightPeak = lightPeak;
            RingRadius = ringRadius;
            PillarHeight = pillarHeight;
            AuraWisps = auraWisps;
            AuraPetals = auraPetals;
        }

        public bool HasRing { get; }
        public bool HasPillar { get; }
        public bool HasShockwave { get; }
        public bool HasFlash { get; }
        public int BurstPetals { get; }
        public double DimAlpha { get; }
        public int ShakePixels { get; }
        public double LightPeak { get; }
        public double RingRadius { get; }
        public double PillarHeight { get; }
        public int AuraWisps { get; }
        public int AuraPetals { get; }
    }
}
