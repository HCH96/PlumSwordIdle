# 030_file_save_store — 파일 저장소

## 목표
`PlumSword.Game.Save.FileSaveStore` 를 만든다. 지정한 폴더의 `save.dat` 하나를 읽어 `SaveCodec.Decode` 로 판정하고, `save.tmp` 에 쓴 뒤 바꿔 넣어 실패해도 기존 파일을 남긴다. 화면 코드 어셈블리(`PlumSword.Game`)의 일이며, 실제 파일을 쓰므로 플레이 모드 테스트 어셈블리에서 검사한다. `GameRoot` 에 붙이는 것은 032 가 한다.

## 묶음
저장

## 선행 작업
- 026_save_data
- 027_save_codec
- 028_auto_save

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/save.md ("동작" 절의 "파일 저장소"·"읽기 실패", "경계 상황" 표의 파일 줄, "수치" 절의 파일 이름과 글자 인코딩, "연결 지점" 절의 `FileSaveStore`·`ISaveStore`)

## 요구사항
- 새 파일 `Assets/Scripts/Game/Save/FileSaveStore.cs` (네임스페이스 `PlumSword.Game.Save`). `System.IO` 만 쓰고 `UnityEngine` 은 쓰지 않아도 된다 (`Application.persistentDataPath` 는 032 가 넘긴다).
- 새 테스트 파일 `Assets/Tests/PlayMode/Save/FileSaveStoreTests.cs` (네임스페이스 `PlumSword.Game.Tests.Save`, 클래스 `FileSaveStoreTests`). 실제 실행이 필요 없으므로 `[Test]` 로 써도 된다. 테스트마다 `Path.Combine(Path.GetTempPath(), "PlumSwordSaveTest_" + Guid.NewGuid().ToString("N"))` 같은 새 임시 폴더를 쓰고 `[TearDown]` 에서 지운다 (지우다 난 예외는 무시). `Application.persistentDataPath` 는 쓰지 않는다.
- 선행 작업이 만든 것을 쓴다: 028 의 `PlumSword.Core.Save.ISaveStore`(`bool TryWrite(string text)`), 027 의 `SaveCodec.Decode(string)`·`SaveCodec.Encode(SaveData)`, 026 의 `SaveLoadResult(status, data)`·`SaveLoadStatus`·`SaveData`.
- **공개 모양** (사양 "연결 지점" 그대로):

  ```csharp
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
  }
  ```

  - 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/save.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 생성자: `directory` 가 `null` 이면 `ArgumentNullException`(이것도 `ArgumentException` 이다), `""` 이면 `ArgumentException`. 폴더를 만들지 않는다 (쓸 때 만든다).
- **`Load()`**: `save.dat` 이 없으면 `SaveCodec.Decode(null)`(`Empty`). 있으면 전체를 UTF-8 로 읽어 `SaveCodec.Decode(text)`. 읽다가 예외가 나면 `new SaveLoadResult(SaveLoadStatus.Corrupt, null)`. 예외를 밖으로 내보내지 않는다. 폴더가 없어도 `Empty` 다.
- **`TryWrite(text)`**: `text` 가 `null` 이면 `ArgumentNullException`. 순서: 폴더가 없으면 만든다(`Directory.CreateDirectory`) → `save.tmp` 에 전부 쓴다(UTF-8, BOM 없음: `new UTF8Encoding(false)`) → `save.dat` 이 있으면 `File.Replace(save.tmp, save.dat, null)`, 없으면 `File.Move(save.tmp, save.dat)` → `true`. 어느 단계에서든 예외가 나면 `save.tmp` 를 지워 보고(`File.Delete`, 지우다 난 예외도 무시) `false`. 실패해도 기존 `save.dat` 은 그대로다. `ArgumentNullException` 말고는 예외를 밖으로 내보내지 않는다.
- 읽기 중 예외로 `Corrupt` 가 되는 경우는 자동 테스트 대상이 아니다 (사양이 정함). 코드만 넣는다.

## 완료 조건
"임시 폴더"는 테스트마다 새로 정한, 아직 만들지 않은 폴더 경로다 (테스트가 따로 만들라고 적은 경우만 만든다).

- [플레이 모드] `Assets/Tests/PlayMode/Save/FileSaveStoreTests.cs` 에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] `new FileSaveStore(null)` 과 `new FileSaveStore("")` 은 각각 `ArgumentException`(또는 그 하위 타입)이다.
- [플레이 모드] 아직 없는 임시 폴더의 `FileSaveStore.Load()` 는 `Status` `Empty`, `Data` `null` 이다.
- [플레이 모드] 같은 저장소의 `TryWrite("abc")` 는 `true` 이고, 그 뒤 임시 폴더의 `save.dat` 을 읽은 내용은 `"abc"` 이며 `save.tmp` 파일은 없다.
- [플레이 모드] `TryWrite("abc")` 로 쓴 `save.dat` 의 첫 세 바이트는 `0x61 0x62 0x63` 이다 (BOM 없음).
- [플레이 모드] `TryWrite("abc")` 뒤 `Load()` 는 `Corrupt` 이다.
- [플레이 모드] 이어서 `SaveCodec.Encode(new SaveData(1234, 3, 2, 10, 7, 5, 638000000000000000))` 을 `TryWrite` 하면 `true` 이고 (이미 있는 `save.dat` 을 바꾸는 경우), `Load()` 는 `Loaded` 에 일곱 값 1234, 3, 2, 10, 7, 5, 638000000000000000 이며 `save.tmp` 파일은 없다.
- [플레이 모드] 임시 폴더를 만들고 `save.dat` 을 `"old"` 로 써 둔 뒤 같은 폴더에 `save.tmp` 라는 이름의 **폴더**를 만들어 두면, `TryWrite("new")` 는 예외 없이 `false` 이고 `save.dat` 의 내용은 `"old"` 그대로다.
- [플레이 모드] `TryWrite(null)` 은 `ArgumentNullException` 이다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.

## 하지 말 것
- `GameRoot` 를 고치지 않는다 (032 가 한다). `SaveView`, `SaveErrorView` 는 만들지 않는다 (031 이 한다).
- `Application.persistentDataPath` 아래의 실제 저장 파일을 테스트에서 읽거나 쓰지 않는다.
- `Assets/Scripts/Core/` 의 코드를 고치지 않는다 (`ISaveStore`, `SaveCodec` 등은 연결 지점으로만 쓴다).
- `Assets/Tests/PlayMode/Capture/` 와 `GameBootTests` 를 고치지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.

## 보고서

### 한 일
- 파일 저장소의 읽기, BOM 없는 임시 파일 쓰기, 기존 파일 교체와 실패 시 임시 파일 정리를 구현했다.
- 임시 폴더를 사용하는 플레이 모드 테스트로 생성자, 읽기, 쓰기, 교체, 실패와 null 입력을 검사하도록 추가했다.

### 바뀐 파일
- `Assets/Scripts/Game/Save/FileSaveStore.cs` 및 폴더와 파일의 `.meta`
- `Assets/Tests/PlayMode/Save/FileSaveStoreTests.cs` 및 폴더와 파일의 `.meta`

### 가정
없음. 공개 연결 지점과 동작은 기존 사양을 따른다.

### 사람이 확인할 것
- Unity 라이선스 클라이언트 연결을 복구한 환경에서 전체 에딧 모드 및 플레이 모드 테스트를 실행해 통과 여부를 확인한다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: Unity 라이선스 클라이언트 연결 실패 및 초기화 시간 초과로 테스트가 시작되지 않아 실행을 중단했다. 컴파일과 테스트 통과 여부는 확인하지 못했다.


## 루프 기록 (2026-11-17 14:12)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/030_file_save_store
- 루프 브랜치: loop/20261117-134243
