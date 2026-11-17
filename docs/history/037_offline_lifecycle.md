# 037_offline_lifecycle — 보상 시작과 백그라운드 복귀

## 목표
GameRoot에 보상 창과 복귀 재시작 흐름을 연결한다.

## 묶음
오프라인보상

## 선행 작업
- 033_offline_calculator
- 034_offline_claim
- 036_offline_view

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/offline_reward.md
- docs/spec/save.md (연결 지점 절)
- docs/spec/stage.md (연결 지점 절)
- docs/spec/combat.md (연결 지점 절)

## 요구사항
- GameRoot에 OfflineReward, OfflineRewardClaim, IsResumeRestart와 Restart를 사양의 공개 계약대로 추가한다. Awake는 오프라인 보상 사양의 GameRoot.Awake 순서와 저장 사양의 나머지 순서를 따른다. Loaded에서만 현재 UTC를 한 번 읽어 계산한다.
- 보상이 있으면 자동 저장을 끄고 전투를 멈춘 채 창을 만든다. 성공 콜백은 전투 재개 후 Begin한다. 손상과 보상 없음은 사양의 각 경로를 따른다.
- Assets/Scripts/Game/OfflineReward/OfflineResumeWatcher.cs에 사양의 Initialize 및 OnApplicationPause 동작을 구현하고 배치 모드가 아닐 때만 조립한다. 최신 여부는 !HasFailed && !IsDirty다.
- GameRestarter는 사양대로 기존 루트를 비활성화하고 파괴한 다음 프레임에 새 루트를 만든다. 재시작 표시를 정확히 한 번 전달한다. 기존 SaveView의 백그라운드 저장과 자동 재시도를 유지한다.
- 배치 모드 시작 상태는 Assets/Tests/PlayMode/OfflineReward/GameRootOfflineRewardTests.cs에서 검사한다.

## 완료 조건
- [플레이 모드] 배치 모드 시작 시 OfflineReward와 OfflineRewardClaim은 null, IsResumeRestart는 false다. 이 테스트는 루프와 명령줄의 배치 모드에서 판정하며, 건너뛰는 코드(Assert.Ignore·[Ignore] 등)는 넣지 않는다.
- [플레이 모드] 배치 모드 시작 시 AutoSave.Enabled와 StageProgress.HasBegun은 true다.
- [플레이 모드] 배치 모드의 [Game] 아래에 [OfflineRewardView]와 [OfflineResumeWatcher]가 없다.
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다.
- [사람 확인] 10분 전 저장을 불러오면 보상 창이 열린 동안 전투와 저장이 멈추고 CLAIM 성공 후 전투가 시작되는지 확인한다.
- [사람 확인] CLAIM 전 종료 후 다시 켜면 이전 저장 시각부터 보상을 다시 계산하는지 확인한다.
- [사람 확인] 폰 빌드에서 10초 이상 백그라운드에 둔 뒤 복귀하면 저장된 스테이지 처음부터 보상 창과 함께 시작하는지 확인한다.
- [사람 확인] 저장 실패 상태에서 백그라운드 복귀 시 재시작하지 않고 RETRY 창이 남는지 확인한다.

## 하지 말 것
- 다른 기능의 내부 코드를 수정하지 않는다. GameRoot의 기존 기능 조립은 유지한다.
- 시작 화면이나 OnApplicationFocus 처리는 만들지 않는다.
- 씬·프리팹·에셋, 어셈블리 정의와 Capture 테스트를 수정하지 않는다.

## 보고서

### 한 일
- 저장 불러오기 결과에 따른 오프라인 보상 계산, 보상 창과 전투·자동 저장 시작 순서를 GameRoot에 연결했다.
- 백그라운드 복귀 감시와 다음 프레임에 루트를 다시 만드는 흐름을 추가했다.
- 배치 모드 시작 상태를 확인하는 플레이 모드 테스트와 사양 설명을 추가했다.

### 바뀐 파일
- Assets/Scripts/Game/GameRoot.cs, Assets/Scripts/Game/GameRestarter.cs 및 .meta
- Assets/Scripts/Game/OfflineReward/OfflineResumeWatcher.cs 및 .meta
- Assets/Tests/PlayMode/OfflineReward.meta, OfflineReward/GameRootOfflineRewardTests.cs 및 .meta
- docs/spec/offline_reward.md
- tasks/doing/037_offline_lifecycle.md, .loop/result.json

### 가정
- 같은 루트에 Restart가 중복 호출되면 재시작 오브젝트를 한 번만 만든다.
- 다음 프레임 보장을 위해 재시작 오브젝트의 생성 프레임을 기록한다.

### 사람이 확인할 것
- 10분 전 저장을 불러오면 보상 창이 열린 동안 전투와 저장이 멈추고 CLAIM 성공 후 전투가 시작되는지 확인한다.
- CLAIM 전 종료 후 다시 켜면 이전 저장 시각부터 보상을 다시 계산하는지 확인한다.
- 폰 빌드에서 10초 이상 백그라운드에 둔 뒤 복귀하면 저장된 스테이지 처음부터 보상 창과 함께 시작하는지 확인한다.
- 저장 실패 상태에서 백그라운드 복귀 시 재시작하지 않고 RETRY 창이 남는지 확인한다.

### 테스트 결과
- Unity 테스트와 컴파일은 지시에 따라 직접 실행하지 않았다. 추가한 플레이 모드 테스트와 기존 테스트의 루프 판정을 기다린다.
- git diff --check 통과.

## 보고서

### 한 일
- 관문 위반과 작업 문서의 완료 조건을 대조했다. 비배치 환경에서 Assert.Ignore를 요구하는 완료 조건이 관문의 건너뛰기 금지와 충돌하여 구현을 변경하지 못했다.

### 바뀐 파일
- tasks/doing/037_offline_lifecycle.md, .loop/result.json

### 가정
- 없음.

### 사람이 확인할 것
- 비배치 환경에서 Assert.Ignore를 요구하는 완료 조건과 테스트 건너뛰기를 금지하는 관문 중 적용할 기준을 결정한다.
- 10분 전 저장을 불러오면 보상 창이 열린 동안 전투와 저장이 멈추고 CLAIM 성공 후 전투가 시작되는지 확인한다.
- CLAIM 전 종료 후 다시 켜면 이전 저장 시각부터 보상을 다시 계산하는지 확인한다.
- 폰 빌드에서 10초 이상 백그라운드에 둔 뒤 복귀하면 저장된 스테이지 처음부터 보상 창과 함께 시작하는지 확인한다.
- 저장 실패 상태에서 백그라운드 복귀 시 재시작하지 않고 RETRY 창이 남는지 확인한다.

### 테스트 결과
- Unity 테스트와 컴파일은 지시에 따라 직접 실행하지 않았다. 변경 후 루프의 테스트와 관문 판정이 필요하다.


## 보류 사유 (2026-11-17 15:29)

- 사유: 작업 문서가 비배치 모드에서 Assert.Ignore를 명시적으로 요구하지만 관문은 해당 테스트 건너뛰기를 금지한다.
- 질문: 비배치 모드의 Assert.Ignore 요구를 삭제하고 해당 환경에서도 실제 동작을 검증하도록 완료 조건을 변경할 수 있습니까?
- 추천안: Assert.Ignore 요구를 없애고 배치 모드에서는 기존 조건을 모두 검증하며 비배치 모드에서는 복귀 감시 객체가 존재하는지 검증한다.
- 기능 브랜치: task/037_offline_lifecycle (작업 중 상태 커밋됨)
- 작업 에이전트: codex(시도 1~2)

## 답변 (2026-11-17 15:43)

- 보류 종류: 에이전트의 질문 (2026-11-17 15:29 보류 사유에 대한 답)
- 결정: 저장 기능 작업 032와 같이 비배치 모드에서 테스트를 건너뛰라는 요구를 없애고, 루프와 명령줄의 배치 모드에서 시작 상태를 판정한다.
- 이유: 관문은 테스트 건너뛰기 코드를 금지하며 루프는 배치 모드로 테스트한다. 에디터에서 직접 테스트하면 실제 저장 파일 상태에 따라 결과가 달라지거나 저장 파일을 읽고 쓸 수 있음을 사용자가 알고 있다.
- 할 일: Assets/Tests/PlayMode/OfflineReward/GameRootOfflineRewardTests.cs의 Assert.Ignore를 제거하고, 배치 모드 시작 상태에 관한 기존 완료 조건을 모두 검증한다. Assume이나 조건부 조기 종료 등으로 건너뛰기를 대신하지 않는다. 전체 테스트와 관문을 다시 확인한다.
- 바꾼 칸: 완료 조건 첫 줄의 비배치 모드 Assert.Ignore 요구를 삭제하고, 배치 모드 판정과 건너뛰기 코드 금지를 명시했다.
- 기능 브랜치: task/037_offline_lifecycle의 작업 중 커밋을 살려서 이어간다.


## 보류 사유 (2026-11-17 15:53)

- 사유: 재시도 한도(5회) 초과. 마지막 실패:
.loop/result.json 이 없거나 형식이 맞지 않음
- 질문: 실패 원인을 확인하고 작업 문서나 완료 조건을 고쳐 주세요.
- 추천안: 마지막 실패 내용과 기능 브랜치의 작업 중 상태를 보고 판단
- 기능 브랜치: task/037_offline_lifecycle
- 작업 에이전트: codex(시도 1~6)

## 답변 (2026-11-17 16:01)

- 보류 종류: 재시도 한도 초과 (2026-11-17 15:53 보류 사유에 대한 답)
- 원인: Codex의 최초 실행과 5회 재시도에서 Windows 샌드박스가 `registered Core launch requires an installed app` 오류로 명령 실행을 시작하지 못했고 결과 파일을 기록하지 못했다. 앱 재시작 후에도 같은 오류가 재현됐다.
- 결정: 기존 작업과 완료 조건을 유지하고 Claude를 우선 에이전트로 지정해 다시 처리한다.
- 이유: 이번 실패는 작업 조건이 아니라 Codex 실행 환경에서 발생했으며, 앞선 테스트 조건 충돌은 15:43 답변으로 정리됐다.
- 할 일: 앞선 답변에 따라 Assert.Ignore를 제거하고 배치 모드 시작 상태의 완료 조건을 검증한다. 기존 변경을 확인하고 이어서 구현한 뒤 테스트와 관문 및 리뷰를 거친다.
- 바꾼 칸: 없음.
- 기능 브랜치: task/037_offline_lifecycle의 작업 중 커밋을 살려서 이어간다.

## 보고서

### 한 일
- 이전 작업 중 커밋(GameRoot의 OfflineReward·OfflineRewardClaim·IsResumeRestart·Restart, 보상 창 조립, OfflineResumeWatcher, GameRestarter, 플레이 모드 테스트)을 사양의 `GameRoot.Awake` 순서·연결 지점과 대조해 확인하고 그대로 살렸다.
- 15:43 답변에 따라 `GameRootOfflineRewardTests`의 `Assert.Ignore`를 지웠다. 이제 배치 모드에서 완료 조건 세 가지(보상·청구·재시작 표시 없음, 자동 저장 켜짐·스테이지 시작됨, `[OfflineRewardView]`·`[OfflineResumeWatcher]` 없음)를 건너뛰지 않고 판정한다.
- 사양 `docs/spec/offline_reward.md`의 테스트 21번에서 "배치 모드가 아니면 Assert.Ignore" 문장을 지우고 건너뛰기 금지로 바꿨다. 경계 상황 절 끝의 "자동 테스트 대상이 아니다" 문장도 배치 모드 시작 상태는 플레이 모드 테스트로 확인한다고 맞췄다.

### 바뀐 파일
- Assets/Scripts/Game/GameRoot.cs (이전 커밋)
- Assets/Scripts/Game/GameRestarter.cs 및 .meta (이전 커밋)
- Assets/Scripts/Game/OfflineReward/OfflineResumeWatcher.cs 및 .meta (이전 커밋)
- Assets/Tests/PlayMode/OfflineReward.meta, Assets/Tests/PlayMode/OfflineReward/GameRootOfflineRewardTests.cs 및 .meta (이번에 Assert.Ignore 제거)
- docs/spec/offline_reward.md
- tasks/doing/037_offline_lifecycle.md, .loop/result.json

### 가정
- 같은 루트에서 Restart가 여러 번 불리면 첫 호출만 처리한다 (재시작 오브젝트와 재시작 표시를 한 번만 만든다).
- GameRestarter는 Awake 때의 `Time.frameCount`를 기억하고, 그 프레임이 지난 뒤의 Update에서만 새 루트를 만든다 (같은 프레임에 Update가 불려도 "다음 프레임"을 지키기 위해서).
- 재시작 표시는 GameRoot의 internal 정적 필드(`NextIsResumeRestart`)로 전달하고, 새 루트의 Awake가 읽자마자 지운다.

### 사람이 확인할 것
- 10분 전 저장을 불러오면 보상 창이 열린 동안 전투와 저장이 멈추고 CLAIM 성공 후 전투가 시작되는지 확인한다.
- CLAIM 전 종료 후 다시 켜면 이전 저장 시각부터 보상을 다시 계산하는지 확인한다.
- 폰 빌드에서 10초 이상 백그라운드에 둔 뒤 복귀하면 저장된 스테이지 처음부터 보상 창과 함께 시작하는지 확인한다.
- 저장 실패 상태에서 백그라운드 복귀 시 재시작하지 않고 RETRY 창이 남는지 확인한다.
- 에디터에서 플레이 모드 테스트를 직접 돌리면 실제 저장 파일을 읽어 GameRootOfflineRewardTests·GameRootSaveTests 결과가 달라질 수 있다 (15:43 답변에서 알고 있다고 한 것).

### 테스트 결과
- `python tools/loop/run_unity_tests.py` (worktree, 배치 모드): EditMode 438개 중 438개 통과, PlayMode 25개 중 25개 통과 (새 GameRootOfflineRewardTests 3개 포함). 컴파일 오류 없음.
- `git diff --check` 통과.


## 보류 사유 (2026-11-17 16:06)

- 사유: 교차 리뷰(codex)가 사람의 결정이 필요하다고 판정함.
리뷰 요약: 작업 폴더를 읽으려 했으나 실행 환경이 프로세스 생성을 거부해 코드와 테스트를 확인하지 못했습니다. 따라서 병합 가능 여부를 판정할 수 없습니다.
- [human] 작업 문서, 변경 코드와 테스트를 읽는 도구가 모두 'registered Core launch requires an installed app' 오류로 실행되지 않았습니다. 조건 1~3의 '약함'은 테스트가 약하다고 확인한 뜻이 아니라 검증할 수 없다는 뜻입니다. 파일과 줄은 확인하지 못했습니다.
- 질문: 작업 폴더를 읽을 수 있는 실행 환경을 복구한 뒤 이 리뷰를 다시 실행해 주시겠습니까?
- 추천안: 환경을 복구하고 동일한 기준 커밋과 작업 커밋으로 리뷰를 재실행한 뒤 판정에 따라 병합하는 것을 권합니다.
- 기능 브랜치: task/037_offline_lifecycle (작업 중 상태 커밋됨)
- 작업 에이전트: claude(시도 1)


## 답변 (2026-11-17 16:08)

- 보류 종류: 교차 리뷰가 사람의 결정을 요구 (2026-11-17 16:06 보류 사유에 대한 답). 실제로는 리뷰 실행 환경 문제다.
- 결정: 구현은 그대로 두고 교차 리뷰만 다시 받는다. Codex 실행 환경이 복구될 때까지 사용자가 루프 설정에서 Codex 를 빼므로, 이번 리뷰는 Claude 가 새 세션으로 한다 (기록에 "같은 에이전트"로 남는다).
- 이유: 16:06 리뷰는 Codex 샌드박스가 `registered Core launch requires an installed app` 오류로 명령 실행을 거부해 파일을 하나도 읽지 못한 채 낸 판정이다. 작업 내용에 대한 판단이 아니다. 16:01 답변의 Codex 실행 실패와 같은 원인이다.
- 리뷰 지적에 대한 판단: 16:06 리뷰의 [human] 지적(도구 실행 불가로 확인 못 함)과 완료 조건 1~3의 "약함"은 검증하지 못했다는 뜻이므로 받아들이지 않는다. 이번 리뷰는 기능 브랜치의 코드와 GameRootOfflineRewardTests 를 직접 읽고 완료 조건마다 새로 판정한다. 사람의 결정이 필요한 사항은 현재 없다.
- 할 일: 구현 에이전트는 기능 브랜치의 작업 중 커밋(GameRoot, GameRestarter, OfflineResumeWatcher, GameRootOfflineRewardTests, docs/spec/offline_reward.md)과 마지막 보고서를 확인한다. 문제가 없으면 코드를 더 바꾸지 않고 테스트·관문을 다시 통과시켜 리뷰로 넘긴다. 리뷰어는 15:43 답변(비배치 모드 Assert.Ignore 요구 삭제, 배치 모드에서만 판정)이 반영됐는지도 확인한다.
- 바꾼 칸: 없음.
- 기능 브랜치: task/037_offline_lifecycle 의 작업 중 커밋을 살려서 이어간다.

## 보고서

### 한 일
- 16:08 답변에 따라 코드는 더 바꾸지 않았다. 기능 브랜치의 작업 중 커밋(GameRoot, GameRestarter, OfflineResumeWatcher, GameRootOfflineRewardTests, docs/spec/offline_reward.md)을 사양 `docs/spec/offline_reward.md`의 "`GameRoot.Awake` 순서"(4·5·6번 끼움, 배치 모드가 아닐 때만 감시 조립), `Restart`·`GameRestarter`·`OfflineResumeWatcher` 동작과 다시 대조했고 어긋난 곳이 없었다.
- 15:43 답변 반영을 확인했다: `GameRootOfflineRewardTests`에 `Assert.Ignore`·`Assume`·조건부 조기 종료가 없고, 배치 모드 시작 상태의 완료 조건 1~3을 모두 판정한다. 사양 테스트 21번도 같은 내용이다.
- 전체 유니티 테스트를 다시 돌려 통과를 확인했다.

### 바뀐 파일
- Assets/Scripts/Game/GameRoot.cs (이전 커밋)
- Assets/Scripts/Game/GameRestarter.cs 및 .meta (이전 커밋)
- Assets/Scripts/Game/OfflineReward/OfflineResumeWatcher.cs 및 .meta (이전 커밋)
- Assets/Tests/PlayMode/OfflineReward.meta, Assets/Tests/PlayMode/OfflineReward/GameRootOfflineRewardTests.cs 및 .meta (이전 커밋)
- docs/spec/offline_reward.md (이전 커밋)
- tasks/doing/037_offline_lifecycle.md, .loop/result.json (이번)

### 가정
- 같은 루트에서 Restart가 여러 번 불리면 첫 호출만 처리한다 (재시작 오브젝트와 재시작 표시를 한 번만 만든다).
- GameRestarter는 Awake 때의 `Time.frameCount`를 기억하고, 그 프레임이 지난 뒤의 Update에서만 새 루트를 만든다 ("다음 프레임" 보장).
- 재시작 표시는 GameRoot의 internal 정적 필드(`NextIsResumeRestart`)로 전달하고, 새 루트의 Awake가 읽자마자 지운다.

### 사람이 확인할 것
- 10분 전 저장을 불러오면 보상 창이 열린 동안 전투와 저장이 멈추고 CLAIM 성공 후 전투가 시작되는지 확인한다.
- CLAIM 전 종료 후 다시 켜면 이전 저장 시각부터 보상을 다시 계산하는지 확인한다.
- 폰 빌드에서 10초 이상 백그라운드에 둔 뒤 복귀하면 저장된 스테이지 처음부터 보상 창과 함께 시작하는지 확인한다.
- 저장 실패 상태에서 백그라운드 복귀 시 재시작하지 않고 RETRY 창이 남는지 확인한다.
- 에디터에서 플레이 모드 테스트를 직접 돌리면 실제 저장 파일 상태에 따라 GameRootOfflineRewardTests 결과가 달라질 수 있다 (15:43 답변에서 알고 있다고 한 것).

### 테스트 결과
- `python tools/loop/run_unity_tests.py` (worktree, 배치 모드): EditMode 438개 중 438개 통과, PlayMode 25개 중 25개 통과 (GameRootOfflineRewardTests 3개 포함). 컴파일 오류 없음.
- `git diff --check` 통과.


## 루프 기록 (2026-11-17 16:12)

- 작업 에이전트: claude(시도 1)
- 리뷰: claude (같은 에이전트) 통과
- 기능 브랜치: task/037_offline_lifecycle
- 루프 브랜치: loop/20261117-161017
