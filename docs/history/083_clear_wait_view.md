# 083_clear_wait_view — 게임에 결판 뒤 1.5초 대기를 적용

## 목표
Game(`PlumSword.Game`)에서 `StageProgress`를 결판 뒤 대기 1.5초로 만든다. 스테이지 화면의 쓰러짐 대기가 그 대기와 겹치지 않게 맞춘다. 마지막 적이 쓰러진 뒤 제자가 대기 동작으로 돌아오고, 피해 숫자가 사라진 다음 전환된다.

## 묶음
clear_wait

## 선행 작업
- 082_stage_clear_wait

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/stage.md ("화면"의 쓰러짐 대기 항목들, "연결 지점". 082가 `StagePhase.ClearWait`, `ClearWaitSeconds`, `DefaultClearWaitSeconds`를 더했다)
- docs/spec/audio.md (`StageTransition` 효과음 연결 시점)

## 요구사항
- `Assets/Scripts/Game/GameRoot.cs`의 `CreateStage`에서 `StageProgress`를 만들 때 마지막 인자로 `PlumSword.Core.Stage.StageProgress.DefaultClearWaitSeconds`(1.5)를 넘긴다. 이 줄 말고 다른 기능의 조립 줄은 고치지 않는다.
- `Assets/Scripts/Game/Stage/StageView.cs`의 쓰러짐 대기(`_defeatAnimationRemaining`):
  - 지금은 `Playing`에서 바로 `FadingOut`이 된 성공 전환에서, 스테이지별 쓰러짐 시간만큼 `stage.Tick`을 늦춘다.
  - 이 규칙은 그대로 두고, `ClearWait`에서 `FadingOut`으로 바뀐 성공 전환에도 같은 조건(스테이지·구간·시트 확인)으로 대기를 정한다. 다만 대기는 `max(0, 쓰러짐 시간 − stage.ClearWaitSeconds)`이다. 결판 뒤 대기 동안 쓰러짐 그림이 이미 재생되었기 때문이다. 예: 쓰러짐 1.275초(17프레임)는 0초, 5스테이지 최종 보스 1.875초(25프레임)는 0.375초다.
  - 실패 전환(`StageFailed`)의 제자 쓰러짐 대기는 바꾸지 않는다.
- `ClearWait` 동안 전투는 멈추지 않으므로, 화면(`CombatView`)의 피해 숫자·쓰러짐·스킬 애니메이션은 지금 코드대로 계속 움직인다. 제자는 이동·공격하지 않아 대기 동작이 된다. `CombatView`는 고치지 않는다.
- `StageView`가 `Phase`를 보는 다른 곳(`_failedTransition` 초기화, 페이드 막)은 `ClearWait`을 `Playing`과 같이 페이드 0으로 다룬다. `FadeAlpha`가 0이므로 따로 고칠 것이 없으면 그대로 둔다.
- 소리(`SfxTriggers`)의 `StageTransition`은 "이전 단계가 `FadingOut`이 아니고 지금 `FadingOut`이며 보스 없음"일 때라 `ClearWait`→`FadingOut`에서도 그대로 울린다. 고치지 않는다. 다르게 동작하면 보고서에 적는다.
- 끝나면 `docs/spec/stage.md` "화면"에 "게임은 결판 뒤 1.5초 기다린 뒤 페이드아웃한다. 쓰러짐 대기는 그 시간을 뺀 나머지만 기다린다"를 반영한다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다. 플레이 모드 흐름 테스트(`Assets/Tests/PlayMode/Flow/`)도 포함이다. 흐름 테스트가 시간 제한에 걸리면 제한값을 바꾸지 말고 보고서에 적어 blocked로 보고한다.
- [플레이 모드] `Assets/Tests/PlayMode/Stage/StageClearWaitViewTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 첫 씬을 불러와 `[Game]`의 `GameRoot.StageProgress.ClearWaitSeconds`는 1.5다.
- [플레이 모드] `StageView`를 `new StageProgress(wallet, new StatUpgrades(wallet, 1000, 10000), battle, new System.Random(1), 2, 5, 1.5)`로 직접 만들고, 구간 소개를 `Tick`으로 끝낸 뒤 결판까지 `battle.Step()`을 부른다. 그 뒤 실제 시간 1.2초가 지났을 때 `Phase`는 `ClearWait`이고 `FadeAlpha`는 0이다.
- [플레이 모드] 같은 상황에서 실제 시간 2.0초가 지났을 때 `Phase`는 `ClearWait`이 아니다.
- [사람 확인] 게임을 실행해 구간의 마지막 적을 쓰러뜨리면, 약 1.5초 동안 제자가 공격 자세가 아닌 대기 동작으로 서 있고 피해 숫자가 사라진 뒤 화면이 어두워진다. 에디터에서 재생하며 확인한다.
- [사람 확인] 5스테이지 최종 보스를 이기면 쓰러짐 그림이 끝까지 재생된 뒤 화면이 어두워진다. 에디터에서 확인한다.

## 하지 말 것
- `Assets/Scripts/Core/`(전투·스테이지·소리 로직)를 고치지 않는다.
- `CombatView`와 다른 기능의 화면 코드를 고치지 않는다.
- 흐름 테스트의 시간 제한·배속을 바꾸지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 게임의 결판 뒤 대기를 1.5초로 설정하고, 성공 전환에서 쓰러짐 연출 시간 중 이미 기다린 시간을 제외했다.
- 첫 씬 설정, 대기 중과 대기 종료, 최종 보스의 남은 쓰러짐 대기를 확인하는 플레이 모드 테스트를 추가했다.
- 스테이지 화면 사양을 갱신했다. 효과음 조건은 기존 구현이 해당 전환을 처리함을 확인했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Scripts/Game/Stage/StageView.cs`
- `Assets/Tests/PlayMode/Stage/StageClearWaitViewTests.cs` 및 `.meta`
- `docs/spec/stage.md`
- `tasks/doing/083_clear_wait_view.md`, `.loop/result.json`

### 가정
- 없음.

### 사람이 확인할 것
- 게임을 실행해 구간의 마지막 적을 쓰러뜨리면, 약 1.5초 동안 제자가 공격 자세가 아닌 대기 동작으로 서 있고 피해 숫자가 사라진 뒤 화면이 어두워지는지 에디터에서 확인한다.
- 5스테이지 최종 보스를 이기면 쓰러짐 그림이 끝까지 재생된 뒤 화면이 어두워지는지 에디터에서 확인한다.

### 테스트 결과
- `git diff --check` 통과.
- Unity 컴파일과 전체 테스트 및 플레이 모드 흐름 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 13:54)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/083_clear_wait_view
- 루프 브랜치: loop/20261122-133943
