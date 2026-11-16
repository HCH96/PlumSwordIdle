using System;
using PlumSword.Core.Combat;
using UnityEngine;

namespace PlumSword.Game.Combat
{
    /// <summary>
    /// 스테이지 기능이 생기기 전까지만 쓰는 임시 코드다. 스테이지 기능을 구현할 때 이 클래스와 GameRoot 의 조립 줄을 지운다.
    /// 일반 몬스터 3마리와 보스 1마리를 놓고, 모두 처치하거나 제자가 패배하면 잠깐 뒤 처음 배치로 되돌려 반복한다.
    /// 금화는 주지 않고 그리지도 않는다 (그리는 것은 CombatView). 사양: docs/spec/combat.md "화면"의 "임시 적 배치".
    /// </summary>
    public sealed class TempEncounter : MonoBehaviour
    {
        // 아래 숫자는 모두 임시값 (docs/spec/combat.md "화면").
        private const float RespawnDelaySeconds = 1.0f;
        private const int Stage = 1;

        private static readonly (EnemyKind Kind, double Position)[] Layout =
        {
            (EnemyKind.Normal, 6.0),
            (EnemyKind.Normal, 9.0),
            (EnemyKind.Normal, 9.3),
            (EnemyKind.Boss, 15.0),
        };

        private Battle _battle;
        private bool _waiting;
        private float _waitElapsed;

        /// <summary>
        /// battle 의 알림을 구독하고(OnDestroy 에서 끊는다) 바로 적을 한 번 배치한다.
        /// 두 번 부르면 InvalidOperationException.
        /// </summary>
        public void Initialize(Battle battle)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (_battle != null) throw new InvalidOperationException("TempEncounter 는 한 번만 Initialize 할 수 있다.");

            _battle = battle;
            _battle.EnemyDefeated += OnEnemyDefeated;
            _battle.DiscipleDefeated += OnDiscipleDefeated;

            Place();
        }

        private void Update()
        {
            if (_battle == null || !_waiting) return;

            _waitElapsed += Time.deltaTime;
            if (_waitElapsed < RespawnDelaySeconds) return;

            _waiting = false;
            _waitElapsed = 0f;
            Place();
        }

        private void OnDestroy()
        {
            if (_battle == null) return;
            _battle.EnemyDefeated -= OnEnemyDefeated;
            _battle.DiscipleDefeated -= OnDiscipleDefeated;
        }

        private void Place()
        {
            _battle.Reset();
            foreach (var (kind, position) in Layout)
            {
                _battle.AddEnemy(kind, Stage, position);
            }
        }

        // 알림 처리 안에서는 Reset/AddEnemy 를 부르지 않는다. 대기만 시작하고 Update 에서 다시 놓는다.
        private void OnEnemyDefeated(Enemy enemy)
        {
            if (_battle.Enemies.Count == 0) StartWaiting();
        }

        private void OnDiscipleDefeated()
        {
            StartWaiting();
        }

        private void StartWaiting()
        {
            if (_waiting) return;
            _waiting = true;
            _waitElapsed = 0f;
        }
    }
}
