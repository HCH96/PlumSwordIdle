# 029_auto_save_retry — 자동 저장의 쓰기 실패와 재시도

## 목표
`PlumSword.Core.Save.AutoSave` 에 쓰기 실패 처리를 더한다: 실패하면 `HasFailed = true` 와 재시도 대기 3.0초, `HasFailed` 가 바뀔 때만 `FailedChanged`, 저장소가 예외를 던져도 실패로 보고 밖으로 내보내지 않는다. 에딧 모드 테스트로 검사한다.

## 묶음
저장

## 선행 작업
- 028_auto_save

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작" 절의 "자동 저장"(쓰기·시간), "경계 상황" 표의 쓰기 실패·재시도 대기·계속 실패·실패 뒤 성공·예외 줄, "수치" 절의 재시도 대기와 허용 오차, "연결 지점" 절의 `AutoSave`)
- docs/spec/currency.md ("연결 지점" 절의 `BalanceChanged`)

## 요구사항
- 고칠 파일: `Assets/Scripts/Core/Save/AutoSave.cs` (028 이 만들었다).
- 새 테스트 파일: `Assets/Tests/EditMode/Save/AutoSaveRetryTests.cs` (네임스페이스 `PlumSword.Core.Tests.Save`, 클래스 `AutoSaveRetryTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양. 028 의 `AutoSaveTests` 는 고치지 않는다 (그대로 통과해야 한다).
- 선행 작업 028_auto_save 가 만든 것을 쓴다: `AutoSave`(공개 모양은 그대로: `RetrySeconds = 3.0`, `Enabled`, `IsDirty`, `HasFailed`, `event Action<bool> FailedChanged`, `Enable()`, `Tick(double)`, `Flush(long)`, `SaveNow(long)`), 테스트용 `FakeSaveStore`(`WriteCount` 시도 횟수, `LastText`, `Succeeds`, `Throws` — `Throws` 면 `InvalidOperationException`). 027 의 `SaveCodec.Decode`.
- **쓰기**(028 의 private 쓰기)를 아래처럼 완성한다.
  - `store.TryWrite` 가 예외를 던지면 잡아서 `false` 로 본다. 예외를 밖으로 내보내지 않는다 (`Flush`·`SaveNow` 가 예외 없이 `false` 를 돌려준다).
  - 성공: `IsDirty = false`, 재시도 대기 0, `HasFailed = false`, `true`. (028 그대로)
  - 실패: `IsDirty = true`, 재시도 대기 = `RetrySeconds`(3.0), `HasFailed = true`, `false`. 실패할 때마다 대기를 3.0 으로 다시 둔다.
  - `HasFailed` 가 실제로 바뀐 때에만, 바꾼 뒤에 `FailedChanged(새 값)` 를 한 번 보낸다. 계속 실패하면 처음 한 번만, 실패 뒤 성공하면 `FailedChanged(false)` 한 번.
- 재시도 대기 중(`대기 > 0`) `Flush` 는 저장소를 부르지 않고 `false` 다 (028 의 조건 그대로). 그사이 새 변화가 와도 대기가 끝날 때까지 쓰지 않는다. `SaveNow` 는 대기와 상관없이 쓴다.
- `Tick` 은 028 그대로 (대기에서 빼고 `1e-9` 이하면 0). 예: 대기 3.0 에서 `Tick(2.9)` → 약 0.1 남음, 이어서 `Tick(0.1)` → 1e-9 이하라 0.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- Core 규칙: `UnityEngine` 을 쓰지 않는다.

## 완료 조건
따로 적지 않으면 `wallet = new Wallet(0)`, `upgrades = new StatUpgrades(wallet, 10, 100, 1.0)`, `stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0))`, `store = new FakeSaveStore()`, `autoSave = new AutoSave(store, wallet, upgrades, stage)` 이다. "시도"는 `store.WriteCount`, "마지막 글자"는 `store.LastText` 이다. `FailedChanged` 는 `AutoSave` 를 만든 직후 구독해 받은 값을 차례로 기록한다.

- `Assets/Tests/EditMode/Save/AutoSaveRetryTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `store.Succeeds = false` 로 하고 `wallet.Add(10)` → `Flush(100)` 을 하면 `false`, 시도 1, `HasFailed` `true`, `IsDirty` `true` 이다.
- 위 실패에서 `FailedChanged` 는 `true` 를 담아 정확히 한 번 온다.
- 위 실패에 이어 `store.Succeeds = true` 로 바꾸고 바로 `Flush(200)` 을 하면 `false` 이고 시도는 1 그대로다.
- 이어서 `Tick(2.9)` 뒤 `Flush(250)` 은 `false` 이고 시도는 1 그대로다.
- 이어서 `Tick(0.1)` 뒤 `Flush(300)` 은 `true`, 시도 2, `HasFailed` `false`, `IsDirty` `false` 이고 마지막 글자를 `Decode` 하면 `SavedAtUtcTicks` 300 이다.
- 위 흐름 전체에서 `FailedChanged` 는 모두 두 번, 차례로 `true`, `false` 를 담아 온다.
- `store.Succeeds = false` 로 둔 채 `wallet.Add(10)` → `Flush(100)` → `Tick(3.0)` → `Flush(200)` 을 하면 시도 2, `HasFailed` `true` 이고 `FailedChanged` 는 한 번만(`true`) 온다.
- 이어서 `Tick(2.9)` → `Flush(300)` 을 하면 `false` 이고 시도는 2 그대로다 (두 번째 실패에서 대기를 다시 3.0 으로 셌다).
- `store.Succeeds = false` 로 `wallet.Add(10)` → `Flush(100)` 실패 뒤 `store.Succeeds = true` 로 바꾸고 `wallet.Add(5)` → `Tick(1.0)` → `Flush(400)` 을 하면 `false` 이고 시도는 1 그대로다 (대기 중의 새 변화도 쓰지 않는다).
- `store.Throws = true` 로 하고 `wallet.Add(10)` → `Flush(100)` 을 하면 예외 없이 `false`, `HasFailed` `true`, `IsDirty` `true` 이다.
- `store.Throws = true` 로 하고 `SaveNow(100)` 을 부르면 예외 없이 `false`, `HasFailed` `true` 이다.
- `store.Succeeds = false` 로 `wallet.Add(10)` → `Flush(100)` 실패 뒤 대기 중에 `store.Succeeds = true` 로 바꾸고 `SaveNow(600)` 을 부르면 `true`, 시도 2, `HasFailed` `false`, `IsDirty` `false`, 마지막 글자의 `SavedAtUtcTicks` 600 이다.
- 위 `SaveNow(600)` 성공 뒤 바로 `wallet.Add(1)` → `Flush(700)` 은 `true`, 시도 3 이다 (성공으로 대기가 0 이 되었다).
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `AutoSave` 의 공개 모양(메서드·속성·이벤트·상수의 이름과 모양)을 바꾸지 않는다.
- `FakeSaveStore`, `AutoSaveTests`, `MemorySaveStoreTests` 를 고치지 않는다.
- `Assets/Scripts/Core/` 의 다른 기능 폴더와 `PlumSword.Game` 의 코드를 고치지 않는다.
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 저장 실패와 저장소 예외를 실패 상태로 처리하고 3초 재시도 대기 및 상태 변화 알림을 구현했다.
- 재시도, 연속 실패, 대기 중 변경, 예외, 즉시 저장의 에딧 모드 테스트를 추가했다.
- 기존 저장 사양에 해당 동작이 이미 명시되어 있어 사양 변경은 없다.

### 바뀐 파일
- `Assets/Scripts/Core/Save/AutoSave.cs`
- `Assets/Tests/EditMode/Save/AutoSaveRetryTests.cs`
- `Assets/Tests/EditMode/Save/AutoSaveRetryTests.cs.meta`

### 가정
- 없음. 공개 연결 지점은 변경하지 않았다.

### 사람이 확인할 것
- Unity 라이선스 클라이언트 연결 실패로 테스트를 직접 완료하지 못했으므로 라이선스가 정상인 환경에서 전체 테스트 결과를 확인한다.

### 테스트 결과
- `git diff --check`: 통과.
- `python tools/loop/run_unity_tests.py`: Unity 라이선스 클라이언트 연결이 거부되고 초기화가 시간 초과되어 테스트 시작을 확인하지 못해 중단했다. 신규 및 기존 테스트의 통과 여부는 미확인이다.


## 루프 기록 (2026-11-17 14:08)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/029_auto_save_retry
- 루프 브랜치: loop/20261117-134243
