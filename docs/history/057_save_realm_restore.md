# 057_save_realm_restore — 경지·내공 자동 저장 확인과 불러오기 복원

## 목표
돌파와 내공 강화가 자동 저장으로 판 2 파일에 들어가는지 Core 에딧 모드 테스트로 확인하고, `PlumSword.Game.GameRoot` 가 불러온 저장의 경지·내공 단계로 `RealmProgress` 와 `StatUpgrades` 를 만들게 한다. Core 는 테스트만 더하고, 코드 변경은 Game 의 `GameRoot` 두 줄이다.

## 묶음
save_m3

## 선행 작업
- 056_save_data_v2

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작"의 "불러오기"·"자동 저장", "연결 지점"의 "`GameRoot.Awake` 순서", "완료 조건 초안" 37~40, "변경 기록" 절)
- docs/spec/realm.md ("연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-19 — 3단계: 판 2, 공격 속도 칸 → 내공 칸, 경지 칸 추가` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 저장 형식(판 2 아홉 줄), `SaveData` 의 `InnerPowerLevel`·`RealmLevel`, `SaveData.Capture(wallet, upgrades, realm, stage, savedAtUtcTicks)`, `AutoSave(store, wallet, upgrades, realm, stage, enabled = true)` 는 `056_save_data_v2` 가 이미 만들었다. 이 작업은 그 나머지(돌파·내공 강화의 저장 확인, `GameRoot` 의 경지·내공 복원)만 한다.
- `Assets/Scripts/Game/GameRoot.cs` 의 `Awake`:
  - `Realm = new PlumSword.Core.Realm.RealmProgress(Wallet);` 를 `Realm = new PlumSword.Core.Realm.RealmProgress(Wallet, data != null ? data.RealmLevel : 0);` 로 바꾼다. (`data` 는 지금 코드의 `LoadResult.Data` 이고 `Loaded` 일 때만 `null` 이 아니다.)
  - `StatUpgrades` 생성자의 내공 인자 `0` 을 `data != null ? data.InnerPowerLevel : 0` 으로 바꾼다. 다른 인자(시작 공격력, 시작 최대 체력, 공격력·체력 단계, `Realm`)는 그대로다.
  - 범위 밖 값은 각 생성자가 잘라낸다 (경지 0 미만 → 0, 4 초과 → 4. 내공 음수 → 0). `GameRoot` 에서 따로 자르지 않는다.
  - `Awake` 의 다른 순서와 줄은 바꾸지 않는다.
- 새 에딧 모드 테스트 파일 `Assets/Tests/EditMode/Save/AutoSaveRealmTests.cs` (네임스페이스 `PlumSword.Core.Tests.Save`, 클래스 `AutoSaveRealmTests`). 준비: 지갑 → 경지 객체 `new RealmProgress(wallet)` (삼류) → `new StatUpgrades(wallet, 10, 100, 0, 0, 0, realm)` → 스테이지 객체 `new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), 1, 5)` → 같은 폴더의 기존 `FakeSaveStore` → `new AutoSave(store, wallet, upgrades, realm, stage)`. 마지막 글자는 `SaveCodec.Decode(store.LastText)` 로 읽는다.
- 수치 (이미 구현된 값): 삼류 → 이류 돌파 비용 200, 경지 보너스 일류 20%. 강화 비용 `20 + 5 × 단계` (내공 단계 0 이면 20). 공격력 `(10 + 2 × 공격력 단계) × (100 + 보너스) / 100` 내림, 최대 체력 `(100 + 20 × 체력 단계) × (100 + 보너스) / 100` 내림.
- 복원 확인 테스트(아래 완료 조건의 마지막 Core 항목)는 `Assets/Tests/EditMode/Save/SaveDataTests.cs` 에 새 메서드로 더한다. 이름은 직접 정한다.
- `GameRoot` 가 불러온 경지·내공을 넘기는 것은 배치 모드에서 저장 파일을 읽지 않으므로(`MemorySaveStore`, 늘 `Empty`) 자동 테스트로 판정하지 않는다. 사람이 에디터에서 확인한다.

## 완료 조건
- `Assets/Tests/EditMode/Save/AutoSaveRealmTests.cs` 와 `Assets/Tests/EditMode/Save/SaveDataTests.cs` 에 아래 [컴파일]·[사람 확인] 이 아닌 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `new Wallet(1000)` 으로 만든 `AutoSave` 에서 `realm.TryBreakthrough()` 는 `true` 이고 그 뒤 금화 800, `realm.Current` `RealmLevel.SecondRate` 이다.
- 위 돌파 뒤 `IsDirty` 가 `true` 이고 쓴 횟수는 0 이다.
- 위에 이어 `Flush(100)` 은 `true`, 쓴 횟수 1 이고, 마지막 글자를 `Decode` 하면 `RealmLevel` 1, `Gold` 800, `SavedAtUtcTicks` 100 이다.
- `new Wallet(100)` 으로 만든 `AutoSave` 에서 `upgrades.TryUpgrade(StatType.InnerPower)` 는 `true` 이고 그 뒤 금화 80, 내공 단계 1 이다.
- 위 내공 강화 뒤 `Flush(100)` 은 `true`, 쓴 횟수 1 이고 마지막 글자를 `Decode` 하면 `InnerPowerLevel` 1, `Gold` 80 이다.
- `new Wallet(100)` 으로 만든 `AutoSave` 에서 `realm.TryBreakthrough()` 는 `false` (비용 200 부족)이고 `IsDirty` 는 `false` 다.
- 위 실패한 돌파 뒤 `Flush(100)` 은 `false`, 쓴 횟수 0 이다.
- `new Wallet(1234)`, `new RealmProgress(wallet, 2)`, `new StatUpgrades(wallet, 10, 100, 3, 2, 4, realm)` 을 만들면 `Balance` 1234, `realm.Current` `RealmLevel.FirstRate`, `GetLevel` 이 공격력 3·체력 2·내공 4, `Attack` 19 (`(10 + 2 × 3) × 120 / 100 = 19.2` 내림), `MaxHealth` 168 (`(100 + 20 × 2) × 120 / 100`) 이다.
- [컴파일] `GameRoot` 가 `new RealmProgress(Wallet, data != null ? data.RealmLevel : 0)` 과 내공 인자 `data != null ? data.InnerPowerLevel : 0` 으로 컴파일되고, 플레이 모드 `GameBootTests`·`GameRootSaveTests` 를 포함한 기존 테스트가 모두 통과한다.
- [사람 확인] 에디터에서 플레이해 금화를 모아 돌파 한 번과 내공 강화 한 번을 한 뒤 끄고 다시 켜면, 경지 표시가 이류이고 `INNER` 단계가 1 로 그대로다.
- [사람 확인] 2단계에서 쓴 판 1 저장(`attackSpeedLevel` 줄이 있는 `save.dat`)이 남아 있는 기기에서 처음 켜면 오류 창 없이 금화 0·1스테이지로 새로 시작한다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `Assets/Scripts/Core/Save/` 의 코드를 고치지 않는다 (`056_save_data_v2` 가 끝냈다). 고쳐야 할 것 같으면 보고서의 가정에 적는다.
- `GameRoot` 에서 `RealmProgress` 생성 줄과 `StatUpgrades` 의 내공 인자 말고는 고치지 않는다. 다른 기능의 조립 줄을 고치지 않는다.
- 다른 기능의 내부 코드(`Assets/Scripts/Core/Realm/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Stage/`, `Assets/Scripts/Core/Combat/`, `Assets/Scripts/Game/` 의 다른 기능 폴더)를 고치지 않는다.
- 판 1 저장을 판 2 로 옮기는 처리를 만들지 않는다.
- 기존 테스트 메서드의 이름을 바꾸거나 지우지 않고, 검사 수를 줄이지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 고치지 않는다. Core 테스트에서 `UnityEngine` 을 쓰지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- GameRoot가 불러온 경지와 내공 단계로 객체를 생성하도록 연결했다.
- 돌파 성공·실패와 내공 강화의 자동 저장, 저장 값으로 복원한 능력치를 검증하는 에딧 모드 테스트를 추가했다.
- 저장 사양의 조립 순서 설명을 현재 구현에 맞췄다.

### 바뀐 파일
- Assets/Scripts/Game/GameRoot.cs
- Assets/Tests/EditMode/Save/AutoSaveRealmTests.cs 및 .meta
- Assets/Tests/EditMode/Save/SaveDataTests.cs
- docs/spec/save.md
- tasks/doing/057_save_realm_restore.md
- .loop/result.json

### 가정
없음.

### 사람이 확인할 것
- 에디터에서 금화를 모아 돌파와 내공 강화를 한 번씩 하고 다시 켜면 경지가 이류, INNER 단계가 1로 유지되는지 확인한다.
- 판 1 저장이 남은 기기에서 처음 켰을 때 오류 창 없이 금화 0·1스테이지로 시작하는지 확인한다.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 컴파일과 전체 에딧·플레이 모드 테스트는 루프 판정을 기다린다.
- git diff --check 통과.


## 루프 기록 (2026-11-19 19:15)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/057_save_realm_restore
- 루프 브랜치: loop/20261119-190813
