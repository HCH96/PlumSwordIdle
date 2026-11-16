using System;
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

        private readonly Wallet _wallet;
        private readonly StatUpgrades _upgrades;
        private readonly Battle _battle;
        private readonly Random _random;

        /// <summary>
        /// wallet: 처치 금화를 줄 지갑. upgrades: 페이드 동안 잠글 강화. battle: 적을 놓고 알림을 받을 전투.
        /// random: 무리 구성에 쓸 난수. 넷 중 하나라도 null 이면 ArgumentNullException.
        /// startStage: 1 보다 작으면 1 로 만든다 (예외 없음).
        /// 생성자는 전투·지갑·강화를 건드리지 않고 알림을 보내지 않는다.
        /// </summary>
        public StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, Random random, int startStage = 1)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            StageNumber = startStage < 1 ? 1 : startStage;
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

#pragma warning disable CS0067 // 017_stage_progress_outcome 이 보낸다.
        /// <summary>보스를 처치해 성공했을 때 끝난 스테이지 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.</summary>
        public event Action<int> StageCleared;

        /// <summary>제자가 쓰러져 실패했을 때 그 스테이지 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.</summary>
        public event Action<int> StageFailed;
#pragma warning restore CS0067

        /// <summary>화면이 다시 그려야 할 때 보낸다. 가는 때는 사양 "동작"의 목록이 전부다.</summary>
        public event Action Changed;

        /// <summary>1 이상.</summary>
        public int StageNumber { get; private set; }

        public StagePhase Phase { get; private set; }

        /// <summary>Begin 이 불린 뒤 true.</summary>
        public bool HasBegun { get; private set; }

        /// <summary>0 이상 10 이하.</summary>
        public int DefeatedNormalCount { get; private set; }

        /// <summary>이번 스테이지에서 낸 보스. 없으면 null.</summary>
        public Enemy Boss { get; private set; }

        public bool IsBossActive => Boss != null;

        /// <summary>Boss 가 null 이면 처치 수 / 10.0, 아니면 보스 체력 비율.</summary>
        public double BarRatio => Boss == null
            ? DefeatedNormalCount / (double)StageLayout.NormalCount
            : CombatLayout.HealthRatio(Boss.Health, Boss.MaxHealth);

        /// <summary>페이드 막의 진하기. Playing 이면 0. 페이드 중의 값은 018_stage_progress_fade 가 만든다.</summary>
        public double FadeAlpha => 0.0;

        /// <summary>
        /// 첫 스테이지를 배치한다 (battle.Reset 뒤 일반 몬스터 10 마리).
        /// StageStarted, Changed 를 순서대로 보낸다. 페이드·멈춤·잠금은 없다. 두 번째부터는 아무 일도 없다.
        /// </summary>
        public void Begin()
        {
            if (HasBegun)
            {
                return;
            }

            PlaceStage();
            HasBegun = true;
            StageStarted?.Invoke(StageNumber);
            Changed?.Invoke();
        }

        /// <summary>
        /// 페이드 시간을 센다. Begin 전이거나 Playing 이면 아무것도 하지 않는다.
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

            // 페이드 처리는 018_stage_progress_fade 가 만든다.
        }

        /// <summary>
        /// 스테이지 배치: battle.Reset → 무리 구성 → 좌표마다 일반 몬스터 추가 → 처치 수·보스 초기화.
        /// Begin 과 가려진 순간의 교체(018_stage_progress_fade)가 함께 쓴다.
        /// </summary>
        private void PlaceStage()
        {
            _battle.Reset();

            int[] groups = StageLayout.RollGroups(_random);
            double[] positions = StageLayout.Positions(groups);
            for (int i = 0; i < positions.Length; i++)
            {
                _battle.AddEnemy(EnemyKind.Normal, StageNumber, positions[i]);
            }

            DefeatedNormalCount = 0;
            Boss = null;
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
                OnNormalDefeated();
            }

            // 보스 처치는 017_stage_progress_outcome 이 채운다.
        }

        /// <summary>일반 몬스터 한 마리: 금화 → 처치 수 → (열 번째면) 보스 등장 → Changed.</summary>
        private void OnNormalDefeated()
        {
            _wallet.Add(StageRewards.NormalGold(StageNumber));

            if (DefeatedNormalCount < StageLayout.NormalCount)
            {
                DefeatedNormalCount++;

                if (DefeatedNormalCount == StageLayout.NormalCount && !_battle.IsDiscipleDefeated)
                {
                    Boss = _battle.AddEnemy(
                        EnemyKind.Boss,
                        StageNumber,
                        _battle.DisciplePosition + StageLayout.BossDistance);
                }
            }

            Changed?.Invoke();
        }

        private void OnDiscipleDefeated()
        {
            // 017_stage_progress_outcome 이 채운다.
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
