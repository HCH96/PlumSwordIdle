using System;
using System.Collections.Generic;

namespace PlumSword.Core.StartMenu
{
    /// <summary>
    /// 시작 화면 매화 꽃잎 흩날림 (docs/spec/start_menu.md "꽃잎"). 꽃잎마다 깊이를 정해 크기·속도·불투명도를 맞추고,
    /// 바람(천천히 세졌다 약해짐)에 실려 오른쪽 아래로 떨어지며 좌우로 흔들리고, 돌고, 뒤집힌다.
    /// 영역 밖으로 나간 꽃잎은 위쪽 또는 왼쪽(바람이 불어오는 쪽) 바깥에서 기다리다가, 일정한 간격(EmitInterval, 흔들림 있음)으로
    /// 한 장씩 다시 떨어진다. 그래서 몰려오거나 한동안 끊기지 않고 꾸준히 새 꽃잎이 들어온다.
    /// 처음 만들 때 PrewarmSeconds 만큼 미리 움직여 첫 화면부터 그 흐름의 중간 상태로 시작한다. 수치는 모두 임시다.
    /// </summary>
    public sealed class PetalField
    {
        public const int DefaultCount = 50;
        public const double MinSize = 7, MaxSize = 14;
        public const double MinFallSpeed = 12, MaxFallSpeed = 26;
        public const double MinAlpha = 0.55, MaxAlpha = 1.0;
        public const double MinSwayAmplitude = 6, MaxSwayAmplitude = 16;
        public const double MinSwayRate = 0.8, MaxSwayRate = 1.8;
        public const double MinSpinSpeed = 20, MaxSpinSpeed = 70;
        public const double MinFlipRate = 1.0, MaxFlipRate = 3.0;
        public const double MinFlipScale = 0.25;
        public const double BaseWind = 8, GustWind = 6, FlutterWind = 3;
        public const double Margin = 16;
        public const double SpawnBand = 24;
        public const double MaxStep = 0.1;
        public const double PrewarmSeconds = 40;
        // 기다리는 꽃잎을 내보내는 평균 간격(초)과 흔들림 (간격 × MinEmitJitter ~ MaxEmitJitter).
        public const double EmitInterval = 0.7;
        public const double MinEmitJitter = 0.5, MaxEmitJitter = 1.5;
        public const double EdgeJitter = 4;
        // 왼쪽에서 다시 나타날 때 세로 위치: 아래에서 LeftSpawnBottom × 높이 위 ~ 위 끝 (아래쪽에서 들어와 바로 나가지 않게).
        public const double LeftSpawnBottom = 0.25;

        private readonly Random random;
        private readonly int variantCount;
        private readonly List<Petal> petals = new List<Petal>();
        private readonly Queue<Petal> waiting = new Queue<Petal>();
        private double emitTimer;
        private bool emitDue;

        public PetalField(double width, double height, int count, int variantCount, Random random)
        {
            CheckSize(width, height);
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (variantCount < 1) throw new ArgumentOutOfRangeException(nameof(variantCount));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.variantCount = variantCount;
            Width = width;
            Height = height;
            for (int i = 0; i < count; i++)
            {
                var petal = new Petal();
                Spawn(petal);
                // 처음에는 화면 전체에 흩어 둔다.
                petal.X = Lerp(-width / 2, width / 2, random.NextDouble());
                petal.Y = Lerp(-height / 2, height / 2, random.NextDouble());
                petals.Add(petal);
            }
            emitTimer = NextEmitInterval();
            // 미리 움직여 꽃잎이 나가고 들어오는 흐름이 자리 잡은 상태로 시작한다.
            for (double t = 0; t < PrewarmSeconds; t += MaxStep) Tick(MaxStep);
            Elapsed = 0;
        }

        public double Width { get; private set; }
        public double Height { get; private set; }
        public double Elapsed { get; private set; }
        public IReadOnlyList<Petal> Petals => petals;

        // 화면 밖에서 차례를 기다리는 꽃잎 수.
        public int WaitingCount => waiting.Count;

        // 지금 바람 세기 (UI 단위/초, 오른쪽 +). BaseWind ± (GustWind + FlutterWind) 사이, 즉 −1 ~ 17.
        public double Wind => BaseWind + GustWind * Math.Sin(Elapsed * 0.35) + FlutterWind * Math.Sin(Elapsed * 0.9 + 1.0);

        // 영역 크기가 바뀌면 부른다 (화면 비율 변경, 첫 프레임의 캔버스 크기 조정). 꽃잎 위치를 같은 비율로 늘이거나 줄여
        // 흩어진 모양을 그대로 둔다. 크기·속도는 바꾸지 않는다.
        public void Resize(double width, double height)
        {
            CheckSize(width, height);
            double sx = width / Width, sy = height / Height;
            foreach (var petal in petals)
            {
                petal.X *= sx;
                petal.Y *= sy;
            }
            Width = width;
            Height = height;
        }

        // deltaSeconds 가 0 이하, NaN, 무한이면 아무것도 하지 않는다. MaxStep 보다 길면 MaxStep 만큼만 움직인다.
        public void Tick(double deltaSeconds)
        {
            if (!(deltaSeconds > 0) || double.IsInfinity(deltaSeconds)) return;
            double dt = Math.Min(deltaSeconds, MaxStep);
            Elapsed += dt;
            double wind = Wind;
            foreach (var petal in petals)
            {
                if (petal.IsWaiting) continue;
                petal.SwayPhase += petal.SwayRate * dt;
                double sin = Math.Sin(petal.SwayPhase);
                // 옆으로 크게 흔들릴 때(sin² 작음)는 덜 떨어진다. 나뭇잎처럼 미끄러지는 느낌.
                double fall = petal.FallSpeed * (0.6 + 0.4 * sin * sin);
                double sway = petal.SwayAmplitude * petal.SwayRate * Math.Cos(petal.SwayPhase);
                petal.X += (wind * (0.5 + 0.5 * petal.Depth) + sway) * dt;
                petal.Y -= fall * dt;
                petal.Rotation = Wrap(petal.Rotation + petal.SpinSpeed * dt);
                petal.FlipPhase += petal.FlipRate * dt;
                petal.FlipScale = FlipScaleOf(petal.FlipPhase);

                if (petal.Y < -Height / 2 - Margin || petal.X > Width / 2 + Margin || petal.X < -Width / 2 - Margin * 4)
                {
                    Spawn(petal);
                    petal.IsWaiting = true;
                    waiting.Enqueue(petal);
                }
            }

            // 간격이 되면 기다리는 꽃잎을 한 장 내보낸다. 그때 기다리는 꽃잎이 없으면, 다음에 나가는 꽃잎을 바로 내보낸다.
            emitTimer -= dt;
            if (emitTimer <= 0)
            {
                emitDue = true;
                emitTimer += NextEmitInterval();
                if (emitTimer <= 0) emitTimer = NextEmitInterval();
            }
            if (emitDue && waiting.Count > 0)
            {
                Release(waiting.Dequeue());
                emitDue = false;
            }
        }

        // 기다리던 자리에서 화면 가장자리 바로 바깥(꽃잎 크기의 0.6배 + 0~EdgeJitter)으로 옮겨 떨어지기 시작한다.
        // 그래서 내보낸 뒤 화면에 보이기까지 걸리는 시간이 짧고 고르다 (내보내는 간격이 그대로 보이는 간격이 된다).
        private void Release(Petal petal)
        {
            double offset = petal.Size * 0.6 + random.NextDouble() * EdgeJitter;
            if (petal.FromLeft) petal.X = -Width / 2 - offset;
            else petal.Y = Height / 2 + offset;
            petal.IsWaiting = false;
        }

        private double NextEmitInterval() => EmitInterval * Lerp(MinEmitJitter, MaxEmitJitter, random.NextDouble());

        // 기다리는 자리이자 다시 떨어지기 시작하는 자리: 위쪽 바깥 띠(Height/2 + Margin ~ + SpawnBand) 또는 왼쪽 바깥 띠(−Width/2 − Margin ~ − SpawnBand)에서 다시 나타난다.
        // 어느 쪽인지는 그 변으로 들어오는 양(위: 떨어지는 속도 × 폭, 왼쪽: 바람 × 높이)에 비례해 고른다. LeftShare 참고.
        private void Spawn(Petal petal)
        {
            double depth = random.NextDouble();
            petal.Depth = depth;
            petal.Size = Lerp(MinSize, MaxSize, depth);
            petal.FallSpeed = Lerp(MinFallSpeed, MaxFallSpeed, depth) * Lerp(0.85, 1.15, random.NextDouble());
            petal.Alpha = Lerp(MinAlpha, MaxAlpha, depth);
            petal.Variant = random.Next(variantCount);
            petal.SwayAmplitude = Lerp(MinSwayAmplitude, MaxSwayAmplitude, random.NextDouble());
            petal.SwayRate = Lerp(MinSwayRate, MaxSwayRate, random.NextDouble());
            petal.SwayPhase = random.NextDouble() * Math.PI * 2;
            petal.SpinSpeed = Lerp(MinSpinSpeed, MaxSpinSpeed, random.NextDouble()) * (random.Next(2) == 0 ? -1 : 1);
            petal.FlipRate = Lerp(MinFlipRate, MaxFlipRate, random.NextDouble());
            petal.FlipPhase = random.NextDouble() * Math.PI * 2;
            petal.FlipScale = FlipScaleOf(petal.FlipPhase);
            petal.Rotation = random.NextDouble() * 360;
            petal.FromLeft = random.NextDouble() < LeftShare;
            if (petal.FromLeft)
            {
                petal.X = -Width / 2 - Margin - random.NextDouble() * SpawnBand;
                petal.Y = Lerp(-Height / 2 + Height * LeftSpawnBottom, Height / 2, random.NextDouble());
            }
            else
            {
                petal.X = Lerp(-Width / 2, Width / 2, random.NextDouble());
                petal.Y = Height / 2 + Margin + random.NextDouble() * SpawnBand;
            }
        }

        // 다시 나타날 때 왼쪽 변을 고를 몫. 왼쪽 들어오는 양 = 평균 바람 × 평균 바람 받는 정도(0.75) × 높이,
        // 위 들어오는 양 = 평균 떨어지는 속도 × 평균 감속(0.8) × 폭.
        public double LeftShare
        {
            get
            {
                double left = BaseWind * 0.75 * Height * (1 - LeftSpawnBottom);
                double top = (MinFallSpeed + MaxFallSpeed) / 2 * 0.8 * Width;
                return left / (left + top);
            }
        }

        private static double FlipScaleOf(double phase)
        {
            double c = Math.Cos(phase);
            double magnitude = MinFlipScale + (1 - MinFlipScale) * Math.Abs(c);
            return c < 0 ? -magnitude : magnitude;
        }

        private static double Wrap(double degrees)
        {
            degrees %= 360;
            return degrees < 0 ? degrees + 360 : degrees;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static void CheckSize(double width, double height)
        {
            if (!(width > 0) || double.IsInfinity(width)) throw new ArgumentOutOfRangeException(nameof(width));
            if (!(height > 0) || double.IsInfinity(height)) throw new ArgumentOutOfRangeException(nameof(height));
        }
    }
}
