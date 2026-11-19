# 055_combat_skill_view — 스킬 검기 띠 표시와 게임 조립

## 목표
Core의 `CombatLayout`에 검기 띠 불투명도 규칙을 더하고, Game의 `CombatView`가 `SkillUsed`를 받아 임시 검기 띠를 보여 주며, `GameRoot`가 스킬과 경지를 켠 `Battle`을 만들게 한다. Core 표시 규칙 + Game 화면 작업이다.

## 묶음
combat_m3

## 선행 작업
- 054_battle_skill_attack

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/dev/art.md
- docs/spec/combat.md ("수치"의 검기 띠, "연결 지점"의 `CombatLayout`·`GameRoot`, "화면"의 "스킬 (이십사수매화검법)", "완료 조건 초안"의 68~71, "변경 기록")
- docs/spec/realm.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-19 — 3단계: 이십사수매화검법, 돌파 때 현재 체력 증가, 공격 속도 제거 반영` 항목 중 **검기 띠 표시와 `GameRoot` 조립** 부분만 구현한다. `Battle`의 스킬과 돌파 체력은 052~054가 이미 했다.
- `Assets/Scripts/Core/Combat/CombatLayout.cs`:
  - `public const double SkillSlashSeconds = 0.25;` (임시값)
  - `public static double SkillSlashAlpha(double elapsedSeconds)`: `0.6 × (1 − elapsedSeconds / 0.25)`를 0.0 ~ 0.6으로 잘라낸다. 음수 시간은 0.6, `NaN`은 0.0, 0.25 이상은 0.0.
- `Assets/Scripts/Game/Combat/CombatView.cs`:
  - `Initialize`에서 `battle.SkillUsed`를 구독하고 `OnDestroy`에서 끊는다 (다른 알림과 같은 방식).
  - 검기 띠 오브젝트 `[SkillSlash]`를 `CombatView`의 자식으로 하나 만들어 두고 처음에는 숨긴다. 기존 `CreateRect`처럼 1×1 흰색 스프라이트를 쓰고, 크기 가로 2.0 × 세로 0.4, 색 RGB (1.0, 0.55, 0.75), 그리는 순서(`sortingOrder`) 20000 (모두 임시값).
  - `SkillUsed`를 받으면 띠를 보이고, 가운데를 `(DisciplePosition − 2.25 + 1.0, −1.5 + 0.75, 0)`에 둔다 (제자 표시 위치 기준: 왼쪽 끝이 발의 x, 세로 가운데가 발 위 0.75). 쏜 뒤 흐른 시간을 0으로 한다. 띠는 제자를 따라가지 않는다.
  - 매 프레임(`Update`에서 `Tick` 뒤) 흐른 시간에 `Time.deltaTime`을 더하고, 색의 알파를 `(float)CombatLayout.SkillSlashAlpha(흐른 시간)`으로 둔다. 흐른 시간이 `CombatLayout.SkillSlashSeconds` 이상이면 숨긴다.
  - 보이는 중에 또 `SkillUsed`가 오면 새 위치에서 처음부터 센다. `ResetCompleted`를 받으면 숨긴다.
  - 제자 애니메이션(`DiscipleAnimator`)과 나머지 표시는 바꾸지 않는다. 스킬도 `EnemyDamaged`를 보내므로 기존 공격 애니메이션이 그대로 재생된다.
- `Assets/Scripts/Game/GameRoot.cs`: `Battle = new PlumSword.Core.Combat.Battle(StatUpgrades);` 한 줄만 `Battle = new PlumSword.Core.Combat.Battle(StatUpgrades, Realm, true);`로 바꾼다. `Realm`은 이미 그 앞에서 만들어진다. 다른 줄은 고치지 않는다.
- 쓰는 연결 지점 (054까지 만든 것): `Battle.SkillUsed`, `Battle.DisciplePosition`, `Battle.ResetCompleted`, `Battle.SkillEnabled`, 생성자 `Battle(StatUpgrades upgrades, RealmProgress realm = null, bool skillEnabled = false)`.
- 에딧 모드 테스트는 `Assets/Tests/EditMode/Combat/CombatLayoutTests.cs`(이미 있는 파일, 네임스페이스 `PlumSword.Core.Tests.Combat`)에 더한다. 있던 테스트는 고치지 않는다.
- 플레이 모드 테스트는 `Assets/Tests/PlayMode/Combat/CombatSkillTests.cs`(네임스페이스 `PlumSword.Game.Tests.Combat`)에 쓴다. `GameBootTests`처럼 `SceneManager.LoadSceneAsync(0)`으로 첫 씬을 불러온 뒤 `GameObject.Find(GameBootstrap.RootName)`의 `GameRoot`를 읽는다.

## 완료 조건
- [테스트] `Assets/Tests/EditMode/Combat/CombatLayoutTests.cs`에 아래 `CombatLayout` 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [테스트] `CombatLayout.SkillSlashAlpha`는 0 → 0.6, 0.125 → 0.3, 0.25 → 0.0, 0.3 → 0.0을 돌려준다.
- [테스트] `CombatLayout.SkillSlashAlpha`는 −0.1 → 0.6, `double.NaN` → 0.0을 돌려준다.
- [테스트] `CombatLayout.SkillSlashSeconds`는 0.25다.
- [플레이 모드] `Assets/Tests/PlayMode/Combat/CombatSkillTests.cs`에서 첫 씬을 불러온 뒤 `[Game]`의 `GameRoot.Battle.SkillEnabled`는 `true`다.
- [플레이 모드] 같은 테스트에서 `[Game]` 아래(자식의 자식 포함)에 `[SkillSlash]` 오브젝트가 있고, 그 `SpriteRenderer`는 처음에 보이지 않는다(`enabled`가 `false`이거나 오브젝트가 비활성).
- [컴파일] 화면 코드가 컴파일되고 기존 테스트(`GameBootTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity`, 기존 전투 테스트 포함)가 모두 통과한다.
- [사람 확인] 에디터에서 게임을 시작해 제자가 첫 몬스터를 공격할 때 제자 앞쪽에 연분홍 띠가 잠깐 보였다 사라지는지, 그 뒤 약 5초마다 같은 띠가 보이는지 본다.
- [사람 확인] 에디터에서 금화를 모아 이류로 돌파해, 그 자리에서 제자 체력 바가 늘어난 최대 체력에 맞게 차오르는지 본다 (체력이 가득일 때 돌파하면 계속 가득이다).

## 하지 말 것
- Core의 `Battle.cs`는 고치지 않는다 (052~054가 끝냈다). Core에서 고치는 것은 `CombatLayout.cs`뿐이다.
- `DiscipleAnimator`, 제자·몬스터 스프라이트 처리, 다른 기능의 화면 코드(`Assets/Scripts/Game/Realm/`, `Stage/`, `StatUpgrade/` 등)는 고치지 않는다.
- `GameRoot`는 `Battle` 생성 한 줄만 바꾸고 다른 기능의 줄은 고치지 않는다.
- 스킬 버튼, 재사용 대기 표시, 스킬 애니메이션 시트는 만들지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의는 고치지 않는다. 화면은 코드로 조립한다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`는 고치지 않는다.

## 보고서

### 한 일
- 검기 띠 불투명도와 지속 시간 규칙을 추가하고 스킬 발동·시간 경과·초기화에 따라 띠를 표시한다.
- GameRoot가 경지와 스킬을 연결한 전투를 만들도록 조립하고 에딧·플레이 모드 테스트를 추가했다.
- 전투 사양의 화면 테스트 설명을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/Combat/CombatLayout.cs`
- `Assets/Scripts/Game/Combat/CombatView.cs`
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Tests/EditMode/Combat/CombatLayoutTests.cs`
- `Assets/Tests/PlayMode/Combat/CombatSkillTests.cs` 및 `.meta`
- `docs/spec/combat.md`

### 가정
- 없음. 띠의 크기·색·시간과 위치는 작업 문서에 지정된 임시값을 사용했다.

### 사람이 확인할 것
- 에디터에서 게임을 시작해 제자가 첫 몬스터를 공격할 때 제자 앞쪽에 연분홍 띠가 잠깐 보였다 사라지는지, 그 뒤 약 5초마다 같은 띠가 보이는지 본다.
- 에디터에서 금화를 모아 이류로 돌파해, 그 자리에서 제자 체력 바가 늘어난 최대 체력에 맞게 차오르는지 본다 (체력이 가득일 때 돌파하면 계속 가득이다).

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 컴파일과 전체 테스트 결과는 루프 판정을 기다린다.


## 루프 기록 (2026-11-19 18:39)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/055_combat_skill_view
- 루프 브랜치: loop/20261119-183146
