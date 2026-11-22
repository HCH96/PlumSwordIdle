# 080_combat_skill_view — 스킬 발 기준점, 13타 애니메이션, 매화색 피해 숫자

## 목표
Game(`PlumSword.Game.Combat`)의 스킬 화면을 고친다. 스킬 시트의 발 기준점을 바로잡아 제자가 떠오르지 않게 하고, 애니메이션을 13타 일정에 맞춘다. 스킬 타의 피해 숫자는 매화색 숫자 그림으로 표시한다.

## 묶음
skill_hits

## 선행 작업
- 078_battle_skill_hits

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/dev/assets.md (제자 스킬 시트, 피해 숫자 줄)
- docs/spec/combat.md ("화면"의 스킬·피해 숫자 항목, "연결 지점"의 Battle)

## 요구사항
- **발 기준점**: `Assets/Scripts/Game/Combat/DiscipleSprites.cs`의 `SliceSkill`에서 스킬 프레임 기준점을 (58, 6)px에서 **(58, 18)px**로 바꾼다. 시트(99×105 칸, 8열×4행, 32프레임)의 발바닥은 위에서 86번째 줄(아래에서 18px)이라, 지금 값이면 스킬 동안 제자가 12px(화면 1.5배) 떠오른다. 대기 그림의 기준점 (38, 12)px은 발바닥 바로 아래로, 이 값과 같은 기준이다.
- **애니메이션 일정**: 078의 `Battle`은 스킬을 쓴 칸을 0번째로 세어 `Battle.SkillHitSteps` 칸마다 13타를 주고, `Battle.SkillActiveSteps`(63칸, 1.26초) 동안 `IsSkillActive`를 `true`로 둔다. 화면은 `SkillUsed` 때 스킬 시트를 **프레임 2부터** 24fps로 재생해 프레임 31에서 끝낸다 (30프레임, 1.25초). 0번 타가 프레임 2의 검격에 맞는다. 재생 중 멈춤·패배·초기화 처리(멈추면 프레임도 멈춤, 패배·초기화하면 중단, 다시 발동하면 프레임 2부터)는 지금과 같다.
- **공격 동작 표시**: `IsSkillActive`가 `true`일 때 받은 `EnemyDamaged`는 `DiscipleAnimator`에 넘기는 `attacked`로 세지 않는다. 그래서 스킬이 끝난 뒤 기본 공격 동작이 잘못 시작되지 않는다. 피격 번쩍임과 체력 바는 지금처럼 매 타마다 반영한다.
- **매화색 피해 숫자**: `Assets/Resources/Art/UI/damage_numbers_plum.png`(2172×724, 커밋되어 있음, 가져오기 Point·4096)를 `Resources.Load<Sprite>("Art/UI/damage_numbers_plum")`로 불러 0~9와 쉼표를 자른다. 이 시트는 아이보리 시트와 글자 위치가 달라 따로 잘라야 한다. 글자별 가로 범위(왼쪽~오른쪽 px, 끝 포함, 0~9·쉼표 순서): 31~217, 233~384, 403~581, 589~760, 771~954, 959~1144, 1152~1326, 1344~1517, 1522~1696, 1710~1887, 1912~2004. 세로는 아이보리와 같이 아래에서 214px 위치부터 높이 300px을 쓴다 (글자는 위에서 231~507줄에 있다). 크기 검사(2172×724가 아니면 쓰지 않음)도 아이보리와 같다.
- `EnemyDamaged`를 받는 순간 `Battle.IsSkillActive`가 `true`면 그 적의 피해 숫자를 매화색 글자로 만든다. 그 밖의 피해(기본 공격, 제자가 받는 피해)는 아이보리 그대로다. 크기·간격·떠오름·사라짐 규칙은 같다. 매화색 그림이 없으면 아이보리로 대신한다.
- 끝나면 `docs/spec/combat.md` "화면"의 스킬 항목(기준점 (58, 18), 프레임 2~31 재생, 13타)과 피해 숫자 항목("스킬 타의 피해는 매화색"), `docs/dev/assets.md`의 제자 스킬 줄(기준점)과 피해 숫자 줄(매화색 적용, 글자 범위)을 고친다.
- **기존 테스트** (`Assets/Tests/PlayMode/Combat/CombatSkillTests.cs`): 이름은 그대로 두고 값만 바꾼다. 어서트 수는 줄이지 않는다.
  - `SkillUsed_PlaysApprovedAnimation_PausesAndResetStopsIt`: `battle.Step()` 직후의 `Is.EqualTo("disciple_skill_00")` → `Is.EqualTo("disciple_skill_02")`. 0.2초 뒤의 `Is.Not.EqualTo("disciple_skill_00")` → `Is.Not.EqualTo("disciple_skill_02")`.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (`CombatSkillTests`는 요구사항에 적은 값으로).
- [플레이 모드] `Assets/Tests/PlayMode/Combat/CombatSkillHitViewTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 쓰는 법은 `docs/dev/architecture.md` "테스트" 절을 따르고, 기존 `CombatSkillTests`처럼 `CombatView`를 직접 만든다.
- [플레이 모드] `DiscipleSprites`로 자른 스킬 프레임 0~31의 `pivot`은 모두 (58, 18)px이다.
- [플레이 모드] 스킬이 켜진 전투(공격력 10, 체력 100)에 `AddEnemy(Boss, 2, 1.0)`을 두고 `battle.Step()` 1번 뒤, `[DamageNumber]` 가운데 보스 쪽 숫자의 글자 스프라이트 텍스처 이름은 `damage_numbers_plum`이다.
- [플레이 모드] 스킬이 꺼진 전투에 `AddEnemy(Normal, 1, 0.8)`을 두고 `battle.Step()` 1번 뒤, 적 쪽 피해 숫자의 글자 스프라이트 텍스처 이름은 `damage_numbers`다.
- [플레이 모드] 스킬이 켜진 전투에 `AddEnemy(Boss, 2, 1.0)`을 두고 `battle.Step()` 1번 뒤 제자 몸 스프라이트 이름은 `disciple_skill_02`다.
- [플레이 모드] 같은 상황에서 실제 시간 1.4초 뒤 제자 몸 스프라이트 이름은 `disciple_skill_`로 시작하지 않는다 (스킬 재생이 1.25초에 끝난다).
- [사람 확인] 게임을 실행해 스킬이 나갈 때 제자의 발이 대기·달리기 때와 같은 높이에 있고 떠오르지 않는다. 에디터에서 스킬 발동 순간을 멈춰 보며 확인한다.
- [사람 확인] 스킬이 나가는 동안 제자가 앞으로 이동하지 않고, 검격마다 적 위에 매화색 숫자가 하나씩 떠오른다. 에디터에서 재생하며 확인한다.

## 하지 말 것
- `Assets/Scripts/Core/`(전투·소리 로직)를 고치지 않는다. 078·079가 했다.
- 스킬 시트·피해 숫자 그림 파일과 그 `.meta`를 고치지 않는다.
- 스킬 버튼, 재사용 대기 표시, 스킬 소리를 더하지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- `GameRoot`의 다른 기능 조립 줄을 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 스킬 프레임의 발 기준점을 고치고 2~31번을 24fps로 재생한다.
- 스킬 타는 기본 공격 동작으로 세지 않고 매화색 피해 숫자를 표시하며, 매화색 시트를 쓸 수 없으면 아이보리로 대체한다.
- 프레임 피벗, 피해 숫자 색상, 애니메이션 시작과 종료를 검증하는 플레이 모드 테스트를 추가하고 기존 테스트의 기대 프레임을 수정했다.
- 피해 숫자 색상 테스트 두 개가 각각 본문에서 텍스처 이름을 직접 검증하도록 수정해 관문이 검사를 인식하도록 했다.
- 전투 사양과 에셋 목록에 화면 동작과 글자 범위를 반영했다.

### 바뀐 파일
- `Assets/Scripts/Game/Combat/DiscipleSprites.cs`, `Assets/Scripts/Game/Combat/CombatView.cs`
- `Assets/Tests/PlayMode/Combat/CombatSkillTests.cs`, `Assets/Tests/PlayMode/Combat/CombatSkillHitViewTests.cs` 및 `.meta`
- `docs/spec/combat.md`, `docs/dev/assets.md`
- `tasks/doing/080_combat_skill_view.md`, `.loop/result.json`

### 가정
- 없음.

### 사람이 확인할 것
- 게임을 실행해 스킬이 나갈 때 제자의 발이 대기·달리기 때와 같은 높이에 있고 떠오르지 않는지 에디터에서 발동 순간을 멈춰 확인한다.
- 스킬이 나가는 동안 제자가 앞으로 이동하지 않고, 검격마다 적 위에 매화색 숫자가 하나씩 떠오르는지 에디터에서 재생하며 확인한다.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 컴파일 및 테스트 판정을 기다린다.
- `git diff --check`에서 공백 오류가 없었다. 관문 재검사도 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 13:37)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/080_combat_skill_view
- 루프 브랜치: loop/20261122-131718
