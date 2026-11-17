# 025_stage_view_intro — 소개 패널과 GameRoot 의 소개 켜기

## 목표
`PlumSword.Game` 의 스테이지 화면 `StageView` 에 소개 패널(테마 이름과 소개 문구)을 더하고, `GameRoot` 가 `StageProgress` 를 `seenIntroStage: 0` 으로 만들어 실제 게임에서 1회차 첫 진입 때 소개가 나오게 한다. 표시 판단은 Core 의 `StageProgress.IsIntroShowing`, `DisplayStageNumber`, `StageText.ThemeName`, `IntroLine` 을 그대로 쓴다.

## 묶음
스테이지소개

## 선행 작업
- 022_stage_view_round_theme
- 024_stage_intro_tick

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다" 절)
- docs/spec/stage.md ("화면" 절의 "조립", "소개 패널", "바뀌는 때", "누르는 것", "연출", "연결 지점" 절의 `GameRoot`, `StageView`, "변경 기록"의 "2026-11-17 — 스테이지 소개")
- docs/spec/currency.md ("연결 지점" 절의 `GameRoot.UiCanvas`, "화면" 절의 TextMeshPro)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-17 — 스테이지 소개` 항목 중 화면과 `GameRoot` 부분만 구현한다. 로직은 023, 024 가 이미 만들었다.
- 고칠 파일: `Assets/Scripts/Game/Stage/StageView.cs`, `Assets/Scripts/Game/GameRoot.cs`.
- 선행 작업이 만든 것을 쓴다. 내부는 고치지 않는다.
  - 023_stage_intro_begin, 024_stage_intro_tick: `PlumSword.Core.Stage.StageProgress` 생성자 `new StageProgress(Wallet wallet, StatUpgrades upgrades, Battle battle, System.Random random, int startStage = 1, int seenIntroStage = 5)`, `bool IsIntroShowing { get; }`, `int DisplayStageNumber { get; }`(1~5). 소개가 시작되거나 끝날 때 `Changed` 가 온다. 소개 중에는 `StageProgress` 가 전투를 멈추고(`battle.SetPaused`) 3초 뒤 스스로 푼다. `PlumSword.Core.Stage.StageText` 의 `string ThemeName(int stage)`, `string IntroLine(int stage)`(1~5 밖이면 `ArgumentOutOfRangeException`).
  - 022_stage_view_round_theme: 지금의 `StageView` 는 페이드 막(캔버스의 첫 번째 자식), 스테이지 글자(`RoundStageLabel`), 보스 이름(`ThemedBossName`), 진행 바를 만들고, `Initialize` 에서 한 번, `stage.Changed` 를 받을 때마다 다시 그린다. TMP 오류는 `WarnOnce` 로 경고 한 번만 남긴다. 이 동작은 그대로 둔다.
- **`GameRoot`**: `StageProgress` 를 만드는 줄만 `new PlumSword.Core.Stage.StageProgress(Wallet, StatUpgrades, Battle, new System.Random(), 1, 0)` 으로 바꾼다 (`startStage: 1`, `seenIntroStage: 0`). 저장 기능이 생기기 전에는 실행할 때마다 1스테이지 소개가 나온다. 조립 순서(`[StageView]` 조립 뒤 맨 마지막에 `Begin()`)와 다른 줄은 그대로다.
- **소개 패널** (사양 "화면"의 "소개 패널" 그대로, 수치는 모두 임시값):
  - 이름 `StageIntroPanel` 인 오브젝트를 캔버스의 자식으로 만든다. 앵커 (0.5, 0.5), 피벗 (0.5, 0.5), 위치 (0, 0), 크기 640×72. 바탕은 검은색 알파 0.6 `Image`.
  - 그 안 위쪽 절반에 제목(`TextMeshProUGUI`, 글자 크기 22), 아래쪽 절반에 문구(`TextMeshProUGUI`, 글자 크기 14). 둘 다 흰색, 가운데 정렬, TMP 기본 글꼴. 제목은 `StageText.ThemeName(stage.DisplayStageNumber)`, 문구는 `StageText.IntroLine(stage.DisplayStageNumber)` 다.
  - 바탕과 글자 모두 `raycastTarget = false`. 누를 수 없다.
  - 스테이지 글자·보스 이름·진행 바보다 **나중에** 만들어 그 위에 그린다. 페이드 막은 지금처럼 캔버스의 첫 번째 자식이므로 소개 패널은 페이드 막보다도 위다.
  - `stage.IsIntroShowing` 이 `true` 일 때만 보인다 (`SetActive`).
- **바뀌는 때**: 소개 패널의 보이기와 제목·문구도 다른 글자와 같이 `Initialize` 에서 한 번, 그 뒤로 `stage.Changed` 가 올 때마다 다시 그린다. 매 프레임 다시 그리지 않는다.
- TMP 기본 리소스가 없어 글자를 만들거나 넣지 못해도 지금처럼 `WarnOnce` 로 경고 한 번만 남기고 게임을 멈추지 않는다. 이때도 바탕 `Image` 의 보이기는 `IsIntroShowing` 을 따른다.
- 소개 패널의 등장·퇴장 애니메이션, 소리, 넘기기 입력은 없다. 화면 코드에서 소개 시간을 세거나 `SetPaused` 를 부르지 않는다.
- 이 작업에는 에딧 모드 테스트를 새로 쓰지 않는다 (판정은 023, 024 의 에딧 모드 테스트가 한다). 플레이 모드 테스트도 새로 쓰지 않는다.

## 완료 조건
- [컴파일] `StageView.cs` 와 `GameRoot.cs` 가 컴파일 오류 없이 컴파일되고, `GameRoot.cs` 의 `StageProgress` 생성 호출에 `seenIntroStage` 값으로 0 이 들어가며, 기존 테스트가 모두 통과한다. (플레이 모드의 `GameBootTests`, 에딧 모드의 `StageProgressIntroTests`, `StageTextTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)
- [사람 확인] 실행하고 1초 뒤 화면 가운데에 반투명 검은 바탕 위로 `JONGNAM` 과 `A Mount Hua disciple faces a first sparring match at Jongnam.` 이 잘리지 않고 보이고, 화면 위쪽의 `ROUND 1 · STAGE 1 · JONGNAM` 과 빈 노란 진행 바도 보인다.
- [사람 확인] 실행하고 10초 뒤에는 소개 패널이 보이지 않고 제자와 몬스터가 싸우고 있다.
- [사람 확인] 에디터에서 실행해 처음 약 3초 동안 제자와 몬스터가 움직이지 않고, 그동안에도 금화가 충분하면 강화 버튼을 눌러 강화할 수 있으며, 소개가 사라지면 전투가 시작된다.
- [사람 확인] 에디터에서 실행해 1스테이지 보스를 처치하면 전투 화면이 가려졌다가 열린 뒤 화면 가운데에 `BEGGAR CLAN` 과 그 소개 문구가 약 3초 보이고, 그동안 전투가 멈춰 있다가 소개가 사라지면 시작된다.
- [사람 확인] 에디터에서 실행해 제자의 체력이 0이 되면 가려졌다가 같은 스테이지로 다시 시작하고, 이때 소개는 다시 나오지 않는다.
- [사람 확인] 위를 해 보는 동안 콘솔에 예외(빨간 줄)가 없다.

## 하지 말 것
- `Assets/Scripts/Core/` 를 고치지 않는다. 로직을 바꿔야 하면 고치지 말고 보고서의 가정에 적는다.
- `GameRoot` 에서는 `StageProgress` 생성 줄의 인자만 바꾸고, 다른 기능의 줄과 조립 순서는 고치지 않는다.
- `GameBootstrap.cs`, `Assets/Scripts/Game/Combat/`, `Assets/Scripts/Game/Currency/`, `Assets/Scripts/Game/StatUpgrade/` 를 고치지 않는다.
- 스테이지 글자, 보스 이름, 진행 바, 페이드 막의 위치·크기·색을 바꾸지 않는다.
- 소개 넘기기, 애니메이션, 소리, 한글 문구와 한글 글꼴, 2회차 이후의 소개, 저장·불러오기를 만들지 않는다.
- 확인용 시작 번호 변경이나 치트 키를 넣지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의를 고치지 않는다.

## 보고서

### 한 일
- `StageView` 에 소개 패널(`StageIntroPanel`)을 더했다. 캔버스 가운데(앵커·피벗 (0.5, 0.5), 위치 (0, 0), 640×72), 검은색 알파 0.6 `Image` 바탕, 위쪽 절반에 제목(`TextMeshProUGUI` 22, `StageText.ThemeName(DisplayStageNumber)`), 아래쪽 절반에 문구(14, `StageText.IntroLine(DisplayStageNumber)`). 흰색·가운데 정렬·TMP 기본 글꼴, 모두 `raycastTarget = false`.
- 패널은 진행 바 다음(마지막)에 만들어 스테이지 글자·보스 이름·진행 바·페이드 막 위에 그린다.
- `Redraw`(=`Initialize` 한 번, `stage.Changed` 때마다) 끝에서 `RedrawIntro` 를 불러 `IsIntroShowing` 으로 `SetActive` 하고 제목·문구를 넣는다. 매 프레임 다시 그리지 않는다.
- TMP 글자 생성·대입 실패는 기존 `WarnOnce` 로 한 번만 경고한다. 글자가 없어도 바탕 `Image` 의 보이기는 `IsIntroShowing` 을 따른다.
- `GameRoot.CreateStage` 의 `StageProgress` 생성 줄만 `new StageProgress(Wallet, StatUpgrades, Battle, new System.Random(), 1, 0)` 으로 바꿨다 (`startStage: 1`, `seenIntroStage: 0`).

### 바뀐 파일
- `Assets/Scripts/Game/Stage/StageView.cs`
- `Assets/Scripts/Game/GameRoot.cs`
- `tasks/doing/025_stage_view_intro.md` (이 보고서)

### 가정
- 제목·문구 글자 상자는 패널의 위쪽/아래쪽 절반에 앵커로 꽉 채우고(여백 0), 기존 글자와 같이 줄바꿈 없음(`NoWrap`)으로 했다. 문구(약 60자, 14pt)는 640 폭 안에 들어간다고 보았다.
- 소개 패널 수치(위치·크기·색·글자 크기)는 작업 문서·사양의 임시값을 그대로 썼다.
- 사양(`docs/spec/stage.md` "화면", "연결 지점", 변경 기록)이 이미 이 구현과 같아 사양 문서는 고치지 않았다.
- `GameRoot` 의 생성 줄 끝에 `// startStage: 1, seenIntroStage: 0` 주석을 같은 줄에 붙였다.

### 사람이 확인할 것
- [사람 확인] 실행하고 1초 뒤 화면 가운데에 반투명 검은 바탕 위로 `JONGNAM` 과 `A Mount Hua disciple faces a first sparring match at Jongnam.` 이 잘리지 않고 보이고, 화면 위쪽의 `ROUND 1 · STAGE 1 · JONGNAM` 과 빈 노란 진행 바도 보인다.
- [사람 확인] 실행하고 10초 뒤에는 소개 패널이 보이지 않고 제자와 몬스터가 싸우고 있다.
- [사람 확인] 에디터에서 실행해 처음 약 3초 동안 제자와 몬스터가 움직이지 않고, 그동안에도 금화가 충분하면 강화 버튼을 눌러 강화할 수 있으며, 소개가 사라지면 전투가 시작된다.
- [사람 확인] 에디터에서 실행해 1스테이지 보스를 처치하면 전투 화면이 가려졌다가 열린 뒤 화면 가운데에 `BEGGAR CLAN` 과 그 소개 문구가 약 3초 보이고, 그동안 전투가 멈춰 있다가 소개가 사라지면 시작된다.
- [사람 확인] 에디터에서 실행해 제자의 체력이 0이 되면 가려졌다가 같은 스테이지로 다시 시작하고, 이때 소개는 다시 나오지 않는다.
- [사람 확인] 위를 해 보는 동안 콘솔에 예외(빨간 줄)가 없다.
- 추가: 소개 패널이 강화 버튼 등 다른 UI 와 겹쳐 가리지 않는지 (패널은 캔버스 자식 중 늦게 만들어지는 편이라 겹치면 위에 그려진다. 누름은 막지 않는다).

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 333개 중 333개 통과, PlayMode 2개 중 2개 통과 (컴파일 오류 없음). 새 테스트는 작업 문서 지시대로 쓰지 않았다.


## 루프 기록 (2026-11-17 12:37)

- 작업 에이전트: claude(시도 1)
- 리뷰: codex (교차) 통과
- 기능 브랜치: task/025_stage_view_intro
- 루프 브랜치: loop/20261117-123053
