using System;
using System.Collections.Generic;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Stage
{
    /// <summary>
    /// 스테이지 번호, 몬스터 배치, 처치 수, 보스, 성공·실패와 페이드 전환을 맡는 진행 로직.
    /// docs/spec/stage.md "동작", "연결 지점".
    /// </summary>
    public sealed class StageProgress
    {
        /// <summary>페이드아웃·페이드인 각각의 길이 (초, 임시).</summary>
        public const double FadeSeconds = 0.5;

        /// <summary>스테이지 소개를 보여 주는 길이 (초, 임시).</summary>
        public const double IntroSeconds = 3.0;
        public const double SubstageIntroSeconds = 1.2;

        private readonly Wallet _wallet;
        private readonly StatUpgrades _upgrades;
        private readonly Battle _battle;
        private readonly Random _random;
        private readonly HashSet<Enemy> _substageEnemies = new HashSet<Enemy>();

/// <summary>페이드 시간 비교의 허용 오차. 페이드 시간 ≥ FadeSeconds − FadeTolerance 이면 끝난 것으로 본다.</summary>
        private const double FadeTolerance = 1e-9;

        /// <summary>전환 시작 때 기억한 다음 스테이지 번호. 가려진 순간의 교체에 쓴다.</summary>
        private int _nextStageNumber;

        /// <summary>현재 페이드 단계에서 흐른 시간 (초).</summary>
        private double _fadeElapsed;

        /// <summary>소개 단계에서 흐른 시간 (초).</summary>
        private double _introElapsed;

        /// <summary>가려진 순간의 교체에서 한 소개 판단의 결과. 페이드인이 끝날 때 Intro 로 갈지 정한다.</summary>
        private bool _showIntroAfterFade;
        private bool _substageTransition;

        /// <summary>
        /// wallet: 처치 금화를 줄 지갑. upgrades: 페이드 동안 잠글 강화. battle: 적을 놓고 알림을 받을 전투.
        /// random: 무리 구성에 쓸 난수. 넷 중 하나라도 null 이면 ArgumentNullException.
        /// startStage: 1 보다 작으면 1 로 만든다 (예외 없음).
        /// 생성자는 전투·지갑·강화를 건드리지 않고 알림을 보내지 않는다.
        /// </summary>
        public StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, Random random, int startStage = 1, int seenIntroStage = 5)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            StageNumber = startStage < 1 ? 1 : startStage;
            SeenIntroStage = Math.Max(0, Math.Min(5, seenIntroStage));
            Phase = StagePhase.Playing;
            HasBegun = false;
            DefeatedNormalCount = 0;
            Boss = null;

            // StageProgress 와 Battle 은 게임에 하나씩이고 수명이 같아 구독을 끊지 않는다.
            _battle.EnemyDefeated += OnEnemyDefeated;
            _battle.DiscipleDefeated += OnDiscipleDefeated;
            _battle.EnemyDamaged += OnEnemyDamaged;
        }

        /// <summary>스테이지가 배치된 뒤에 그 번호를 담아 보낸다 (Begin, 그리고 가려진 순간의 교체).</summary>
        public event Action<int> StageStarted;

        /// <summary>보스를 처치해 성공했을 때 끝난 스테이지 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.</summary>
        public event Action<int> StageCleared;

        /// <summary>제자가 쓰러져 실패했을 때 그 스테이지 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.</summary>
        public event Action<int> StageFailed;

        /// <summary>화면이 다시 그려야 할 때 보낸다. 가는 때는 사양 "동작"의 목록이 전부다.</summary>
        public event Action Changed;

        /// <summary>1 이상.</summary>
        public int StageNumber { get; private set; }

        /// <summary>전체 진행 번호에서 계산한 화면상 스테이지 번호 (1~5).</summary>
        public int DisplayStageNumber => (StageNumber - 1) % 5 + 1;

        /// <summary>전체 진행 번호에서 계산한 회차.</summary>
        public int RoundNumber => (StageNumber - 1) / 5 + 1;

        public StagePhase Phase { get; private set; }

        /// <summary>1회차에서 소개를 본 마지막 화면상 스테이지 번호 (0~5). 저장 값.</summary>
        public int SeenIntroStage { get; private set; }

        /// <summary>Phase == StagePhase.Intro.</summary>
        public bool IsIntroShowing => Phase == StagePhase.Intro;
        public bool IsSubstageIntroShowing => Phase == StagePhase.SubstageIntro;
        public int SubstageNumber { get; private set; } = 1;

        /// <summary>Begin 이 불린 뒤 true.</summary>
        public bool HasBegun { get; private set; }

        /// <summary>이번 큰 스테이지에서 처치한 일반 상대 수.</summary>
        public int DefeatedNormalCount { get; private set; }

        /// <summary>이번 스테이지에서 낸 보스. 없으면 null.</summary>
        public Enemy Boss { get; private set; }

        public bool IsBossActive => Boss != null;

        /// <summary>보스 전에는 완료한 일반 구간 / 6, 보스전에는 보스 체력 비율.</summary>
        public double BarRatio => Boss == null
            ? (SubstageNumber - 1) / (double)StageLayout.NormalSubstageCount
            : CombatLayout.HealthRatio(Boss.Health, Boss.MaxHealth);

        /// <summary>
        /// 페이드 막의 진하기 (0.0 ~ 1.0). Playing 이면 0.
        /// FadingOut 이면 min(1, 페이드 시간 / FadeSeconds), FadingIn 이면 max(0, 1 − 페이드 시간 / FadeSeconds).
        /// </summary>
        public double FadeAlpha
        {
            get
            {
                switch (Phase)
                {
                    case StagePhase.FadingOut:
                        return Math.Min(1.0, _fadeElapsed / FadeSeconds);
                    case StagePhase.FadingIn:
                        return Math.Max(0.0, 1.0 - _fadeElapsed / FadeSeconds);
                    default:
                        return 0.0;
                }
            }
        }

        /// <summary>
        /// 첫 무리를 배치하고 소개 중 전투를 멈춘다. StageStarted, Changed 를 순서대로 보낸다.
        /// 두 번째부터는 아무 일도 없다.
        /// </summary>
        public void Begin()
        {
            if (HasBegun)
            {
                return;
            }

            PlaceStage();
            if (ShouldShowIntro())
            {
                Phase = StagePhase.Intro;
                _introElapsed = 0.0;
                _battle.SetPaused(true);
            }
            else BeginSubstageIntro();
            HasBegun = true;
            StageStarted?.Invoke(StageNumber);
            Changed?.Invoke();
        }

        /// <summary>
        /// 페이드 시간(FadingOut·FadingIn)이나 소개 시간(Intro)을 센다. Begin 전이거나 Playing 이면 아무것도 하지 않는다.
        /// 교체 → 페이드인 끝(소개로 가거나 Playing) → 소개 끝 순서로 본다. 페이드인·소개 뒤 남은 시간은 버린다.
        /// 음수·NaN·무한대: ArgumentOutOfRangeException (상태와 상관없이).
        /// </summary>
        public void Tick(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "경과 시간은 0 이상의 유한한 값이어야 한다.");
            }

            if (!HasBegun || Phase == StagePhase.Playing)
            {
                return;
            }

            if (Phase == StagePhase.Intro || Phase == StagePhase.SubstageIntro)
                _introElapsed += deltaSeconds;
            else
                _fadeElapsed += deltaSeconds;

            // 1. 가려진 순간의 교체. 남은 시간은 페이드인으로 넘긴다.
            if (Phase == StagePhase.FadingOut && _fadeElapsed >= FadeSeconds - FadeTolerance)
            {
                bool newStage = !_substageTransition;
                if (_substageTransition)
                {
                    SubstageNumber++;
                    PlaceSubstage();
                    _showIntroAfterFade = false;
                    _substageTransition = false;
                }
                else
                {
                    StageNumber = _nextStageNumber;
                    PlaceStage();
                    _showIntroAfterFade = ShouldShowIntro();
                }
                _fadeElapsed = Math.Max(0.0, _fadeElapsed - FadeSeconds);
                Phase = StagePhase.FadingIn;
                if (newStage) StageStarted?.Invoke(StageNumber);
                Changed?.Invoke();
            }

            // 2. 전환을 끝낸다. 같은 Tick 에서 1번 뒤에 이어질 수 있다.
            if (Phase == StagePhase.FadingIn && _fadeElapsed >= FadeSeconds - FadeTolerance)
            {
                _fadeElapsed = 0.0;
                _upgrades.SetLocked(false);
                if (_showIntroAfterFade)
                {
                    Phase = StagePhase.Intro;
                    _introElapsed = 0.0;
                }
                else
                {
                    BeginSubstageIntro();
                }
                Changed?.Invoke();
            }

            // 3. 소개를 끝낸다. 2번에서 막 들어온 소개는 시간이 0 이라 같은 Tick 에서 끝나지 않는다.
            if (Phase == StagePhase.Intro && _introElapsed >= IntroSeconds - FadeTolerance)
            {
                BeginSubstageIntro();
                Changed?.Invoke();
            }
            else if (Phase == StagePhase.SubstageIntro && _introElapsed >= SubstageIntroSeconds - FadeTolerance)
            {
                Phase = StagePhase.Playing;
                _introElapsed = 0.0;
                _battle.SetPaused(false);
                Changed?.Invoke();
            }
        }

        private void BeginSubstageIntro()
        {
            Phase = StagePhase.SubstageIntro;
            _introElapsed = 0.0;
            _battle.SetPaused(true);
        }

        /// <summary>
        /// 소개 판단: 1회차이고 화면상 스테이지 번호가 SeenIntroStage 보다 크면 소개를 보여 주기로 하고
        /// SeenIntroStage 를 그 번호로 올린다. 아니면 false 이고 SeenIntroStage 는 그대로다.
        /// </summary>
        private bool ShouldShowIntro()
        {
            if (RoundNumber != 1 || DisplayStageNumber <= SeenIntroStage) return false;
            SeenIntroStage = DisplayStageNumber;
            return true;
        }

        /// <summary>
        /// 큰 스테이지 배치: battle.Reset → 처치 수·보스 초기화 → 첫 구간 배치.
        /// Begin 과 가려진 순간의 교체(Tick)가 함께 쓴다.
        /// </summary>
        private void PlaceStage()
        {
            _battle.Reset();

            DefeatedNormalCount = 0;
            Boss = null;
            SubstageNumber = DisplayStageNumber == StageLayout.FinalDisplayStage
                ? StageLayout.NormalSubstageCount + 1 : 1;
            PlaceSubstage();
        }

        private void PlaceSubstage()
        {
            _substageEnemies.Clear();
            if (SubstageNumber > StageLayout.NormalSubstageCount)
            {
                Boss = _battle.AddEnemy(EnemyKind.Boss, StageNumber, _battle.DisciplePosition + StageLayout.BossDistance);
                return;
            }
            var positions = StageLayout.SubstagePositions(_random, _battle.DisciplePosition + StageLayout.EncounterDistance);
            foreach (double position in positions)
                _substageEnemies.Add(_battle.AddEnemy(EnemyKind.Normal, StageNumber, position));
        }

        /// <summary>Begin 전이거나 Playing 이 아니면 전투의 알림을 모두 무시한다.</summary>
        private bool IsListening => HasBegun && Phase == StagePhase.Playing;

        private void OnEnemyDefeated(Enemy enemy)
        {
            if (!IsListening || enemy == null)
            {
                return;
            }

            // 적은 Kind 로만 가린다 (누가 추가했는지는 보지 않는다).
            if (enemy.Kind == EnemyKind.Normal)
            {
                OnNormalDefeated(enemy);
            }

            else if (enemy.Kind == EnemyKind.Boss)
            {
                OnBossDefeated();
            }
        }

        /// <summary>
        /// 보스: 제자와 함께 쓰러졌으면 아무것도 하지 않는다 (실패는 뒤따르는 DiscipleDefeated 가 처리).
        /// 아니면 성공: 보스 금화 → 전환 시작(다음 번호 +1) → StageCleared → Changed.
        /// </summary>
        private void OnBossDefeated()
        {
            if (_battle.IsDiscipleDefeated)
            {
                return;
            }

            int cleared = StageNumber;
            _wallet.Add(StageRewards.BossGold(cleared));
            BeginTransition(cleared == int.MaxValue ? int.MaxValue : cleared + 1);
            StageCleared?.Invoke(cleared);
            Changed?.Invoke();
        }

        /// <summary>
        /// 전환 시작: 다음 번호를 기억하고 FadingOut, 페이드 시간 0, 전투 멈춤, 강화 잠금.
        /// Tick 이 _nextStageNumber, _fadeElapsed 를 읽어 교체를 한다.
        /// </summary>
        private void BeginTransition(int nextStageNumber)
        {
            _substageTransition = false;
            _nextStageNumber = nextStageNumber;
            Phase = StagePhase.FadingOut;
            _fadeElapsed = 0.0;
            _battle.SetPaused(true);
            _upgrades.SetLocked(true);
        }

        /// <summary>일반 상대 보상·처치 수를 갱신하고 현재 무리를 모두 이기면 다음 구간을 시작한다.</summary>
        private void OnNormalDefeated(Enemy enemy)
        {
            _wallet.Add(StageRewards.NormalGold(StageNumber));
            DefeatedNormalCount++;
            if (_substageEnemies.Remove(enemy) && _substageEnemies.Count == 0 && !_battle.IsDiscipleDefeated)
            {
                _substageTransition = true;
                Phase = StagePhase.FadingOut;
                _fadeElapsed = 0.0;
                _battle.SetPaused(true);
                _upgrades.SetLocked(true);
            }

            Changed?.Invoke();
        }

        /// <summary>제자: 실패. 전환 시작(다음 번호 = 같은 번호) → StageFailed → Changed.</summary>
        private void OnDiscipleDefeated()
        {
            if (!IsListening)
            {
                return;
            }

            int failed = StageNumber;
            BeginTransition(failed);
            StageFailed?.Invoke(failed);
            Changed?.Invoke();
        }

        private void OnEnemyDamaged(Enemy enemy)
        {
            if (!IsListening)
            {
                return;
            }

            if (enemy != null && ReferenceEquals(enemy, Boss))
            {
                Changed?.Invoke();
            }
        }
    }
}
