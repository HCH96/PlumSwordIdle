# 053_battle_skill_state — 스킬 켜기, 스킬 상태와 피해 계산

## 목표
`PlumSword.Core.Combat.Battle`에 이십사수매화검법의 켜기 인자, 상수, 상태 속성(`SkillEnabled`, `SkillCooldown`, `IsSkillReady`, `SkillDamage`)과 피해 계산 `CalculateSkillDamage`를 더한다. 스킬 발동(공격 차례에 쓰기)은 054가 한다. Core 작업이다.

## 묶음
combat_m3

## 선행 작업
- 052_battle_realm_health

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/combat.md ("동작"의 "스킬 켜기"·"강화·경지 반영", "수치", "연결 지점", "완료 조건 초안"의 46~48, "변경 기록")
- docs/spec/stat_upgrade.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-19 — 3단계: 이십사수매화검법, 돌파 때 현재 체력 증가, 공격 속도 제거 반영` 항목 중 **스킬의 상태와 피해 계산** 부분만 구현한다. 돌파 체력은 052가 이미 했고, 스킬 발동·`SkillUsed`·스킬 대기 감소는 054가 한다.
- `Assets/Scripts/Core/Combat/Battle.cs`:
  - 상수: `public const double SkillRange = 2.0;`, `public const double SkillCooldownSeconds = 5.0;`, `public const int SkillBasePercent = 200;`, `public const int SkillPercentPerInnerLevel = 10;` (모두 임시값).
  - 생성자 끝(052가 더한 `realm` 뒤)에 `bool skillEnabled = false`를 더한다. 모양: `public Battle(StatUpgrades upgrades, RealmProgress realm = null, bool skillEnabled = false)`.
  - `public bool SkillEnabled { get; }`: 생성자의 `skillEnabled`.
  - `public double SkillCooldown { get; }`: 스킬을 다시 쓸 수 있을 때까지 남은 시간(초). 0 이상. 처음 0. 이 작업에서는 바뀌는 곳이 없다 (054가 줄이고 정한다). `Reset()`에서 0으로 되돌리는 줄은 이 작업에서 넣는다.
  - `public bool IsSkillReady { get; }`: `SkillEnabled`이고 `SkillCooldown <= Epsilon`(1e-9)이면 `true`.
  - `public long SkillDamage { get; }`: `CalculateSkillDamage(upgrades.Attack, upgrades.GetLevel(StatType.InnerPower))`. 읽을 때마다 계산하고 저장해 두지 않는다. `SkillEnabled`가 `false`여도 같은 값을 돌려준다.
  - `public static long CalculateSkillDamage(long attack, int innerPowerLevel)`: `attack × (SkillBasePercent + SkillPercentPerInnerLevel × innerPowerLevel) / 100`. `long`으로 계산한다 (`innerPowerLevel`을 `long`으로 바꾼 뒤 곱한다). 나눗셈은 `long` 정수 나눗셈(= 소수점 내림). `attack`이 음수이거나 `innerPowerLevel`이 음수면 `ArgumentOutOfRangeException`. 아주 큰 값의 넘침은 다루지 않는다.
  - 생성자·클래스 주석에 `skillEnabled` 설명을 더한다.
- 이 작업이 끝나도 전투 동작(이동·공격·대기)은 지금과 똑같아야 한다. 스킬은 아직 발동하지 않는다.
- 수치(참고, 강화 기능의 것): `StatUpgrades.Attack` = `(baseAttack + 2 × 공격력 단계) × (100 + 경지 보너스) / 100`. 강화 비용은 공격력·내공 모두 단계 0에서 20금화.
- 테스트에서 지갑은 `new Wallet(1000)`, 강화는 `new StatUpgrades(wallet, 10, 100)`이다. "스킬 전투"는 `new Battle(upgrades, null, true)`, "기본 전투"는 `new Battle(upgrades)`다.
- 새 테스트는 `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`(네임스페이스 `PlumSword.Core.Tests.Combat`)에 쓴다. 054도 이 파일에 테스트를 더한다.
- 기존 테스트는 고치지 않는다. `skillEnabled` 기본값이 `false`라 기존 테스트는 그대로 통과해야 한다.
- 구현하다 연결 지점 모양이 사양과 달라지면 사양 문서 `docs/spec/combat.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Combat/BattleSkillTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `Battle.CalculateSkillDamage`는 (10, 0) → 20, (10, 1) → 21, (13, 2) → 28, (11, 0) → 22, (0, 5) → 0을 돌려준다.
- `Battle.CalculateSkillDamage(10, int.MaxValue)`는 예외 없이 2147483667을 돌려준다.
- `Battle.CalculateSkillDamage(-1, 0)`과 `Battle.CalculateSkillDamage(10, -1)`은 각각 `ArgumentOutOfRangeException`을 던진다.
- 스킬 전투를 새로 만들면 `SkillEnabled`는 `true`, `SkillCooldown`은 0, `IsSkillReady`는 `true`, `SkillDamage`는 20이다.
- 기본 전투를 새로 만들면 `SkillEnabled`는 `false`, `SkillCooldown`은 0, `IsSkillReady`는 `false`다.
- 스킬 전투에서 `upgrades.TryUpgrade(StatType.InnerPower)` 뒤 `SkillDamage`는 21이다.
- 바로 위 상태에서 이어서 `upgrades.TryUpgrade(StatType.Attack)` 뒤 `SkillDamage`는 25다 (공격력 12 × 210% = 25.2 → 25).
- `Battle.SkillRange`는 2.0, `Battle.SkillCooldownSeconds`는 5.0, `Battle.SkillBasePercent`는 200, `Battle.SkillPercentPerInnerLevel`은 10이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 스킬 발동(공격 차례에 스킬 쓰기), `SkillUsed` 이벤트, 스킬 대기 감소는 만들지 않는다. 054가 한다.
- 강화 기능의 코드 `Assets/Scripts/Core/StatUpgrade/`는 고치지 않는다. `Attack`, `GetLevel(StatType.InnerPower)`, `TryUpgrade`(테스트에서만)로만 쓴다.
- 화면 코드 `Assets/Scripts/Game/`과 `GameRoot`는 고치지 않는다. 055가 한다.
- `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 스킬 활성화 인자, 상수, 준비 상태와 현재 강화 수치를 반영하는 피해 계산을 추가했다.
- 초기화 시 스킬 대기를 0으로 되돌리고 기존 공격 동작을 유지했다.
- 계산 경계값, 초기 상태, 강화 반영, 상수 및 기존 공격 유지를 검사하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/Battle.cs`
- `Assets/Tests/EditMode/Combat/BattleSkillTests.cs` 및 `.meta`
- `tasks/doing/053_battle_skill_state.md` (보고서)
- `.loop/result.json`

### 가정
- 없음. 공개 연결 지점과 수치는 기존 사양에 따른다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 전체 테스트 판정을 기다린다.


## 루프 기록 (2026-11-19 18:20)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/053_battle_skill_state
- 루프 브랜치: loop/20261119-181417
