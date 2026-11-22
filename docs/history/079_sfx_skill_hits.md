# 079_sfx_skill_hits — 스킬 타마다 휘두름·타격음을 내지 않기

## 목표
Core(`PlumSword.Core.Audio.SfxTriggers`)가 스킬이 이어지는 동안 받는 `EnemyDamaged`에는 휘두름·타격 효과음을 요청하지 않게 한다. 스킬은 13타로 나뉘어 `EnemyDamaged`가 여러 번 오기 때문이다.

## 묶음
skill_hits

## 선행 작업
- 078_battle_skill_hits

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/audio.md ("효과음" 연결 시점 표, 합치기 규칙, "완료 조건 초안" 17·25·26)
- docs/spec/combat.md ("연결 지점"의 Battle. 078이 `IsSkillActive`를 더했다)

## 요구사항
- 078에서 `Battle`이 스킬을 13타로 나누어 때리게 되었다. 발동 칸에 첫 타의 `EnemyDamaged`와 `SkillUsed`가 오고, 그 뒤 46칸 동안 나머지 12타의 `EnemyDamaged`가 온다. 스킬이 이어지는 동안에는 `Battle.IsSkillActive`가 `true`다 (`EnemyDamaged`를 받는 순간에도 `true`).
- `SfxTriggers`의 `EnemyDamaged` 처리: `battle.IsSkillActive`가 `true`면 아무것도 요청하지 않는다. `false`면 지금처럼 `SwordSwing`, `AttackHit`을 요청한다.
- `SkillUsed` 처리(`PlumSwordSkill` 요청)와 `SfxQueue`의 "스킬이 대신" 합치기 규칙은 그대로 둔다.
- 끝나면 `docs/spec/audio.md`의 효과음 연결 시점 표(`SwordSwing`, `AttackHit` 줄)와 완료 조건 초안에 "스킬이 이어지는 동안(`Battle.IsSkillActive`)의 피해에는 요청하지 않는다"를 반영한다.

## 완료 조건
- `Assets/Tests/EditMode/Audio/SfxTriggersTests.cs`에 아래 조건마다 테스트가 하나 이상 있고 모두 통과한다. 기존 도우미 `Create`, `Drain`을 쓴다.
- `Create(attack: 10, skill: true)` 뒤 `battle.AddEnemy(EnemyKind.Boss, 2, battle.DisciplePosition + 1.0)`를 두고 `battle.Step()` 1번 뒤 `Drain()` 결과에 `PlumSwordSkill`이 있고 `SwordSwing`, `AttackHit`은 없다.
- 같은 상황에서 이어서 `battle.Step()`과 `Drain()`을 62번 번갈아 부르는 동안 어느 결과에도 `SwordSwing`, `AttackHit`이 없다.
- 같은 상황에서 이어서 `battle.Step()` 1번(처음부터 64번째 칸, 스킬이 끝난 뒤의 기본 공격) 뒤 `Drain()` 결과에 `SwordSwing`과 `AttackHit`이 있다.
- 기존 테스트 `SkillAttack_ReplacesSwingAndHit`과 스킬이 꺼진 전투의 휘두름·타격 테스트가 그대로 통과한다.
- 기존 테스트가 모두 통과한다.

## 하지 말 것
- `Assets/Scripts/Core/Combat/`(전투 기능 내부)를 고치지 않는다. `Battle.IsSkillActive`를 읽기만 한다.
- `SfxQueue`의 합치기·간격 규칙과 다른 효과음의 연결을 바꾸지 않는다.
- 화면 코드(`Assets/Scripts/Game/`)를 고치지 않는다.
- `UnityEngine`을 쓰지 않는다.

## 보고서

### 한 일
- 스킬 활성 중 피해 알림에서는 휘두름·타격음을 요청하지 않고, 스킬 종료 후 기본 공격에서는 다시 요청하도록 했다.
- 첫 타, 후속 12타가 포함된 62칸, 64번째 칸의 기본 공격을 검사하는 테스트와 소리 사양을 추가했다.

### 바뀐 파일
- `Assets/Scripts/Core/Audio/SfxTriggers.cs`
- `Assets/Tests/EditMode/Audio/SfxTriggersTests.cs`
- `docs/spec/audio.md`
- `tasks/doing/079_sfx_skill_hits.md`
- `.loop/result.json`

### 가정
없음.

### 사람이 확인할 것
없음.

### 테스트 결과
- `git diff --check` 통과.
- Unity 테스트는 지시에 따라 직접 실행하지 않았으며, 기존 테스트와 새 테스트의 결과는 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-22 13:28)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/079_sfx_skill_hits
- 루프 브랜치: loop/20261122-131718
