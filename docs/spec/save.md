# 저장과 불러오기 사양

- 기능 이름: save (네임스페이스 `PlumSword.Core.Save`)
- 작성일: 2026-11-17
- 고친 날: 2026-11-19

## 목표

게임의 진행(보유 금화, 공격력·체력·내공 강화 단계, 경지, 스테이지 전체 진행 번호와 본 소개 번호)과 마지막 저장 시각을 기기의 파일 하나에 저장하고, 다시 켰을 때 그 값으로 각 기능을 되살린다. 각 기능의 내부는 바꾸지 않는다. 저장 기능이 각 기능의 연결 지점(속성, 알림, 생성자 인자)으로 값을 모으고 되살린다. 저장 값을 글자로 바꾸고 되돌리는 일, 언제 쓸지와 실패 뒤 다시 시도하는 판단은 Core, 실제 파일 읽기·쓰기와 현재 시각은 Game 이 맡는다. 손상된 저장을 알리는 간단한 오류 창과 저장 실패 글자도 이 기능이 만든다.

3단계 규칙(판 2)이다. 판 1(2단계)의 공격 속도 칸은 내공 칸으로 바뀌고 경지 칸이 더해진다. 2단계에서 쓴 판 1 저장은 옮기지 않고 처음부터 새로 시작한다.

출처: `docs/design/milestone_2.md` "추가하거나 변경할 기능 → 저장과 불러오기", "마일스톤 달성 기준"(저장·손상 처리), "미결 사항과 결정 기록"(저장 재시도의 세부 방식). `docs/design/milestone_3.md` "목표와 현재 방향"(2단계 저장을 지우고 새로 시작), "자동 발동 스킬"(스킬 재사용 대기는 복원하지 않음), "저장과 반복 진행". 저장할 값과 되살리는 법은 `docs/spec/currency.md`, `docs/spec/stat_upgrade.md`, `docs/spec/realm.md`, `docs/spec/stage.md` 의 "저장"·"연결 지점" 절.

## 동작

- **저장 값** (판 2): 보유 금화(`Wallet.Balance`), 공격력·체력·내공 강화 단계(`StatUpgrades.GetLevel(StatType.Attack / Health / InnerPower)`), 현재 경지(`(int)RealmProgress.Current`, 0~4), 그 경지에서의 처치 수(`RealmProgress.Kills`), 전체 진행 번호(`StageProgress.StageNumber`), 본 소개 번호(`StageProgress.SeenIntroStage`), 마지막 저장 시각(`savedAtUtcTicks`, 저장할 때 Game 이 넘긴 현재 UTC 시각의 `DateTime.Ticks`). 작은 스테이지 번호, 스테이지별 처치 수, 남은 체력, 보스, 단계(`Phase`), 무리 구성, 강화 잠금, 기본 공격 대기, 이십사수매화검법의 재사용 대기는 저장하지 않는다. 불러오면 저장된 큰 스테이지의 1구간부터 다시 시작하고, 스킬은 사용할 수 있는 상태로 시작한다 (전투 기능의 규칙). 천마 뒤 1스테이지 테마로 돌아가도 금화·강화·경지는 그대로이므로 저장 값도 그대로 이어진다.
- **모으기** `SaveData.Capture(wallet, upgrades, realm, stage, savedAtUtcTicks)`: 각 기능의 지금 값을 그대로 읽어 `SaveData` 를 만든다. 아무것도 바꾸지 않는다.
- **글자로 바꾸기** `SaveCodec.Encode(data)`: 아래 "수치"의 파일 형식대로 열 줄을 만든다.
- **되돌리기** `SaveCodec.Decode(text)`: 아래 순서로 판정해 `SaveLoadResult` 를 돌려준다. 예외를 던지지 않는다.
  1. `text` 가 `null` (파일 없음): `Empty`.
  2. 줄로 나눈다 (`\n` 기준, 줄 끝의 `\r` 하나는 지운다). 빈 줄은 건너뛴다. 남은 줄 중 `=` 가 없는 줄이 있거나, 같은 이름이 두 번 나오면 `Corrupt`.
  3. `version` 줄이 없거나 값이 정수가 아니면 `Corrupt`. 값이 `2` 가 아니면 (2단계의 판 1, 0, 음수, 3 이상) 나머지 줄을 보지 않고 `UnsupportedVersion`.
  4. 여덟 필수 이름(`gold`, `attackLevel`, `healthLevel`, `innerPowerLevel`, `realmLevel`, `stageNumber`, `seenIntroStage`, `savedAtUtcTicks`) 중 하나라도 없거나, 값이 그 타입의 정수로 읽히지 않으면 (글자 섞임, 앞뒤 공백, 범위 넘침) `Corrupt`. `realmKills`는 기존 파일에서 생략 가능하며 없으면 0, 있으면 정수여야 한다.
  5. 아니면 `Loaded` 와 그 값의 `SaveData`. 모르는 이름의 줄은 무시한다 (판 2 파일에 `attackSpeedLevel` 줄이 있어도 무시). 값이 기능의 범위를 벗어나도(금화 음수, 내공 음수, 경지 7, 번호 0 등) 그대로 담는다. 잘라내기는 각 기능의 생성자가 한다.
- **불러오기** (`GameRoot.Awake` 맨 앞): 저장소를 고르고(아래 "저장소"), 저장을 읽어 `LoadResult` 로 들고 있는다. 상태에 따라 각 기능을 만든다.
  - `Loaded`: `new Wallet(Gold)`, `new RealmProgress(Wallet, RealmLevel, RealmKills)`, `new StatUpgrades(Wallet, 시작 공격력, 시작 최대 체력, AttackLevel, HealthLevel, InnerPowerLevel, Realm)`, `new StageProgress(Wallet, StatUpgrades, Battle, random, StageNumber, SeenIntroStage)`. 범위 밖 값은 각 생성자가 잘라낸다 (금화 음수 → 0, 단계 음수 → 0, 경지 0 미만 → 0·4 초과 → 4, 번호 1 미만 → 1, 본 소개 번호 0~5 밖 → 자름).
  - `Empty`, `UnsupportedVersion`, `Corrupt`: 처음 값으로 만든다. 금화 0, 세 단계 0, 경지 0 (삼류), `startStage: 1`, `seenIntroStage: 0`.
  - 불러온 뒤의 시작은 스테이지 기능의 `Begin()` 이 한다: 저장된 스테이지의 처음부터 체력을 전부 채우고 새 무리 구성으로 시작한다. 이미 본 소개는 다시 나오지 않는다.
- **자동 저장** `AutoSave`: 만들 때 `wallet.BalanceChanged`, `upgrades.Upgraded`, `stage.StageStarted` 를 구독한다. `realm.ProgressChanged`도 구독해 처치 수와 돌파를 저장한다. 반드시 `stage.Begin()` 보다 먼저 만든다 (새 게임의 첫 소개 기록 `SeenIntroStage` 0 → 1 을 놓치지 않기 위해).
  - 네 알림 중 하나가 오면 켜져 있을 때(`Enabled`) `IsDirty = true` 만 한다. 바로 쓰지 않는다. 꺼져 있으면 무시한다.
  - **프레임 끝** `Flush(nowUtcTicks)`: 켜져 있고, `IsDirty` 이고, 재시도 대기 중이 아니면 쓴다 ("쓰기"). 아니면 아무것도 하지 않고 `false`. 그래서 같은 프레임의 여러 변화(보스 처치의 금화 등)는 한 번에 저장된다.
  - **즉시** `SaveNow(nowUtcTicks)`: 켜져 있으면 `IsDirty` 와 재시도 대기에 상관없이 바로 쓴다. 꺼져 있으면 `false`. 게임을 끌 때와 앱이 백그라운드로 갈 때 쓴다.
  - **쓰기**: `SaveCodec.Encode(SaveData.Capture(wallet, upgrades, realm, stage, nowUtcTicks))` 를 `store.TryWrite` 로 넘긴다. `TryWrite` 가 예외를 던지면 `false` 로 본다 (예외를 밖으로 내보내지 않는다).
    - 성공: `IsDirty = false`, 재시도 대기 0, `HasFailed = false`. 돌려주는 값 `true`.
    - 실패: `IsDirty = true` (그대로 남김), 재시도 대기 3.0초, `HasFailed = true`. 돌려주는 값 `false`. 기존 파일은 저장소가 그대로 둔다.
    - `HasFailed` 가 실제로 바뀐 때에만 `FailedChanged(새 값)` 를 한 번 보낸다. 계속 실패하면 처음 한 번만 간다.
  - **시간** `Tick(deltaSeconds)`: 재시도 대기에서 `deltaSeconds` 를 뺀다. `1e-9` 이하가 되면 0 이다. 대기가 0 이면 아무 일도 없다. 실패 뒤 그사이에 새 변화가 와도 대기가 끝날 때까지 쓰지 않는다 (`SaveNow` 만 예외).
  - **켜기** `Enable()`: 꺼져 있으면 켠다. 이미 켜져 있으면 아무 일도 없다. 끄는 메서드는 없다.
- **저장하는 때** (이것이 전부다): 금화가 바뀐 프레임 끝(처치 보상, 강화 구매, 오프라인 보상 지급 등 `BalanceChanged` 가 가는 모든 때), 경지 처치 수가 바뀐 프레임 끝, 강화한 프레임 끝(내공 포함), 스테이지가 배치된 프레임 끝(`Begin`, 가려진 순간의 교체. 보스 처치 뒤 다음 스테이지로 넘어갈 때와 실패 뒤 재도전이 여기 든다), 실패 뒤 재시도 대기가 끝난 프레임 끝, 게임을 끌 때, 앱이 백그라운드로 갈 때.
- **페이드 중에 끄면**: 보스 금화는 처치한 프레임 끝에 끝난 스테이지의 번호로 이미 저장되어 있다. 다음 번호는 가려진 순간의 교체(`StageStarted`)에서 저장된다. 그래서 그사이(페이드아웃 0.5초)에 끄고 다시 켜면 끝난 스테이지부터 다시 하고, 보스 금화는 이미 받은 채다. 이 동작을 허용한다.
- **화면 코드의 갱신** (`SaveView`): 매 프레임 `LateUpdate` 에서 `AutoSave.Tick(Time.unscaledDeltaTime)` 다음 `AutoSave.Flush(DateTime.UtcNow.Ticks)`. `OnApplicationPause(true)` 와 `OnApplicationQuit` 에서 `AutoSave.SaveNow(DateTime.UtcNow.Ticks)`. 끌 때 실패하면 더 할 것이 없다 (기존 파일은 남는다).
- **손상** (`LoadResult.Status == Corrupt`): `AutoSave` 를 꺼진 상태로 만들고, `Battle.SetPaused(true)` 를 하고, `Begin()` 을 부르지 않고, 손상 오류 창을 띄운다. 창이 떠 있는 동안 파일은 읽지도 쓰지도 않는다. 그동안 게임을 꺼도 아무것도 쓰지 않아서, 다시 켜면 같은 창이 다시 뜬다.
  - `NEW GAME` 을 누르면: 창을 없앤다 → `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()`. `Begin` 의 `StageStarted` 로 그 프레임 끝에 처음 값이 저장되며 손상된 파일을 덮어쓴다.
- **판이 다름** (`UnsupportedVersion`): 오류 없이 처음 값으로 시작한다 (`Empty` 와 같다). 첫 저장에서 그 파일을 판 2 로 덮어쓴다. 2단계에서 쓴 판 1 파일도 여기 든다: 금화·강화·스테이지를 옮기지 않고 처음부터 시작한다.
- **읽기 실패** (파일은 있는데 읽다가 예외): `Corrupt` 와 같다.
- **저장 데이터가 있는지**: `GameRoot.LoadResult.HasSaveData` (`Status == Loaded` 이면 `true`). 시작 화면 기능이 `게임 시작` / `이어하기` 를 고를 때 쓴다.
- **저장소** (`GameRoot` 가 고른다):
  - `Application.isBatchMode` 가 `true` (루프와 명령줄 테스트): 파일을 읽지도 쓰지도 않는 `MemorySaveStore`. `BatchSaveStore` 가 `null` 이면 새 저장소와 `SaveCodec.Decode(null)` (`Empty`), 아니면 지정한 저장소와 `SaveCodec.Decode(BatchSaveStore.Text)` 를 쓴다.
  - 아니면 (에디터 플레이, 빌드): `FileSaveStore(Application.persistentDataPath)`.
- **파일 저장소** `FileSaveStore(directory)`: 파일 `save.dat` 하나 (UTF-8, BOM 없음).
  - `Load()`: `save.dat` 가 없으면 `SaveCodec.Decode(null)`. 있으면 전체를 읽어 `SaveCodec.Decode(text)`. 읽다가 예외가 나면 `new SaveLoadResult(SaveLoadStatus.Corrupt, null)`. 예외를 밖으로 내보내지 않는다.
  - `TryWrite(text)`: 폴더가 없으면 만들고, `save.tmp` 에 전부 쓴 뒤, `save.dat` 가 있으면 `File.Replace(save.tmp, save.dat, null)`, 없으면 `File.Move(save.tmp, save.dat)`. 성공하면 `true`. 어느 단계에서든 예외가 나면 `save.tmp` 를 지워 보고(지우다 난 예외도 무시) `false`. 실패해도 기존 `save.dat` 는 그대로다. 예외를 밖으로 내보내지 않는다 (`text` 가 `null` 인 경우만 `ArgumentNullException`).

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 저장 파일 없음 (첫 실행) | `Empty`. 오류 아님. 처음 값으로 시작 | 금화 0, 단계 0·0·0, 경지 0, 번호 1, 본 소개 0 |
| 빈 파일, `=` 없는 줄, 같은 이름 두 번 | `Corrupt` | 처음 값으로 만들고 저장 꺼짐, 오류 창 |
| `version` 줄 없음 또는 정수 아님 | `Corrupt` | 위와 같다 |
| `version` 이 2 가 아닌 정수 (1, 0, -1, 3 …) | `UnsupportedVersion`. 오류 없이 처음 값으로 시작 | 첫 저장에서 판 2 로 덮어씀 |
| 2단계에서 쓴 판 1 파일 (`attackSpeedLevel` 이 있는 여덟 줄) | `UnsupportedVersion`. 오류 없이 처음 값으로 시작 | 옛 금화·강화·스테이지는 옮기지 않음. 첫 저장에서 판 2 로 덮어씀 |
| 여덟 값 중 하나가 없거나, 정수가 아니거나, 타입 범위를 넘침 | `Corrupt` | 처음 값, 저장 꺼짐, 오류 창 |
| 값이 기능의 범위 밖 (금화 -5, 내공 -3, 경지 7, 번호 0) | `Loaded`, 값은 그대로 담김 | 생성자가 잘라냄: 금화 0, 내공 0, 경지 4 (초절정), 번호 1 |
| 판 2 파일에 `attackSpeedLevel` 줄이 더 있음 | 모르는 이름이라 무시하고 정상 판정 | |
| 모르는 이름의 줄, 빈 줄, `\r\n` 줄 끝 | 무시하고 정상 판정 | |
| 지금 문화권이 독일어 등 | 영향 없음 (불변 문화권, 자릿수 구분 없음) | |
| `Encode(null)`, `Capture` 의 네 기능 중 `null` | `ArgumentNullException` | |
| `SaveLoadResult` 를 `Loaded` 인데 `data` 가 `null`, 또는 `Loaded` 가 아닌데 `data` 가 있음 | `ArgumentException` | |
| `AutoSave` 생성자 인자 중 `null` | `ArgumentNullException` | |
| 바뀌지 않은 변화 (`Add(0)`, 실패한 `TryUpgrade`, `TrySpend` 실패) | 알림이 없으므로 저장할 것 없음 | `IsDirty` 그대로 |
| 몬스터 처치·돌파 | `ProgressChanged`로 그 프레임 끝에 저장 | 경지와 해당 경지의 처치 수 |
| 저장할 것 없을 때 `Flush` | 쓰지 않음, `false` | |
| 쓰기 실패 | `false`, `HasFailed = true`, `IsDirty` 남김, 3초 뒤 다시 | 기존 파일 그대로 |
| 재시도 대기 중 `Flush` | 쓰지 않음, `false` (저장소를 부르지도 않음) | |
| 재시도 대기 중 `SaveNow` | 바로 씀 | 성공하면 대기 0 |
| 계속 실패 | 실패할 때마다 대기 3초를 다시 셈. `FailedChanged` 는 처음 한 번만 | |
| 실패 뒤 성공 | `HasFailed = false`, `FailedChanged(false)` 한 번 | |
| 저장소 `TryWrite` 가 예외를 던짐 | 실패와 같다. 예외는 밖으로 나가지 않음 | |
| 꺼진 동안의 알림, `Flush`, `SaveNow` | 무시, 쓰지 않음, `false` | `IsDirty` false 그대로 |
| `Enable()` 두 번 | 두 번째는 아무 일 없음 | |
| `Tick` 에 음수·NaN·무한대 | `ArgumentOutOfRangeException` | |
| `Tick(0)`, 대기 없을 때 `Tick` | 아무 일 없음 | |
| 손상 오류 창이 떠 있는 동안 끔 | 아무것도 쓰지 않음 | 다시 켜면 같은 창 |
| 끌 때 쓰기 실패 | 더 하지 않음 | 기존 파일 그대로 |
| 파일 쓰기 중 문제 (`save.tmp` 를 못 씀 등) | `TryWrite` 가 `false` | `save.dat` 그대로 |
| 파일 읽기 중 예외 | `Corrupt` 와 같다 | |
| 저장 시각이 음수이거나 아주 큼 | 그대로 저장·읽음. 쓰는 규칙은 오프라인 보상이 정한다 | |

끌 때의 쓰기 실패와 파일 읽기 중 예외는 자동 테스트 대상이 아니다. 판단은 23·26번(실패 처리)과 같은 코드이고, 화면 코드의 호출은 사람이 확인한다.

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 저장 판 번호 `SaveData.CurrentVersion` | 2 | `int`. 판 1 은 2단계 형식 |
| 재시도 대기 `AutoSave.RetrySeconds` | 3.0초 (임시) | `double`. `Time.unscaledDeltaTime` 으로 센다 |
| 대기 허용 오차 | 1e-9 | 남은 대기가 이 값 이하면 0 |
| 저장 시각 | `DateTime.UtcNow.Ticks` (Game 이 넘김) | `long`. Core 는 시각을 직접 읽지 않는다 |
| 파일 이름 | `save.dat`, 쓰는 중 `save.tmp` | `Application.persistentDataPath` 아래 |
| 글자 인코딩 | UTF-8, BOM 없음 | |

**파일 형식** (판 2): 한 줄에 `이름=값` 하나, 아래 순서, 줄 끝은 `\n`, 마지막 줄 뒤에도 `\n`. 숫자는 불변 문화권(`CultureInfo.InvariantCulture`)의 십진수로, 자릿수 구분 없이, 음수만 앞에 `-`. 이름은 대소문자를 구분한다. 읽을 때 숫자는 `NumberStyles.AllowLeadingSign` 과 불변 문화권으로 읽는다.

| 줄 | 이름 | 타입 | 처음 값 (저장 없음) |
| --- | --- | --- | --- |
| 1 | `version` | `int` | 1 |
| 2 | `gold` | `long` | 0 |
| 3 | `attackLevel` | `int` | 0 |
| 4 | `healthLevel` | `int` | 0 |
| 5 | `innerPowerLevel` | `int` | 0 |
| 6 | `realmLevel` | `int` | 0 (삼류. `(int)RealmLevel`, 0~4) |
| 7 | `realmKills` | `int` | 0 (기존 판 2 파일에서 생략되면 0) |
| 8 | `stageNumber` | `int` | 1 |
| 9 | `seenIntroStage` | `int` | 0 |
| 10 | `savedAtUtcTicks` | `long` | (없음. 저장할 때 넣는다) |

예: 금화 1234, 단계 3·2·4 (공격력·체력·내공), 경지 2 (일류), 번호 7, 본 소개 5, 시각 638000000000000000 →

```
version=2
gold=1234
attackLevel=3
healthLevel=2
innerPowerLevel=4
realmLevel=2
realmKills=0
stageNumber=7
seenIntroStage=5
savedAtUtcTicks=638000000000000000
```

**표시 글자** (`SaveText`, 한글 글꼴 전이라 영문):

| 상수 | 값 |
| --- | --- |
| `SaveFailed` | `SAVE FAILED` |
| `CorruptTitle` | `SAVE DATA IS CORRUPTED` |
| `CorruptMessage` | `START A NEW GAME? THE OLD SAVE WILL BE REPLACED.` |
| `NewGameButton` | `NEW GAME` |

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Save
{
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        // 값을 검사하지 않고 그대로 담는다 (예외 없음). 잘라내기는 각 기능의 생성자가 한다.
        public SaveData(
            long gold,
            int attackLevel,
            int healthLevel,
            int innerPowerLevel,
            int realmLevel,
            int stageNumber,
            int seenIntroStage,
            long savedAtUtcTicks,
            int realmKills = 0);

        public long Gold { get; }
        public int AttackLevel { get; }
        public int HealthLevel { get; }
        public int InnerPowerLevel { get; }
        public int RealmLevel { get; }      // (int)RealmLevel
        public int RealmKills { get; }
        public int StageNumber { get; }
        public int SeenIntroStage { get; }
        public long SavedAtUtcTicks { get; }

        // 각 기능의 지금 값을 읽어 만든다: wallet.Balance, upgrades.GetLevel(Attack / Health / InnerPower),
        // (int)realm.Current, stage.StageNumber, stage.SeenIntroStage. 아무것도 바꾸지 않고 알림을 보내지 않는다.
        // wallet, upgrades, realm, stage 중 null 이 있으면 ArgumentNullException.
        public static SaveData Capture(
            PlumSword.Core.Currency.Wallet wallet,
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Realm.RealmProgress realm,
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

    public static class SaveCodec
    {
        // "수치"의 파일 형식으로 열 줄. data 가 null 이면 ArgumentNullException.
        public static string Encode(SaveData data);

        // null 이면 Empty. 나머지는 "동작"의 판정 순서대로. 예외를 던지지 않는다.
        public static SaveLoadResult Decode(string text);
    }

    public interface ISaveStore
    {
        // text 를 저장한다. 다 쓰면 true. 실패하면 false 이고 그 전에 저장된 내용은 그대로 남긴다.
        // text 가 null 이면 ArgumentNullException. 그 밖에는 예외를 던지지 않는다.
        bool TryWrite(string text);
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        // 처음 값 null. 마지막으로 쓴 글자.
        public string Text { get; }

        // Text = text, 언제나 true. text 가 null 이면 ArgumentNullException.
        public bool TryWrite(string text);
    }

    public sealed class AutoSave
    {
        public const double RetrySeconds = 3.0;

        // store, wallet, upgrades, realm, stage 중 null 이 있으면 ArgumentNullException.
        // wallet.BalanceChanged, upgrades.Upgraded, stage.StageStarted 를 구독한다. realm 은 구독하지 않고 쓸 때 읽는다.
        // stage.Begin() 보다 먼저 만든다.
        // 생성자는 쓰지 않고 알림을 보내지 않는다. 시작 상태: Enabled = enabled, IsDirty false, HasFailed false, 대기 0.
        public AutoSave(
            ISaveStore store,
            PlumSword.Core.Currency.Wallet wallet,
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Realm.RealmProgress realm,
            PlumSword.Core.Stage.StageProgress stage,
            bool enabled = true);

        public bool Enabled { get; }
        public bool IsDirty { get; }      // 저장하지 않은 변화가 있으면 true
        public bool HasFailed { get; }    // 마지막 쓰기가 실패했으면 true. 성공하면 false

        // HasFailed 가 실제로 바뀐 뒤에 한 번, 새 값을 담아 보낸다.
        public event System.Action<bool> FailedChanged;

        // 꺼져 있으면 켠다. 이미 켜져 있으면 아무 일도 없다.
        public void Enable();

        // 재시도 대기를 줄인다 (1e-9 이하면 0). 0: 아무 일 없음. 음수·NaN·무한대: ArgumentOutOfRangeException.
        public void Tick(double deltaSeconds);

        // 켜져 있고 IsDirty 이고 대기가 0 이면 쓰고 그 성공 여부를 돌려준다. 아니면 쓰지 않고 false.
        public bool Flush(long nowUtcTicks);

        // 켜져 있으면 IsDirty·대기와 상관없이 바로 쓰고 그 성공 여부를 돌려준다. 꺼져 있으면 쓰지 않고 false.
        public bool SaveNow(long nowUtcTicks);
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

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 둘을 더한다
    {
        // Awake 맨 앞에서 저장을 읽은 결과. 시작 화면 기능이 HasSaveData 로 이어하기를 고른다.
        public PlumSword.Core.Save.SaveLoadResult LoadResult { get; private set; }

        // Awake 에서 StageProgress 를 만든 뒤, Begin 전에 하나 만든다.
        public PlumSword.Core.Save.AutoSave AutoSave { get; private set; }

        // 테스트 전용(internal): 배치 모드에서 사용할 저장소와 시작 방식 지정 재생성.
        internal static PlumSword.Core.Save.MemorySaveStore BatchSaveStore;
        internal void RecreateAs(PlumSword.Core.StartMenu.StartEntry entry);
    }
}

namespace PlumSword.Game.Save
{
    public sealed class FileSaveStore : PlumSword.Core.Save.ISaveStore
    {
        // directory 아래 save.dat 을 쓴다. directory 가 null 이거나 빈 글자면 ArgumentException.
        public FileSaveStore(string directory);

        // save.dat 이 없으면 Decode(null), 있으면 Decode(내용). 읽다가 예외가 나면 Corrupt. 예외를 던지지 않는다.
        public PlumSword.Core.Save.SaveLoadResult Load();

        // save.tmp 에 쓰고 save.dat 과 바꾼다. 실패하면 false, save.dat 그대로. text 가 null 이면 ArgumentNullException.
        public bool TryWrite(string text);
    }

    public sealed class SaveView : MonoBehaviour
    {
        // canvas 아래에 저장 실패 글자를 만들고 autoSave.HasFailed 로 바로 표시한다.
        // autoSave.FailedChanged 를 구독하고 OnDestroy 에서 끊는다.
        // LateUpdate: Tick(Time.unscaledDeltaTime) → Flush(DateTime.UtcNow.Ticks).
        // OnApplicationPause(true), OnApplicationQuit: SaveNow(DateTime.UtcNow.Ticks).
        public void Initialize(PlumSword.Core.Save.AutoSave autoSave, UnityEngine.Canvas canvas);
    }

    public sealed class SaveErrorView : MonoBehaviour
    {
        // canvas 아래에 손상 오류 창을 만든다. NEW GAME 을 누르면 onNewGame 을 한 번 부르고 자기 오브젝트를 없앤다.
        // onNewGame 이 null 이면 ArgumentNullException.
        public void Initialize(UnityEngine.Canvas canvas, System.Action onNewGame);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot` 가 저장소 하나(`MemorySaveStore` 또는 `FileSaveStore`), `LoadResult`, `AutoSave` 하나를 만들어 들고 있는다. 게임에 `AutoSave` 는 하나뿐이다. `Tick`·`Flush`·`SaveNow` 를 부르는 것은 `SaveView` 하나뿐이다.
- **`GameRoot.Awake` 순서** (지금의 순서에서 바뀌는 곳만):
  1. 맨 앞: 저장소를 고르고 `LoadResult` 를 읽는다 (배치 모드에서는 위 저장소 규칙, 아니면 `FileSaveStore.Load()`). `Loaded` 면 `LoadResult.Data` 의 값, 아니면 처음 값을 쓴다.
  2. `Wallet`, `Realm`, `StatUpgrades` 를 그 값으로 만든다 (`new Wallet(gold)`, `new RealmProgress(Wallet, realmLevel, realmKills)`, `StatUpgrades` 생성자의 단계 인자 셋(`attackLevel`, `healthLevel`, `innerPowerLevel`)과 `Realm`). 저장이 없으면 경지와 내공 단계는 0 이다.
  3. 화면 조립(금화, 강화, 전투)은 그대로다.
  4. 스테이지: `StageProgress` 를 `startStage`, `seenIntroStage` 에 그 값을 넣어 만들고 `[StageView]` 를 붙인다. **`Begin()` 은 아직 부르지 않는다.**
  5. `AutoSave = new AutoSave(저장소, Wallet, StatUpgrades, Realm, StageProgress, enabled: …)`. `enabled` 는 지금 코드의 규칙 그대로다 (시작 화면 사양 `docs/spec/start_menu.md` 의 `StartMenuRules.AutoSaveStartsEnabled(…)`). `[Game]` 의 자식 `[SaveView]` 를 만들어 `SaveView.Initialize(AutoSave, UiCanvas)`.
  6. `Corrupt` 가 아니면 `StageProgress.Begin()`. `Corrupt` 면 `Battle.SetPaused(true)` 하고 `[Game]` 의 자식 `[SaveErrorView]` 를 만들어 `Initialize(UiCanvas, 새 게임 시작)`. 새 게임 시작은 `AutoSave.Enable()` → `Battle.SetPaused(false)` → `StageProgress.Begin()` 이다.
- 시작 화면 기능(2단계, 아직 사양 없음)이 쓰는 방법: `GameRoot.LoadResult.HasSaveData` 가 `true` 면 `이어하기`, 아니면 `게임 시작`. 시작 화면이 생기면 `Begin()` 을 부르는 때와 손상 창을 띄우는 곳을 그 사양이 옮긴다.
- 오프라인 보상 기능(2단계, 아직 사양 없음)이 쓰는 방법: `GameRoot.LoadResult.Data.SavedAtUtcTicks` 가 마지막 저장 시각이다 (`Loaded` 일 때만). 보상을 `Wallet.Add` 로 주면 `BalanceChanged` 로 그 프레임 끝에 금화와 새 시각이 함께 저장된다. 지급 직후 끄는 경우까지 막으려면 그 기능이 `SaveNow` 를 부를지 정한다. 보상 기준 시각을 저장 시각과 따로 둘지(시작 화면에 머문 시간을 빼는 규칙 등)도 그 기능이 정하고, 따로 두면 이 사양의 판 번호를 올린다.

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `Wallet(long initialBalance)`, `Balance`, `BalanceChanged`.
- `PlumSword.Core.StatUpgrade.StatUpgrades`, `StatType` (`docs/spec/stat_upgrade.md`): 생성자의 `attackLevel`, `healthLevel`, `innerPowerLevel`, `realm`, `GetLevel(StatType)` (`StatType.InnerPower` 포함), `TryUpgrade(StatType)`, `Upgraded`. 이미 구현되어 있다.
- `PlumSword.Core.Realm.RealmProgress`, `RealmLevel` (`docs/spec/realm.md`): 생성자 `RealmProgress(Wallet wallet, int startLevel = 0, int kills = 0)`, `Current`, `Kills`, `BonusPercent`, `RecordKill()`, `ProgressChanged`, `BrokeThrough`. 이미 구현되어 있다.
- `PlumSword.Core.Stage.StageProgress` (`docs/spec/stage.md`): 생성자의 `startStage`, `seenIntroStage`, `StageNumber`, `SeenIntroStage`, `StageStarted`, `Begin()`. `seenIntroStage` 와 `SeenIntroStage` 는 스테이지 사양의 변경 기록 "스테이지 소개"가 구현된 뒤에 쓸 수 있다 (고정 integration `a1d7148` 의 `StageProgress` 생성자는 아직 `startStage` 까지만 받는다).
- `PlumSword.Core.Combat.Battle` (`docs/spec/combat.md`): `SetPaused(bool)`. 시작 능력치 `Battle.DiscipleBaseAttack`, `DiscipleBaseMaxHealth`.
- `PlumSword.Game.GameRoot.Wallet`, `Realm`, `UiCanvas`, `StatUpgrades`, `Battle`, `StageProgress`.

## 화면

- **조립**: 위 "`GameRoot.Awake` 순서"의 5·6번.
- **저장 실패 글자** (`SaveView`): 화면 위쪽 오른쪽, 진행 바 아래. 캔버스에 앵커 (1, 1), 피벗 (1, 1), 위치 (−8, −62), 크기 160×16 (임시). 글자는 `SaveText.SaveFailed`. `TextMeshProUGUI`, 글자 크기 14 (임시), 빨강 RGB (1.0, 0.35, 0.35) (임시), 오른쪽 정렬, TMP 기본 글꼴, `raycastTarget = false`. `autoSave.HasFailed` 가 `true` 일 때만 보인다 (`SetActive`). `Initialize` 에서 한 번, 그 뒤로 `FailedChanged` 가 올 때마다 맞춘다.
- **손상 오류 창** (`SaveErrorView`): 캔버스의 **마지막 자식**으로 만든다 (`SetAsLastSibling`, 모든 UI 위).
  - 가림막: 캔버스를 꽉 채우는 검은색 알파 0.8 `Image` (임시). `raycastTarget = true` 라 뒤의 강화 버튼이 눌리지 않는다.
  - 창: 가운데, 앵커·피벗 (0.5, 0.5), 위치 (0, 0), 크기 360×140 (임시), 바탕 RGB (0.12, 0.12, 0.12) (임시).
  - 제목 `SaveText.CorruptTitle`: 창 위쪽, 위치 (0, 40), 크기 340×24, 글자 크기 18 (임시), 흰색, 가운데 정렬.
  - 문구 `SaveText.CorruptMessage`: 위치 (0, 8), 크기 340×32, 글자 크기 12 (임시), 흰색, 가운데 정렬, 줄바꿈 허용.
  - 버튼: 위치 (0, −40), 크기 160×32 (임시). `Image` + `Button`, 색 RGB (0.20, 0.45, 0.25) (강화 버튼의 누를 수 있는 색과 같다), `Button.transition = None`. 글자 `SaveText.NewGameButton`, 글자 크기 16 (임시), 흰색, 가운데 정렬, `raycastTarget = false`.
  - 글자는 모두 `TextMeshProUGUI`, TMP 기본 글꼴. 위치는 창 가운데 기준이다.
  - **누르면**: `onNewGame` 을 한 번 부르고 창 오브젝트를 없앤다 (`Destroy`). 두 번 눌러도 한 번만 부른다.
  - 창이 떠 있는 동안 뒤의 전투는 멈춰 있고(`Battle.SetPaused(true)`) 몬스터는 없다. 금화·강화 표시는 처음 값으로 보인다.
- **사람이 먼저 해 둘 것**: 금화 사양과 같다. TextMeshPro 기본 리소스(TMP Essentials)가 있어야 글자가 보인다. 없어도 예외로 게임이 멈추지 않아야 한다.
- 표시 글자(`SaveText`)와 언제 보일지(`HasFailed`, `LoadResult.Status`)의 판단은 Core 에 있고 에딧 모드 테스트로 판정한다. `SaveView` 와 `SaveErrorView` 의 상태 갱신·구조·입력은 플레이 모드 테스트로 확인하고, 실제 배치와 글자는 사람이 실행해 확인한다 (손상 창은 `save.dat` 을 아무 글자로 바꿔 확인).

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| (이 기능 자신의 값 없음) | | | 저장하는 값은 각 기능의 값과 저장 시각이다 ("수치"의 파일 형식) |

- `IsDirty`, `HasFailed`, 재시도 대기, `Enabled` 는 저장하지 않는다. 켤 때마다 처음 값이다.
- 다른 기능의 값 중 강화 잠금, 기본 공격 대기, 스킬 재사용 대기도 저장하지 않는다. 켤 때 잠기지 않은 상태, 스킬은 사용할 수 있는 상태로 시작한다.
- 설정·음량은 이 파일에 넣지 않는다. 진행과 따로 저장한다 ("새로 시작"이나 손상 처리로 음량이 함께 지워지지 않게). 저장 방법은 시작 화면 기능이 정한다.
- 오프라인 보상: 꺼져 있는 동안 저장 값(경지·내공 포함)은 바뀌지 않는다. 마지막 저장 시각(`SavedAtUtcTicks`)은 꺼지기 전 마지막으로 성공한 저장의 시각이다 (보통 끌 때나 백그라운드로 갈 때의 `SaveNow`).

## 범위 밖

- 오프라인 보상 계산, 보상 창, 보상 기준 시각의 갱신 순서, 중복 지급 방지, 시작 화면에 머문 시간을 빼는 처리: 오프라인 보상 기능이 정한다. 백그라운드에서 돌아올 때 스테이지 처음으로 돌아가고 보상 창을 띄우는 것도 그 기능이 정한다.
- 시작 화면, `게임 시작` / `이어하기`, 시작 화면으로 돌아가기 전 저장과 그 실패 처리, 게임 종료 메뉴, 설정·음량과 그 저장: 시작 화면 기능이 정한다. 손상 창을 시작 화면으로 옮기는 것도 그때 한다.
- 판 1(2단계) 저장을 판 2 로 옮기기(금화·강화·스테이지 이어받기), 공격 속도 강화 환불: 하지 않는다 (3단계는 저장을 지우고 새로 시작한다는 기획). 판 1 파일은 `UnsupportedVersion` 으로 새로 시작한다.
- 스킬 재사용 대기·기본 공격 대기 저장: 하지 않는다 (켤 때 준비된 상태로 시작한다는 기획. 전투 기능의 규칙).
- 손상 파일 따로 남기기(백업), 저장 파일 검사값(체크섬), 저장 슬롯 여러 개, 계정 연동·클라우드 저장, 암호화·조작 방지: 하지 않는다.
- 손상 창의 `다시 읽기` 버튼, 저장 실패 때 사용자가 누르는 재시도 버튼: 하지 않는다 (재시도는 자동).
- 저장 중 표시(아이콘, `SAVING...`), 한글 문구와 글꼴: 하지 않는다. 글꼴이 준비되면 `SaveText` 의 글자만 바꾼다.
- 처치 수, 남은 체력, 보스 체력, 스테이지 단계와 페이드·소개 시간의 저장: 하지 않는다 (기획이 정함).
- 페이드아웃 중에 끄면 끝난 스테이지부터 다시 하는 것을 막는 처리: 하지 않는다.

## 완료 조건 초안

테스트에서 "가짜 저장소"는 `ISaveStore` 를 구현한 테스트용 클래스다: 쓴 횟수와 마지막 글자를 세고, 실패하게(`false`) 하거나 예외를 던지게 바꿀 수 있다. "스테이지 객체"는 새 `Battle(upgrades)` 와 `new System.Random(0)` 으로 만든다. 시작 능력치는 `(10, 100)` 이다. "경지 객체"는 그 지갑으로 만든 `new RealmProgress(wallet)` (따로 적지 않으면 삼류)이고, 강화 객체는 `new StatUpgrades(wallet, 10, 100, 단계 셋, realm)` 으로 그 경지 객체를 받는다. `AutoSave` 는 `new AutoSave(가짜 저장소, wallet, upgrades, realm, stage)` 로 만든다.

이미 구현되고 바뀌지 않은 완료 조건(3, 4, 8, 9, 10, 15, 16, 18~33)은 기록 파일로 옮겼다. 번호는 그대로 두고, 새 조건은 34번부터 붙인다.

1. `new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000)` 을 `SaveCodec.Encode` 하면 `"version=2\ngold=1234\nattackLevel=3\nhealthLevel=2\ninnerPowerLevel=4\nrealmLevel=2\nrealmKills=0\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n"` 이다.
2. 1번의 글자를 `Decode` 하면 `Status` 가 `Loaded`, `HasSaveData` 가 `true` 이고, `Data` 의 여덟 값(`Gold`, `AttackLevel`, `HealthLevel`, `InnerPowerLevel`, `RealmLevel`, `StageNumber`, `SeenIntroStage`, `SavedAtUtcTicks`)이 1234, 3, 2, 4, 2, 7, 5, 638000000000000000 이다.
5. 1번 글자에서 `version=2` 줄을 뺀 글자, `version=2` 를 `version=abc` 로 바꾼 글자는 둘 다 `Corrupt` 이다.
6. 1번 글자의 `version=2` 를 `version=1`, `version=3`, `version=0` 으로 바꾼 글자와 `"version=1\n"` 한 줄짜리 글자는 모두 `UnsupportedVersion`, `Data` `null`, `HasSaveData` `false` 이다.
7. 1번 글자에서 `gold`, `innerPowerLevel`, `realmLevel`, `savedAtUtcTicks` 줄을 하나씩 뺀 글자 넷과, `realmLevel=2` 를 `realmLevel=2a` 로 바꾼 글자는 모두 `Corrupt` 이다.
11. `new SaveData(-5, 0, 0, -3, 7, 0, 9, 0)` 을 `Encode` 한 뒤 `Decode` 하면 `Loaded` 이고 `Gold` -5, `InnerPowerLevel` -3, `RealmLevel` 7, `StageNumber` 0, `SeenIntroStage` 9 가 그대로다. 그 값으로 `new Wallet(-5)`, `new RealmProgress(wallet, 7)`, `new StatUpgrades(wallet, 10, 100, 0, 0, -3, realm)`, `new StageProgress(wallet, upgrades, battle, random, 0, 9)` 를 만들면 `Balance` 0, `realm.Current` `RealmLevel.Transcendent`, `GetLevel(StatType.InnerPower)` 0, `StageNumber` 1, `SeenIntroStage` 5 이다.
12. 지금 문화권을 `de-DE` 로 바꾼 채 `new SaveData(1234567, 0, 0, 0, 0, 1, 0, -1)` 을 `Encode` 하면 `gold=1234567` 줄과 `savedAtUtcTicks=-1` 줄이 들어 있고, 그 글자를 `Decode` 하면 `Loaded` 에 `Gold` 1234567, `SavedAtUtcTicks` -1 이다.
13. `SaveCodec.Encode(null)` 은 `ArgumentNullException`. `SaveData.Capture` 에 `wallet`, `upgrades`, `realm`, `stage` 를 하나씩 `null` 로 넣으면 각각 `ArgumentNullException` 이다.
14. `new Wallet(500)`, `new RealmProgress(wallet, 2)`, 단계 `(2, 1, 3)` 이고 그 경지 객체를 받은 `StatUpgrades`, `startStage: 4`, `seenIntroStage: 3` 인 스테이지 객체(`Begin` 전)를 `SaveData.Capture(wallet, upgrades, realm, stage, 100)` 하면 여덟 값이 500, 2, 1, 3, 2, 4, 3, 100 이고, 지갑·강화·경지·스테이지 값은 그대로이며 `BalanceChanged`, `Upgraded`, `BrokeThrough`, `StageStarted` 가 하나도 가지 않는다.
17. `AutoSave` 생성자에 `store`, `wallet`, `upgrades`, `realm`, `stage` 를 하나씩 `null` 로 넣으면 각각 `ArgumentNullException` 이다.
34. `SaveData.CurrentVersion` 은 2 이다.
35. 2단계 판 1 의 글자 `"version=1\ngold=1234\nattackLevel=3\nhealthLevel=2\nattackSpeedLevel=10\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n"` 를 `Decode` 하면 `UnsupportedVersion`, `Data` `null`, `HasSaveData` `false` 이다.
36. 1번 글자 끝에 `attackSpeedLevel=10\n` 을 붙인 글자는 `Loaded` 이고 값이 2번과 같다.
37. 금화 1000에서 몬스터 30마리를 처치하면 금화를 소비하지 않고 이류가 되며 `ProgressChanged`로 저장된다. 다시 불러와도 경지와 처치 수가 유지된다.
38. `new Wallet(100)` 으로 만든 `AutoSave` 에서 `TryUpgrade(StatType.InnerPower)` (금화 80, 내공 단계 1) 뒤 `Flush(100)` 을 하면 `true`, 쓴 횟수 1 이고 마지막 글자의 `InnerPowerLevel` 1, `Gold` 80 이다.
39. 판 2 파일에 `realmKills`가 없으면 0으로 읽고, 값이 정수가 아니면 `Corrupt`다.
40. 2번의 값으로 `new Wallet(1234)`, `new RealmProgress(wallet, 2)`, `new StatUpgrades(wallet, 10, 100, 3, 2, 4, realm)` 을 만들면 `Balance` 1234, `realm.Current` `RealmLevel.FirstRate`, `GetLevel` 이 공격력 3·체력 2·내공 4, `Attack` 19 (`(10 + 2 × 3) × 120 / 100 = 19.2` 내림), `MaxHealth` 168 (`(100 + 20 × 2) × 120 / 100`) 이다.

`GameRoot` 가 불러온 내공·경지를 넘기는 것은 배치 모드에서 저장 파일을 읽지 않으므로 자동 테스트 대상이 아니다. 사람이 에디터에서 돌파 한 번과 내공 강화 한 번을 한 뒤 껐다 켜서, 경지 표시와 `INNER` 단계가 그대로인지 확인한다. 2단계 저장이 남아 있는 기기에서는 처음 켤 때 금화 0·1스테이지로 새로 시작하는지 확인한다.

## 결정 기록

결정 기록은 [records/save.md](records/save.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.

## 변경 기록

### 2026-11-19 — 3단계: 판 2, 공격 속도 칸 → 내공 칸, 경지 칸 추가 (기준 커밋: c02cb2a)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표 | 금화, 강화 단계 셋, 스테이지 번호·본 소개 번호 | 금화, 공격력·체력·내공 단계, 경지, 스테이지 번호·본 소개 번호. 판 2, 판 1 은 옮기지 않고 새로 시작 |
| 동작 | 저장 값 판 1: 공격 속도 단계 포함. `Capture(wallet, upgrades, stage, ticks)`. 여덟 줄. `version` 이 1 이 아니면 `UnsupportedVersion`. 일곱 이름 | 판 2: 공격 속도 대신 내공 단계, 경지 추가. 잠금·공격 대기·스킬 대기는 저장 안 함. `Capture(wallet, upgrades, realm, stage, ticks)`. 아홉 줄. `version` 이 2 가 아니면(판 1 포함) `UnsupportedVersion`. 여덟 이름 |
| 동작 | 불러오기: `StatUpgrades(…, AttackSpeedLevel)`, 공격 속도 10 초과 → 10 | `new RealmProgress(Wallet, RealmLevel, RealmKills)`, `StatUpgrades(…, InnerPowerLevel, Realm)`. 경지 0~4 로 자름. 처음 값에 경지 0 |
| 동작 | `AutoSave` 는 세 알림을 구독. 쓰기는 `Capture(wallet, upgrades, stage, now)` | 같은 세 알림. `realm` 은 구독하지 않고 읽기만. 쓰기는 `Capture(wallet, upgrades, realm, stage, now)`. 돌파도 금화 소비로 저장됨 |
| 경계 상황 | 판 1 기준 행, 범위 밖 예 공격 속도 15 → 10, `Capture` 의 세 기능 | 판 2 기준. 판 1 파일 행, `attackSpeedLevel` 줄 무시 행, 돌파 성공 행 추가. 범위 밖 예 내공 -3 → 0, 경지 7 → 4. 네 기능. 실패한 `TryBreakthrough` |
| 수치 | `CurrentVersion` 1, 파일 5줄 `attackSpeedLevel` | `CurrentVersion` 2, 5줄 `innerPowerLevel`, 6줄 `realmLevel`, 아홉 줄 |
| 연결 지점 | `SaveData(gold, attackLevel, healthLevel, attackSpeedLevel, stageNumber, seenIntroStage, ticks)`, `AttackSpeedLevel`, `Capture(wallet, upgrades, stage, ticks)`, `AutoSave(store, wallet, upgrades, stage, enabled)` | `SaveData(gold, attackLevel, healthLevel, innerPowerLevel, realmLevel, stageNumber, seenIntroStage, ticks)`, `InnerPowerLevel`, `RealmLevel`, `Capture(wallet, upgrades, realm, stage, ticks)`, `AutoSave(store, wallet, upgrades, realm, stage, enabled)`. `GameRoot` 가 저장된 경지·내공을 넘김. 필요한 것에 `RealmProgress` 추가, `DiscipleBaseAttackInterval` 뺌 |
| 저장 | 꺼져 있는 동안 저장 값 그대로 | 잠금·공격 대기·스킬 대기는 저장하지 않음. 오프라인 동안 경지·내공도 그대로 |
| 범위 밖 | 3단계 경지·내공 저장은 나중에 | 판 1 → 판 2 옮기기·환불 안 함. 스킬 대기 저장 안 함 |
| 완료 조건 | 1~33 | 아래 |

- 완료 조건 초안: 새로 34~40 / 바뀜 1, 2, 5, 6, 7, 11, 12, 13, 14, 17 / 나머지(3, 4, 8, 9, 10, 15, 16, 18~33)는 기록 파일로 옮김 (이미 구현됨).
- 바뀌는 연결 지점: `SaveData.CurrentVersion` 1 → 2. `SaveData` 생성자 `(long gold, int attackLevel, int healthLevel, int attackSpeedLevel, int stageNumber, int seenIntroStage, long savedAtUtcTicks)` → `(long gold, int attackLevel, int healthLevel, int innerPowerLevel, int realmLevel, int stageNumber, int seenIntroStage, long savedAtUtcTicks)`. `SaveData.AttackSpeedLevel` 없어짐, `InnerPowerLevel`·`RealmLevel` 추가. `SaveData.Capture(wallet, upgrades, stage, ticks)` → `Capture(wallet, upgrades, realm, stage, ticks)`. `AutoSave(store, wallet, upgrades, stage, enabled = true)` → `AutoSave(store, wallet, upgrades, realm, stage, enabled = true)`.
- 고쳐야 할 기존 코드:
  - `Assets/Scripts/Core/Save/SaveData.cs` — 판 2, 칸 교체, `Capture` 에 `realm`.
  - `Assets/Scripts/Core/Save/SaveCodec.cs` — 아홉 줄 쓰기·읽기.
  - `Assets/Scripts/Core/Save/AutoSave.cs` — 생성자에 `realm`, 쓰기의 `Capture`.
  - `Assets/Scripts/Game/GameRoot.cs` — `new RealmProgress(Wallet, data.RealmLevel)`, `StatUpgrades` 의 내공 인자에 `data.InnerPowerLevel`, `new AutoSave(…, Realm, StageProgress, …)`.
  - 그 밖에 `new AutoSave(` 를 부르는 코드가 있으면 `realm` 을 넣는다 (고정 integration `e7b6b96` 에서는 `GameRoot` 하나뿐).
- 기존 테스트 (고정 integration `e7b6b96` 기준). 관문 때문에 이름은 모두 그대로 두고 본문만 바꾼다. 검사(Assert) 수는 줄이지 않는다 (`AttackSpeedLevel` 검사는 `InnerPowerLevel` 검사로 바꾼다).

| 테스트 | 지금 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| `SaveDataTests.CurrentVersion_IsOne` | 1 | 2 | 이름 유지, 본문 교체 (사용자 선택) |
| `SaveDataTests.Constructor_StoresAllValues` | `(1234, 3, 2, 10, 7, 5, t)`, `AttackSpeedLevel` 10 | `(1234, 3, 2, 4, 2, 7, 5, t)`, `InnerPowerLevel` 4, `RealmLevel` 2 | 이름 유지, 인자·검사 교체와 추가 |
| `SaveDataTests.Constructor_OutOfRangeValues_ArePreserved` | `(-5, 0, 0, 15, 0, 9, 0)`, `AttackSpeedLevel` 15 | `(-5, 0, 0, -3, 7, 0, 9, 0)`, `InnerPowerLevel` -3, `RealmLevel` 7 | 이름 유지, 본문 교체 |
| `SaveDataTests.Restore_OutOfRangeValues_FeatureConstructorsClamp` | 금화·번호·본 소개만 | 11번대로 경지 `Transcendent`, 내공 0 도 확인 | 이름 유지, 본문 교체와 추가 |
| `SaveDataTests.Capture_BeforeBegin_CopiesValuesWithoutChangesOrEvents` | `AttackSpeedLevel` 0, 경지 없음 | 14번대로 `InnerPowerLevel` 3, `RealmLevel` 2, `BrokeThrough` 도 0 번 | 이름 유지, 본문 교체와 추가 |
| `SaveDataTests.Capture_NullDependencies_ThrowsArgumentNull` | 세 인자 | `realm` 이 `null` 인 경우 추가 | 이름 유지, 검사 추가 |
| `SaveCodecTests` 의 `Standard` 상수 | 판 1 여덟 줄 | 1번의 판 2 아홉 줄 | 교체 |
| `SaveCodecTests.Encode_StandardData_ProducesExactText` | 일곱 인자 | 1번의 여덟 인자 | 이름 유지, 본문 교체 |
| `SaveCodecTests.Decode_StandardText_LoadsAllValues`, `Decode_Crlf_LoadsAllValues`, `Decode_BlankAndExtraLines_LoadsAllValues` | `AttackSpeedLevel` 10 | `InnerPowerLevel` 4, `RealmLevel` 2 | 이름 유지, 검사 교체와 추가 |
| `SaveCodecTests.Decode_MissingOrInvalidField_IsCorrupt` | `TestCase("version=1\n", "")`, `("version=1", "version=abc")` | `("version=2\n", "")`, `("version=2", "version=abc")`. 7번의 `innerPowerLevel`·`realmLevel` 줄 빼기, `realmLevel=2a` 경우 추가 | 이름 유지, `TestCase` 교체와 추가 |
| `SaveCodecTests.Decode_UnknownVersion_IsUnsupported` | `Replace("version=1", …)`, 경우 `2`, `0` | `Replace("version=2", …)`, 경우 `1`, `3`, `0` | 이름 유지, 본문·`TestCase` 교체와 추가 |
| `SaveCodecTests.Decode_UnknownVersionAlone_IsUnsupported` | `"version=2\n"` | `"version=1\n"` | 이름 유지, 본문 교체 |
| `SaveCodecTests.RoundTrip_OutOfRangeValues_ArePreserved` | `(-5, 0, 0, 15, 0, 9, 0)`, `AttackSpeedLevel` 15 | 11번의 `(-5, 0, 0, -3, 7, 0, 9, 0)`, `InnerPowerLevel` -3, `RealmLevel` 7 | 이름 유지, 본문 교체 |
| `SaveCodecTests.RoundTrip_GermanCulture_UsesInvariantNumbers` | `(1234567, 0, 0, 0, 1, 0, -1)` | `(1234567, 0, 0, 0, 0, 1, 0, -1)` | 이름 유지, 인자 교체 |
| `SaveCodecTests.Decode_UnknownVersionWithMalformedOrDuplicateLine_IsCorrupt` | `"version=2\noops\n"` 등이 `Corrupt` | 판 2 에서도 줄 검사가 먼저라 그대로 `Corrupt` | 고치지 않음 |
| `SaveLoadResultTests` 의 `new SaveData(0, 0, 0, 0, 1, 0, 0)` 두 곳 | 일곱 인자 | `new SaveData(0, 0, 0, 0, 0, 1, 0, 0)` | 이름 유지, 인자만 교체 |
| `AutoSaveTests.Constructor_NullDependency_ThrowsArgumentNull` | 네 인자 | `realm` 이 `null` 인 경우 추가 | 이름 유지, 검사 추가 |
| `AutoSaveTests`, `AutoSaveRetryTests`, `LeaveGameTests`, `OfflineRewardClaimTests`, `SaveViewTests`(플레이 모드)의 `new AutoSave(…)` | 경지 없음 | 경지 객체 `new RealmProgress(wallet)` 를 만들어 `new AutoSave(store, wallet, upgrades, realm, stage, …)` 로 (강화 객체는 그대로 둬도 된다) | 이름 유지, 준비 코드만 교체 |
| `FileSaveStoreTests`(플레이 모드)의 `new SaveData(1234, 3, 2, 10, 7, 5, t)` 와 `AttackSpeedLevel` 10 | 판 1 | 1번 인자, `InnerPowerLevel` 4, `RealmLevel` 2 | 이름 유지, 본문 교체 |
| `GameRootSaveTests`(플레이 모드) | 배치 모드 `Empty` | 그대로 통과 | 고치지 않음 |
