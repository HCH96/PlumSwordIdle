# 040_leave_game — 떠나기 전 저장 판정

## 목표
Core(`PlumSword.Core.StartMenu`)에 `TO TITLE`·`QUIT GAME`이 함께 쓰는 떠나기 판정 `LeaveGame`을 만든다.

## 묶음
시작화면

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/start_menu.md ("동작"의 "떠나기", "연결 지점", "완료 조건 초안"의 떠나기 준비)
- docs/spec/save.md ("연결 지점" 절)

## 요구사항
- `Assets/Scripts/Core/StartMenu/LeaveGame.cs`, `public sealed class LeaveGame` (네임스페이스 `PlumSword.Core.StartMenu`).
- 생성자 `public LeaveGame(PlumSword.Core.Save.AutoSave autoSave)`: `null`이면 `ArgumentNullException`. 아무것도 바꾸지 않는다 (저장하지 않음). 시작 상태 `HasFailed == false`.
- `public bool HasFailed { get; }`: 마지막 `TryLeave`가 실패했으면 `true`, 성공했으면 `false`.
- `public bool TryLeave(long nowUtcTicks)`: `autoSave.SaveNow(nowUtcTicks)`를 부른다. `true`이면 `HasFailed = false`이고 `true`를 돌려준다. `false`이면(자동 저장이 꺼져 있어 `false`인 경우 포함) `HasFailed = true`이고 `false`를 돌려준다.
- 이미 있는 `AutoSave`(`SaveNow(long)`, `HasFailed`, 생성자의 `enabled`)를 그대로 쓴다. 고치지 않는다.
- 테스트의 "떠나기 준비": `new Wallet(0)`, 그 지갑으로 만든 `StatUpgrades`(시작 능력치 `(10, 100, 1.0)`), 스테이지 객체(새 `Battle(upgrades)`, `new System.Random(0)`, `startStage: 1`, `seenIntroStage: 1`로 만든 `StageProgress`), 가짜 저장소로 켜진(`enabled: true`) `AutoSave`, 그리고 `new LeaveGame(autoSave)`. 가짜 저장소는 기존 `Assets/Tests/EditMode/Save/FakeSaveStore.cs`(`PlumSword.Core.Tests.Save.FakeSaveStore`: `WriteCount`, `LastText`, `Succeeds`)를 쓰고 고치지 않는다. "쓴 횟수"와 "시도"는 `WriteCount`다. 기존 저장 테스트(`Assets/Tests/EditMode/Save/AutoSaveTests.cs`)가 이 준비를 어떻게 만드는지 참고한다.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/start_menu.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 테스트 네임스페이스는 `PlumSword.Core.Tests.StartMenu`.

## 완료 조건
- `Assets/Tests/EditMode/StartMenu/LeaveGameTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `new LeaveGame(null)`은 `ArgumentNullException`이다.
- 떠나기 준비 직후 `HasFailed`는 `false`이고 쓴 횟수는 0이다.
- 떠나기 준비 뒤 `wallet.Add(10)`을 하고 `TryLeave(100)`을 하면 `true`이고, `HasFailed`는 `false`, 쓴 횟수는 1이다.
- 위 경우 마지막 글자를 `SaveCodec.Decode`하면 `Gold` 10, `SavedAtUtcTicks` 100이다.
- 떠나기 준비에서 가짜 저장소를 실패하게 하고 `TryLeave(100)`을 하면 `false`, `HasFailed` `true`, 시도 1, `autoSave.HasFailed` `true`이다.
- 이어서 `TryLeave(200)`을 하면 `false`, 시도 2이다.
- 이어서 저장소를 성공하게 바꾸고 `TryLeave(300)`을 하면 `true`, `HasFailed` `false`, 시도 3, 마지막 글자의 `SavedAtUtcTicks`는 300이다.
- 떠나기 준비를 `AutoSave`를 `enabled: false`로 바꿔 만들고 `TryLeave(100)`을 하면 `false`, `HasFailed` `true`, 쓴 횟수 0이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `UnityEngine`을 쓰지 않는다.
- `Assets/Scripts/Core/Save/`와 `Assets/Tests/EditMode/Save/`(가짜 저장소 포함)를 고치지 않는다.
- 화면(`SettingsView`)은 만들지 않는다 (043이 한다).
- 씬·프리팹·에셋, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- 저장 결과에 따라 떠나기 성공과 마지막 실패 상태를 판정하는 `LeaveGame`을 구현했다.
- 초기 상태, 저장 내용, 실패와 즉시 재시도, 비활성 자동 저장을 확인하는 에딧 모드 테스트를 작성했다.

### 바뀐 파일
- `Assets/Scripts/Core/StartMenu/LeaveGame.cs` 및 `.meta`
- `Assets/Tests/EditMode/StartMenu/LeaveGameTests.cs` 및 `.meta`
- `tasks/doing/040_leave_game.md`, `.loop/result.json`

### 가정
없음. 공개 연결 지점과 동작은 기존 사양을 따른다.

### 사람이 확인할 것
없음.

### 테스트 결과
- Unity 테스트는 직접 실행하지 않았으며, 신규 및 기존 테스트의 실행과 결과 판정은 루프를 기다린다.
- `git diff --check` 통과.


## 루프 기록 (2026-11-17 18:31)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/040_leave_game
- 루프 브랜치: loop/20261117-182336
