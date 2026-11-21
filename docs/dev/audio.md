# 오디오 에셋 목록

2026-11-22에 Soundza MCP로 AI 생성한 배경음악과 효과음이다. 파일은 `Assets/Resources/Audio/`에 있으며, 아래 리소스 경로는 `Assets/Resources/` 다음부터 확장자를 뺀 이름이다. 현재 파일과 Unity `.meta`만 준비되어 있으며 게임 재생에 연결하지 않았고 Unity 에디터에서 임포트·재생을 확인하지 않았다.

- 출처: [Soundza](https://soundza.com). 생성 결과에 표시된 조건은 상업적 이용 가능, 출처 표기 불필요다. 게임 공개 시 감사 링크는 선택 사항이다.
- 배경음악은 사용자가 네 곡을 확정했다. 효과음은 14종을 생성해 저장했으며 개별 청취·확정 여부는 아직 정해지지 않았다.
- WAV 원본은 모두 44.1kHz 스테레오다. 배경음악은 16비트, 효과음은 24비트다. `.meta`를 함께 보관한다.
- 현재 파일과 이 문서는 로컬 작업 폴더에 있으며 커밋 전이다. 루프 worktree나 통합 브랜치에는 자동으로 들어가지 않는다.

## 배경음악

`Assets/Resources/Audio/Music/`의 WAV는 반복 재생용으로 생성됐다. 아래 길이는 반올림한 값이다. 실제 게임의 반복 연결과 전환은 후속 작업에서 확인한다.

| 리소스 경로 | 용도 | 길이 |
| --- | --- | --- |
| `Audio/Music/battle_normal` | 1~4스테이지 일반 전투 | 약 27.7초 |
| `Audio/Music/battle_boss` | 1~4스테이지 보스 전투 | 약 22.9초 |
| `Audio/Music/title` | 시작 화면 | 약 25.9초 |
| `Audio/Music/battle_final_boss` | 5스테이지 천마 최종 보스 전투 | 약 20.9초 |

## 효과음

`Assets/Resources/Audio/SFX/` 아래의 WAV다. 용도는 제작 당시 계획이며 실제 재생 시점과 음량은 연결 작업에서 정하고 확인한다.

| 리소스 경로 | 용도 |
| --- | --- |
| `Audio/SFX/sword_swing` | 검 휘두르기 |
| `Audio/SFX/attack_hit` | 공격 명중 |
| `Audio/SFX/upgrade_success` | 능력치 강화 성공 |
| `Audio/SFX/plum_sword_skill` | 이십사수매화검법 발동 |
| `Audio/SFX/realm_breakthrough` | 경지 돌파 연출 |
| `Audio/SFX/ui_confirm` | 버튼 선택·확인 |
| `Audio/SFX/boss_appear` | 보스 등장 |
| `Audio/SFX/boss_defeated` | 보스 격파 |
| `Audio/SFX/player_defeated` | 제자 패배 |
| `Audio/SFX/offline_reward` | 오프라인 보상 받기 |
| `Audio/SFX/enemy_defeated` | 일반 적 쓰러짐 |
| `Audio/SFX/upgrade_unavailable` | 강화 불가 |
| `Audio/SFX/window_open_close` | 창 열기·닫기 |
| `Audio/SFX/stage_transition` | 스테이지 전환 |
