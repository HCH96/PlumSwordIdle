using System;

namespace PlumSword.Core.Combat
{
    /// <summary>
    /// 제자 스프라이트의 동작(대기·달리기·공격)과 프레임 번호를 고르는 표시 규칙. 거리·피해 계산에는 쓰지 않는다.
    /// 화면 코드가 매 프레임 Battle.Tick 뒤에 Update 를 부르고, Motion·Frame 을 읽어 그 칸의 그림을 보여 준다.
    ///
    /// 공격 맞추기: Battle 은 대기 시간이 0 이 되는 칸에 피해를 준다. 그래서
    /// - 적이 공격 범위 안에 있으면, 남은 대기 시간을 보고 타격 프레임(HitFrame)이
    ///   대기 0 이 되는 순간에 오도록 앞 동작(0 ~ HitFrame-1)을 미리 재생한다.
    /// - 공격이 일어난 칸부터 타격 프레임과 마무리 동작(HitFrame ~ 끝)을 재생한다.
    /// - 공격 재생 속도는 AttackFrameCount / 공격 간격이라, 계속 공격하면 끊김 없이 이어진다.
    /// - 달려와 멈춘 칸에는 Battle 이 대기를 공격 간격 × Battle.DiscipleAttackWindUpRatio(= HitFrame / AttackFrameCount)
    ///   로 올리므로 첫 공격도 0번 프레임부터 재생된다.
    /// 수치는 임시값. docs/spec/combat.md "화면".
    /// </summary>
    public sealed class DiscipleAnimator
    {
        public const int IdleFrameCount = 24;
        public const int RunFrameCount = 24;
        public const int AttackFrameCount = 17;

        /// <summary>검기가 처음 보이는 공격 프레임(0부터). 이 프레임이 피해를 주는 순간과 겹친다.</summary>
        public const int AttackHitFrame = 7;

        public const double IdleFramesPerSecond = 10.0;
        public const double RunFramesPerSecond = 20.0;

        /// <summary>검기(분홍 궤적)가 보이는 프레임 수. 타격 프레임부터 이만큼은 달리기 시작해도 끊지 않는다.</summary>
        public const int AttackSlashFrameCount = 3;

        /// <summary>이보다 많이 움직인 프레임을 "달리는 중" 으로 본다 (유닛).</summary>
        public const double MoveThreshold = 1e-6;

        /// <summary>
        /// 달리다가 이만큼(초) 움직이지 않아야 멈춘 것으로 본다. Battle 은 0.02 초 칸으로 움직여서
        /// 화면 프레임이 더 짧으면 움직이지 않는 프레임이 끼어든다. 그때 달리기가 끊기지 않게 한다.
        /// </summary>
        public const double RunGraceSeconds = 0.1;

        private const double Epsilon = 1e-9;

        private double _idleSeconds;
        private double _runSeconds;
        private double _stillSeconds = RunGraceSeconds;
        private bool _recovering;
        private double _sinceHitSeconds;

        public DiscipleMotion Motion { get; private set; } = DiscipleMotion.Idle;

        /// <summary>지금 동작의 프레임 번호. 0 이상 그 동작의 프레임 수 미만.</summary>
        public int Frame { get; private set; }

        /// <summary>
        /// 공격 재생 속도(초당 프레임) = AttackFrameCount / attackInterval.
        /// attackInterval 이 0 이하, NaN, 무한대면 ArgumentOutOfRangeException.
        /// </summary>
        public static double AttackFramesPerSecond(double attackInterval)
        {
            if (double.IsNaN(attackInterval) || double.IsInfinity(attackInterval) || attackInterval <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackInterval), attackInterval, "공격 간격은 0 보다 큰 유한한 값이어야 한다.");
            }

            return AttackFrameCount / attackInterval;
        }

        /// <summary>
        /// 한 프레임 진행한다.
        /// deltaSeconds: 지난 시간(초). 음수·NaN 이면 0 으로 본다.
        /// moved: 이번 프레임에 제자 좌표가 MoveThreshold 보다 많이 바뀌었는가.
        /// attacked: 이번 프레임의 Tick 안에서 제자가 공격했는가 (Battle.EnemyDamaged 를 받았는가).
        /// engaged: 제자의 공격 범위 안에 적이 있고, 제자가 패배하지 않았고, 전투가 멈추지 않았는가.
        /// cooldown: Tick 뒤의 Battle.DiscipleAttackCooldown. attackInterval: Battle.DiscipleAttackInterval.
        /// </summary>
        public void Update(double deltaSeconds, bool moved, bool attacked, bool engaged, double cooldown, double attackInterval)
        {
            double dt = double.IsNaN(deltaSeconds) || deltaSeconds < 0.0 ? 0.0 : deltaSeconds;
            double attackFps = AttackFramesPerSecond(attackInterval);

            _stillSeconds = moved ? 0.0 : _stillSeconds + dt;
            bool running = moved || _stillSeconds < RunGraceSeconds - Epsilon;

            if (attacked)
            {
                // 공격은 Tick 안의 어느 칸에서 일어났고, 그 뒤 흐른 시간만큼 대기가 줄었다.
                _recovering = true;
                _sinceHitSeconds = Math.Max(0.0, attackInterval - cooldown);
            }
            else if (_recovering)
            {
                _sinceHitSeconds += dt;
            }

            if (_recovering)
            {
                int frame = AttackHitFrame + (int)Math.Floor(_sinceHitSeconds * attackFps + Epsilon);
                // 적을 쓰러뜨려 다시 달리면, 검기 프레임까지만 보여 주고 마무리 동작은 끊는다 (미끄러지지 않게).
                bool interrupted = moved && frame >= AttackHitFrame + AttackSlashFrameCount;
                if (frame < AttackFrameCount && !interrupted)
                {
                    Set(DiscipleMotion.Attack, frame);
                    return;
                }

                _recovering = false;
            }

            if (engaged && cooldown > Epsilon)
            {
                // 타격까지 남은 프레임 수. HitFrame 이하로 남았으면 앞 동작을 보여 준다.
                double framesToHit = cooldown * attackFps;
                if (framesToHit <= AttackHitFrame + Epsilon)
                {
                    int frame = AttackHitFrame - (int)Math.Ceiling(framesToHit - Epsilon);
                    Set(DiscipleMotion.Attack, Math.Max(0, Math.Min(AttackHitFrame - 1, frame)));
                    return;
                }
            }

            if (running)
            {
                _idleSeconds = 0.0;
                _runSeconds += dt;
                Set(DiscipleMotion.Run, Loop(_runSeconds, RunFramesPerSecond, RunFrameCount));
                return;
            }

            _runSeconds = 0.0;
            _idleSeconds += dt;
            Set(DiscipleMotion.Idle, Loop(_idleSeconds, IdleFramesPerSecond, IdleFrameCount));
        }

        /// <summary>처음 상태(대기 0번 프레임)로 되돌린다. Battle.Reset 뒤에 부른다.</summary>
        public void Reset()
        {
            _idleSeconds = 0.0;
            _runSeconds = 0.0;
            _stillSeconds = RunGraceSeconds;
            _recovering = false;
            _sinceHitSeconds = 0.0;
            Set(DiscipleMotion.Idle, 0);
        }

        private void Set(DiscipleMotion motion, int frame)
        {
            if (motion != Motion)
            {
                // 다른 동작으로 넘어가면 반복 동작은 처음부터 다시 센다.
                if (motion != DiscipleMotion.Idle) _idleSeconds = 0.0;
                if (motion != DiscipleMotion.Run) _runSeconds = 0.0;
            }

            Motion = motion;
            Frame = frame;
        }

        private static int Loop(double seconds, double fps, int count)
        {
            long frame = (long)Math.Floor(seconds * fps + Epsilon);
            return (int)(frame % count);
        }
    }
}
