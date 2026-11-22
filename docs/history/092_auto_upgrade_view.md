# 092_auto_upgrade_view — 자동 강화 버튼 AutoUpgradeView 와 GameRoot 조립

## 목표
Game 에 금화 현판 옆의 켜고 끄는 버튼 `AutoUpgradeView`(`PlumSword.Game.StatUpgrade`)를 만들고, `GameRoot`가 `AutoUpgrade`를 하나 만들어 들고 있으며 화면을 붙이게 한다. 게임에서 자동 강화가 켜지면 금화를 더하는 흐름 테스트 도우미가 깨지므로 도우미도 고친다.

## 묶음
auto_upgrade

## 선행 작업
- 091_auto_upgrade

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("화면: 코드로 조립한다", "테스트")
- docs/spec/auto_upgrade.md ("연결 지점"의 GameRoot·AutoUpgradeView, "화면", "저장")
- docs/spec/stat_upgrade.md ("화면" 절)
- docs/spec/currency.md ("화면" 절, 금화 현판 위치)

## 요구사항
- `Assets/Scripts/Game/StatUpgrade/AutoUpgradeView.cs`: `public sealed class AutoUpgradeView : MonoBehaviour`, `public void Initialize(PlumSword.Core.StatUpgrade.AutoUpgrade autoUpgrade, UnityEngine.Canvas canvas)`.
  - `canvas` 아래에 버튼 오브젝트 `AutoUpgradeButton`(`Image` + `Button`)을 만든다. `RectTransform`: 앵커 (0, 0)~(0, 0), 피벗 (0, 1), `anchoredPosition` (216, 86), `sizeDelta` (96, 26) (임시). 금화 현판(왼쪽 위 (8, 86), 최대 너비 200)과 겹치지 않는다.
  - 바탕: `Resources.Load<Sprite>("Art/UI/StatUpgrade/button")`이 있으면 그 그림, 없으면 단색 사각형.
  - 자식 `Label`(`TextMeshProUGUI`): `AutoUpgradeText.Label(Enabled)`, 폰트 Galmuri11(기존 `StatUpgradeView`가 폰트를 읽는 방식을 따른다), 크기 11 (임시), 가운데 정렬, `raycastTarget = false`.
  - 켜짐: 바탕 색 RGB (0.20, 0.45, 0.25), 글자 흰색. 꺼짐: 바탕 색 RGB (0.25, 0.25, 0.25), 글자 RGB (0.6, 0.6, 0.6). 그림 바탕이면 그림에 이 색을 곱한다 (`Image.color`) (임시).
  - 누르면 `autoUpgrade.SetEnabled(!autoUpgrade.Enabled)`. 잠금(페이드) 중에도 누를 수 있다. 소리·연출 없음.
  - `Initialize`에서 한 번 그리고, `EnabledChanged`를 구독해 올 때마다 다시 그린다. `OnDestroy`에서 구독을 끊는다.
- `Assets/Scripts/Game/GameRoot.cs`:
  - 공개 속성 `public PlumSword.Core.StatUpgrade.AutoUpgrade AutoUpgrade { get; private set; }`.
  - `Awake`에서 `StatUpgrades`를 만든 바로 다음 줄에 `AutoUpgrade = new PlumSword.Core.StatUpgrade.AutoUpgrade(StatUpgrades, Wallet, true);`. 저장 칸은 아직 없으므로 `true`를 넣는다 (095·096 이 저장된 값으로 바꾼다).
  - 기능별 화면 조립에서 `CreateStatUpgradeView();` 다음 줄에 `[Game]`의 자식 `[AutoUpgradeView]`를 만들어 `AutoUpgradeView.Initialize(AutoUpgrade, UiCanvas)`를 부르는 한 줄(`CreateAutoUpgradeView();`)을 더한다.
  - `OnDestroy`를 더해 `AutoUpgrade?.Dispose()`를 부른다 (`GameRoot`에는 지금 `OnDestroy`가 없다).
- **흐름 테스트 도우미** `Assets/Tests/PlayMode/Flow/GameFlow.cs` (이름·기존 검사는 그대로 두고 동작만 더한다):
  - 지금 흐름 테스트들은 금화를 더한 뒤 그 금화로 직접 강화하거나(`PowerUp`), 금화가 정확히 얼마인지 확인한다 (`FullRoundFlowTests`, `GameFlowTests`, `OfflineRewardFlowTests`, `SaveReloadFlowTests`). 자동 강화가 켜져 있으면 금화를 더하는 순간 자동으로 사 버려서 실패한다.
  - `Relaunch(MemorySaveStore store, StartEntry entry)`에 끝 인자 `bool autoUpgrade = false`를 더한다. 새 루트를 찾은 즉시(대기 프레임 전) `autoUpgrade`가 `false`면 `current.AutoUpgrade.SetEnabled(false)`를 부른다.
  - `ResetToFreshLaunch()`는 `Relaunch(null, StartEntry.Launch, true)`를 부른다 (정리 뒤의 루트는 게임 그대로 자동 강화가 켜진 상태로 남긴다).
  - `PowerUp(root, levels)`는 맨 앞에서 `root.AutoUpgrade.SetEnabled(false)`를 부른다.
  - 위 흐름 테스트들의 본문과 기대값은 고치지 않는다. 그 밖에 `GameRoot`를 쓰는 플레이 모드 테스트가 자동 구매 때문에 실패하면, 그 테스트의 준비 단계에서 `GameRoot.AutoUpgrade.SetEnabled(false)`를 부르게 고치고(검사와 기대값은 그대로) 보고서의 가정에 테스트 이름을 적는다.
- `docs/spec/auto_upgrade.md` "화면"과 실제가 달라지면 그 절을 고친다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [플레이 모드] `Assets/Tests/PlayMode/StatUpgrade/AutoUpgradeViewTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 쓰는 법은 `docs/dev/architecture.md` "테스트" 절을 따른다 (15·16번은 테스트가 직접 만든 캔버스와 `AutoUpgrade`로, 17번은 첫 씬을 불러와 `[Game]`으로).
- [플레이 모드] 15. 꺼진 `AutoUpgrade`로 `AutoUpgradeView.Initialize`를 부르면 캔버스 아래 `AutoUpgradeButton`이 있고 `anchoredPosition`은 (216, 86), 크기는 (96, 26), `Label` 글자는 `"자동 강화 꺼짐"`이다.
- [플레이 모드] 16. 그 버튼을 누르면 `Enabled`는 `true`이고 `Label` 글자는 `"자동 강화 켜짐"`이다.
- [플레이 모드] 17. 첫 씬을 불러오면 `[Game]`의 `GameRoot.AutoUpgrade`가 `null`이 아니고 `Enabled`는 `true`다.
- [사람 확인] 18. 게임을 실행하고 10초 뒤 화면에 금화 현판 오른쪽에 `자동 강화 켜짐` 버튼이 현판과 겹치지 않게 보인다 (갈무리로 확인). 금화가 쌓이는 대로 강화 레벨이 저절로 오르는지는 에디터에서 실행해 확인한다.

## 하지 말 것
- `Assets/Scripts/Core/`를 고치지 않는다 (091 이 했다).
- `GameRoot`에서 자동 강화 말고 다른 기능의 조립 줄을 고치지 않는다.
- 저장 칸(`autoUpgrade`)을 만들지 않는다 (095). `AutoSave`에 `AutoUpgrade`를 넘기지 않는다 (096).
- 흐름 테스트의 이름·기대값을 바꾸거나 검사를 줄이지 않는다.
- 자동 강화 전용 그림, 소리, 연출을 더하지 않는다.
- 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/`를 건드리지 않는다.

## 보고서

### 한 일
- 자동 강화 버튼을 캔버스에 조립하고 상태 변경과 클릭을 연결했다. GameRoot에서 자동 강화 객체와 화면을 만들고 파괴 시 구독을 정리한다.
- 흐름 테스트 도우미에서 자동 구매가 기존 검사를 방해하지 않게 했고, 화면 초기 상태·클릭·첫 씬 조립 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`, `Assets/Scripts/Game/StatUpgrade/AutoUpgradeView.cs`와 메타 파일
- `Assets/Tests/PlayMode/Flow/GameFlow.cs`, `Assets/Tests/PlayMode/StatUpgrade/AutoUpgradeViewTests.cs`와 폴더·파일 메타 파일
- `tasks/doing/092_auto_upgrade_view.md`, `.loop/result.json`

### 가정
- 폰트를 불러올 수 없으면 기존 TMP 기본 글꼴로 표시한다. 버튼을 다시 초기화하면 이전 버튼과 구독을 정리한다.
- 화면과 연결 지점은 기존 사양과 같아 사양 변경은 없다.

### 사람이 확인할 것
- 게임을 실행하고 10초 뒤 화면에 금화 현판 오른쪽에 `자동 강화 켜짐` 버튼이 현판과 겹치지 않게 보이는지 갈무리로 확인한다. 금화가 쌓이는 대로 강화 레벨이 저절로 오르는지 에디터에서 실행해 확인한다.

### 테스트 결과
- `git diff --check` 통과. Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 16:43)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/092_auto_upgrade_view
- 루프 브랜치: loop/20261122-163234
