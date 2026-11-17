# 028_auto_save — 저장소 연결 지점과 자동 저장 (성공하는 쓰기)

## 목표
`PlumSword.Core.Save` 에 저장소 연결 지점 `ISaveStore`, 메모리 저장소 `MemorySaveStore`, 자동 저장 `AutoSave` 를 만든다. `AutoSave` 는 세 기능의 알림으로 저장할 것이 생겼음을 기록하고 `Flush`·`SaveNow` 로 쓴다. 이 작업은 쓰기가 성공하는 경우, 켜기·끄기, `Tick` 의 인자 검사까지 한다. 쓰기 실패 뒤의 재시도 대기·`HasFailed`·`FailedChanged`·저장소 예외 처리는 029 가 한다. 에딧 모드 테스트로 검사한다.

## 묶음
저장

## 선행 작업
- 026_save_data
- 027_save_codec

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작" 절의 "자동 저장"과 "저장하는 때", "경계 상황" 표의 `AutoSave`·`Flush`·`SaveNow`·`Enable`·`Tick` 줄, "수치" 절의 재시도 대기와 허용 오차, "연결 지점" 절의 `ISaveStore`·`MemorySaveStore`·`AutoSave`, "완료 조건 초안"의 "가짜 저장소" 설명)
- docs/spec/currency.md ("연결 지점" 절의 `BalanceChanged`)
- docs/spec/stat_upgrade.md ("연결 지점" 절의 `Upgraded`, `TryUpgrade`)
- docs/spec/stage.md ("연결 지점" 절의 `StageStarted`, `Begin`)

## 요구사항
- 새 파일 (네임스페이스 `PlumSword.Core.Save`): `Assets/Scripts/Core/Save/ISaveStore.cs`, `Assets/Scripts/Core/Save/MemorySaveStore.cs`, `Assets/Scripts/Core/Save/AutoSave.cs`.
- 새 테스트 파일 (네임스페이스 `PlumSword.Core.Tests.Save`): `Assets/Tests/EditMode/Save/MemorySaveStoreTests.cs`, `Assets/Tests/EditMode/Save/AutoSaveTests.cs`, 그리고 테스트용 가짜 저장소 `Assets/Tests/EditMode/Save/FakeSaveStore.cs`. 테스트 이름은 `<상황>_<기대 결과>` 모양.
- **가짜 저장소** `public sealed class FakeSaveStore : ISaveStore` (029 도 그대로 쓰므로 아래 모양을 다 만든다):
  - `int WriteCount` — `TryWrite` 가 불린 횟수 (실패·예외 포함, 시도 횟수). 처음 0.
  - `string LastText` — 마지막으로 `TryWrite` 에 넘어온 글자 (실패해도 기록). 처음 `null`.
  - `bool Succeeds` (처음 `true`) — `TryWrite` 가 돌려줄 값.
  - `bool Throws` (처음 `false`) — `true` 면 `TryWrite` 가 `WriteCount`·`LastText` 를 기록한 뒤 `InvalidOperationException` 을 던진다.
- 선행 작업이 만든 것을 쓴다: 026 의 `SaveData.Capture(wallet, upgrades, stage, savedAtUtcTicks)`, 027 의 `SaveCodec.Encode(data)`·`SaveCodec.Decode(text)` (테스트에서 쓴 글자를 되돌려 값을 확인한다).
- 쓰는 기존 연결 지점 (고치지 않는다): `Wallet.BalanceChanged`(`Action<long>`, 잔액이 실제로 바뀐 때만), `StatUpgrades.Upgraded`(`Action<StatType>`, 강화 성공 때만), `StageProgress.StageStarted`(`Action<int>`, `Begin` 과 가려진 순간의 교체), `StageProgress.Begin()`.
- **공개 모양** (사양 "연결 지점" 그대로):

  ```csharp
  public interface ISaveStore
  {
      // text 를 저장한다. 다 쓰면 true. 실패하면 false 이고 그 전에 저장된 내용은 그대로 남긴다.
      // text 가 null 이면 ArgumentNullException. 그 밖에는 예외를 던지지 않는다.
      bool TryWrite(string text);
  }

  public sealed class MemorySaveStore : ISaveStore
  {
      public string Text { get; }          // 처음 값 null. 마지막으로 쓴 글자.
      public bool TryWrite(string text);   // Text = text, 언제나 true. text 가 null 이면 ArgumentNullException.
  }

  public sealed class AutoSave
  {
      public const double RetrySeconds = 3.0;   // (임시값)

      // store, wallet, upgrades, stage 중 null 이 있으면 ArgumentNullException.
      // wallet.BalanceChanged, upgrades.Upgraded, stage.StageStarted 를 구독한다. stage.Begin() 보다 먼저 만든다.
      // 생성자는 쓰지 않고 알림을 보내지 않는다. 시작 상태: Enabled = enabled, IsDirty false, HasFailed false, 대기 0.
      public AutoSave(ISaveStore store,
                      PlumSword.Core.Currency.Wallet wallet,
                      PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
                      PlumSword.Core.Stage.StageProgress stage,
                      bool enabled = true);

      public bool Enabled { get; }
      public bool IsDirty { get; }      // 저장하지 않은 변화가 있으면 true
      public bool HasFailed { get; }    // 마지막 쓰기가 실패했으면 true. 성공하면 false
      public event System.Action<bool> FailedChanged;   // HasFailed 가 실제로 바뀐 뒤에 한 번, 새 값

      public void Enable();
      public void Tick(double deltaSeconds);
      public bool Flush(long nowUtcTicks);
      public bool SaveNow(long nowUtcTicks);
  }
  ```

  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **알림**: 세 알림 중 하나가 오면 `Enabled` 일 때 `IsDirty = true` 만 한다 (바로 쓰지 않는다). 꺼져 있으면 무시한다. `AutoSave` 와 세 기능은 게임에 하나씩이고 수명이 같아 구독을 끊지 않는다.
- **쓰기** (private): `store.TryWrite(SaveCodec.Encode(SaveData.Capture(wallet, upgrades, stage, nowUtcTicks)))`.
  - 성공(`true`): `IsDirty = false`, 재시도 대기(private `double`) 0, `HasFailed = false`, `true` 를 돌려준다.
  - 실패(`false`): 이 작업에서는 `IsDirty` 를 `true` 로 두고 `false` 를 돌려주기만 한다. 재시도 대기 3.0초, `HasFailed`, `FailedChanged`, `TryWrite` 의 예외 처리는 029 가 더한다. (이 작업에서 `HasFailed` 는 늘 `false`, `FailedChanged` 는 선언만 한다)
- **`Flush(nowUtcTicks)`**: `Enabled` 이고 `IsDirty` 이고 재시도 대기가 0 이면 쓰고 그 결과를 돌려준다. 아니면 저장소를 부르지 않고 `false`.
- **`SaveNow(nowUtcTicks)`**: `Enabled` 이면 `IsDirty`·대기와 상관없이 바로 쓰고 그 결과를 돌려준다. 꺼져 있으면 저장소를 부르지 않고 `false`.
- **`Enable()`**: 꺼져 있으면 `Enabled = true`. 이미 켜져 있으면 아무 일도 없다. 끄는 메서드는 없다. 켤 때 `IsDirty` 를 바꾸지 않는다.
- **`Tick(deltaSeconds)`**: 음수·NaN·무한대면 `ArgumentOutOfRangeException` (상태와 상관없이). 재시도 대기에서 `deltaSeconds` 를 빼고, 남은 값이 `1e-9` 이하면 0 으로 만든다. 대기가 0 이면 아무 일도 없다. `Tick` 은 쓰지 않는다.
- 시각은 인자(`nowUtcTicks`)로만 받는다. Core 는 `DateTime.UtcNow` 를 읽지 않는다.
- Core 규칙: `UnityEngine` 을 쓰지 않는다.

## 완료 조건
"스테이지 객체"는 `new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), startStage, seenIntroStage)`(따로 적지 않으면 `startStage: 1`, `seenIntroStage: 5`), 강화는 `new StatUpgrades(wallet, 10, 100, 1.0)` 이다. "가짜 저장소"는 `FakeSaveStore`, "쓴 횟수"는 `WriteCount`, "마지막 글자"는 `LastText` 이다. `AutoSave` 는 따로 적지 않으면 `new AutoSave(가짜 저장소, wallet, upgrades, stage)` 이다.

- `Assets/Tests/EditMode/Save/MemorySaveStoreTests.cs` 와 `Assets/Tests/EditMode/Save/AutoSaveTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `new MemorySaveStore()` 의 `Text` 는 `null` 이고, `TryWrite("abc")` 는 `true` 이며 그 뒤 `Text` 는 `"abc"` 이다.
- `MemorySaveStore.TryWrite(null)` 은 `ArgumentNullException` 이다.
- `AutoSave` 생성자에 `store`, `wallet`, `upgrades`, `stage` 를 하나씩 `null` 로 넣으면 각각 `ArgumentNullException` 이다.
- `AutoSave` 를 만든 직후 쓴 횟수 0, `Enabled` `true`, `IsDirty` `false`, `HasFailed` `false` 이고, 그 상태에서 `Flush(100)` 은 `false`, 쓴 횟수 0 이다.
- `new Wallet(0)` 으로 만든 `AutoSave` 에서 `wallet.Add(10)` 을 하면 `IsDirty` 가 `true` 이고 쓴 횟수는 0 이다.
- 이어서 `Flush(100)` 은 `true`, 쓴 횟수 1, `IsDirty` `false` 이고, 마지막 글자를 `SaveCodec.Decode` 하면 `Loaded` 에 `Gold` 10, `SavedAtUtcTicks` 100 이다.
- 이어서 바로 `Flush(200)` 은 `false`, 쓴 횟수 1 이다.
- `new Wallet(100)` 으로 만든 `AutoSave` 에서 `upgrades.TryUpgrade(StatType.Attack)`(금화 80, 공격력 단계 1) 뒤 `wallet.Add(5)` 를 하고 `Flush(100)` 을 한 번 부르면 쓴 횟수 1 이고 마지막 글자의 `Gold` 85, `AttackLevel` 1 이다.
- `new Wallet(0)` 으로 만든 `AutoSave` 에서 `wallet.Add(0)`, 실패하는 `upgrades.TryUpgrade(StatType.Attack)`, `wallet.TrySpend(5)` 를 해도 `IsDirty` 는 `false` 이다.
- `startStage: 1`, `seenIntroStage: 0` 인 스테이지 객체로 `AutoSave` 를 만든 뒤 `stage.Begin()` 을 부르면 `IsDirty` 가 `true` 이다.
- 이어서 `Flush(100)` 은 `true` 이고 마지막 글자를 `Decode` 하면 `StageNumber` 1, `SeenIntroStage` 1 이다.
- 변화가 없는 `AutoSave` 에서 `SaveNow(500)` 은 `true`, 쓴 횟수 1, 마지막 글자의 `SavedAtUtcTicks` 500 이다.
- 가짜 저장소의 `Succeeds` 를 `false` 로 하고 `wallet.Add(10)` → `Flush(100)` 을 하면 `false`, 쓴 횟수 1, `IsDirty` `true` 이다.
- `new Wallet(0)`, `enabled: false` 로 만든 `AutoSave` 에서 `wallet.Add(10)` 을 하면 `IsDirty` 는 `false` 이고, `Flush(100)` 과 `SaveNow(100)` 은 둘 다 `false`, 쓴 횟수 0 이다.
- 이어서 `Enable()` 뒤 `Enabled` 는 `true`, `IsDirty` 는 `false` 이고, `wallet.Add(5)` → `Flush(200)` 은 `true`, 쓴 횟수 1, 마지막 글자의 `Gold` 15 이다.
- 이어서 `Enable()` 을 한 번 더 불러도 `Enabled` `true`, `IsDirty` `false`, 쓴 횟수 1 그대로다.
- `AutoSave.Tick` 에 −0.1, `double.NaN`, `double.PositiveInfinity` 를 넣으면 각각 `ArgumentOutOfRangeException` 이다.
- 대기가 없는 `AutoSave` 에서 `Tick(0)` 과 `Tick(1.0)` 뒤 `IsDirty` `false`, `HasFailed` `false`, 쓴 횟수 0 그대로다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 쓰기 실패 뒤의 재시도 대기 설정, `HasFailed = true`, `FailedChanged` 보내기, `TryWrite` 의 예외 잡기는 만들지 않는다 (029 가 한다).
- 026·027 이 만든 `SaveData`, `SaveLoadResult`, `SaveCodec` 의 공개 모양을 바꾸지 않는다.
- `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Stage/`, `Assets/Scripts/Core/Combat/` 의 코드를 고치지 않는다.
- 파일 저장소(`FileSaveStore`)와 `PlumSword.Game` 의 코드(`GameRoot` 포함)는 만들거나 고치지 않는다 (030·031·032 가 한다).
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 저장소 인터페이스와 메모리 저장소, 변경 알림을 모아 저장하는 AutoSave를 구현했다.
- 저장 성공·실패의 이번 작업 범위, 비활성화와 활성화, 시간 인자 검사를 검증하는 에딧 모드 테스트와 가짜 저장소를 추가했다.

### 바뀐 파일
- Assets/Scripts/Core/Save/ISaveStore.cs, MemorySaveStore.cs, AutoSave.cs 및 각 .meta
- Assets/Tests/EditMode/Save/FakeSaveStore.cs, MemorySaveStoreTests.cs, AutoSaveTests.cs 및 각 .meta
- tasks/doing/028_auto_save.md, .loop/result.json

### 가정
- 없음. 공개 연결 지점과 수치는 기존 사양과 작업 문서를 따른다.

### 사람이 확인할 것
- Unity 라이선스 클라이언트 연결이 복구된 환경에서 전체 테스트 결과를 확인한다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py` 실행을 시도했으나 Unity 라이선스 클라이언트 연결이 거부되고 재연결이 반복되어 테스트를 직접 실행하지 못했다. 실행을 중단했으며 통과 여부는 확인하지 못했다.


## 루프 기록 (2026-11-17 14:03)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/028_auto_save
- 루프 브랜치: loop/20261117-134243
