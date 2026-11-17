namespace PlumSword.Core.StartMenu
{
    /// <summary>
    /// 시작 화면에 흩날리는 매화 꽃잎 한 장의 지금 상태. PetalField 가 움직이고, 화면 코드는 읽어서 그리기만 한다.
    /// 좌표는 꽃잎 영역 가운데가 원점, 오른쪽·위쪽이 +인 UI 단위(기준 해상도 픽셀)다.
    /// </summary>
    public sealed class Petal
    {
        internal Petal() { }

        public double X { get; internal set; }
        public double Y { get; internal set; }

        // 긴 변 길이 (UI 단위). 깊이가 가까울수록 크다.
        public double Size { get; internal set; }

        // 그림 종류 0 ~ variantCount-1.
        public int Variant { get; internal set; }

        // 화면 평면 회전 (도, 반시계 +).
        public double Rotation { get; internal set; }

        // 뒤집히며 도는 느낌을 내는 가로 배율. 부호는 앞뒷면, 크기는 MinFlipScale ~ 1.
        public double FlipScale { get; internal set; }

        // 불투명도. 멀수록(작을수록) 옅다.
        public double Alpha { get; internal set; }

        // 0 = 가장 멂, 1 = 가장 가까움.
        public double Depth { get; internal set; }

        // 화면 밖(위쪽·왼쪽 바깥 띠)에서 다시 떨어질 차례를 기다리는 중이면 true. 움직이지 않고, 화면 코드는 그리지 않는다.
        public bool IsWaiting { get; internal set; }

        internal bool FromLeft;
        internal double FallSpeed;
        internal double SwayAmplitude;
        internal double SwayRate;
        internal double SwayPhase;
        internal double SpinSpeed;
        internal double FlipRate;
        internal double FlipPhase;
    }
}
