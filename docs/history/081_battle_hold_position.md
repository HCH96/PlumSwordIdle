# 081_battle_hold_position — 제자를 제자리에 세워 두는 전투 연결 지점

## 목표
Core(`PlumSword.Core.Combat.Battle`)에 제자의 전진만 막는 "제자리 유지"를 더한다. 스테이지 기능이 마지막 적을 이긴 뒤 다음 전환까지 기다리는 동안(082) 전투를 멈추지 않고 제자만 세워 두는 데 쓴다.

## 묶음
clear_wait

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작"의 2. 이동과 초기화, "연결 지점"의 Battle)

## 요구사항
- `Battle`에 아래를 더한다.
  - `public bool IsHoldingPosition { get; }`: 처음 값 `false`.
  - `public void SetHoldPosition(bool hold)`: 값을 정한다. 같은 값이면 아무 일도 없다. 알림은 없다. 멈춤·패배 상태에서도 값은 바뀐다.
- `IsHoldingPosition`이 `true`인 칸에서는 "2. 이동"의 **제자 이동만** 건너뛴다. 적이 없어도 전진하지 않는다. 대기 감소, 적 이동, 앞 동작 판단, 종남 검기, 제자·적의 공격, 쓰러짐 판정은 평소와 같다. 제자가 움직이지 않으니 앞 동작(달려와 멈춘 칸)도 생기지 않는다.
- `Reset()`은 `IsHoldingPosition`을 `false`로 되돌린다.
- 이름은 082(`StageProgress`)가 그대로 쓴다. 구현하다 모양이 이 문서와 달라지면 `docs/spec/combat.md`의 연결 지점 절을 고치고 보고서의 가정에 적는다.
- 끝나면 `docs/spec/combat.md`의 "동작"(2. 이동, 초기화), "연결 지점"(Battle), "완료 조건 초안"에 반영한다.
- 이 묶음과 별개로 078(다른 묶음)이 스킬 중 제자리 유지를 따로 넣을 수 있다. 둘은 서로 독립이다. 둘 다 있으면 둘 중 하나라도 해당하면 전진하지 않는다.

## 완료 조건
- `Assets/Tests/EditMode/Combat/BattleHoldPositionTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. "기본 전투"는 `new Battle(new StatUpgrades(new Wallet(1000), 10, 100))`이다.
- 기본 전투를 새로 만들면 `IsHoldingPosition`은 `false`다.
- 기본 전투에서 `SetHoldPosition(true)` 뒤 적 없이 `Step()`을 10번 부르면 `DisciplePosition`은 0이다.
- 이어서 `SetHoldPosition(false)` 뒤 `Step()`을 10번 부르면 `DisciplePosition`은 1.296이다.
- 기본 전투에서 `AddEnemy(Boss, 2, 5.0)`을 두고 `SetHoldPosition(true)` 뒤 `Step()`을 50번 부르면 `DisciplePosition`은 0이고 보스 `Health`는 130이다.
- 기본 전투에서 `AddEnemy(Boss, 2, 1.0)`을 두고 `SetHoldPosition(true)` 뒤 `Step()` 1번이면 보스 `Health`는 120이다 (제자리에서도 범위 안 적은 공격한다).
- 같은 상황에서 `DiscipleHealth`는 86이다 (적도 평소처럼 공격한다).
- 기본 전투에서 `SetHoldPosition(true)` 뒤 `Reset()`을 부르면 `IsHoldingPosition`은 `false`다.
- 기본 전투에서 `SetPaused(true)` 상태로 `SetHoldPosition(true)`를 부르면 `IsHoldingPosition`은 `true`다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 스테이지 코드(`Assets/Scripts/Core/Stage/`)와 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다. 082·083이 한다.
- `SetPaused`, 스킬, 공격 규칙을 바꾸지 않는다.
- `UnityEngine`을 쓰지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 제자리 유지 상태와 설정 메서드를 추가하고, 스킬 이동 제한과 독립적으로 제자의 전진을 막도록 했다. 초기화 시 유지 상태를 해제한다.
- 전투 사양과 완료 조건을 갱신하고 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs`
- `Assets/Tests/EditMode/Combat/BattleHoldPositionTests.cs` 및 `.meta`
- `docs/spec/combat.md`
- `tasks/doing/081_battle_hold_position.md` (보고서)
- `.loop/result.json` (결과)

### 가정
없음

### 사람이 확인할 것
없음

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 신규 및 기존 테스트의 결과는 루프 판정을 기다린다.


## 루프 기록 (2026-11-22 13:44)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/081_battle_hold_position
- 루프 브랜치: loop/20261122-133943
