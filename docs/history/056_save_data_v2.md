# 056_save_data_v2 — 저장 판 2: 내공·경지 칸과 경지 인자

## 목표
`PlumSword.Core.Save` 의 저장 값을 판 2 로 바꾼다. `SaveData` 의 공격 속도 칸을 내공 칸으로 바꾸고 경지 칸을 더하며, `SaveCodec` 은 아홉 줄을 쓰고 읽고, `SaveData.Capture` 와 `AutoSave` 생성자는 경지(`RealmProgress`)를 받는다. 바뀐 연결 지점을 쓰는 기존 코드(`GameRoot` 의 `new AutoSave(…)` 한 줄)와 기존 테스트를 함께 고쳐 컴파일이 깨지지 않게 한다. 대부분 Core 의 일이다.

## 묶음
save_m3

## 선행 작업
- 046_realm_progress
- 049_stat_upgrade_inner_power

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작", "경계 상황", "수치", "연결 지점", "완료 조건 초안", "변경 기록" 절)
- docs/spec/realm.md ("연결 지점" 절)
- docs/spec/stat_upgrade.md ("연결 지점" 절)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-19 — 3단계: 판 2, 공격 속도 칸 → 내공 칸, 경지 칸 추가` 항목만 구현한다. 그 밖의 규칙(빈 줄·`\r\n`·중복 줄 처리, 재시도, `Enable`, `Tick`, 파일 저장소 등)은 이미 구현되어 있다. 이 작업은 그 항목 중 저장 형식과 경지 인자까지 하고, 저장 값으로 경지·내공을 되살리는 `GameRoot` 의 복원과 돌파·내공 강화의 자동 저장 확인은 `057_save_realm_restore` 가 한다.
- `Assets/Scripts/Core/Save/SaveData.cs` (네임스페이스 `PlumSword.Core.Save`):
  - `public const int CurrentVersion = 2;` (1 에서 바꿈)
  - 생성자를 `public SaveData(long gold, int attackLevel, int healthLevel, int innerPowerLevel, int realmLevel, int stageNumber, int seenIntroStage, long savedAtUtcTicks)` 로 바꾼다. 값을 검사하지 않고 그대로 담는다 (예외 없음).
  - `AttackSpeedLevel` 속성을 없애고 `public int InnerPowerLevel { get; }`, `public int RealmLevel { get; }` 를 더한다. 나머지 속성(`Gold`, `AttackLevel`, `HealthLevel`, `StageNumber`, `SeenIntroStage`, `SavedAtUtcTicks`)은 그대로다.
  - `Capture` 를 `public static SaveData Capture(PlumSword.Core.Currency.Wallet wallet, PlumSword.Core.StatUpgrade.StatUpgrades upgrades, PlumSword.Core.Realm.RealmProgress realm, PlumSword.Core.Stage.StageProgress stage, long savedAtUtcTicks)` 로 바꾼다. 값: `wallet.Balance`, `upgrades.GetLevel(StatType.Attack)`, `upgrades.GetLevel(StatType.Health)`, `upgrades.GetLevel(StatType.InnerPower)`, `(int)realm.Current`, `stage.StageNumber`, `stage.SeenIntroStage`, `savedAtUtcTicks`. 아무것도 바꾸지 않고 알림을 보내지 않는다. `wallet`, `upgrades`, `realm`, `stage` 중 `null` 이 있으면 `ArgumentNullException`. 지금 코드의 "공격 속도 칸: 0" 주석과 `0` 인자는 없앤다.
- `Assets/Scripts/Core/Save/SaveCodec.cs`:
  - `Encode` 는 아홉 줄을 이 순서로 쓴다: `version`, `gold`, `attackLevel`, `healthLevel`, `innerPowerLevel`, `realmLevel`, `stageNumber`, `seenIntroStage`, `savedAtUtcTicks`. 줄 끝 `\n`, 마지막 줄 뒤에도 `\n`, 숫자는 `CultureInfo.InvariantCulture`. `attackSpeedLevel` 줄은 쓰지 않는다.
  - `Decode` 의 판정 순서는 그대로 두고, 4단계의 이름을 여덟 개(`gold`(long), `attackLevel`, `healthLevel`, `innerPowerLevel`, `realmLevel`, `stageNumber`, `seenIntroStage`(int), `savedAtUtcTicks`(long))로 바꾼다. `attackSpeedLevel` 은 이제 모르는 이름이라 있어도 무시한다. `version` 이 `SaveData.CurrentVersion`(2) 이 아니면 나머지 줄을 보지 않고 `UnsupportedVersion` 이다 (2단계의 판 1 파일도 여기 든다. 판 1 을 판 2 로 옮기는 처리는 만들지 않는다).
- `Assets/Scripts/Core/Save/AutoSave.cs`:
  - 생성자를 `public AutoSave(ISaveStore store, PlumSword.Core.Currency.Wallet wallet, PlumSword.Core.StatUpgrade.StatUpgrades upgrades, PlumSword.Core.Realm.RealmProgress realm, PlumSword.Core.Stage.StageProgress stage, bool enabled = true)` 로 바꾼다. `realm` 이 `null` 이면 `ArgumentNullException` (다른 인자와 같다).
  - `realm` 은 구독하지 않는다 (`BrokeThrough` 를 구독하지 않는다. 돌파는 금화 소비의 `BalanceChanged` 로 저장된다). 구독은 지금처럼 `wallet.BalanceChanged`, `upgrades.Upgraded`, `stage.StageStarted` 셋이다.
  - 쓰기는 `SaveCodec.Encode(SaveData.Capture(wallet, upgrades, realm, stage, nowUtcTicks))`. 그 밖의 동작은 바꾸지 않는다.
- `Assets/Scripts/Game/GameRoot.cs`: `AutoSave = new AutoSave(saveStore, Wallet, StatUpgrades, StageProgress, enabled: …)` 를 `new AutoSave(saveStore, Wallet, StatUpgrades, Realm, StageProgress, enabled: …)` 로 바꾼다. `enabled` 식은 그대로다. `GameRoot` 의 다른 줄(특히 `new RealmProgress(Wallet)` 와 `StatUpgrades` 의 내공 인자 `0`)은 이 작업에서 고치지 않는다 (`057_save_realm_restore` 가 한다).
- 테스트에서 경지 객체는 그 지갑으로 만든 `new RealmProgress(wallet)` (따로 적지 않으면 삼류)이다. 스테이지 객체는 `new Battle(upgrades)` 와 `new System.Random(0)` 으로 만든다.
- 기존 테스트는 아래처럼 고친다. **모든 테스트 메서드의 이름은 그대로 두고, 검사(Assert) 수와 `TestCase` 수는 줄이지 않는다.** `AttackSpeedLevel` 검사는 `InnerPowerLevel` 검사로 바꾼다. 테스트를 지우지 않는다.
  - `SaveDataTests.CurrentVersion_IsOne`: 이름은 그대로 두고, 기대값을 1 에서 2 로 바꾼다.
  - `SaveDataTests.Constructor_StoresAllValues`: 인자를 `(1234, 3, 2, 10, 7, 5, t)` 에서 `(1234, 3, 2, 4, 2, 7, 5, 638000000000000000L)` 로, `AttackSpeedLevel` 10 검사를 `InnerPowerLevel` 4 로 바꾸고 `RealmLevel` 2 검사를 더한다.
  - `SaveDataTests.Constructor_OutOfRangeValues_ArePreserved`: 인자를 `(-5, 0, 0, -3, 7, 0, 9, 0)` 로, `AttackSpeedLevel` 15 검사를 `InnerPowerLevel` -3 으로 바꾸고 `RealmLevel` 7 검사를 더한다.
  - `SaveDataTests.Restore_OutOfRangeValues_FeatureConstructorsClamp`: 데이터 `(-5, 0, 0, -3, 7, 0, 9, 0)`. `new Wallet(-5)`, `new RealmProgress(wallet, 7)`, `new StatUpgrades(wallet, 10, 100, 0, 0, -3, realm)`, `new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), 0, 9)` 로 만들고, 지금의 검사(`Balance` 0, `StageNumber` 1, `SeenIntroStage` 5)에 `realm.Current` `RealmLevel.Transcendent`, `GetLevel(StatType.InnerPower)` 0 검사를 더한다.
  - `SaveDataTests.Capture_BeforeBegin_CopiesValuesWithoutChangesOrEvents`: `new Wallet(500)`, `new RealmProgress(wallet, 2)`, `new StatUpgrades(wallet, 10, 100, 2, 1, 3, realm)`, `startStage: 4`, `seenIntroStage: 3` 인 스테이지 객체. `Capture(wallet, upgrades, realm, stage, 100)`. `AttackSpeedLevel` 0 검사를 `InnerPowerLevel` 3 으로 바꾸고, `RealmLevel` 2, 캡처 뒤 `realm.Current` `RealmLevel.FirstRate`, `GetLevel(StatType.InnerPower)` 3 검사를 더한다. `realm.BrokeThrough` 도 세는 알림(`events`)에 더한다.
  - `SaveDataTests.Capture_NullDependencies_ThrowsArgumentNull`: 경지 객체를 만들어 세 호출을 `Capture(null, upgrades, realm, stage, 0)`, `Capture(wallet, null, realm, stage, 0)`, `Capture(wallet, upgrades, realm, null, 0)` 로 바꾸고 `Capture(wallet, upgrades, null, stage, 0)` 검사를 더한다.
  - `SaveCodecTests` 의 `Standard` 상수: `"version=2\ngold=1234\nattackLevel=3\nhealthLevel=2\ninnerPowerLevel=4\nrealmLevel=2\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n"`.
  - `SaveCodecTests.Encode_StandardData_ProducesExactText`: 인자를 `new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000L)` 로.
  - `SaveCodecTests.Decode_StandardText_LoadsAllValues`, `Decode_Crlf_LoadsAllValues`, `Decode_BlankAndExtraLines_LoadsAllValues`: `AttackSpeedLevel` 10 검사를 `InnerPowerLevel` 4 로 바꾸고 `RealmLevel` 2 검사를 더한다.
  - `SaveCodecTests.Decode_MissingOrInvalidField_IsCorrupt`: `TestCase("version=1\n", "")` 를 `("version=2\n", "")` 로, `("version=1", "version=abc")` 를 `("version=2", "version=abc")` 로 바꾸고, `("innerPowerLevel=4\n", "")`, `("realmLevel=2\n", "")`, `("realmLevel=2", "realmLevel=2a")` 를 더한다. 나머지 `TestCase` 는 그대로.
  - `SaveCodecTests.Decode_UnknownVersion_IsUnsupported`: `Standard.Replace("version=1", …)` 를 `Standard.Replace("version=2", …)` 로, `TestCase` `"2"`, `"0"` 을 `"1"`, `"3"`, `"0"` 으로 바꾼다.
  - `SaveCodecTests.Decode_UnknownVersionAlone_IsUnsupported`: 입력 `"version=2\n"` 을 `"version=1\n"` 로 바꾼다 (판 2 가 지금 판이라 `"version=2\n"` 은 이제 `Corrupt` 다).
  - `SaveCodecTests.RoundTrip_OutOfRangeValues_ArePreserved`: 인자를 `(-5, 0, 0, -3, 7, 0, 9, 0)` 로, `AttackSpeedLevel` 15 검사를 `InnerPowerLevel` -3 으로 바꾸고 `RealmLevel` 7 검사를 더한다.
  - `SaveCodecTests.RoundTrip_GermanCulture_UsesInvariantNumbers`: 인자를 `new SaveData(1234567, 0, 0, 0, 0, 1, 0, -1)` 로.
  - `SaveCodecTests.Decode_UnknownVersionWithMalformedOrDuplicateLine_IsCorrupt`: 고치지 않는다 (줄 검사가 먼저라 그대로 `Corrupt`).
  - `SaveLoadResultTests` 의 `new SaveData(0, 0, 0, 0, 1, 0, 0)` 두 곳: `new SaveData(0, 0, 0, 0, 0, 1, 0, 0)` 로 인자만 바꾼다.
  - `AutoSaveTests.Constructor_NullDependency_ThrowsArgumentNull`: 네 호출에 `realm` 인자를 넣고 `new AutoSave(store, wallet, upgrades, null, stage)` 검사를 더한다. `AutoSaveTests.Create` 는 경지 객체를 만들어 필드로 들고 `new AutoSave(store, wallet, upgrades, realm, stage, enabled)` 로 만든다.
  - 아래 파일의 `new AutoSave(…)` 준비 코드는 경지 객체 `new RealmProgress(wallet)` 를 만들어 `realm` 자리(`upgrades` 와 `stage` 사이)에 넣는다. 강화 객체는 그대로 둔다. 검사는 바꾸지 않는다: `Assets/Tests/EditMode/Save/AutoSaveRetryTests.cs`, `Assets/Tests/EditMode/StartMenu/LeaveGameTests.cs`, `Assets/Tests/EditMode/OfflineReward/OfflineRewardClaimTests.cs`, `Assets/Tests/PlayMode/Save/SaveViewTests.cs`, `Assets/Tests/PlayMode/StartMenu/SettingsViewTests.cs`.
  - `Assets/Tests/PlayMode/Save/FileSaveStoreTests.cs` 의 `Load_InvalidThenReplacedSave_ReturnsCorruptThenLoaded`: `new SaveData(1234, 3, 2, 10, 7, 5, t)` 를 `new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000L)` 로, `AttackSpeedLevel` 10 검사를 `InnerPowerLevel` 4 로 바꾸고 `RealmLevel` 2 검사를 더한다.
  - `GameRootSaveTests`(플레이 모드): 고치지 않는다.
  - 위에 없는 곳에서 `new SaveData(`, `SaveData.Capture(`, `new AutoSave(`, `AttackSpeedLevel` 이 컴파일 오류를 내면 같은 규칙(이름 유지, 검사 수 유지, 인자만 판 2 로)으로 고치고 보고서의 가정에 적는다.
- 새 테스트(아래 완료 조건 35, 36)는 `Assets/Tests/EditMode/Save/SaveCodecTests.cs` 에 새 메서드로 더한다. 이름은 직접 정한다.
- 구현하다 연결 지점의 모양이 사양과 달라지면 `docs/spec/save.md` 의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.

## 완료 조건
- `Assets/Tests/EditMode/Save/SaveDataTests.cs`, `SaveCodecTests.cs`, `SaveLoadResultTests.cs`, `AutoSaveTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `SaveData.CurrentVersion` 은 2 이다.
- `new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000)` 을 `SaveCodec.Encode` 하면 `"version=2\ngold=1234\nattackLevel=3\nhealthLevel=2\ninnerPowerLevel=4\nrealmLevel=2\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n"` 이다.
- 위 글자를 `Decode` 하면 `Status` 가 `Loaded`, `HasSaveData` 가 `true` 이고, `Data` 의 여덟 값(`Gold`, `AttackLevel`, `HealthLevel`, `InnerPowerLevel`, `RealmLevel`, `StageNumber`, `SeenIntroStage`, `SavedAtUtcTicks`)이 1234, 3, 2, 4, 2, 7, 5, 638000000000000000 이다.
- 위 글자의 `\n` 을 모두 `\r\n` 으로 바꾼 글자를 `Decode` 하면 `Loaded` 이고 `InnerPowerLevel` 4, `RealmLevel` 2 이다.
- 위 글자에서 `version=2` 줄을 뺀 글자는 `Corrupt` 이다.
- 위 글자의 `version=2` 를 `version=abc` 로 바꾼 글자는 `Corrupt` 이다.
- 위 글자의 `version=2` 를 `version=1`, `version=3`, `version=0` 으로 바꾼 글자는 모두 `UnsupportedVersion`, `Data` `null`, `HasSaveData` `false` 이다.
- `"version=1\n"` 한 줄짜리 글자는 `UnsupportedVersion`, `Data` `null`, `HasSaveData` `false` 이다.
- 위 글자에서 `gold`, `innerPowerLevel`, `realmLevel`, `savedAtUtcTicks` 줄을 하나씩 뺀 글자 넷은 모두 `Corrupt` 이다.
- 위 글자의 `realmLevel=2` 를 `realmLevel=2a` 로 바꾼 글자는 `Corrupt` 이다.
- 2단계 판 1 의 글자 `"version=1\ngold=1234\nattackLevel=3\nhealthLevel=2\nattackSpeedLevel=10\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n"` 를 `Decode` 하면 `UnsupportedVersion`, `Data` `null`, `HasSaveData` `false` 이다.
- 판 2 표준 글자 끝에 `attackSpeedLevel=10\n` 을 붙인 글자를 `Decode` 하면 `Loaded` 이고 여덟 값이 1234, 3, 2, 4, 2, 7, 5, 638000000000000000 이다.
- `new SaveData(-5, 0, 0, -3, 7, 0, 9, 0)` 을 `Encode` 한 뒤 `Decode` 하면 `Loaded` 이고 `Gold` -5, `InnerPowerLevel` -3, `RealmLevel` 7, `StageNumber` 0, `SeenIntroStage` 9 가 그대로다.
- 그 값으로 `new Wallet(-5)`, `new RealmProgress(wallet, 7)`, `new StatUpgrades(wallet, 10, 100, 0, 0, -3, realm)`, `new StageProgress(wallet, upgrades, battle, random, 0, 9)` 를 만들면 `Balance` 0, `realm.Current` `RealmLevel.Transcendent`, `GetLevel(StatType.InnerPower)` 0, `StageNumber` 1, `SeenIntroStage` 5 이다.
- 지금 문화권을 `de-DE` 로 바꾼 채 `new SaveData(1234567, 0, 0, 0, 0, 1, 0, -1)` 을 `Encode` 하면 `gold=1234567` 줄과 `savedAtUtcTicks=-1` 줄이 들어 있고, 그 글자를 `Decode` 하면 `Loaded` 에 `Gold` 1234567, `SavedAtUtcTicks` -1 이다.
- `SaveCodec.Encode(null)` 은 `ArgumentNullException` 이다.
- `SaveData.Capture` 에 `wallet`, `upgrades`, `realm`, `stage` 를 하나씩 `null` 로 넣으면 각각 `ArgumentNullException` 이다.
- `new Wallet(500)`, `new RealmProgress(wallet, 2)`, 단계 `(2, 1, 3)` 이고 그 경지 객체를 받은 `StatUpgrades`, `startStage: 4`, `seenIntroStage: 3` 인 스테이지 객체(`Begin` 전)를 `SaveData.Capture(wallet, upgrades, realm, stage, 100)` 하면 여덟 값이 500, 2, 1, 3, 2, 4, 3, 100 이다.
- 위 `Capture` 뒤 지갑 500, 단계 2·1·3, 경지 `RealmLevel.FirstRate`, 스테이지 번호 4·본 소개 3, `HasBegun` `false` 가 그대로이고 `BalanceChanged`, `Upgraded`, `BrokeThrough`, `StageStarted` 가 하나도 가지 않는다.
- `AutoSave` 생성자에 `store`, `wallet`, `upgrades`, `realm`, `stage` 를 하나씩 `null` 로 넣으면 각각 `ArgumentNullException` 이다.
- `new SaveLoadResult(SaveLoadStatus.Empty, new SaveData(0, 0, 0, 0, 0, 1, 0, 0))` 은 `ArgumentException` 이다.
- [컴파일] `GameRoot` 와 플레이 모드 테스트(`SaveViewTests`, `SettingsViewTests`, `FileSaveStoreTests`)가 바뀐 생성자로 컴파일된다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 판 1 저장을 판 2 로 옮기는 처리(금화·강화·스테이지 이어받기), 공격 속도 강화 환불을 만들지 않는다.
- `AutoSave` 가 `realm.BrokeThrough` 를 구독하게 하지 않는다.
- `GameRoot` 에서 `new AutoSave(…)` 한 줄 말고는 고치지 않는다. 경지·내공 복원은 `057_save_realm_restore` 가 한다.
- 다른 기능의 내부 코드(`Assets/Scripts/Core/Realm/`, `Assets/Scripts/Core/StatUpgrade/`, `Assets/Scripts/Core/Stage/`, `Assets/Scripts/Core/Combat/`, `Assets/Scripts/Core/Currency/`, `Assets/Scripts/Core/OfflineReward/`, `Assets/Scripts/Core/StartMenu/`)를 고치지 않는다. 그 기능들의 테스트 파일은 `new AutoSave(…)` 준비 코드만 고친다.
- `FileSaveStore`, `MemorySaveStore`, `SaveView`, `SaveErrorView`, `SaveText`, `SaveLoadResult`, `SaveLoadStatus` 의 동작을 바꾸지 않는다.
- 기존 테스트 메서드의 이름을 바꾸거나 지우지 않고, 검사 수를 줄이지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 고치지 않는다. Core 에서 `UnityEngine` 을 쓰지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 저장 데이터와 코덱을 판 2의 내공·경지 칸으로 변경하고 캡처와 자동 저장에 경지 객체를 전달했다.
- 기존 호출과 테스트를 새 생성자에 맞추고 판 1 거부 및 판 2의 옛 칸 무시 테스트를 추가했다.
- 저장 사양의 연결 지점은 이미 구현과 같아 변경하지 않았다.

### 바뀐 파일
- `Assets/Scripts/Core/Save/SaveData.cs`, `SaveCodec.cs`, `AutoSave.cs`, `Assets/Scripts/Game/GameRoot.cs`
- `Assets/Tests/EditMode/Save/SaveDataTests.cs`, `SaveCodecTests.cs`, `SaveLoadResultTests.cs`, `AutoSaveTests.cs`, `AutoSaveRetryTests.cs`
- `Assets/Tests/EditMode/StartMenu/LeaveGameTests.cs`, `Assets/Tests/EditMode/OfflineReward/OfflineRewardClaimTests.cs`
- `Assets/Tests/PlayMode/Save/FileSaveStoreTests.cs`, `SaveViewTests.cs`, `Assets/Tests/PlayMode/StartMenu/SettingsViewTests.cs`
- 이 작업 보고서와 `.loop/result.json`

### 가정
- 없음.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- `git diff --check` 통과. 호출부와 경지 이벤트 서명을 정적으로 확인했다.
- Unity 테스트와 컴파일은 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-19 19:13)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/056_save_data_v2
- 루프 브랜치: loop/20261119-190813
