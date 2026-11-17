using System;
using System.Linq;
using NUnit.Framework;
using PlumSword.Core.StartMenu;

namespace PlumSword.Core.Tests.StartMenu
{
    public class PetalFieldTests
    {
        private const double Width = 640, Height = 360;
        private const double Frame = 1.0 / 30;
        private const double MaxDrop = PetalField.MaxFallSpeed * 1.15 * Frame + 1e-9;

        private static PetalField Create(int seed = 1, int count = PetalField.DefaultCount, int variants = 3) =>
            new PetalField(Width, Height, count, variants, new Random(seed));

        private static double[] Snapshot(PetalField field) =>
            field.Petals.SelectMany(p => new[] { p.X, p.Y, p.Size, p.Variant, p.Rotation, p.FlipScale, p.Alpha, p.Depth }).ToArray();

        private static bool Visible(Petal p, PetalField field) => Math.Abs(p.X) <= field.Width / 2 && Math.Abs(p.Y) <= field.Height / 2;

        // 다시 나타나는(기다리는·내보내지는) 자리: 위쪽 바깥, 또는 왼쪽 바깥. 화면 끝에서 Margin + SpawnBand 안.
        private static bool AtSpawn(Petal p, PetalField field)
        {
            double top = field.Height / 2, left = -field.Width / 2, band = PetalField.Margin + PetalField.SpawnBand;
            bool topBand = p.Y > top && p.Y <= top + band && Math.Abs(p.X) <= field.Width / 2;
            bool leftBand = p.X < left && p.X >= left - band
                && p.Y >= -field.Height / 2 + field.Height * PetalField.LeftSpawnBottom && p.Y <= field.Height / 2;
            return topBand || leftBand;
        }

        // 내보낸 꽃잎은 화면 가장자리 바로 바깥에서 떨어지기 시작한다 (기다리던 띠보다 가깝다).
        [Test]
        public void Tick_ReleasedPetalStartsJustOutsideEdge()
        {
            var field = Create(seed: 9);
            int checkedCount = 0;
            for (int frame = 0; frame < 30 * 30; frame++)
            {
                var wasWaiting = field.Petals.Select(p => p.IsWaiting).ToArray();
                field.Tick(Frame);
                for (int i = 0; i < wasWaiting.Length; i++)
                {
                    var p = field.Petals[i];
                    if (!wasWaiting[i] || p.IsWaiting) continue;
                    checkedCount++;
                    Assert.That(Visible(p, field), Is.False, "still hidden right after release");
                    double gap = p.Y > field.Height / 2 ? p.Y - field.Height / 2 : -field.Width / 2 - p.X;
                    Assert.That(gap, Is.LessThanOrEqualTo(p.Size * 0.6 + PetalField.EdgeJitter), "just outside the edge");
                }
            }
            Assert.That(checkedCount, Is.GreaterThan(20));
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PetalField(0, Height, 1, 1, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PetalField(Width, -1, 1, 1, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PetalField(double.NaN, Height, 1, 1, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PetalField(Width, double.PositiveInfinity, 1, 1, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PetalField(Width, Height, -1, 1, new Random(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PetalField(Width, Height, 1, 0, new Random(1)));
            Assert.Throws<ArgumentNullException>(() => new PetalField(Width, Height, 1, 1, null));
        }

        [Test]
        public void Constructor_StartsMidFlowWithMostPetalsSpreadOnScreen()
        {
            var field = Create();
            Assert.That(field.Petals.Count, Is.EqualTo(PetalField.DefaultCount));
            Assert.That(field.Elapsed, Is.EqualTo(0));
            foreach (var p in field.Petals)
            {
                Assert.That(p.Variant, Is.InRange(0, 2));
                Assert.That(p.Depth, Is.InRange(0.0, 1.0));
                Assert.That(p.Size, Is.EqualTo(PetalField.MinSize + (PetalField.MaxSize - PetalField.MinSize) * p.Depth).Within(1e-9));
                Assert.That(p.Alpha, Is.EqualTo(PetalField.MinAlpha + (PetalField.MaxAlpha - PetalField.MinAlpha) * p.Depth).Within(1e-9));
                Assert.That(Math.Abs(p.FlipScale), Is.InRange(PetalField.MinFlipScale, 1.0));
            }
            Assert.That(field.Petals.Select(p => p.Variant).Distinct().Count(), Is.EqualTo(3));
            var visible = field.Petals.Where(p => Visible(p, field)).ToArray();
            Assert.That(visible.Length, Is.GreaterThanOrEqualTo(20), "petals already on screen");
            Assert.That(visible.All(p => !p.IsWaiting), Is.True);
            // 위아래·좌우로 고르게 흩어져 있다 (한쪽에 몰리지 않음).
            Assert.That(visible.Count(p => p.Y > 0), Is.InRange(visible.Length / 4, visible.Length * 3 / 4));
            Assert.That(visible.Count(p => p.X > 0), Is.InRange(visible.Length / 4, visible.Length * 3 / 4));
            // 미리 움직였으므로 화면 밖에 차례를 기다리는 꽃잎이 남아 있다.
            Assert.That(field.WaitingCount, Is.GreaterThan(0));
            Assert.That(field.Petals.Count(p => p.IsWaiting), Is.EqualTo(field.WaitingCount));
            Assert.That(field.Petals.Where(p => p.IsWaiting).All(p => AtSpawn(p, field)), Is.True);
        }

        [Test]
        public void Constructor_ZeroCount_HasNoPetals()
        {
            Assert.That(Create(count: 0).Petals, Is.Empty);
        }

        [Test]
        public void Tick_SameSeed_SameResult()
        {
            var a = Create(seed: 7);
            var b = Create(seed: 7);
            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));
            for (int i = 0; i < 300; i++) { a.Tick(Frame); b.Tick(Frame); }
            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));
        }

        [Test]
        public void Tick_PetalsFallEveryFrameUnlessRespawned()
        {
            var field = Create(count: 100);
            for (int frame = 0; frame < 300; frame++)
            {
                var before = field.Petals.Select(p => p.Y).ToArray();
                field.Tick(Frame);
                for (int i = 0; i < before.Length; i++)
                {
                    var drop = before[i] - field.Petals[i].Y;
                    if (drop <= 0 || drop > MaxDrop)
                    {
                        // 위로 올라가거나 한 번에 크게 움직인 것은 다시 나타난 꽃잎뿐이다.
                        Assert.That(AtSpawn(field.Petals[i], field), Is.True, $"respawn at ({field.Petals[i].X}, {field.Petals[i].Y})");
                    }
                }
            }
        }

        [Test]
        public void Tick_LongRun_PetalsStayAroundAreaAndKeepComing()
        {
            var field = Create();
            for (int frame = 0; frame < 30 * 120; frame++)
            {
                field.Tick(Frame);
                foreach (var p in field.Petals)
                {
                    Assert.That(p.X, Is.InRange(-Width / 2 - PetalField.Margin * 4, Width / 2 + PetalField.Margin));
                    Assert.That(p.Y, Is.InRange(-Height / 2 - PetalField.Margin, Height / 2 + PetalField.Margin + PetalField.SpawnBand));
                    Assert.That(Math.Abs(p.FlipScale), Is.InRange(PetalField.MinFlipScale, 1.0));
                    Assert.That(p.Rotation, Is.InRange(0.0, 360.0));
                }
                Assert.That(field.Petals.Count(p => Visible(p, field)), Is.GreaterThanOrEqualTo(18), $"frame {frame}");
            }
        }

        // 꽃잎이 한꺼번에 몰려오지 않고 꾸준히 새로 들어온다: 2분 동안 새 꽃잎이 화면에 들어오는 간격이 3.5초를 넘지 않고,
        // 평균 초당 1.2장 이상이며, 화면 안 꽃잎 수가 크게 출렁이지 않는다.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Tick_NewPetalsEnterScreenSteadily(int seed)
        {
            var field = Create(seed: seed);
            int fromLeft = 0, total = 0, minVisible = int.MaxValue, maxVisible = 0;
            double time = 0, lastEntry = 0, maxGap = 0;
            for (int frame = 1; frame <= 30 * 120; frame++)
            {
                var wasVisible = field.Petals.Select(p => Visible(p, field)).ToArray();
                field.Tick(Frame);
                time += Frame;
                for (int i = 0; i < wasVisible.Length; i++)
                {
                    var p = field.Petals[i];
                    if (!wasVisible[i] && Visible(p, field))
                    {
                        maxGap = Math.Max(maxGap, time - lastEntry);
                        lastEntry = time;
                        total++;
                        if (p.X < -Width / 2 + 5) fromLeft++;
                    }
                }
                int visible = field.Petals.Count(p => Visible(p, field));
                minVisible = Math.Min(minVisible, visible);
                maxVisible = Math.Max(maxVisible, visible);
            }
            Assert.That(maxGap, Is.LessThanOrEqualTo(3.5), "no long pause between new petals");
            Assert.That(total / 120.0, Is.GreaterThanOrEqualTo(1.2), "new petals per second");
            Assert.That(fromLeft, Is.GreaterThan(0), "some come in from the left with the wind");
            Assert.That(minVisible, Is.GreaterThanOrEqualTo(18));
            Assert.That(maxVisible, Is.LessThanOrEqualTo(PetalField.DefaultCount - 5), "always some in reserve");
        }

        // 기다리던 꽃잎은 EmitInterval × (0.5 ~ 1.5) 간격으로 한 장씩 나간다. 기다리는 꽃잎이 바닥나지 않는다.
        [Test]
        public void Tick_ReleasesWaitingPetalsAtSteadyIntervals()
        {
            var field = Create(seed: 11);
            double time = 0, lastRelease = double.NaN, maxGap = 0;
            int releases = 0;
            for (int frame = 0; frame < 30 * 120; frame++)
            {
                var wasWaiting = field.Petals.Select(p => p.IsWaiting).ToArray();
                field.Tick(Frame);
                time += Frame;
                int released = field.Petals.Where((p, i) => wasWaiting[i] && !p.IsWaiting).Count();
                Assert.That(released, Is.LessThanOrEqualTo(1), "one at a time");
                if (released == 1)
                {
                    if (!double.IsNaN(lastRelease)) maxGap = Math.Max(maxGap, time - lastRelease);
                    lastRelease = time;
                    releases++;
                }
                Assert.That(field.WaitingCount, Is.GreaterThan(0), $"reserve at {time:0.0}s");
            }
            Assert.That(maxGap, Is.LessThanOrEqualTo(PetalField.EmitInterval * PetalField.MaxEmitJitter + Frame * 2));
            Assert.That(releases, Is.InRange(150, 195), "about one per EmitInterval (0.7 s)");
        }

        [Test]
        public void Tick_WaitingPetalsDoNotMove()
        {
            var field = Create(seed: 4);
            var waiting = field.Petals.Where(p => p.IsWaiting).Select(p => (p, p.X, p.Y)).ToArray();
            Assert.That(waiting, Is.Not.Empty);
            field.Tick(Frame);
            foreach (var (p, x, y) in waiting)
                if (p.IsWaiting) Assert.That((p.X, p.Y), Is.EqualTo((x, y)));
        }

        [Test]
        public void LeftShare_IsSmallerShareThanTop()
        {
            var field = Create(count: 0);
            Assert.That(field.LeftShare, Is.InRange(0.05, 0.3));
        }

        [Test]
        public void Tick_WindDrivesPetalsRightOnAverage()
        {
            var field = Create(count: 100);
            double drift = 0;
            for (int frame = 0; frame < 300; frame++)
            {
                var before = field.Petals.Select(p => p.X).ToArray();
                var beforeY = field.Petals.Select(p => p.Y).ToArray();
                field.Tick(Frame);
                for (int i = 0; i < before.Length; i++)
                    if (field.Petals[i].Y <= beforeY[i]) drift += field.Petals[i].X - before[i];
            }
            Assert.That(drift, Is.GreaterThan(0));
        }

        [Test]
        public void Wind_StaysWithinBaseAndGusts()
        {
            var field = Create(count: 0);
            for (int frame = 0; frame < 30 * 60; frame++)
            {
                field.Tick(Frame);
                Assert.That(field.Wind, Is.InRange(
                    PetalField.BaseWind - PetalField.GustWind - PetalField.FlutterWind,
                    PetalField.BaseWind + PetalField.GustWind + PetalField.FlutterWind));
            }
        }

        [TestCase(0.0)]
        [TestCase(-0.5)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void Tick_InvalidDelta_DoesNothing(double delta)
        {
            var field = Create();
            var before = Snapshot(field);
            field.Tick(delta);
            Assert.That(Snapshot(field), Is.EqualTo(before));
            Assert.That(field.Elapsed, Is.EqualTo(0));
        }

        [Test]
        public void Tick_LongDelta_MovesOnlyMaxStep()
        {
            var a = Create(seed: 3);
            var b = Create(seed: 3);
            a.Tick(10);
            b.Tick(PetalField.MaxStep);
            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));
            Assert.That(a.Elapsed, Is.EqualTo(PetalField.MaxStep));
        }

        [Test]
        public void Resize_InvalidSize_Throws()
        {
            var field = Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => field.Resize(0, 360));
            Assert.Throws<ArgumentOutOfRangeException>(() => field.Resize(800, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => field.Resize(double.PositiveInfinity, 360));
        }

        // 첫 프레임에 캔버스가 화면 픽셀 크기(예: 1920×1080)였다가 640×360 으로 줄어드는 경우.
        // 위치를 같은 비율로 줄여, 꽃잎이 화면 밖으로 밀려나 한꺼번에 다시 나타나지 않는다.
        [Test]
        public void Resize_ScalesPositionsAndKeepsSpread()
        {
            var field = new PetalField(1920, 1080, PetalField.DefaultCount, 3, new Random(5));
            var before = field.Petals.Select(p => (p.X, p.Y)).ToArray();
            int visibleBefore = field.Petals.Count(p => Visible(p, field));
            field.Resize(Width, Height);
            Assert.That(field.Width, Is.EqualTo(Width));
            Assert.That(field.Height, Is.EqualTo(Height));
            for (int i = 0; i < before.Length; i++)
            {
                Assert.That(field.Petals[i].X, Is.EqualTo(before[i].X / 3).Within(1e-9));
                Assert.That(field.Petals[i].Y, Is.EqualTo(before[i].Y / 3).Within(1e-9));
            }
            Assert.That(field.Petals.Count(p => Visible(p, field)), Is.EqualTo(visibleBefore));
            field.Tick(Frame);
            // 줄인 직후 한 번 움직여도 화면 밖으로 밀려 다시 나타나는 꽃잎은 거의 없다.
            Assert.That(field.Petals.Count(p => AtSpawn(p, field)), Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void Resize_Wider_PetalsPassThroughNewRightSide()
        {
            var field = Create(count: 60);
            field.Resize(800, 360);
            bool sawWide = false;
            for (int frame = 0; frame < 30 * 60; frame++)
            {
                field.Tick(Frame);
                foreach (var p in field.Petals)
                {
                    Assert.That(p.X, Is.LessThanOrEqualTo(400 + PetalField.Margin));
                    if (p.X > 330) sawWide = true;
                }
            }
            Assert.That(sawWide, Is.True, "넓어진 오른쪽에도 꽃잎이 지나간다");
        }
    }
}
