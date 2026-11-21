# 071_sfx_triggers — 게임 알림을 효과음 요청으로 잇기

## 목표
Core(`PlumSword.Core.Audio`)에 전투·스테이지·강화·경지 알림을 구독해 `SfxQueue`에 효과음을 요청하는 `SfxTriggers`를 만든다.

## 묶음
audio

## 선행 작업
- 070_sfx_queue_voice_pool

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/audio.md ("동작"의 "효과음"(붙는 곳 표, 구간 사이 페이드 판정), "경계 상황" 표, "연결 지점"의 `SfxTriggers`와 "필요한 것", "완료 조건 초안" 23~31)
- docs/spec/combat.md ("연결 지점" 절: `Battle`, `Enemy`, `EnemyKind`)
- docs/spec/stage.md ("연결 지점" 절: `StageProgress`, `StagePhase`)
- docs/spec/stat_upgrade.md ("연결 지점" 절: `StatUpgrades`)
- docs/spec/realm.md ("연결 지점" 절: `RealmProgress`)

## 요구사항
- 파일 `Assets/Scripts/Core/Audio/SfxTriggers.cs`, `public sealed class SfxTriggers : System.IDisposable` (네임스페이스 `PlumSword.Core.Audio`). `UnityEngine`을 쓰지 않는다.
- 070이 만든 `SfxQueue`(`Request(SfxId)`, `Drain(double)`, `HasPending`)와 068이 만든 `SfxId`를 쓴다.
- 이 작업이 정하는 연결 지점은 사양 "연결 지점" 절 그대로다. 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/audio.md`)의 연결 지점 절을 고치고 보고서의 가정에 적는다.
  - `public SfxTriggers(SfxQueue queue, PlumSword.Core.Combat.Battle battle, PlumSword.Core.Stage.StageProgress stage, PlumSword.Core.StatUpgrade.StatUpgrades upgrades, PlumSword.Core.Realm.RealmProgress realm)`: 인자 중 하나라도 `null`이면 `ArgumentNullException`이고 아무것도 구독하지 않는다 (null 검사를 모두 끝낸 뒤 구독한다).
  - `public void Dispose()`: 구독을 모두 끊는다. 두 번째부터는 아무 일 없음.
- 구독과 요청 (다른 기능의 공개 알림만 쓴다):
  - `battle.EnemyDamaged` → `SwordSwing`, `AttackHit`. (스킬 프레임에서 이 둘이 빠지는 것은 `SfxQueue.Drain`의 "스킬이 대신"이 한다. 여기서는 그대로 요청한다)
  - `battle.SkillUsed` → `PlumSwordSkill`.
  - `battle.EnemyDefeated`에서 `enemy.Kind == EnemyKind.Normal` → `EnemyDefeated`. 보스는 요청하지 않는다.
  - `battle.EnemyAdded`에서 `enemy.Kind == EnemyKind.Boss` → `BossAppear`. 일반 적은 요청하지 않는다.
  - `stage.StageCleared` → `BossDefeated`.
  - `stage.StageFailed` → `PlayerDefeated`.
  - `stage.Changed`: 직전 `Phase`가 `StagePhase.FadingOut`이 아니었고 지금 `stage.Phase == StagePhase.FadingOut`이며 `stage.Boss == null`이고 `battle.IsDiscipleDefeated == false`이면 `StageTransition`. 판정 뒤 직전 `Phase`를 지금 값으로 바꾼다. 직전 `Phase`는 생성자에서 `stage.Phase`로 시작한다.
  - `upgrades.Upgraded` → `UpgradeSuccess`.
  - `realm.BrokeThrough` → `RealmBreakthrough`.
- 테스트는 `Assets/Tests/EditMode/Audio/SfxTriggersTests.cs` (네임스페이스 `PlumSword.Core.Tests.Audio`).
  - 테스트의 큐는 `new SfxQueue(0)`(간격 없음)을 써서, 앞 프레임의 같은 소리가 간격 규칙으로 가려지지 않게 한다. `Drain`에 넘기는 시각은 프레임마다 늘어나는 아무 값이어도 된다.
  - "그 프레임의 `Drain`"은: 전투·스테이지를 한 번 진행(`Battle.Step()` 또는 `StageProgress.Tick(...)`)할 때마다 바로 `Drain`을 불러 그 결과를 그 진행의 결과로 본다. 조건이 일어난 진행(예: `Phase`가 `FadingOut`이 된 진행, `StageCleared`가 온 진행)의 결과를 검사한다.
  - 실제 객체로 만든다: `var wallet = new Wallet(<금화>)`, `var realm = new RealmProgress(wallet)`, `var upgrades = new StatUpgrades(wallet, <공격력>, <최대 체력>, realm: realm)`, `var battle = new Battle(upgrades, realm, <skillEnabled>)`, `var stage = new StageProgress(wallet, upgrades, battle, new System.Random(0) 또는 같은 테스트 어셈블리의 MinRandom, <시작 스테이지>)`. 진행 방법은 기존 테스트 `Assets/Tests/EditMode/Stage/StageProgressOutcomeTests.cs`의 `FullStagePlayed_SixGroupsThenBossCleared`, `BossAndDiscipleDefeatedTogether_FailedWithoutBossGold`를 참고한다 (`Begin()` 뒤 `Tick(StageProgress.SubstageIntroSeconds)`로 구간 소개를 넘기고, `FadingOut`·`FadingIn`이면 `Tick(StageProgress.FadeSeconds * 2)`, 구간 소개면 `Tick(StageProgress.SubstageIntroSeconds)`, 아니면 `battle.Step()`). 반복에는 상한(예: 20000번)을 둔다.
  - 완료 조건 24·25·26·27의 위치: "제자 좌표"는 `battle.DisciplePosition`. 적은 `battle.AddEnemy(kind, 1, battle.DisciplePosition + 0.5)`처럼 둔다. 이 조건들은 `StageProgress` 없이 `Battle`만으로 해도 된다 (생성자에는 `StageProgress`가 필요하니 만들되 `Begin`하지 않아도 된다).
  - **보스를 이기게 하는 방법**: `StatUpgrades`를 충분히 강하게 만든다. 예: `new StatUpgrades(wallet, 1000, 100000, realm: realm)` (기존 `FullStagePlayed_SixGroupsThenBossCleared`가 이 값으로 1스테이지 보스까지 이긴다). 그리고 보스는 반드시 **`StageProgress`가 배치한 보스**여야 한다 (1스테이지에서 6구간을 모두 이겨 보스 구간에 가거나, 시작 스테이지 5로 만들어 `Begin()`이 최종 보스를 바로 배치하게 한다). `battle.AddEnemy`로 직접 넣은 보스는 `StageProgress.Boss`가 `null`이라 사양 규칙상 `StageTransition`이 함께 나므로 완료 조건 29의 "`StageTransition` 없음"에 쓰지 않는다.
  - 제자가 쓰러지게 하는 방법: 약한 `StatUpgrades`(예: 공격력 1, 최대 체력 1). 일반 구간은 시작 스테이지 1, 보스 구간은 시작 스테이지 5(`Begin()`이 최종 보스를 배치)로 한다. 보스와 제자가 같은 칸에 쓰러지는 경우는 `BossAndDiscipleDefeatedTogether_FailedWithoutBossGold`의 설정(공격력 100, 최대 체력 10, 보스를 `battle.AddEnemy(EnemyKind.Boss, 1, 1.0)`으로 넣고 `Step()` 한 번)을 그대로 써도 된다. 이 경우는 `StageCleared`가 오지 않으므로 `BossDefeated`가 없고 `StageFailed`로 `PlayerDefeated`가 난다.
  - 완료 조건 30의 강화 실패: 금화 0인 지갑에서 `upgrades.TryUpgrade(StatType.Attack)`. 성공은 금화를 충분히(예: 1000000) 준 지갑에서. 돌파: `realm.RecordKill()`을 `realm.KillsRequired`번 불러 `BrokeThrough`가 오게 한다.

## 완료 조건
- `Assets/Tests/EditMode/Audio/SfxTriggersTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- (사양 23) `SfxTriggers` 생성자의 다섯 인자(`queue`, `battle`, `stage`, `upgrades`, `realm`) 중 어느 하나만 `null`이어도 `ArgumentNullException`이다 (다섯 경우 모두 검사).
- (사양 24) `battle.AddEnemy(EnemyKind.Boss, 1, 제자 좌표 + 0.5)` 뒤 `Drain` → `[BossAppear]`이다.
- (사양 24) `battle.AddEnemy(EnemyKind.Normal, 1, 제자 좌표 + 0.5)` 뒤 `Drain` → 빈 목록이다.
- (사양 25) 스킬이 꺼진 `Battle`(`skillEnabled: false`)에 일반 적 하나를 제자 앞 0.5에 두고 첫 `EnemyDamaged`가 올 때까지 `Step` → 그 진행의 `Drain` → `[SwordSwing, AttackHit]`이다.
- (사양 26) 스킬이 켜진 `Battle`(`skillEnabled: true`)에 일반 적 하나를 제자 앞 0.5에 두고 첫 `SkillUsed`가 올 때까지 `Step` → 그 진행의 `Drain` 결과에 `PlumSwordSkill`이 있고 `SwordSwing`, `AttackHit`이 없다.
- (사양 27) 일반 적이 쓰러진 진행의 `Drain` 결과에 `EnemyDefeated`가 있다.
- (사양 27) 보스가 쓰러진 진행의 `Drain` 결과에 `EnemyDefeated`가 없다.
- (사양 28) `StageProgress`로 1스테이지 1구간의 무리를 모두 쓰러뜨려 `Phase`가 `FadingOut`이 된 진행의 `Drain` 결과에 `StageTransition`과 `EnemyDefeated`가 있다.
- (사양 29) `StageProgress`가 배치한 보스를 제자가 혼자 이긴 진행(`StageCleared`가 온 진행)의 `Drain` 결과에 `BossDefeated`가 있고 `StageTransition`이 없다.
- (사양 29) 일반 구간(시작 스테이지 1)에서 제자가 쓰러진 진행(`StageFailed`가 온 진행)의 `Drain` 결과에 `PlayerDefeated`가 있고 `StageTransition`이 없다.
- (사양 29) 보스 구간(시작 스테이지 5의 최종 보스)에서 제자가 쓰러진 진행의 `Drain` 결과에 `PlayerDefeated`가 있고 `StageTransition`이 없다.
- (사양 29) 보스와 제자가 같은 칸에 쓰러진 진행의 `Drain` 결과에 `BossDefeated`가 없고 `PlayerDefeated`가 있다.
- (사양 30) 금화가 충분한 지갑에서 `StatUpgrades.TryUpgrade` 성공 뒤 `Drain` → `[UpgradeSuccess]`이다.
- (사양 30) 금화 0인 지갑에서 `TryUpgrade` 실패 뒤 `Drain` → 빈 목록이다.
- (사양 30) `RealmProgress`가 돌파(`BrokeThrough`)하면 그 뒤 `Drain` 결과에 `RealmBreakthrough`가 있다.
- (사양 31) `Dispose` 뒤에는 보스 추가(24의 알림)와 강화 성공(30의 알림)이 와도 `Drain` → 빈 목록이다.
- (사양 31) `Dispose`를 두 번 불러도 예외가 없다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 다른 기능의 Core 코드(`Assets/Scripts/Core/Combat/`, `Stage/`, `StatUpgrade/`, `Realm/`, `Currency/` 등 `Audio` 밖의 폴더)는 고치지 않는다. 공개 알림과 속성(연결 지점)만 쓴다. 테스트를 쉽게 하려고 그쪽에 메서드나 알림을 더하지 않는다.
- 기존 테스트 파일(`Assets/Tests/EditMode/Stage/` 등)은 고치지 않는다. 참고만 한다 (`MinRandom`은 같은 어셈블리라 그대로 써도 된다).
- 068·070이 만든 `SfxId`, `AudioCatalog`, `SfxQueue` 등의 공개 모양과 값을 바꾸지 않는다. `AudioCatalog`의 임시값(배율, 켜짐, 간격, 재생기 수, 교차 페이드 시간)을 사양 표와 다르게 바꾸지 않는다.
- `UiSoundWatch`(072), Game 쪽 화면 코드(073)는 만들지 않는다.
- 다른 기능의 화면 코드(`StatUpgradeView`, `SettingsView`, `StartScreenView`, `OfflineRewardView`, `SaveErrorView` 등)는 고치지 않는다.
- 씬·프리팹·에셋 파일, 오디오 파일, 오디오 `.meta` 가져오기 설정, 프로젝트 오디오 설정은 고치지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 전투, 스테이지, 강화, 경지의 공개 알림을 효과음 요청에 연결하고 구독 해제를 구현했다.
- 실제 Core 객체를 사용해 발생 프레임의 큐 결과와 생성자 및 해제 조건을 검사하는 에딧 모드 테스트를 추가했다.
- NUnit 목록 멤버 검사로 수정해 `SfxId`를 문자열 인자로 취급한 컴파일 오류를 바로잡았다.

### 바뀐 파일
- `Assets/Scripts/Core/Audio/SfxTriggers.cs` 및 `.meta`
- `Assets/Tests/EditMode/Audio/SfxTriggersTests.cs` 및 `.meta`
- `tasks/doing/071_sfx_triggers.md` 보고서, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점과 동작은 기존 오디오 사양을 따른다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- 이전 루프 실행에서는 테스트의 `Does.Contain(SfxId)`에서 컴파일 오류가 발생해 수정했다.
- 수정 후 Unity 테스트는 직접 실행하지 않았다. 추가한 테스트와 기존 테스트의 실행 및 판정은 루프 스크립트를 기다린다.


## 루프 기록 (2026-11-22 05:30)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/071_sfx_triggers
- 루프 브랜치: loop/20261122-050216
