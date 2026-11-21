# 067_combat_tianmo_final — 천마 일반 상대를 4스테이지로, 최종 보스 그림 적용

## 목표
`PlumSword.Game`의 전투 화면(`CombatView`, `JongnamSprites`)이 천마신교 일반 상대(마검사·흑갑 호위) 그림을 4스테이지에만 쓰고, 5스테이지 보스에 천마신교 최종 보스 시트(대기·이동·공격·기술·쓰러짐)를 적용하게 한다.

## 묶음
stage45_final

## 선행 작업
- 063_stage_final_label
- 064_stage_final_progress

## 참고 문서
- docs/index.md
- docs/dev/architecture.md ("테스트" 절 포함)
- docs/spec/combat.md ("화면" 절의 "천마신교 일반 상대"·"천마신교 4스테이지 보스"·"천마신교 최종 보스" 줄, "완료 조건 초안" 절의 72~76, "변경 기록" 절의 2026-11-22 항목)
- docs/spec/stage.md ("연결 지점" 절의 `StageLayout.FinalDisplayStage`)
- docs/dev/assets.md (5스테이지 천마신교 최종 보스 줄)

## 요구사항
- `docs/spec/combat.md`의 `## 변경 기록` `2026-11-22 — 천마신교 일반 상대를 4스테이지로, 5스테이지 최종 보스 외형 적용` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 스테이지 쓰러짐 뒤 페이드 대기는 064·065가 했다.
- 최종 보스 시트 `Assets/Resources/Art/Enemies/tianmo_final_boss_idle.png`, `_walk`, `_attack`, `_down`, `_skill`과 각 `.meta`는 이미 커밋되어 있다. 새로 만들거나 고치지 말고 코드에서 불러오기만 한다.
- 일반 상대 그림 선택 (`Assets/Scripts/Game/Combat/CombatView.cs`의 `OnEnemyAdded`): 지금 `stage == 5 && enemy.Kind == EnemyKind.Normal`인 마검사·흑갑 호위 분기를 `stage == 4 && enemy.Kind == EnemyKind.Normal`로 바꾼다 (인덱스 짝수 마검사 `_tianmoSwordsman`, 홀수 흑갑 호위 `_tianmoGuard`). 5스테이지 일반 상대에는 천마 일반 상대 그림을 쓰지 않는다 (기존 대체 표시). 4스테이지 보스 분기는 그대로다.
- 최종 보스 불러오기 (`Assets/Scripts/Game/Combat/JongnamSprites.cs`): `public static JongnamSprites TryLoadTianmoFinalBoss()`를 추가한다.
  - `idle`, `walk`, `attack`, `down`: 기존 `Slice(path, 104, 104, 25, 16)` (칸 104×104px, 가로 25칸, 기준점 (52, 16)px = 가로 가운데).
  - `skill`: 기존 `SliceGrid(path, 260, 80, 5, 5, 222, 5)` (칸 260×80px, 5열×5행, 위 왼쪽부터 가로 순서, 기준점 (222, 5)px).
  - `attackHitFrame: 11`, `skillHitFrame: 15`, `walkFramesPerSecond: 10f` (임시값). PPU 32 (기존 `Slice`·`SliceGrid`와 같음).
  - `idle`·`walk`·`attack`·`down` 중 하나라도 없거나 크기가 다르면 만든 프레임을 정리하고 `null`을 돌려준다. `skill`만 없으면 `Skill = null`로 만든다.
  - 경로 앞부분은 `JongnamSprites` 안의 `private static string TianmoFinalBossPrefix = "Art/Enemies/tianmo_final_boss_";`, 기술 시트 경로는 `private static string TianmoFinalBossSkillPath = "Art/Enemies/tianmo_final_boss_skill";` 필드로 둔다 (readonly 아님). 테스트는 리플렉션(`BindingFlags.NonPublic | BindingFlags.Static`)으로 없는 경로를 넣고 `finally`에서 되돌린다 (`JongnamSprites`는 internal 이라 `Type.GetType("PlumSword.Game.Combat.JongnamSprites, PlumSword.Game")` 등으로 찾는다).
- 최종 보스 표시 (`CombatView`):
  - `Initialize`에서 다른 시트와 같이 `_tianmoFinalBoss = JongnamSprites.TryLoadTianmoFinalBoss()`를 불러오고, 정리 시 `Dispose`한다.
  - `OnEnemyAdded`에서 `stage == StageLayout.FinalDisplayStage && enemy.Kind == EnemyKind.Boss`이면 `actor.Jongnam = _tianmoFinalBoss`. 없으면(`null`) 기존 보스 대체 표시.
  - 표시 배율 1.5 (다른 천마 보스와 같은 방식). 보스는 기존처럼 제자리다. 대기·이동 10fps, 공격 25프레임은 공격 간격에 맞춰 재생하고 피해 시점 11번 프레임(0부터).
  - 매 세 번째 공격(기존 `enemy.AttackCount % 3 == 0` 규칙)에는 기술 25프레임을 공격 간격에 맞춰 재생하고 피해 시점 15번 프레임이다. 기술 칸에는 캐릭터가 들어 있으므로 재생하는 동안 몸(`Body`) 스프라이트를 기술 칸으로 바꾸고 기본 캐릭터를 따로 그리지 않는다 (기존 기술 재생 흐름이 이미 몸 스프라이트를 바꾸면 그것을 쓴다).
  - 피해는 기존 근접 공격력·범위로 한 번만 준다 (전투 로직은 바꾸지 않는다. 검기는 화면 연출이고 투사체·추가 타격 없음).
  - 쓰러짐은 `down` 25프레임을 0.075초(`JongnamSprites.DownFrameSeconds`)씩 재생한다. 기존 쓰러짐 재생이 17프레임 고정이면 시트 길이(`Down.Length`)를 쓰도록 바꾸되 다른 상대의 동작은 그대로 둔다.
  - 기술 시트가 없으면 세 번째 공격도 `attack` 칸을 쓴다.
- 기존 테스트 처리: `Assets/Tests/PlayMode/Combat/TianmoSpriteTests.cs`의 `TianmoSpriteTests.StageFourAndNextRound_UseBothSpritesAndPlayDown`에서 `foreach (int number in new[] { 5, 10 })`를 `new[] { 4, 9 }`로 바꾼다. 이름과 나머지 검사는 그대로 두고 검사 수는 줄이지 않는다. `TianmoSpriteTests`의 다른 메서드, `TianmoBossSpriteTests`, `StageBackgroundTests`는 고치지 않고 계속 통과해야 한다.
- 새 플레이 모드 테스트는 `Assets/Tests/PlayMode/Combat/TianmoFinalBossSpriteTests.cs`(클래스 `TianmoFinalBossSpriteTests`, 네임스페이스는 같은 폴더의 기존 테스트와 같게)에 쓴다. 만드는 법은 기존 `TianmoSpriteTests.StageFourAndNextRound_UseBothSpritesAndPlayDown`과 `TianmoBossSpriteTests`를 따른다 (첫 씬을 불러오고, `GameRoot.StageProgress`를 리플렉션으로 바꿔 넣고, `battle.AddEnemy`로 적을 직접 넣고, `CombatView.Initialize`).

## 완료 조건
- [플레이 모드] `Assets/Tests/PlayMode/Combat/TianmoFinalBossSpriteTests.cs`(와 고친 `TianmoSpriteTests`)에 아래 [플레이 모드] 조건마다 테스트가 하나 이상 있고 모두 통과한다.
- [플레이 모드] 시작 번호 4·9 각각에 일반 상대 두 명을 넣으면 인덱스 짝수는 `tianmo_swordsman_`, 홀수는 `tianmo_guard_`로 시작하는 몸 스프라이트다 (고친 `StageFourAndNextRound_UseBothSpritesAndPlayDown`).
- [플레이 모드] 시작 번호 5에 일반 상대를 직접 넣으면 몸 스프라이트 이름이 `tianmo_`로 시작하지 않는다 (대체 표시).
- [플레이 모드] `Art/Enemies/tianmo_final_boss_idle`·`walk`·`attack`·`down` 텍스처는 각각 2600×104이다.
- [플레이 모드] `Art/Enemies/tianmo_final_boss_skill` 텍스처는 1300×400이다.
- [플레이 모드] 위 다섯 텍스처는 모두 `FilterMode.Point`이고 `mipmapCount`는 1이다.
- [플레이 모드] 시작 번호 5·10 각각에 보스를 넣으면 몸 스프라이트 이름은 `tianmo_final_boss_idle_00`으로 시작한다.
- [플레이 모드] 위 보스의 몸 스프라이트 `pixelsPerUnit`은 32이고 표시 배율(`localScale.x`의 절댓값)은 1.5이다.
- [플레이 모드] 시작 번호 5의 보스가 세 번째 공격을 하는 동안 몸 스프라이트 이름은 `tianmo_final_boss_skill_`로 시작한다.
- [플레이 모드] 그 세 번째 공격에서 제자 체력은 보스 공격력만큼 정확히 한 번 준다.
- [플레이 모드] 시작 번호 5의 보스가 쓰러지면(전투 일시정지 상태로 확인) 몸 스프라이트가 `tianmo_final_boss_down_00`으로 시작한다.
- [플레이 모드] 위 상황에서 약 1.9초 뒤 몸 스프라이트는 `tianmo_final_boss_down_24`이다.
- [플레이 모드] `TianmoFinalBossSkillPath`를 없는 경로로 바꾸면 시작 번호 5 보스의 세 번째 공격 중 몸 스프라이트 이름은 `tianmo_final_boss_attack_`으로 시작한다.
- [플레이 모드] `TianmoFinalBossPrefix`를 없는 경로로 바꾸면 시작 번호 5 보스의 몸 스프라이트 이름은 `tianmo_`로 시작하지 않는다 (보스 대체 표시).
- [컴파일] 화면 코드가 컴파일되고 기존 테스트(`TianmoBossSpriteTests`, `StageBackgroundTests`, `CoreAssemblyTests.Core_DoesNotReferenceUnity` 포함)가 모두 통과한다.
- [사람 확인] 에디터에서 5스테이지까지 진행하면 최종 보스가 발이 땅에 붙은 채 대기하고, 세 번째 공격마다 검기가 날아가는 기술 동작이 기본 캐릭터와 겹쳐 두 번 그려지지 않는다.

## 하지 말 것
- `Assets/Resources/Art/Enemies/tianmo_final_boss_*.png`와 `.meta`를 새로 만들거나 고치지 않는다.
- `Assets/Scripts/Core/`(전투·스테이지 로직)는 고치지 않는다. 피해 횟수·범위는 기존 전투 로직 그대로다.
- `Assets/Scripts/Game/Stage/`는 고치지 않는다 (064~066이 한다).
- 1~4스테이지 보스와 1~3스테이지 일반 상대의 그림·타이밍을 바꾸지 않는다.
- 기존 테스트 메서드를 지우거나 이름을 바꾸지 않는다. 요구사항에 적은 `StageFourAndNextRound_UseBothSpritesAndPlayDown`의 번호 교체 말고는 기존 테스트를 고치지 않는다.
- 씬·프리팹·에셋 파일, 어셈블리 정의를 바꾸지 않는다.
- `Assets/Tests/PlayMode/Capture/`는 고치지 않는다.

## 보고서

### 한 일
- 천마 일반 상대를 4스테이지에 연결하고 5스테이지 최종 보스의 대기·이동·공격·기술·쓰러짐 시트와 대체 표시를 연결했다.
- 회차별 그림, 시트 설정, 세 번째 공격과 피해, 쓰러짐 및 시트 누락을 확인하는 플레이 모드 테스트를 추가했다.
- 에셋 문서의 최종 보스와 일반 상대 연결 상태를 갱신했다.

### 바뀐 파일
- `Assets/Scripts/Game/Combat/CombatView.cs`, `Assets/Scripts/Game/Combat/JongnamSprites.cs`
- `Assets/Tests/PlayMode/Combat/TianmoSpriteTests.cs`, `Assets/Tests/PlayMode/Combat/TianmoFinalBossSpriteTests.cs` 및 `.meta`
- `docs/dev/assets.md`

### 가정
- 추가 가정 없음. 공격·기술 피해 프레임과 이동 속도는 작업 문서의 임시값을 사용했다.

### 사람이 확인할 것
- 에디터에서 5스테이지까지 진행하면 최종 보스가 발이 땅에 붙은 채 대기하고, 세 번째 공격마다 검기가 날아가는 기술 동작이 기본 캐릭터와 겹쳐 두 번 그려지지 않는지 확인한다.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며 루프 스크립트의 실행 및 판정을 기다린다.


## 루프 기록 (2026-11-22 04:16)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/067_combat_tianmo_final
- 루프 브랜치: loop/20261122-035241
