# 032_save_game_root — 게임 조립에 불러오기와 자동 저장 붙이기

## 목표
`PlumSword.Game.GameRoot.Awake` 를 고쳐, 맨 앞에서 저장을 읽고(`LoadResult`) 그 값으로 지갑·강화·스테이지를 만들고, `Begin()` 전에 `AutoSave` 와 `[SaveView]` 를 붙인다. 손상이면 전투를 멈추고 `[SaveErrorView]` 를 띄워 `NEW GAME` 으로 새로 시작하게 한다. 화면 조립 작업이다.

## 묶음
저장

## 선행 작업
- 029_auto_save_retry
- 030_file_save_store
- 031_save_views

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작" 절의 "불러오기"·"자동 저장"·"손상"·"판이 다름"·"저장 데이터가 있는지"·"저장소", "연결 지점" 절의 `GameRoot` 와 "`GameRoot.Awake` 순서", "화면" 절의 "조립")
- docs/spec/currency.md ("연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절)
- docs/spec/stage.md ("연결 지점" 절과 "화면" 절의 "조립")
- docs/spec/combat.md ("연결 지점" 절의 `SetPaused`, 시작 능력치 상수)

## 요구사항
- 고칠 파일: `Assets/Scripts/Game/GameRoot.cs`.
- 새 테스트 파일: `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs` (네임스페이스 `PlumSword.Game.Tests.Save`, 클래스 `GameRootSaveTests`). 쓰는 법은 `GameBootTests` 처럼 `SceneManager.LoadSceneAsync(0)` 뒤 몇 프레임 기다리고 `GameObject.Find(GameBootstrap.RootName)` 의 `GameRoot` 를 본다.
- 선행 작업이 만든 것을 쓴다: 026 의 `SaveLoadResult`·`SaveLoadStatus`·`SaveData`, 027 의 `SaveCodec.Decode`, 028·029 의 `ISaveStore`·`MemorySaveStore`·`AutoSave(store, wallet, upgrades, stage, enabled)`·`Enable()`, 030 의 `PlumSword.Game.Save.FileSaveStore(directory)`·`Load()`, 031 의 `PlumSword.Game.Save.SaveView.Initialize(AutoSave, Canvas)`·`SaveErrorView.Initialize(Canvas, Action)`.
- **더할 공개 속성** (사양 "연결 지점" 그대로):

  ```csharp
  // Awake 맨 앞에서 저장을 읽은 결과. 시작 화면 기능이 HasSaveData 로 이어하기를 고른다.
  public PlumSword.Core.Save.SaveLoadResult LoadResult { get; private set; }

  // Awake 에서 StageProgress 를 만든 뒤, Begin 전에 하나 만든다.
  public PlumSword.Core.Save.AutoSave AutoSave { get; private set; }
  ```

  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **`Awake` 순서** (지금 순서에서 바뀌는 곳만):
  1. 맨 앞: 저장소를 고른다. `Application.isBatchMode` 가 `true` 면 `new MemorySaveStore()` 이고 `LoadResult = SaveCodec.Decode(null)`(`Empty`). 아니면 `new FileSaveStore(Application.persistentDataPath)` 이고 `LoadResult = 그 저장소.Load()`. 저장소는 private 필드로 들고 있는다.
  2. 시작 값: `LoadResult.Status == Loaded` 면 `LoadResult.Data` 의 값, 아니면(`Empty`, `UnsupportedVersion`, `Corrupt`) 처음 값 — 금화 0, 세 단계 0, `startStage: 1`, `seenIntroStage: 0`.
  3. `Wallet = new Wallet(gold)`, `StatUpgrades = new StatUpgrades(Wallet, Battle.DiscipleBaseAttack, Battle.DiscipleBaseMaxHealth, Battle.DiscipleBaseAttackInterval, attackLevel, healthLevel, attackSpeedLevel)`. 범위 밖 값은 각 생성자가 잘라낸다 (여기서 자르지 않는다). `Battle` 은 지금 그대로.
  4. 캔버스·이벤트 시스템, 금화·강화·전투 화면 조립은 그대로다.
  5. 스테이지: `StageProgress = new StageProgress(Wallet, StatUpgrades, Battle, new System.Random(), startStage, seenIntroStage)` 를 만들고 `[StageView]` 를 붙인다. **여기서 `Begin()` 을 부르지 않는다.**
  6. `AutoSave = new AutoSave(저장소, Wallet, StatUpgrades, StageProgress, enabled: LoadResult.Status != SaveLoadStatus.Corrupt)`. `[Game]` 의 자식 `[SaveView]` 오브젝트를 만들어 `SaveView.Initialize(AutoSave, UiCanvas)`.
  7. `Corrupt` 가 아니면 `StageProgress.Begin()`. `Corrupt` 면 `Battle.SetPaused(true)` 하고, `[Game]` 의 자식 `[SaveErrorView]` 오브젝트를 만들어 `SaveErrorView.Initialize(UiCanvas, StartNewGame)`. `StartNewGame`(private 메서드)은 `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()` 이다. (`Begin` 의 `StageStarted` 로 그 프레임 끝에 처음 값이 저장되어 손상된 파일을 덮어쓴다)
- `AutoSave` 는 반드시 `StageProgress.Begin()` 보다 먼저 만든다 (새 게임의 첫 소개 기록 `SeenIntroStage` 0 → 1 을 저장하기 위해).
- `Begin()` 을 부르는 곳은 `GameRoot` 뿐이다 (위 7번의 두 곳).
- 지금 `Awake` 의 "1단계는 저장이 없어 0 에서 시작한다" 주석은 지금 동작에 맞게 고친다. `StageProgress` 속성의 설명 주석의 "Begin 은 GameRoot 만 부른다"는 그대로 맞다.
- 배치 모드(루프와 명령줄 테스트)에서는 파일을 읽지도 쓰지도 않는다. 에디터 테스트 러너에서 직접 돌리면 실제 `Application.persistentDataPath` 의 `save.dat` 을 쓰는 것은 사양이 허용한 동작이다.

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 이 클래스의 테스트는 배치 모드(루프와 명령줄 테스트)에서 판정하며, 건너뛰는 코드(`Assert.Ignore`·`[Ignore]` 등)는 넣지 않는다.
- [플레이 모드] 배치 모드에서 게임이 켜지면 `GameRoot.LoadResult.Status` 가 `Empty` 이고 `GameRoot.LoadResult.HasSaveData` 는 `false` 다.
- [플레이 모드] 배치 모드에서 게임이 켜지면 `GameRoot.AutoSave` 는 `null` 이 아니고 `Enabled` 가 `true` 다.
- [플레이 모드] 배치 모드에서 게임이 켜지면 `GameRoot.StageProgress.StageNumber` 가 1, `GameRoot.StageProgress.HasBegun` 이 `true` 다.
- [플레이 모드] 배치 모드에서 게임이 켜지고 몇 프레임 뒤 `GameRoot.AutoSave.IsDirty` 는 `false`, `HasFailed` 는 `false` 다 (첫 `Begin` 의 저장이 `[SaveView]` 의 `Flush` 로 끝났다).
- [플레이 모드] 배치 모드에서 `[Game]` 의 자식에 `[SaveView]` 가 있고 `SaveView` 컴포넌트가 붙어 있다.
- [플레이 모드] 배치 모드에서 `[Game]` 의 자식에 `[SaveErrorView]` 가 없고, `UiCanvas` 아래에 `SaveErrorPanel` 이 없다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트(`GameBootTests` 포함)가 모두 통과한다.
- [사람 확인] 루프의 화면 갈무리(1초·10초·30초)에 손상 창이 없고 `SAVE FAILED` 글자도 보이지 않으며, 지금까지처럼 스테이지 1 소개와 전투가 보인다.
- [사람 확인] 에디터에서 `Application.persistentDataPath` 의 `save.dat` 을 지우고 실행하면 스테이지 1 소개부터 시작한다. 금화를 모아 강화한 뒤 끄고 다시 켜면 금화·강화 단계·스테이지 번호가 끄기 전과 같고, 이미 본 소개는 다시 나오지 않는다.
- [사람 확인] 에디터에서 `save.dat` 을 아무 글자(예: `hello`)로 바꾸고 실행하면 손상 창이 뜨고 뒤의 전투는 멈춰 있으며 강화 버튼이 눌리지 않는다. 그 상태로 끄고 다시 켜면 같은 창이 다시 뜬다. `NEW GAME` 을 누르면 창이 사라지고 처음 값(금화 0, 스테이지 1 소개)으로 시작하며, 그 뒤 `save.dat` 이 `version=1` 로 시작하는 여덟 줄로 바뀌어 있다.
- [사람 확인] 에디터에서 `save.dat` 의 첫 줄을 `version=2` 로 바꾸고 실행하면 오류 창 없이 처음 값으로 시작하고, 그 뒤 `save.dat` 의 첫 줄이 `version=1` 로 바뀌어 있다.
- [사람 확인] 에디터에서 실행 중에 `save.dat` 을 읽기 전용으로 바꾸고 금화를 얻으면 오른쪽 위에 `SAVE FAILED` 가 뜨고, 읽기 전용을 풀면 몇 초 안에 사라진다.

## 하지 말 것
- `GameRoot` 에서 바꾸는 곳은 `Awake` 의 Core 객체 생성, `CreateStage`(`Begin` 을 빼고 시작 값을 넣음), 새로 더하는 저장 조립(저장소·`LoadResult`·`AutoSave`·`[SaveView]`·`[SaveErrorView]`·`StartNewGame`)뿐이다. `CreateUiCanvas`, `EnsureEventSystem`, `CreateCurrencyView`, `CreateStatUpgradeView`, `CreateCombatView` 는 고치지 않는다.
- `Assets/Scripts/Core/` 의 코드, `Assets/Scripts/Game/Save/` 의 `FileSaveStore`·`SaveView`·`SaveErrorView`, 다른 화면 코드를 고치지 않는다.
- 시작 화면, `게임 시작`/`이어하기`, 오프라인 보상은 만들지 않는다 (다른 기능의 일).
- `Assets/Tests/PlayMode/Capture/` 와 `GameBootTests` 를 고치지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 시작 시 배치 모드 메모리 저장소 또는 파일 저장소를 선택하고 저장 결과로 지갑, 강화, 스테이지를 초기화했다.
- 스테이지 시작 전에 자동 저장과 화면을 연결하고, 손상 시 전투를 멈춰 새 게임 선택을 기다리게 했다.
- 배치 모드 시작 상태와 자동 저장 화면을 확인하는 플레이 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs` 및 `.meta`
- `tasks/doing/032_save_game_root.md` (보고서), `.loop/result.json`

### 가정
- 없음. 공개 연결 지점과 동작은 기존 저장 사양을 따른다.

### 사람이 확인할 것
- 루프의 1초·10초·30초 화면 갈무리에 손상 창과 `SAVE FAILED`가 없고 스테이지 1 소개와 전투가 보이는지 확인한다.
- 에디터에서 저장 파일을 지우고 시작한 뒤 금화를 모아 강화하고 재실행하여 금화·강화 단계·스테이지 복원과 이미 본 소개 생략을 확인한다.
- 손상된 파일로 실행하여 오류 창, 전투 정지, 강화 버튼 차단과 재실행 시 유지 여부를 확인하고, `NEW GAME` 뒤 초기 시작과 판 1의 여덟 줄 저장을 확인한다.
- 첫 줄이 `version=2`인 파일로 실행하여 오류 없이 초기 시작하고 판 1로 덮어쓰는지 확인한다.
- 실행 중 저장 파일을 읽기 전용으로 바꾸어 `SAVE FAILED`가 표시되고 해제 후 몇 초 안에 사라지는지 확인한다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: Unity 라이선스 클라이언트 연결이 반복 거절되어 에딧 모드 시작 단계에서 중단했다. 컴파일 및 전체 에딧·플레이 모드 테스트는 직접 판정하지 못했다. 로그: `.loop/test/unity_EditMode.log`.

## 보고서

### 한 일
- 관문이 지적한 `Assert.Ignore`와 작업 문서의 플레이 모드 완료 조건을 대조했다.
- 완료 조건이 배치 모드가 아닐 때 이 클래스의 테스트를 `Assert.Ignore`로 건너뛰도록 명시하므로, 해당 요구와 관문 금지를 함께 충족할 수 없어 보류한다.

### 바뀐 파일
- `tasks/doing/032_save_game_root.md` (보고서 추가), `.loop/result.json`

### 가정
- 없음.

### 사람이 확인할 것
- 관문을 통과하도록 비배치 모드에서 `Assert.Ignore`를 요구하는 완료 조건을 변경할지 결정한다. 추천: 해당 요구를 없애고 테스트 실행 환경을 배치 모드로 제한하는 방식에 동의한다.
- 루프의 1초·10초·30초 화면 갈무리에서 손상 창과 `SAVE FAILED`가 없고 스테이지 1 소개와 전투가 보이는지 확인한다.
- 에디터에서 저장 삭제 후 시작하고 금화·강화·스테이지 복원과 이미 본 소개 생략을 확인한다.
- 손상 파일에서 오류 창, 전투 정지, 강화 차단, 재실행 시 유지 및 `NEW GAME` 후 초기 시작과 판 1의 여덟 줄 저장을 확인한다.
- `version=2` 파일에서 오류 없이 초기 시작하고 판 1로 덮어쓰는지 확인한다.
- 저장 파일을 읽기 전용으로 바꾸면 `SAVE FAILED`가 표시되고 해제 후 몇 초 안에 사라지는지 확인한다.

### 테스트 결과
- 이번 재시도에서는 요구사항 충돌로 코드를 수정하지 않았고 테스트를 다시 실행하지 않았다. 이전 보고서의 Unity 라이선스 연결 실패 기록은 그대로다.


## 보류 사유 (2026-11-17 14:23)

- 사유: 작업 문서가 비배치 모드에서 Assert.Ignore를 명시적으로 요구하지만 관문은 해당 테스트 건너뛰기를 금지한다.
- 질문: 비배치 모드에서 Assert.Ignore로 건너뛰라는 완료 조건을 변경해도 되는가?
- 추천안: 해당 완료 조건을 제거하고 테스트는 배치 모드에서 실행하도록 정한다. 이후 Assert.Ignore를 제거하고 전체 테스트와 관문을 다시 실행한다.
- 기능 브랜치: task/032_save_game_root (작업 중 상태 커밋됨)
- 작업 에이전트: codex(시도 1~2)


## 답변 (2026-11-17 14:34)

- 보류 종류: 에이전트의 질문 (2026-11-17 14:23 보류 사유에 대한 답)
- 결정: 비배치 모드에서 `Assert.Ignore` 로 건너뛰라는 완료 조건을 없앤다. `GameRootSaveTests` 는 건너뛰는 코드 없이 항상 실행되고, 루프와 명령줄 테스트(배치 모드)에서 판정한다.
- 이유: 관문은 테스트 건너뛰기(`Assert.Pass/Ignore/Inconclusive`, `[Ignore]`, `[Explicit]`)를 금지하고, 이 규칙에는 예외가 없다. 루프는 항상 배치 모드라서 건너뛰기를 빼도 판정 결과는 같다. 사람이 에디터 테스트 러너에서 직접 돌리면 실제 `save.dat` 을 읽고 써서, 저장이 있으면 `Empty` 를 기대하는 테스트가 실패할 수 있다. 사용자는 이 점을 알고 받아들였다. 건너뛰기를 다른 방법(`Assume`, 조건부 `yield break` 등)으로 흉내 내지 않는다.
- 할 일: `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs` 의 `LoadGame` (`[UnitySetUp]`) 첫 줄 `if (!Application.isBatchMode) Assert.Ignore("Batch mode only");` 를 지운다. 그 밖의 테스트와 `Assets/Scripts/Game/GameRoot.cs` 는 그대로 둔다. 그다음 전체 테스트(`python tools/loop/run_unity_tests.py`)와 관문을 다시 돌려 통과를 확인하고 보고서를 쓴다.
- 바꾼 칸: 완료 조건 첫 줄: "`Application.isBatchMode` 가 `false` 면(에디터의 테스트 러너에서 직접 돌림) 이 클래스의 테스트는 `Assert.Ignore` 로 건너뛴다." → "이 클래스의 테스트는 배치 모드(루프와 명령줄 테스트)에서 판정하며, 건너뛰는 코드(`Assert.Ignore`·`[Ignore]` 등)는 넣지 않는다."
- 기능 브랜치: task/032_save_game_root 의 작업 중 커밋(`GameRoot.cs` 수정, `GameRootSaveTests.cs`·`.meta`)은 살려서 이어간다.

## 보고서

### 한 일
- 답변(2026-11-17 14:34)대로 `GameRootSaveTests.LoadGame`(`[UnitySetUp]`) 첫 줄의 `if (!Application.isBatchMode) Assert.Ignore("Batch mode only");` 를 지웠다. 다른 대체 건너뛰기는 넣지 않았다.
- 이전 작업 중 커밋의 `GameRoot.cs`(저장소 선택·`LoadResult`·시작 값 복원·`CreateStage` 에서 `Begin` 제거·`AutoSave`·`[SaveView]`·손상 시 `[SaveErrorView]`·`StartNewGame`)는 답변대로 그대로 두고, 작업 문서의 `Awake` 순서와 맞는지 다시 확인했다.
- 전체 에딧·플레이 모드 테스트와 관문을 다시 돌렸다.

### 바뀐 파일
- `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs` (건너뛰기 줄 삭제)
- (이전 작업 중 커밋) `Assets/Scripts/Game/GameRoot.cs`, `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs`, `Assets/Tests/PlayMode/Save/GameRootSaveTests.cs.meta`
- `tasks/doing/032_save_game_root.md` (보고서), `.loop/result.json`

### 가정
- 시작 값은 `LoadResult.Data != null` 일 때 그 값을 쓴다. `SaveLoadResult` 생성자가 `Data` 는 `Loaded` 일 때만 있다고 보장하므로 `Status == Loaded` 검사와 같다.
- 공개 속성 `LoadResult`·`AutoSave` 는 사양 "연결 지점" 그대로라 사양 문서는 고치지 않았다.

### 사람이 확인할 것
- 루프의 화면 갈무리(1초·10초·30초)에 손상 창이 없고 `SAVE FAILED` 글자도 보이지 않으며, 지금까지처럼 스테이지 1 소개와 전투가 보인다.
- 에디터에서 `Application.persistentDataPath` 의 `save.dat` 을 지우고 실행하면 스테이지 1 소개부터 시작한다. 금화를 모아 강화한 뒤 끄고 다시 켜면 금화·강화 단계·스테이지 번호가 끄기 전과 같고, 이미 본 소개는 다시 나오지 않는다.
- 에디터에서 `save.dat` 을 아무 글자(예: `hello`)로 바꾸고 실행하면 손상 창이 뜨고 뒤의 전투는 멈춰 있으며 강화 버튼이 눌리지 않는다. 그 상태로 끄고 다시 켜면 같은 창이 다시 뜬다. `NEW GAME` 을 누르면 창이 사라지고 처음 값(금화 0, 스테이지 1 소개)으로 시작하며, 그 뒤 `save.dat` 이 `version=1` 로 시작하는 여덟 줄로 바뀌어 있다.
- 에디터에서 `save.dat` 의 첫 줄을 `version=2` 로 바꾸고 실행하면 오류 창 없이 처음 값으로 시작하고, 그 뒤 `save.dat` 의 첫 줄이 `version=1` 로 바뀌어 있다.
- 에디터에서 실행 중에 `save.dat` 을 읽기 전용으로 바꾸고 금화를 얻으면 오른쪽 위에 `SAVE FAILED` 가 뜨고, 읽기 전용을 풀면 몇 초 안에 사라진다.
- 에디터 테스트 러너에서 `GameRootSaveTests` 를 직접 돌리면 실제 `save.dat` 을 읽고 써서, 저장이 있으면 `BatchBoot_LoadResultIsEmpty` 등이 실패할 수 있다 (답변에서 받아들인 동작).

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 391개 중 391개 통과, PlayMode 22개 중 22개 통과 (새 `GameRootSaveTests` 6개 포함, `GameBootTests` 포함). 결과: 통과.
- `python tools/loop/loop_gate.py --base loop/20261117-143608 --head HEAD --mode task`: 커밋된 상태 기준 위반 1건으로, 이번에 지운 `GameRootSaveTests.cs:18` 의 `Assert.Ignore` 줄뿐이다. 작업 트리에서는 그 줄이 없어졌다 (커밋 전이라 관문 재검사는 스크립트가 한다).


## 루프 기록 (2026-11-17 14:39)

- 작업 에이전트: claude(시도 1)
- 리뷰: codex (교차) 통과
- 기능 브랜치: task/032_save_game_root
- 루프 브랜치: loop/20261117-143608
