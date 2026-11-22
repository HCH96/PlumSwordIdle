# 095_save_regression_fields — 저장 세 줄(회귀 횟수·플레이 시간·자동 강화), 저장 지우기, 자동 저장 끄기

## 목표
Core 저장(`PlumSword.Core.Save`)에 `regressionCount`·`playTimeMs`·`autoUpgrade` 세 줄을 더하고, 저장소에 `TryDelete()`를, 자동 저장에 `Disable()`을 더한다. `ISaveStore`가 바뀌므로 Game 의 `FileSaveStore`와 테스트의 가짜 저장소들도 같은 작업에서 맞춘다. `GameRoot` 연결은 096 이 한다.

## 묶음
regression

## 선행 작업
- 091_auto_upgrade
- 093_stage_regression
- 094_play_time_ending_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/regression.md ("저장", "연결 지점"의 저장 기능에 더하는 것과 파일 형식)
- docs/spec/save.md ("동작", "연결 지점" 절)
- docs/spec/auto_upgrade.md ("연결 지점" 절, "저장")

## 요구사항
- `Assets/Scripts/Core/Save/ISaveStore.cs`: `bool TryDelete();`를 더한다. 저장을 지운다. 지울 것이 없어도 `true`, 지우지 못하면 `false` (예외를 밖으로 내지 않는다).
- `Assets/Scripts/Core/Save/MemorySaveStore.cs`: `TryDelete()`는 `Text`를 `null`로 하고 `true`.
- `Assets/Scripts/Game/Save/FileSaveStore.cs`: `TryDelete()`는 `save.dat`(그리고 남아 있으면 `save.tmp`)를 지운다. 파일이 없으면 `true`, 지우다 예외가 나면 `false`. 컴파일을 위해 이 작업에서 함께 고친다.
- `Assets/Scripts/Core/Save/SaveData.cs`:
  - 생성자 끝에 세 인자를 더한다: `public SaveData(long gold, int attackLevel, int healthLevel, int innerPowerLevel, int realmLevel, int stageNumber, int seenIntroStage, long savedAtUtcTicks, int realmKills = 0, int regressionCount = 0, long playTimeMs = 0, bool autoUpgrade = true)`.
  - `public int RegressionCount { get; }`, `public long PlayTimeMs { get; }`, `public bool AutoUpgrade { get; }`. 값은 받은 그대로 담는다 (음수도 그대로).
  - `Capture` 끝에 두 선택 인자: `public static SaveData Capture(Wallet wallet, StatUpgrades upgrades, RealmProgress realm, StageProgress stage, long savedAtUtcTicks, PlumSword.Core.Stage.PlayTime playTime = null, PlumSword.Core.StatUpgrade.AutoUpgrade autoUpgrade = null)`. `RegressionCount` = `stage.RegressionCount`(093), `PlayTimeMs` = `playTime?.Milliseconds ?? 0`(094), `AutoUpgrade` = `autoUpgrade?.Enabled ?? true`(091).
- `Assets/Scripts/Core/Save/SaveCodec.cs`: 판 번호 2 그대로(`SaveData.CurrentVersion` 2).
  - `Encode`: 지금 열 줄(`version` ~ `savedAtUtcTicks`) 뒤에 이 순서로 세 줄을 더한다: `regressionCount=<int>`, `playTimeMs=<long>`, `autoUpgrade=<0 또는 1>` (`true` → 1). 숫자는 지금처럼 `CultureInfo.InvariantCulture`.
  - `Decode`: 줄이 없으면 각각 0, 0, `true`. 값이 정수가 아니면 `Corrupt` (`regressionCount`는 `int`, `playTimeMs`는 `long`, `autoUpgrade`는 `int`로 읽는다). `autoUpgrade`는 0이면 `false`, 다른 정수면 `true`.
- `Assets/Scripts/Core/Save/AutoSave.cs`:
  - 생성자 끝에 두 선택 인자: `public AutoSave(ISaveStore store, Wallet wallet, StatUpgrades upgrades, RealmProgress realm, StageProgress stage, bool enabled = true, PlumSword.Core.Stage.PlayTime playTime = null, PlumSword.Core.StatUpgrade.AutoUpgrade autoUpgrade = null)`. 저장할 때 `SaveData.Capture(…, playTime, autoUpgrade)`에 넘긴다.
  - `public void Disable()`: `Enabled = false`. 이후 `Flush`·`SaveNow`는 쓰지 않는다 (지금 `Enabled` 검사 그대로). `Enable()`로 다시 켤 수 있다.
- **바뀌는 기존 테스트** (이름은 그대로 두고, 검사 수는 줄이지 않는다):
  - `SaveCodecTests`(`Assets/Tests/EditMode/Save/SaveCodecTests.cs`)의 상수 `Standard`: 값 끝에 `regressionCount=0\nplayTimeMs=0\nautoUpgrade=1\n`을 더한다. 즉 `"…savedAtUtcTicks=638000000000000000\n"` → `"…savedAtUtcTicks=638000000000000000\nregressionCount=0\nplayTimeMs=0\nautoUpgrade=1\n"`. 이 값으로 `Encode_StandardData_ProducesExactText`의 기대값이 세 줄 늘어난다. `Standard`를 쓰는 나머지 `Decode_…` 테스트는 고치지 않아도 그대로 통과해야 한다.
  - `ISaveStore`를 구현한 테스트용 가짜 저장소 네 개에 `TryDelete()`를 더한다 (검사는 바꾸지 않는다): `Assets/Tests/EditMode/Save/FakeSaveStore.cs`의 `FakeSaveStore`, `Assets/Tests/PlayMode/OfflineReward/OfflineRewardViewTests.cs`의 `Store`, `Assets/Tests/PlayMode/Save/SaveViewTests.cs`의 `FakeStore`, `Assets/Tests/PlayMode/StartMenu/SettingsViewTests.cs`의 `FakeStore`. 마지막 글자를 들고 있으면 그것을 `null`로 하고 `true`를 돌려준다.
  - 그 밖에 `ISaveStore`를 구현한 것이 있으면 같은 방식으로 `TryDelete()`를 더하고 보고서의 가정에 적는다.
- 구현하다 모양이 사양과 달라지면 `docs/spec/regression.md`의 "연결 지점" 절을 고치고 보고서의 가정에 적는다. 끝나면 `docs/spec/save.md`의 파일 형식 예시와 "연결 지점"(`ISaveStore`, `MemorySaveStore`, `SaveData`, `Capture`, `AutoSave`, `FileSaveStore`)을 새 모양으로 고친다.

## 완료 조건
- `Assets/Tests/EditMode/Save/SaveRegressionFieldsTests.cs`에 아래 25~30번 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 25. `new SaveData(…, regressionCount: 3, playTimeMs: 754900, autoUpgrade: false)`을 `Encode`하면 마지막 세 줄이 `regressionCount=3`, `playTimeMs=754900`, `autoUpgrade=0`이고, `Decode`하면 같은 세 값이 나온다.
- 26. 세 줄이 없는 판 2 글자를 `Decode`하면 `Loaded`이고 `RegressionCount` 0, `PlayTimeMs` 0, `AutoUpgrade` `true`다.
- 27. `playTimeMs=abc` 줄이 있으면 `Corrupt`다. `autoUpgrade=5`는 `true`로 읽는다.
- 28. `Capture`에 `RegressionCount` 2인 `StageProgress`, `Seconds` 12.5인 `PlayTime`, 꺼진 `AutoUpgrade`를 넘기면 `RegressionCount` 2, `PlayTimeMs` 12500, `AutoUpgrade` `false`다. 둘을 `null`로 넘기면 0, `true`다.
- 29. `MemorySaveStore`에 `TryWrite("a")` 뒤 `TryDelete()`는 `true`이고 `Text`는 `null`이다.
- 30. `AutoSave.Disable()` 뒤 금화가 바뀌고 `Flush`를 불러도 저장소에 쓰지 않는다.
- [플레이 모드] (추가) `Assets/Tests/PlayMode/Save/FileSaveStoreTests.cs`에 테스트를 더한다: 임시 폴더의 `FileSaveStore`에 `TryWrite("abc")` 뒤 `TryDelete()`는 `true`이고 `save.dat`는 없다. 파일이 없는 상태에서 `TryDelete()`도 `true`다.
- `SaveCodecTests.Encode_StandardData_ProducesExactText`가 요구사항의 새 `Standard` 값으로 통과한다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 판 번호(`SaveData.CurrentVersion`)를 올리지 않는다.
- `GameRoot`를 고치지 않는다 (096 이 `PlayTime`, `AutoUpgrade`, 저장된 세 값, `Disable`·`TryDelete` 호출을 연결한다).
- `StageProgress`, `PlayTime`, `AutoUpgrade`의 내부 코드를 고치지 않는다. 공개된 `RegressionCount`, `Milliseconds`, `Enabled`만 읽는다.
- 기존 테스트의 이름을 바꾸거나 지우지 않고, 위에 적은 것 말고 기대값을 바꾸지 않는다.
- Core 에서 `UnityEngine`을 쓰지 않는다. 씬·프리팹·에셋 파일과 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 판 2 저장에 회귀 횟수, 플레이 시간과 자동 강화 상태를 추가하고 이전 파일의 기본값과 잘못된 값을 처리했다.
- 메모리·파일 저장 삭제 및 자동 저장 끄기와 다시 켜기를 구현하고 관련 테스트를 추가했다.
- 저장 사양의 파일 형식과 공개 연결 지점을 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Core/Save/AutoSave.cs`, `ISaveStore.cs`, `MemorySaveStore.cs`, `SaveCodec.cs`, `SaveData.cs`
- `Assets/Scripts/Game/Save/FileSaveStore.cs`
- `Assets/Tests/EditMode/Save/FakeSaveStore.cs`, `SaveCodecTests.cs`, `SaveRegressionFieldsTests.cs` 및 그 `.meta`
- `Assets/Tests/PlayMode/OfflineReward/OfflineRewardViewTests.cs`, `Save/FileSaveStoreTests.cs`, `Save/SaveViewTests.cs`, `StartMenu/SettingsViewTests.cs`
- `docs/spec/save.md`

### 가정
- `Disable`은 기존 변경 표시와 재시도 대기를 지우지 않으며, 다시 켜면 기존 상태를 이어간다. 기존 `Enabled` 검사만 바꾸라는 요구를 따랐다.
- 명시된 네 가짜 저장소 외에 추가 `ISaveStore` 구현은 없다. 공개 연결 지점은 회귀 사양과 같다.

### 사람이 확인할 것
없음

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 에딧 모드와 플레이 모드 전체 테스트는 루프 스크립트의 실행 및 판정을 기다린다.


## 루프 기록 (2026-11-22 17:03)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/095_save_regression_fields
- 루프 브랜치: loop/20261122-164616
