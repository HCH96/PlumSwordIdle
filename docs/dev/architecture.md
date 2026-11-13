# 코드 구조

코드를 어디에 두고, 어셈블리끼리 무엇을 참조할 수 있는지, 화면을 어떻게 만드는지 정한다. 새 코드를 쓰기 전에 읽는다.

## 폴더와 어셈블리

| 폴더 | 어셈블리 (네임스페이스 같음) | 넣는 것 | 참조할 수 있는 것 |
| --- | --- | --- | --- |
| `Assets/Scripts/Core/` | `PlumSword.Core` | 게임 로직. 재화, 강화 수치, 스테이지 진행, 경지 돌파, 오프라인 보상, 저장 데이터 | 없음 (순수 C#) |
| `Assets/Scripts/Game/` | `PlumSword.Game` | 화면 코드. `MonoBehaviour`, 화면 조립, 입력 전달, 표시 | `PlumSword.Core`, uGUI, TextMeshPro, Input System |
| `Assets/Tests/EditMode/` | `PlumSword.Core.Tests` | `PlumSword.Core`의 에딧 모드 테스트 | `PlumSword.Core`, NUnit |

- 기능별 하위 폴더를 만들고 네임스페이스를 폴더에 맞춘다. 예: `Assets/Scripts/Core/Currency/` → `PlumSword.Core.Currency`, 테스트는 `Assets/Tests/EditMode/Currency/` → `PlumSword.Core.Tests.Currency`.
- 파일 하나에 공개 타입 하나, 파일 이름은 타입 이름과 같게 한다.
- 테스트 클래스 이름은 `<대상>Tests`, 테스트 이름은 `<상황>_<기대 결과>` (예: `Spend_MoreThanBalance_Fails`).
- 새 어셈블리 정의는 만들지 않는다. 필요하면 작업 문서에 근거를 적고 이 문서를 함께 고친다.

## 순수 C# 규칙이 지켜지는 방식

`PlumSword.Core`는 유니티 엔진을 참조하지 않는다. 사람이 지키는 약속이 아니라 두 겹으로 강제된다.

1. `PlumSword.Core.asmdef`의 `noEngineReferences: true`. `UnityEngine`을 쓰면 컴파일 오류가 난다 (`error CS0246`).
2. `CoreAssemblyTests.Core_DoesNotReferenceUnity` 테스트. 어셈블리 정의가 바뀌어 유니티 참조가 생기면 실패한다.

그래서 Core에서는 아래를 쓰지 않는다. 필요한 값은 Game이 넘겨준다.

| Core에서 못 쓰는 것 | 대신 |
| --- | --- |
| `Time.deltaTime`, `Time.time` | 메서드 인자로 경과 시간(초)을 받는다. 예: `Tick(double deltaSeconds)` |
| `DateTime.Now`를 로직 안에서 직접 호출 | 현재 시각을 인자로 받는다 (오프라인 보상 테스트가 가능해야 한다) |
| `UnityEngine.Random` | 시드를 받는 `System.Random`을 생성자로 주입 |
| `Debug.Log` | 결과 값이나 이벤트로 알린다 |
| `PlayerPrefs`, `JsonUtility`, 파일 경로 | Core는 저장 데이터 구조와 변환만 맡고, 실제 읽기·쓰기는 Game이 한다 |
| `Vector2`, `Mathf` | `System.Math`와 기본 숫자 타입 |

## 화면: 코드로 조립한다

씬과 프리팹을 에디터에서 배치하지 않는다. 실행 시점에 코드로 오브젝트와 UI를 만든다. 씬·프리팹 파일은 텍스트로 고치면 잘 깨지고 테스트로 판정하기도 어렵기 때문이다.

- **진입점**: `GameBootstrap`이 어떤 씬에서든 `[Game]` 루트 오브젝트를 하나 만들고 `GameRoot`를 붙인다. 씬에는 카메라 같은 기본 오브젝트만 둔다.
- **조립**: `GameRoot.Awake`에서 Core 객체를 만들고, 기능별 화면을 `[Game]`의 자식으로 만들어 붙인다. 기능이 늘면 `GameRoot`에 조립 호출을 한 줄씩 추가한다.
- **UI**: uGUI(`Canvas`, `Image`, `Button`)와 TextMeshPro를 코드로 만든다. 입력은 Input System을 쓰므로 `EventSystem`에는 `InputSystemUIInputModule`을 붙인다.
- **화면 코드의 역할**: 입력을 Core에 전달하고 Core의 상태를 표시하는 것만 한다. 계산은 Core에 둔다.
- **하지 않는 것**: `.unity`, `.prefab`, `.asset` 파일을 직접 고치지 않는다. 스프라이트 같은 리소스가 필요하면 `Resources.Load`로 읽고, 리소스가 없을 때도 동작하도록 단색 사각형 같은 대체 표시를 둔다.

참고: `GameBootstrap`은 플레이 모드 테스트에서도 실행된다. 플레이 모드 테스트는 `[Game]` 루트가 이미 있다고 가정하고 쓴다.

## 테스트

- 게임 로직은 에딧 모드 테스트로 검사한다. 작업 문서의 완료 조건마다 테스트를 하나 이상 쓴다.
- 플레이 모드 테스트는 실제 실행이 필요한 부분에만 쓴다. 아직 어셈블리가 없으며, 처음 필요해지는 작업에서 `Assets/Tests/PlayMode/`에 `PlumSword.Game.Tests`로 만들고 이 문서의 표에 추가한다.
- 화면 없이 테스트를 돌리는 명령 (프로젝트 폴더에서):
  ```
  python tools/loop/run_unity_tests.py
  ```
  컴파일 오류, 실패한 테스트 이름과 메시지를 요약해 출력한다. 종료 코드 0이면 통과다. 테스트가 0개여도 실패다. 같은 프로젝트 폴더를 에디터로 열어 둔 상태에서는 실행할 수 없다.
- 루프는 에이전트가 완료를 보고한 뒤 같은 래퍼로 전체 테스트를 돌려 판정한다 ([loop.md](loop.md) "테스트"). 에이전트는 보고 전에 직접 돌려 볼 수 있으면 돌려 보고, 돌릴 수 없는 환경이면 보고서에 "직접 실행하지 못함"이라고 적는다.

## `.meta` 파일

`Assets/` 아래의 모든 파일과 폴더에는 짝이 되는 `.meta` 파일이 있어야 하고, 함께 커밋한다. 유니티가 프로젝트를 열거나 화면 없이 실행될 때 자동으로 만든다. 파일을 옮기거나 지울 때는 `.meta`도 같이 옮기거나 지운다.
