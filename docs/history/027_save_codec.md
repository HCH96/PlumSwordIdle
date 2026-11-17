# 027_save_codec — 저장 값을 글자로 바꾸고 되돌리기

## 목표
`PlumSword.Core.Save.SaveCodec` 을 만든다. `Encode` 는 `SaveData` 를 판 1 파일 형식의 여덟 줄로 바꾸고, `Decode` 는 글자를 판정해 `SaveLoadResult`(`Empty`·`Loaded`·`Corrupt`·`UnsupportedVersion`)를 돌려준다. 에딧 모드 테스트로 검사한다.

## 묶음
저장

## 선행 작업
- 026_save_data

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작" 절의 "글자로 바꾸기"·"되돌리기", "경계 상황" 표, "수치" 절의 "파일 형식"과 줄 표, "연결 지점" 절의 `SaveCodec`)

## 요구사항
- 새 파일 `Assets/Scripts/Core/Save/SaveCodec.cs` (네임스페이스 `PlumSword.Core.Save`, `public static class SaveCodec`).
- 새 테스트 파일 `Assets/Tests/EditMode/Save/SaveCodecTests.cs` (네임스페이스 `PlumSword.Core.Tests.Save`, 클래스 `SaveCodecTests`). 테스트 이름은 `<상황>_<기대 결과>` 모양.
- 선행 작업 026_save_data 가 만든 `SaveData`(생성자, 일곱 속성, `CurrentVersion = 1`), `SaveLoadStatus`, `SaveLoadResult(status, data)` 를 쓴다.
- **공개 모양** (사양 "연결 지점" 그대로):

  ```csharp
  public static class SaveCodec
  {
      // 파일 형식으로 여덟 줄. data 가 null 이면 ArgumentNullException.
      public static string Encode(SaveData data);

      // null 이면 Empty. 나머지는 아래 판정 순서대로. 예외를 던지지 않는다.
      public static SaveLoadResult Decode(string text);
  }
  ```

  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- **파일 형식** (판 1): 한 줄에 `이름=값` 하나, 아래 순서, 줄 끝은 `\n`, 마지막 줄 뒤에도 `\n`. 숫자는 `CultureInfo.InvariantCulture` 의 십진수, 자릿수 구분 없음, 음수만 앞에 `-`. 이름은 대소문자를 구분한다.

  | 줄 | 이름 | 타입 | 넣는 값 |
  | --- | --- | --- | --- |
  | 1 | `version` | `int` | `SaveData.CurrentVersion` (1) |
  | 2 | `gold` | `long` | `Gold` |
  | 3 | `attackLevel` | `int` | `AttackLevel` |
  | 4 | `healthLevel` | `int` | `HealthLevel` |
  | 5 | `attackSpeedLevel` | `int` | `AttackSpeedLevel` |
  | 6 | `stageNumber` | `int` | `StageNumber` |
  | 7 | `seenIntroStage` | `int` | `SeenIntroStage` |
  | 8 | `savedAtUtcTicks` | `long` | `SavedAtUtcTicks` |

- **`Decode(text)` 판정 순서** (예외를 던지지 않는다):
  1. `text` 가 `null`: `new SaveLoadResult(Empty, null)`.
  2. `\n` 으로 나누고 각 줄 끝의 `\r` 하나를 지운다. 그 뒤 빈 줄(길이 0)은 건너뛴다. 남은 줄은 **첫 번째** `=` 앞을 이름, 뒤를 값으로 본다 (이름·값의 앞뒤 공백을 지우지 않는다). `=` 가 없는 줄이 있거나 같은 이름이 두 번 나오면 `Corrupt`. (공백만 있는 줄은 빈 줄이 아니므로 `=` 없는 줄이다)
  3. `version` 이 없거나 값이 `int` 로 읽히지 않으면 `Corrupt`. 값이 1 이 아니면(0, 음수, 2 이상) 나머지를 보지 않고 `UnsupportedVersion`.
  4. 일곱 이름 중 하나라도 없거나, 값이 그 줄의 타입(`int`/`long`)으로 읽히지 않으면(글자 섞임, 앞뒤 공백, 빈 값, 범위 넘침) `Corrupt`.
  5. 아니면 `Loaded` 와 그 값의 `SaveData`. 모르는 이름의 줄은 무시한다. 값이 기능의 범위를 벗어나도(금화 음수, 번호 0 등) 그대로 담는다.
  - 숫자 읽기: `int.TryParse` / `long.TryParse` 에 `NumberStyles.AllowLeadingSign` 과 `CultureInfo.InvariantCulture`.
  - `Empty`·`Corrupt`·`UnsupportedVersion` 의 결과는 `Data` 가 `null` 이다.
- 지금 문화권(`CultureInfo.CurrentCulture`)에 영향받지 않는다. 문화권을 바꾸는 테스트는 `finally` 에서 원래 값으로 되돌린다.
- Core 규칙: `UnityEngine` 을 쓰지 않는다.

## 완료 조건
아래에서 "기준 글자"는 `"version=1\ngold=1234\nattackLevel=3\nhealthLevel=2\nattackSpeedLevel=10\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n"` 이다.

- `Assets/Tests/EditMode/Save/SaveCodecTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `SaveCodec.Encode(new SaveData(1234, 3, 2, 10, 7, 5, 638000000000000000))` 은 기준 글자와 같다.
- 기준 글자를 `Decode` 하면 `Status` `Loaded`, `HasSaveData` `true` 이고 `Data` 의 일곱 값이 1234, 3, 2, 10, 7, 5, 638000000000000000 이다.
- `Decode(null)` 은 `Status` `Empty`, `Data` `null`, `HasSaveData` `false` 이다.
- `Decode("")` 은 `Status` `Corrupt`, `Data` `null`, `HasSaveData` `false` 이다.
- `Decode("hello")` 는 `Status` `Corrupt`, `Data` `null`, `HasSaveData` `false` 이다.
- 기준 글자에서 `version=1\n` 줄을 뺀 글자는 `Corrupt` 이다.
- 기준 글자의 `version=1` 을 `version=abc` 로 바꾼 글자는 `Corrupt` 이다.
- 기준 글자의 `version=1` 을 `version=2` 로 바꾼 글자는 `UnsupportedVersion`, `Data` `null`, `HasSaveData` `false` 이다.
- 기준 글자의 `version=1` 을 `version=0` 으로 바꾼 글자는 `UnsupportedVersion` 이다.
- 한 줄짜리 `"version=2\n"` 은 `UnsupportedVersion` 이다.
- 기준 글자에서 `gold=1234\n` 줄을 뺀 글자는 `Corrupt` 이다.
- 기준 글자에서 `savedAtUtcTicks=638000000000000000\n` 줄을 뺀 글자는 `Corrupt` 이다.
- 기준 글자의 `gold=1234` 를 `gold=12a`, `gold= 12`, `gold=99999999999999999999` 로 각각 바꾼 세 글자는 모두 `Corrupt` 이다.
- 기준 글자의 `attackLevel=3` 을 `attackLevel=3000000000` 으로 바꾼 글자는 `Corrupt` 이다.
- 기준 글자 끝에 `gold=5\n` 을 붙인 글자(같은 이름 두 번)는 `Corrupt` 이다.
- 기준 글자 끝에 `oops\n` 을 붙인 글자(`=` 없는 줄)는 `Corrupt` 이다.
- 기준 글자의 `\n` 을 모두 `\r\n` 으로 바꾼 글자는 `Loaded` 이고 일곱 값이 1234, 3, 2, 10, 7, 5, 638000000000000000 이다.
- 기준 글자의 `gold=1234\n` 뒤에 `\n`(빈 줄)과 `extra=1\n` 을 넣은 글자는 `Loaded` 이고 일곱 값이 1234, 3, 2, 10, 7, 5, 638000000000000000 이다.
- `new SaveData(-5, 0, 0, 15, 0, 9, 0)` 을 `Encode` 한 뒤 `Decode` 하면 `Loaded` 이고 `Gold` −5, `AttackSpeedLevel` 15, `StageNumber` 0, `SeenIntroStage` 9, `SavedAtUtcTicks` 0 이다.
- 지금 문화권을 `de-DE` 로 바꾼 채 `new SaveData(1234567, 0, 0, 0, 1, 0, -1)` 을 `Encode` 하면 `gold=1234567\n` 과 `savedAtUtcTicks=-1\n` 이 들어 있다.
- 지금 문화권을 `de-DE` 로 바꾼 채 위 글자를 `Decode` 하면 `Loaded` 에 `Gold` 1234567, `SavedAtUtcTicks` −1 이다.
- `SaveCodec.Encode(null)` 은 `ArgumentNullException` 이다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- 026 이 만든 `SaveData`, `SaveLoadStatus`, `SaveLoadResult`, `SaveText` 의 공개 모양을 바꾸지 않는다.
- `ISaveStore`, `MemorySaveStore`, `AutoSave`, 파일 읽기·쓰기는 만들지 않는다 (028·029·030 이 한다).
- `Assets/Scripts/Core/` 의 다른 기능 폴더와 `PlumSword.Game` 의 코드를 고치지 않는다.
- `UnityEngine` 을 쓰지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 판 1 저장 데이터의 인코딩과 순서에 따른 손상·판 번호 판정을 구현했다.
- 기준 값, 오류와 경계 상황, 문화권 독립성을 에딧 모드 테스트로 추가했다.
- 공통 헬퍼의 검증을 각 테스트 본문으로 옮겨 관문이 실제 검증을 인식하게 했다.

### 바뀐 파일
- `Assets/Scripts/Core/Save/SaveCodec.cs` 및 `.meta`
- `Assets/Tests/EditMode/Save/SaveCodecTests.cs` 및 `.meta`
- `tasks/doing/027_save_codec.md`, `.loop/result.json`

### 가정
- 없음. 공개 연결 지점과 동작은 기존 사양을 따른다.

### 사람이 확인할 것
- 수정 후 Unity 전체 테스트를 직접 완료하지 못했으므로 루프의 전체 테스트 결과를 확인한다.

### 테스트 결과
- 관문 분석기의 `test_methods`와 `ASSERT_RE`로 확인: 모든 14개 테스트 본문에서 검증을 인식했다.
- 이전 시도는 루프에서 테스트를 통과했다. 이번 수정 후 `python tools/loop/run_unity_tests.py`는 결과 없이 대기하여 중단했으므로 재실행 결과는 확인하지 못했다.


## 루프 기록 (2026-11-17 13:56)

- 작업 에이전트: codex(시도 1~2)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/027_save_codec
- 루프 브랜치: loop/20261117-134243
