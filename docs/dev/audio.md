# 오디오 에셋 목록

2026-11-22에 Soundza MCP로 AI 생성한 배경음악과 효과음이다. 파일은 `Assets/Resources/Audio/`에 있으며, 아래 리소스 경로는 `Assets/Resources/` 다음부터 확장자를 뺀 이름이다. 소리 사양대로 `AudioView`가 게임 재생에 연결했으며, 배치 모드 플레이 모드 테스트로 확인하고 실제 청취는 사람이 확인한다. 게임에 연결하는 규칙(언제 어떤 곡·효과음을 내는지, 음량, 겹침 처리)은 [소리 사양](../spec/audio.md)에 있다.

- 출처: [Soundza](https://soundza.com). 생성 결과에 표시된 조건은 상업적 이용 가능, 출처 표기 불필요다. 게임 공개 시 감사 링크는 선택 사항이다.
- 배경음악은 사용자가 네 곡을 확정했다. 효과음은 14종을 생성해 저장했으며 개별 청취·확정 여부는 아직 정해지지 않았다. 그래서 소리 사양에서 효과음의 연결·배율·켜짐은 모두 임시이고, `AudioCatalog` 표에서 바꾼다.
- WAV 원본은 모두 44.1kHz 스테레오다. 배경음악은 16비트, 효과음은 24비트다. `.meta`를 함께 보관한다.
- 파일과 이 문서는 main에 커밋되어 있다 (`b841ac5`). 루프는 새 루프를 시작할 때 main을 통합 브랜치에 합쳐 가져간다.

## 배경음악

`Assets/Resources/Audio/Music/`의 WAV는 반복 재생용으로 생성됐다. 아래 길이는 반올림한 값이다. 언제 어떤 곡으로 바꾸는지는 소리 사양에서 정했다. 실제 반복 이음새가 자연스러운지는 연결한 뒤 들어 보고 확인한다.

| 리소스 경로 | 용도 | 길이 |
| --- | --- | --- |
| `Audio/Music/battle_normal` | 1~4스테이지 일반 전투 | 약 27.7초 |
| `Audio/Music/battle_boss` | 1~4스테이지 보스 전투 | 약 22.9초 |
| `Audio/Music/title` | 시작 화면 | 약 25.9초 |
| `Audio/Music/battle_final_boss` | 5스테이지 천마 최종 보스 전투 | 약 20.9초 |

## 효과음

`Assets/Resources/Audio/SFX/` 아래의 WAV다. 용도는 제작 당시 계획이다. 실제로 붙는 알림과 기본 음량은 소리 사양의 "효과음"과 "소리 목록"에 있다(임시).

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
