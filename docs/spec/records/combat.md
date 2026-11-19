# 전투 기록

[combat.md](../combat.md) 사양의 이력이다. 구현에 필요한 규칙은 사양 문서에 모두 있으므로 **구현하는 에이전트는 읽지 않아도 된다.** 기획 스킬(`plan-feature`)이 이미 정한 것을 다시 묻지 않으려고 읽는다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 전투 사양에 스테이지(배치, 보스 등장, 성공·실패, 재도전, 페이드)를 함께 담나 | 전투만 담고 스테이지는 따로 기획한다 | 추천 수락 |
| 처치 금화의 계산과 지급은 어느 기능이 맡나 | 스테이지 기능. 전투는 처치만 알린다 | 추천 수락 |
| 전투 화면에 무엇까지 넣나 | 캐릭터 대체 표시, 머리 위 체력 바, 카메라, 임시 배경 | 추천 수락 |
| 전투 시간을 어떻게 흐르게 하나 | 0.02초 고정 간격. 실시간 자동 전투이고 턴제가 아니다. 같은 칸에서 일어난 일이 "동시"다 | 추천 수락 (턴제가 아니라는 설명 뒤) / 임시값 |
| 같은 순간에 제자의 공격으로 체력이 0이 된 몬스터도 공격하나 | 한다. 모두 공격한 뒤 쓰러짐을 판정한다 | 추천 수락 |
| 스테이지 기능이 생기기 전에 적은 누가 놓나 | `TempEncounter`가 임시로 놓고 반복한다. 스테이지 구현 때 지운다 | 추천 수락 |
| 공격이 일어난 것을 화면에서 어떻게 알리나 | 맞은 쪽이 0.1초 하얗게 번쩍인다 | 추천 수락 / 임시값 |
| 몬스터 능력치 공식은 어느 기능이 가지나 | 전투 (`EnemyStats`). 스테이지는 종류·스테이지 번호·좌표만 넘긴다 | 직접 정함 |
| 한 칸의 처리 순서와 알림 순서 | 대기 감소 → 이동 → (2026-11-17 추가) 앞 동작 시작 → 공격 → 쓰러짐 판정. `EnemyDefeated` 다음에 `DiscipleDefeated` | 직접 정함 |
| 제자 이동 속도 (2026-11-17) | 초당 2.0 → 3.6 유닛 (1.8배). 달리기 애니메이션(20 fps)에 비해 몸이 느려 발이 미끄러져 보였다. 애니메이션 속도는 그대로 둔다 | 사람 결정 (제자 에셋 적용) |
| 달려와 멈춘 칸의 첫 공격 (2026-11-17) | 바로 피해를 주지 않고 공격 간격 × 7/17 뒤에 준다. 공격 애니메이션을 0번부터 재생하고 휘두르는 7번 프레임에 피해를 맞추기 위해서다. 그만큼 처치가 늦어 적에게 맞는 횟수가 늘 수 있다 | 사람 결정 (제자 에셋 적용) |
| 이동이 사거리를 넘는 경우 | 넘지 않게 딱 맞춰 멈춘다 (제자 1.0, 몬스터 0.8) | 직접 정함 |
| 적이 하나도 없을 때 제자 | 계속 전진한다 | 직접 정함 |
| 소수 계산 오차 | 거리·대기 비교에 1e-9 허용 | 직접 정함 |
| 한 번에 너무 긴 시간이 들어올 때 | 0.25초로 잘라낸다 | 직접 정함 / 임시값 |
| 잘못된 인자(없는 종류, 스테이지 0, 제자 뒤 좌표, 음수 시간) | `ArgumentOutOfRangeException` | 직접 정함 |
| 패배한 뒤와 페이드 중의 전투 | 패배 뒤에는 `Reset`까지 진행하지 않는다. 멈춤 스위치 `SetPaused`를 둔다 | 직접 정함 |
| 패배한 상태에서 체력 강화 | 현재 체력은 0 그대로 | 직접 정함 |
| 저장할 값 | 없다 (2단계 기획이 체력·진행도를 복원하지 않기로 함) | 직접 정함 |
| 대체 표시의 크기 | 제자·일반 몬스터 1.0×1.0, 보스 2.0×3.0. 몸 크기(32px)에 맞춤 | 직접 정함 / 임시값 |
| 보스 머리 위 체력 바 | 두지 않는다 (스테이지의 위쪽 바가 맡는다) | 직접 정함 |
| 겹침 표시 규칙, 체력 바, 카메라 위치, 임시 배경, 임시 배치 | `Index % 3`으로 −0.15 / 0 / +0.15, 카메라는 제자 앞 4 유닛, 땅과 나무 사각형, 일반 3마리 + 보스 | 직접 정함 / 임시값 |
| 타입·메서드 이름 | `Battle`, `Enemy`, `EnemyKind`, `EnemyStats`, `CombatLayout`, `CombatView`, `TempEncounter` | 직접 정함 |
| 알림 안의 `Reset`을 어떻게 알아채나 (구현) | `Reset`마다 1씩 느는 내부 번호를 두고, `Step`이 알림을 하나 보낼 때마다 번호가 바뀌었으면 그 칸을 바로 끝낸다. 남은 대기 설정(제자의 다음 공격 대기, 공격한 적의 대기)도 하지 않는다 | 직접 정함 (010) |
| 스킬을 켜는 방법 (2026-11-19, 기존 전투 테스트의 기대값) | `Battle` 생성자 `skillEnabled`(기본 `false`). `GameRoot`만 켠다. 기존 테스트는 그대로 통과한다 | 추천 수락 |
| 공격 차례의 기준 (2026-11-19) | 공격 대기가 끝났고 기본 공격 범위 1.0 안에 적이 있을 때. 스킬은 그때 2.0 안의 적을 모두 맞힌다 | 추천 수락 |
| 돌파 때 현재 체력을 늘리는 연결 (2026-11-19) | `Battle` 생성자 `realm`(기본 `null`)으로 받아 `BrokeThrough`를 구독한다 | 추천 수락 |
| 스킬 임시 이펙트 (2026-11-19) | 연분홍 반투명 띠 2.0×0.4, 0.25초 동안 흐려지며 사라진다 | 추천 수락 / 임시값 |
| 스킬을 쓸 때 제자 동작 (2026-11-19) | 지금은 기본 공격 애니메이션 그대로. 스킬 애니메이션은 사람이 새로 만들어 넣을 예정이고 그때 따로 기획한다 | 사용자 선택 |
| 스킬 알림과 순서 (2026-11-19) | 스킬 피해의 `EnemyDamaged`들 다음에 `SkillUsed` 1번. 스킬 대기·공격 대기는 그 뒤에 정한다 (기본 공격과 같은 방식) | 직접 정함 |
| 스킬 대기가 줄어드는 때 (2026-11-19) | 공격 대기처럼 매 칸 (이동 중·적 없음 포함). 멈춤·패배 중에는 줄지 않는다. 새 전투와 `Reset` 뒤에는 준비됨 | 직접 정함 |
| 스킬 피해 계산 (2026-11-19) | `long`, `attack × (200 + 10 × 내공) / 100` 정수 나눗셈(내림) | 기획대로 / 임시값 |
| 강화가 흐르는 스킬 대기에 주는 영향 (2026-11-19) | 없다. 피해를 줄 때마다 공격력·내공을 읽는다 | 직접 정함 |
| 멈춘 동안의 돌파 (2026-11-19) | 체력 강화처럼 바로 반영한다 | 직접 정함 |

## 구현된 완료 조건

### 2026-11-19 옮김 — 처음 사양(c02cb2a 이전)과 2026-11-17 추가 조건 중 구현된 것 (기록 007~012와 사람의 직접 변경)

번호는 사양의 원래 번호다. 아래 말은 그때의 사양 기준이다. 21-1과 23은 공격 속도 강화 제거(사람이 직접, 99ec112)로 없어졌고, 그 밖의 조건의 `new StatUpgrades(wallet, …, 1.0)`은 제거 뒤 `new StatUpgrades(wallet, …)`로 읽는다.

아래에서 따로 적지 않으면 지갑은 `new Wallet(1000)`, 강화는 `new StatUpgrades(wallet, 10, 100, 1.0)`, 전투는 `new Battle(upgrades)`로 만든 것이다. "n칸 뒤"는 `Step()`을 n번 부른 뒤다. 좌표·시간은 오차 1e-6 안에서 비교한다.

몬스터 능력치 (`EnemyStatsTests`)

1. `EnemyStats.MaxHealth(Normal, stage)`는 1·2·3스테이지에서 30·40·50, `EnemyStats.Attack(Normal, stage)`는 5·7·9다.
2. `EnemyStats.MaxHealth(Boss, stage)`는 1·2·3스테이지에서 100·130·160, `EnemyStats.Attack(Boss, stage)`는 10·14·18이다.
3. `EnemyStats.AttackInterval`은 `Normal`·`Boss` 모두 2.0, `EnemyStats.AttackRange`는 `Normal` 0.8, `Boss` 1.0이다.
4. `EnemyStats.MaxHealth(Normal, 0)`, `EnemyStats.Attack(Boss, 0)`, `EnemyStats.MaxHealth((EnemyKind)99, 1)`, `EnemyStats.AttackRange((EnemyKind)99)`는 각각 `ArgumentOutOfRangeException`을 던진다.

전투 (`BattleTests`)

5. 새로 만들면 `DisciplePosition`은 0, `DiscipleHealth`는 100, `DiscipleMaxHealth`는 100, `DiscipleAttackCooldown`은 0, `IsDiscipleDefeated`는 `false`, `Paused`는 `false`, `Enemies.Count`는 0이다.
6. `new Battle(null)`은 `ArgumentNullException`을 던진다.
7. 적이 없을 때 50칸 뒤 `DisciplePosition`은 6.48이다.
8. `AddEnemy(Normal, 2, 5.0)`이 돌려준 적은 `Kind`가 `Normal`, `Index` 0, `Position` 5.0, `MaxHealth`와 `Health` 40, `Attack` 7, `AttackInterval` 2.0, `AttackCooldown` 0, `IsDead` `false`이고, `EnemyAdded`가 그 적을 담아 1번 오며 `Enemies.Count`는 1이다. 이어서 `AddEnemy(Boss, 1, 8.0)`의 `Index`는 1이다.
9. `AddEnemy(Normal, 0, 5.0)`, `AddEnemy((EnemyKind)99, 1, 5.0)`, `AddEnemy(Normal, 1, double.NaN)`, `AddEnemy(Normal, 1, double.PositiveInfinity)`, `AddEnemy(Normal, 1, -0.1)`은 각각 `ArgumentOutOfRangeException`을 던지고 `Enemies.Count`는 0, `EnemyAdded`는 0번 온다. `AddEnemy(Normal, 1, 0.0)`은 예외 없이 추가된다.
10. `AddEnemy(Normal, 1, 10.0)` 뒤 1칸 뒤에 그 적의 `Position`은 10.0이고 `DisciplePosition`은 0.1296이다 (반응 거리 밖의 몬스터는 기다린다).
11. `AddEnemy(Normal, 1, 5.0)` 뒤: 15칸 뒤 적의 `Position`은 5.0, 16칸 뒤 4.98이다 (제자가 3 유닛 안으로 오면 다가온다).
12. 11번에 이어: 29칸 뒤 적의 `Health`는 30이고 `DisciplePosition`은 3.74, 적의 `Position`은 4.72, `DiscipleAttackCooldown`은 7/17이다 (제자는 거리 1.0에서 멈추고 앞 동작 대기를 시작한다). 49칸 뒤 30, 50칸 뒤 20이고 적의 `Position`은 4.54다.
13. 12번에 이어: 37칸 뒤 `DiscipleHealth`는 100, 38칸 뒤 95이고 적의 `Position`은 4.54다 (몬스터는 거리 0.8에서 멈춰 바로 공격한다).
14. 13번에 이어: 100칸 뒤 적의 `Health`는 10, 149칸 뒤 10, 150칸 뒤 0이고 `EnemyDefeated`가 그 적을 담아 1번 오며, 적의 `IsDead`는 `true`, `Enemies.Count`는 0, `DiscipleHealth`는 90이다 (적은 38·138칸에 공격했다. 처치해도 체력은 회복하지 않는다). 151칸 뒤 `DisciplePosition`은 3.8696이다.
15. `AddEnemy(Normal, 1, 0.8)`을 3번 부른 뒤 1칸 뒤: 세 적의 `Health`는 모두 20, `DiscipleHealth`는 85, `EnemyDamaged`는 3번(`Index` 0, 1, 2 순서), `DiscipleDamaged`는 3번 온다.
16. 15번에 이어: 51칸 뒤 세 적의 `Health`는 모두 10이다. 101칸 뒤 `Enemies.Count`는 0, `EnemyDefeated`는 3번(`Index` 0, 1, 2 순서) 오고, `DiscipleHealth`는 70, `IsDiscipleDefeated`는 `false`다 (같은 칸에 쓰러진 몬스터도 그 칸의 공격을 한다).
17. `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 1.5)` 뒤 1칸 뒤: 일반 몬스터의 `Health`는 20, 보스의 `Health`는 100이고 `EnemyDamaged`는 1번 온다 (범위 밖의 적은 맞지 않는다).
18. `AddEnemy(Boss, 1, 1.0)` 뒤: 1칸 뒤 보스의 `Health`는 90, `DiscipleHealth`는 90이다. 450칸 뒤 보스의 `Health`는 10, 451칸 뒤 0이고 `EnemyDefeated`가 1번 오며 `DiscipleHealth`는 50이다.
19. `AddEnemy(Boss, 1, 5.0)` 뒤: 30칸 뒤 `DiscipleHealth`는 100, 31칸 뒤 90이고 보스의 `Health`는 100, `DisciplePosition`은 4.0, 보스의 `Position`은 5.0이다 (보스는 움직이지 않고 도착하자마자 공격한다). 51칸 뒤 보스의 `Health`는 100, 52칸 뒤 90이다 (제자는 앞 동작 21칸 뒤).
20. `new StatUpgrades(wallet, 30, 100, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 2.0)` 뒤: 1칸 뒤 `Enemies.Count`는 1, `DiscipleHealth`는 95다. 26칸 뒤 `DisciplePosition`은 1.0, 보스의 `Health`는 100, `DiscipleAttackCooldown`은 0.5, `DiscipleHealth`는 85다. 50칸 뒤 보스의 `Health`는 100, 51칸 뒤 70이다 (이동 중에도 대기가 흐르고, 남은 시간만큼 기다린다).
21. `new StatUpgrades(wallet, 30, 100, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)`과 `AddEnemy(Boss, 1, 8.0)` 뒤: 76칸 뒤 보스의 `Health`는 100, 77칸 뒤 70이다 (56칸째 도착했을 때 대기가 끝나 있어도 앞 동작 대기를 둔다).
21-1. `new StatUpgrades(wallet, 10, 100, 0.5)`으로 `AddEnemy(Boss, 1, 5.0)` 뒤: 31칸 뒤 `DisciplePosition`은 4.0, `DiscipleAttackCooldown`은 `0.5 × 7/17`이다.
21-2. `AddEnemy(Boss, 1, 1.0)` 뒤(제자가 전진하지 않고 처음부터 범위 안): 1칸 뒤 보스의 `Health`는 90이다 (앞 동작 대기 없음).
22. `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.Attack)`을 부르면, 51칸 뒤 보스의 `Health`는 78이다 (90 − 12).
23. `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고 `upgrades.TryUpgrade(StatType.AttackSpeed)`를 부르면, 50칸 뒤 보스의 `Health`는 90, 51칸 뒤 80, 98칸 뒤 80, 99칸 뒤 70이다 (기다리는 중인 공격은 그대로이고 다음 대기부터 0.95초다).
24. `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 진행하고(`DiscipleHealth` 90) `upgrades.TryUpgrade(StatType.Health)`를 부르면 `DiscipleHealth`는 110, `DiscipleMaxHealth`는 120이다. `TryUpgrade(StatType.Attack)`을 불러도 `DiscipleHealth`는 110 그대로다.
25. `new StatUpgrades(wallet, 30, 5, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `EnemyDefeated` 1번과 `DiscipleDefeated` 1번이 이 순서로 오고, `EnemyDefeated`를 받는 시점에 읽은 `IsDiscipleDefeated`는 `true`다. `DiscipleHealth`는 0, `Enemies.Count`는 0이다.
26. `new StatUpgrades(wallet, 100, 10, 1.0)`으로, `AddEnemy(Boss, 1, 1.0)` 뒤 1칸 뒤: 보스의 `EnemyDefeated`를 받는 시점에 읽은 `IsDiscipleDefeated`는 `true`이고 `DiscipleDefeated`가 1번 온다.
27. `new StatUpgrades(wallet, 10, 5, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 1칸 뒤: `DiscipleHealth`는 0, `IsDiscipleDefeated`는 `true`, `DiscipleDefeated`는 1번, 적의 `Health`는 20이고 `Enemies.Count`는 1이다. 이어서 `Step()` 100번과 `Tick(0.1)`을 불러도 적의 `Health`는 20, `DisciplePosition`은 0이고 알림은 더 오지 않는다.
28. `new StatUpgrades(wallet, 10, 12, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)`을 3번 부른 뒤 1칸 뒤: `DiscipleHealth`는 0 (음수가 아니다), `DiscipleDamaged`는 3번, `DiscipleDefeated`는 1번 온다.
29. 27번 상태(패배)에서 `upgrades.TryUpgrade(StatType.Health)`를 부르면 `DiscipleHealth`는 0 그대로다. 이어서 `Reset()`을 부르면 `DiscipleHealth`는 25다.
30. 18번처럼 1칸 진행한 뒤 `Reset()`을 부르면 `DisciplePosition`은 0, `DiscipleHealth`는 100, `DiscipleAttackCooldown`은 0, `IsDiscipleDefeated`는 `false`, `Enemies.Count`는 0이고, `ResetCompleted`는 1번, `EnemyDefeated`는 0번 온다. 이어서 `AddEnemy(Normal, 1, 5.0)`의 `Index`는 0이다. `Reset()`을 한 번 더 부르면 `ResetCompleted`가 한 번 더 온다.
31. `SetPaused(true)` 뒤 `Paused`는 `true`이고, `Step()` 10번과 `Tick(0.1)`을 불러도 `DisciplePosition`은 0이다. `SetPaused(false)` 뒤 1칸 뒤 `DisciplePosition`은 0.04다 (멈춘 동안의 `Tick` 시간은 버린다).
32. `SetPaused(true)` 상태에서 `AddEnemy(Normal, 1, 5.0)`은 추가되고 `EnemyAdded`가 1번 온다. `Reset()`을 부르면 `Enemies.Count`는 0이고 `Paused`는 `true` 그대로다. `SetPaused(true)`를 한 번 더 불러도 예외가 없다.
33. 적이 없을 때 `Tick(0.1)` 뒤 `DisciplePosition`은 0.2다 (5칸).
34. 적이 없을 때 `Tick(0.01)` 1번 뒤 `DisciplePosition`은 0, 2번 뒤 0.04다.
35. 적이 없을 때 `Tick(0.05)` 뒤 `DisciplePosition`은 0.08이고, 이어서 `Tick(0.01)` 뒤 0.12다 (남은 시간을 넘긴다).
36. 적이 없을 때 `Tick(1.0)` 뒤 `DisciplePosition`은 0.48이다 (0.25초로 잘라내 12칸).
37. `Tick(0)` 뒤 `DisciplePosition`은 0이다. `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)`는 각각 `ArgumentOutOfRangeException`을 던지고 `DisciplePosition`은 0 그대로다.
38. `new StatUpgrades(wallet, 30, 100, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `AddEnemy(Boss, 1, DisciplePosition + 5.0)`을 부르게 하면, 1칸 뒤 예외 없이 `Enemies.Count`는 1이고 그 적의 `Kind`는 `Boss`, `Health`는 100이다.
39. `new StatUpgrades(wallet, 30, 5, 1.0)`으로, `AddEnemy(Normal, 1, 0.8)` 뒤 `EnemyDefeated` 안에서 `Reset()`을 부르게 하면, 1칸 뒤 `DiscipleDefeated`는 0번 오고 `IsDiscipleDefeated`는 `false`, `DiscipleHealth`는 5다.

표시 규칙 (`CombatLayoutTests`)

40. `CombatLayout.EnemyOffsetY`는 0 → −0.15, 1 → 0, 2 → 0.15, 3 → −0.15, 4 → 0을 돌려주고, −1은 `ArgumentOutOfRangeException`을 던진다.
41. `CombatLayout.HealthRatio`는 (50, 100) → 0.5, (0, 100) → 0.0, (100, 100) → 1.0, (150, 100) → 1.0, (−5, 100) → 0.0, (10, 0) → 0.0, (30, 40) → 0.75를 돌려준다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

42. 게임을 실행하면 하늘색 바탕과 땅, 나무 사각형이 보이고, 파란 제자 사각형이 오른쪽으로 전진하며 나무가 왼쪽으로 흘러간다. 제자는 화면에서 같은 자리에 보인다.
43. 주황색 몬스터를 만나면 제자가 멈춰 싸우고, 맞은 쪽이 잠깐 하얗게 번쩍이며 머리 위 체력 바가 줄어든다. 두 마리가 붙으면 위아래로 어긋나 보인다. 쓰러진 몬스터는 사라진다.
44. 보라색 보스까지 처치하거나 제자의 체력이 0이 되면 약 1초 뒤 처음 배치로 돌아가 다시 싸운다.
45. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 구현된 변경 기록

(없음)
