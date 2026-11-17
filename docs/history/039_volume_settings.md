# 039_volume_settings — 음량 값

## 목표
Core(`PlumSword.Core.StartMenu`)에 배경음악·효과음 음량 값 `VolumeSettings`를 만든다.

## 묶음
시작화면

## 선행 작업
없음

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/start_menu.md ("동작"의 "음량", "수치", "연결 지점" 절)

## 요구사항
- `Assets/Scripts/Core/StartMenu/VolumeSettings.cs`, `public sealed class VolumeSettings` (네임스페이스 `PlumSword.Core.StartMenu`).
- 상수: `public const int MinVolume = 0;`, `public const int MaxVolume = 100;`, `public const int DefaultVolume = 80;` (80은 임시값).
- 생성자 `public VolumeSettings(int bgm = DefaultVolume, int sfx = DefaultVolume)`: 두 값을 0~100으로 잘라내 담는다. 예외 없음, `Changed` 없음.
- 속성: `int Bgm { get; }`, `int Sfx { get; }`, `double BgmScale { get; }` = `Bgm / 100.0`, `double SfxScale { get; }` = `Sfx / 100.0` (반올림 없음).
- `public event System.Action Changed;`: `Bgm` 또는 `Sfx`가 실제로 바뀐 뒤에 한 번.
- `void SetBgm(int volume)`, `void SetSfx(int volume)`: 0~100으로 잘라낸 값이 지금 값과 다를 때만 바꾸고 `Changed`를 한 번 보낸다. 같으면 아무 일도 없다. 예외 없음 (`int.MinValue`, `int.MaxValue`도).
- 소리가 없으므로 아무 데도 적용하지 않는다.
- 구현하다 모양이 사양과 달라지면 사양 문서(`docs/spec/start_menu.md`)의 "연결 지점" 절을 고치고 보고서의 가정에 적는다.
- 테스트 네임스페이스는 `PlumSword.Core.Tests.StartMenu`. 실수 비교는 오차 1e-9.

## 완료 조건
- `Assets/Tests/EditMode/StartMenu/VolumeSettingsTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- `VolumeSettings.MinVolume`은 0, `MaxVolume`은 100, `DefaultVolume`은 80이다.
- `new VolumeSettings()`는 `Bgm` 80, `Sfx` 80, `BgmScale` 0.8, `SfxScale` 0.8이다.
- `new VolumeSettings(-5, 150)`은 예외 없이 `Bgm` 0, `Sfx` 100이다.
- `new VolumeSettings(int.MinValue, int.MaxValue)`는 예외 없이 `Bgm` 0, `Sfx` 100이다.
- `new VolumeSettings(30, 55)`는 `Bgm` 30, `Sfx` 55, `BgmScale` 0.3, `SfxScale` 0.55이다.
- `new VolumeSettings()`에 `Changed` 횟수를 기록하고 `SetBgm(30)`을 하면 `Bgm` 30, `Sfx` 80, 알림 1번이다.
- 이어서 `SetBgm(30)`을 다시 하면 알림은 1번 그대로다.
- 이어서 `SetSfx(150)`을 하면 `Sfx` 100, 알림 2번이다.
- 이어서 `SetSfx(int.MaxValue)`를 하면 `Sfx` 100, 알림 2번 그대로다.
- 이어서 `SetBgm(-1)`을 하면 `Bgm` 0, 알림 3번이다.
- 이어서 `SetBgm(int.MinValue)`를 하면 예외 없이 `Bgm` 0, 알림 3번 그대로다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `UnityEngine`을 쓰지 않는다. PlayerPrefs 읽기·쓰기(`VolumePrefs`)는 만들지 않는다 (043이 한다).
- 다른 기능의 코드를 고치지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의, `Assets/Tests/PlayMode/Capture/`를 고치지 않는다.

## 보고서

### 한 일
- 배경음악과 효과음의 음량을 제한하고 비율과 변경 알림을 제공하는 순수 C# `VolumeSettings`를 구현했다.
- 기본값, 경계값, 비율, 변경 알림과 중복 입력을 검증하는 에딧 모드 테스트를 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/StartMenu/VolumeSettings.cs` 및 `.meta`
- `Assets/Tests/EditMode/StartMenu/VolumeSettingsTests.cs` 및 `.meta`
- `tasks/doing/039_volume_settings.md` (보고서 추가)
- `.loop/result.json` (결과 기록)

### 가정
- 없음. 기본 음량 80은 작업 문서와 사양에 명시된 임시값이다.
- 공개 연결 지점은 사양과 같아 사양 수정은 필요하지 않았다.

### 사람이 확인할 것
- 없음.

### 테스트 결과
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 새 테스트와 기존 전체 테스트의 루프 판정을 기다린다.
- `git diff --check`에서 오류가 없었고 새 `.meta` GUID의 중복이 없음을 확인했다.


## 루프 기록 (2026-11-17 18:29)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/039_volume_settings
- 루프 브랜치: loop/20261117-182336
