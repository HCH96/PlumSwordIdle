# 046_realm_progress — 경지 진행과 돌파 (Core)

## 목표
지금 경지를 들고 금화를 써서 한 단계씩 돌파하고, 잠금과 알림을 제공하는 `RealmProgress` 를 `PlumSword.Core.Realm` 에 순수 C# 로 만들고 에딧 모드 테스트로 검사한다. Core 작업이다.

## 묶음
realm

## 선행 작업
- 045_realm_rules_text

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("동작"과 경계 상황 표, "연결 지점"의 `RealmProgress`, "저장", "완료 조건 초안" 1~17)
- docs/spec/currency.md ("연결 지점" 절의 `Wallet`)

## 요구사항
- 파일 `Assets/Scripts/Core/Realm/RealmProgress.cs` (네임스페이스 `PlumSword.Core.Realm`, `.meta` 함께). 045 가 만든 `RealmLevel`, `RealmRules.BonusPercent`, `RealmRules.BreakthroughCost` 를 쓴다.
- 사양 "연결 지점"의 모양을 그대로 만든다:
  ```csharp
  public sealed class RealmProgress
  {
      public RealmProgress(PlumSword.Core.Currency.Wallet wallet, int startLevel = 0);
      public RealmLevel Current { get; }
      public bool IsMax { get; }            // Current == Transcendent
      public int BonusPercent { get; }      // RealmRules.BonusPercent(Current)
      public long NextCost { get; }         // RealmRules.BreakthroughCost(Current). 초절정이면 0
      public bool CanBreakthrough();
      public bool TryBreakthrough();
      public event System.Action<RealmLevel> BrokeThrough;
      public bool Locked { get; }           // 처음 값 false
      public void SetLocked(bool locked);
      public event System.Action<bool> LockedChanged;
  }
  ```
- 생성자: `wallet` 이 `null` 이면 `ArgumentNullException`. `startLevel` 이 음수면 0(삼류), 4 보다 크면 4(초절정)로 만든다 (예외 없음). 생성자는 금화를 쓰지 않고 알림을 보내지 않는다. 잠기지 않은 상태로 시작한다.
- `BonusPercent`, `NextCost`, `IsMax` 는 부를 때마다 지금 경지로 계산한다.
- `TryBreakthrough()` 는 이 순서로 처리한다:
  1. `Locked` 면 `false`.
  2. `IsMax` 면 `false`.
  3. `wallet.TrySpend(NextCost)` 가 `false` 면(금화 부족) `false`.
  4. `Current` 를 한 단계 올린다.
  5. `BrokeThrough` 를 새 경지를 담아 한 번 보내고 `true` 를 돌려준다.
  `false` 인 경우 금화도 경지도 바뀌지 않고 알림도 없다. 알림 순서는 `Wallet.BalanceChanged`(3번에서) → 경지 증가 → `BrokeThrough` 이므로, `BalanceChanged` 를 받는 시점의 `Current` 는 이전 경지이고 `BrokeThrough` 를 받는 쪽이 읽는 `Current`·`BonusPercent`·`NextCost`·`IsMax` 는 새 값이다.
- `CanBreakthrough()`: 잠기지 않았고, 초절정이 아니고, `wallet.CanSpend(NextCost)` 가 `true` 일 때만 `true`. 아무것도 바꾸지 않고 알림도 없다.
- `SetLocked(locked)`: 값이 실제로 바뀐 때에만, 바꾼 뒤에 `LockedChanged` 를 새 값을 담아 한 번 보낸다. 같은 값이면 아무 일도 없다.
- 같은 시점에 여러 번 부르면 부른 순서대로, 그때의 금화와 경지로 따로 판정한다.
- 시간·난수는 쓰지 않는다. `UnityEngine` 을 쓰지 않는다.
- 테스트는 `Assets/Tests/EditMode/Realm/RealmProgressTests.cs` (네임스페이스 `PlumSword.Core.Tests.Realm`). 금화는 `new Wallet(금액)` 으로 준비한다. 아래 완료 조건에서 따로 적지 않으면 `new RealmProgress(wallet)` 으로 만든 것이다.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/realm.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Realm/RealmProgressTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- 새로 만들면 `Current` 는 `ThirdRate`, `IsMax` 는 false, `BonusPercent` 는 0, `NextCost` 는 200, `Locked` 는 false 다.
- 금화 3700 에서 `TryBreakthrough` 를 4번 부르면 모두 true 이고, 부르기 전의 `NextCost` 는 순서대로 200, 500, 1000, 2000 이며, 끝난 뒤 `Current` 는 `Transcendent`, `IsMax` 는 true, `BonusPercent` 는 40, `NextCost` 는 0, 금화는 0 이다.
- 금화 1000 에서 `TryBreakthrough` 를 1번 부르면 `Current` 는 `SecondRate`, `BonusPercent` 는 10, `NextCost` 는 500, 금화는 800 이다.
- 금화 199 에서 `TryBreakthrough` 는 false 이고 `Current` 는 `ThirdRate`, 금화는 199 이며 `BrokeThrough` 와 `BalanceChanged` 는 0번 온다.
- 금화 200 에서 `TryBreakthrough` 는 true 이고 `Current` 는 `SecondRate`, 금화는 0 이다.
- 금화 1000 에서 `TryBreakthrough` 를 3번 부르면 결과는 순서대로 true, true, false 이고 `Current` 는 `FirstRate`, 금화는 300 이다.
- `new RealmProgress(wallet, 4)` 에 금화 10000 이 있어도 `TryBreakthrough` 는 false, `CanBreakthrough` 는 false 이고 금화는 10000, `Current` 는 `Transcendent` 이며 `BrokeThrough` 와 `BalanceChanged` 는 0번 온다.
- 금화 1000 에서 `TryBreakthrough` 를 부르면 `BrokeThrough` 가 `SecondRate` 를 담아 1번 오고, 받는 쪽에서 읽은 `Current` 는 `SecondRate`, `BonusPercent` 는 10, `NextCost` 는 500 이다.
- 금화 1000 에서 `TryBreakthrough` 를 부르면 `Wallet.BalanceChanged` 가 `BrokeThrough` 보다 먼저 오고, `BalanceChanged` 를 받는 시점에 읽은 `Current` 는 `ThirdRate` 다.
- 금화 200 에서 `CanBreakthrough` 는 true, 금화 199 에서는 false 이고, 부른 뒤 금화와 경지는 그대로이며 알림은 0번 온다.
- `SetLocked(true)` 를 부르면 `Locked` 는 true 이고 `LockedChanged` 가 true 를 담아 1번 온다.
- `SetLocked(true)` 뒤에 한 번 더 `SetLocked(true)` 를 부르면 `LockedChanged` 는 더 오지 않는다 (모두 합쳐 1번).
- 금화 1000 에서 `SetLocked(true)` 뒤 `CanBreakthrough` 는 false, `TryBreakthrough` 는 false 이고 금화는 1000, `Current` 는 `ThirdRate` 이며 `BrokeThrough` 는 0번 온다.
- 바로 위 상태에서 `SetLocked(false)` 를 부르면 `LockedChanged` 가 false 를 담아 1번 오고, 그 뒤 `TryBreakthrough` 는 true 다.
- 새로 만든 직후 `SetLocked(false)` 를 부르면 `LockedChanged` 는 0번 온다.
- 금화 500 으로 `new RealmProgress(wallet, 2)` 를 만들면 `Current` 는 `FirstRate`, `BonusPercent` 는 20, `NextCost` 는 1000 이고, 만드는 동안 금화는 500 그대로이며 `BalanceChanged` 는 0번 온다.
- `new RealmProgress(wallet, -1)` 의 `Current` 는 `ThirdRate` 이고 예외가 없다.
- `new RealmProgress(wallet, 7)` 의 `Current` 는 `Transcendent` 이고 예외가 없다.
- `new RealmProgress(null)` 은 `ArgumentNullException` 을 던진다.
- 기존 테스트가 모두 통과한다 (`CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함).

## 하지 말 것
- 045 가 만든 `RealmLevel`·`RealmRules`·`RealmText`·`AuraColor`·`RealmAura` 의 동작과 테스트를 바꾸지 않는다.
- 화면 코드(`Assets/Scripts/Game/`)와 `GameRoot` 는 고치지 않는다 (047, 048 이 한다).
- `UnityEngine` 을 쓰지 않는다.
- `Assets/Scripts/Core/Currency/` 는 고치지 않는다. `Wallet` 의 공개 메서드(`CanSpend`, `TrySpend`, `BalanceChanged`)만 쓴다.
- `StatUpgrades`·`Battle`·`SaveData`·`SaveCodec`·`AutoSave` 를 고치지 않는다 (보너스 적용은 능력치 강화, 돌파 때 현재 체력 증가는 전투, 경지 저장은 저장 기능 사양의 일이다).
- `docs/dev/assets.md` 를 고치지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의를 고치지 않는다.

## 보고서

### 한 일
- 지갑을 통한 경지 돌파, 경지별 보너스와 다음 비용, 잠금 및 변경 알림을 순수 C#으로 구현했다.
- 초기화, 범위 보정, 연속 돌파, 비용 부족, 최대 경지, 잠금, 알림 순서와 상태를 검사하는 에딧 모드 테스트를 작성했다.

### 바뀐 파일
- `Assets/Scripts/Core/Realm/RealmProgress.cs` 및 `.meta`
- `Assets/Tests/EditMode/Realm/RealmProgressTests.cs` 및 `.meta`
- `tasks/doing/046_realm_progress.md` (보고서 추가)
- `.loop/result.json` (루프 결과)

### 가정
- 없음. 공개 연결 지점과 동작은 기존 사양을 따르므로 사양 변경은 없다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았다. 새 테스트와 기존 테스트의 실행 및 통과 여부는 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-18 12:18)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/046_realm_progress
- 루프 브랜치: loop/20261118-121225
