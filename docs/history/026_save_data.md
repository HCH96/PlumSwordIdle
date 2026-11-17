# 026_save_data — 저장 값 묶음, 불러오기 결과, 저장 표시 글자

## 목표
`PlumSword.Core.Save` 에 저장 값을 담는 `SaveData`(지금 값 모으기 `Capture` 포함), 불러오기 상태 `SaveLoadStatus`, 불러오기 결과 `SaveLoadResult`, 표시 글자 `SaveText` 를 만든다. 글자로 바꾸고 되돌리기(`SaveCodec`)는 027, 자동 저장은 028·029 가 한다. 에딧 모드 테스트로 검사한다.

## 묶음
저장

## 선행 작업
- 023_stage_intro_begin

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작" 절의 "저장 값"·"모으기", "경계 상황" 표의 `Capture`·`SaveLoadResult` 줄, "수치" 절의 판 번호와 "표시 글자" 표, "연결 지점" 절의 `SaveData`·`SaveLoadStatus`·`SaveLoadResult`·`SaveText`)
- docs/spec/currency.md ("연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절)
- docs/spec/stage.md ("연결 지점" 절)
- docs/spec/combat.md ("연결 지점" 절의 `Battle` 생성자와 시작 능력치 상수)

## 요구사항
- 새 폴더 `Assets/Scripts/Core/Save/` (네임스페이스 `PlumSword.Core.Save`). 파일 하나에 공개 타입 하나: `SaveData.cs`, `SaveLoadStatus.cs`, `SaveLoadResult.cs`, `SaveText.cs`.
- 새 테스트 파일: `Assets/Tests/EditMode/Save/SaveDataTests.cs`, `Assets/Tests/EditMode/Save/SaveLoadResultTests.cs`, `Assets/Tests/EditMode/Save/SaveTextTests.cs` (네임스페이스 `PlumSword.Core.Tests.Save`). 테스트 이름은 `<상황>_<기대 결과>` 모양.
- **공개 모양** (사양 "연결 지점" 그대로):

  ```csharp
  namespace PlumSword.Core.Save
  {
      public sealed class SaveData
      {
          public const int CurrentVersion = 1;

          // 값을 검사하지 않고 그대로 담는다 (예외 없음). 잘라내기는 각 기능의 생성자가 한다.
          public SaveData(long gold, int attackLevel, int healthLevel, int attackSpeedLevel,
                          int stageNumber, int seenIntroStage, long savedAtUtcTicks);

          public long Gold { get; }
          public int AttackLevel { get; }
          public int HealthLevel { get; }
          public int AttackSpeedLevel { get; }
          public int StageNumber { get; }
          public int SeenIntroStage { get; }
          public long SavedAtUtcTicks { get; }

          // wallet.Balance, upgrades.GetLevel(Attack / Health / AttackSpeed), stage.StageNumber, stage.SeenIntroStage 를 읽어 만든다.
          // 아무것도 바꾸지 않는다. wallet, upgrades, stage 중 null 이 있으면 ArgumentNullException.
          public static SaveData Capture(PlumSword.Core.Currency.Wallet wallet,
                                         PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
                                         PlumSword.Core.Stage.StageProgress stage,
                                         long savedAtUtcTicks);
      }

      public enum SaveLoadStatus
      {
          Empty = 0,               // 저장 없음 (첫 실행). 오류 아님
          Loaded = 1,              // 정상
          Corrupt = 2,             // 손상. 덮어쓰지 않고 오류 창
          UnsupportedVersion = 3,  // 판 번호가 1 이 아님. 오류 없이 새로 시작
      }

      public sealed class SaveLoadResult
      {
          // status 가 Loaded 인데 data 가 null 이거나, Loaded 가 아닌데 data 가 null 이 아니면 ArgumentException.
          public SaveLoadResult(SaveLoadStatus status, SaveData data);

          public SaveLoadStatus Status { get; }
          public SaveData Data { get; }        // Loaded 일 때만 있다. 나머지는 null
          public bool HasSaveData { get; }     // Status == SaveLoadStatus.Loaded
      }

      public static class SaveText
      {
          public const string SaveFailed = "SAVE FAILED";
          public const string CorruptTitle = "SAVE DATA IS CORRUPTED";
          public const string CorruptMessage = "START A NEW GAME? THE OLD SAVE WILL BE REPLACED.";
          public const string NewGameButton = "NEW GAME";
      }
  }
  ```

  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 쓰는 기존 연결 지점 (이미 있다. 고치지 않는다): `Wallet(long initialBalance)`, `Wallet.Balance`; `StatUpgrades(wallet, baseAttack, baseMaxHealth, baseAttackInterval, attackLevel, healthLevel, attackSpeedLevel)`, `StatUpgrades.GetLevel(StatType)`; `StageProgress(wallet, upgrades, battle, random, startStage, seenIntroStage)`, `StageProgress.StageNumber`, `StageProgress.SeenIntroStage`, `StageProgress.HasBegun`; `Battle(StatUpgrades)`. 생성자의 잘라내기: 금화 음수 → 0, 단계 음수 → 0, 공격 속도 10 초과 → 10, `startStage` 1 미만 → 1, `seenIntroStage` 0~5 밖 → 자름.
- `Capture` 는 속성만 읽고 상태를 바꾸는 메서드를 부르지 않는다 (알림이 가지 않는다). `Begin()` 전의 스테이지 객체도 받는다.
- `SaveLoadResult` 의 `status` 가 정의되지 않은 enum 값일 때의 처리는 정하지 않는다 (테스트하지 않는다).
- Core 규칙: `UnityEngine` 을 쓰지 않는다.

## 완료 조건
"스테이지 객체"는 `new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), startStage, seenIntroStage)` 이고, 강화는 시작 능력치 `(10, 100, 1.0)` 으로 만든다.

- `Assets/Tests/EditMode/Save/SaveDataTests.cs`, `SaveLoadResultTests.cs`, `SaveTextTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `SaveData.CurrentVersion` 은 1 이다.
- `new SaveData(1234, 3, 2, 10, 7, 5, 638000000000000000)` 의 일곱 속성은 차례로 1234, 3, 2, 10, 7, 5, 638000000000000000 이다.
- `new SaveData(-5, 0, 0, 15, 0, 9, 0)` 은 예외 없이 만들어지고 `Gold` −5, `AttackSpeedLevel` 15, `StageNumber` 0, `SeenIntroStage` 9 가 그대로다.
- `new SaveData(-5, 0, 0, 15, 0, 9, 0)` 의 값으로 `new Wallet(-5)`, `new StatUpgrades(wallet, 10, 100, 1.0, 0, 0, 15)`, 스테이지 객체(`startStage: 0`, `seenIntroStage: 9`)를 만들면 `Balance` 0, `GetLevel(StatType.AttackSpeed)` 10, `StageNumber` 1, `SeenIntroStage` 5 이다.
- `new Wallet(500)`, 단계 `(2, 1, 3)` 인 `StatUpgrades`, `startStage: 4`, `seenIntroStage: 3` 인 스테이지 객체(`Begin` 전)로 `SaveData.Capture(wallet, upgrades, stage, 100)` 을 하면 일곱 값이 500, 2, 1, 3, 4, 3, 100 이다.
- 위 `Capture` 뒤 `wallet.Balance` 500, 세 단계 2·1·3, `stage.StageNumber` 4, `stage.SeenIntroStage` 3, `stage.HasBegun` `false` 가 그대로이고, `Capture` 동안 `BalanceChanged`·`Upgraded`·`StageStarted` 는 한 번도 오지 않는다.
- `SaveData.Capture` 에 `wallet`, `upgrades`, `stage` 를 하나씩 `null` 로 넣으면 각각 `ArgumentNullException` 이다.
- `new SaveLoadResult(SaveLoadStatus.Loaded, null)` 은 `ArgumentException` 이다.
- `new SaveLoadResult(SaveLoadStatus.Empty, new SaveData(0, 0, 0, 0, 1, 0, 0))` 은 `ArgumentException` 이다.
- `new SaveLoadResult(SaveLoadStatus.Corrupt, null)` 은 `Status` `Corrupt`, `Data` `null`, `HasSaveData` `false` 이다.
- `new SaveLoadResult(SaveLoadStatus.Loaded, data)` 는 `Status` `Loaded`, `Data` 가 넣은 그 객체, `HasSaveData` `true` 이다.
- `SaveLoadStatus` 의 정수 값은 `Empty` 0, `Loaded` 1, `Corrupt` 2, `UnsupportedVersion` 3 이다.
- `SaveText` 의 네 값은 `"SAVE FAILED"`, `"SAVE DATA IS CORRUPTED"`, `"START A NEW GAME? THE OLD SAVE WILL BE REPLACED."`, `"NEW GAME"` 이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Stage/`, `Assets/Scripts/Core/Combat/` 의 코드를 고치지 않는다. 연결 지점으로만 쓴다.
- `SaveCodec`, `ISaveStore`, `MemorySaveStore`, `AutoSave` 는 만들지 않는다 (027·028 이 한다).
- `PlumSword.Game` 의 코드(`GameRoot` 포함)를 고치지 않는다.
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 저장 값과 현재 상태 모으기, 불러오기 결과와 상태, 표시 문자열을 사양에 맞게 구현했다.
- 값 보존과 복원 시 범위 보정, 알림 없는 모으기, null 검사, 결과 상태와 문자열을 에딧 모드 테스트로 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Save/`의 네 C# 파일과 폴더·파일 메타 파일.
- `Assets/Tests/EditMode/Save/`의 세 테스트 파일과 폴더·파일 메타 파일.
- `tasks/doing/026_save_data.md` 보고서와 `.loop/result.json`.

### 가정
- 없음. 공개 연결 지점은 기존 저장 사양과 같다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: 직접 실행하지 못함. Unity 로그에 임시 파일 쓰기 실패와 라이선스 클라이언트 연결 실패가 기록되어 실행을 중단했다. 신규 및 기존 테스트 통과 여부는 루프의 후속 검증에서 확인해야 한다.
- `git diff --check`: 통과 (추적되지 않은 신규 파일은 검사 범위 밖).


## 루프 기록 (2026-11-17 13:47)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/026_save_data
- 루프 브랜치: loop/20261117-134243
