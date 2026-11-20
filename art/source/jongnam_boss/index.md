# 종남 장로 보스 원본

쓰러짐 신규 시안은 [종남 캐릭터 쓰러짐 시안](../jongnam_fall/index.md)에 보관한다.

사용자가 일러스트 시안과 PixelLab 기본 스프라이트 및 애니메이션을 확정했다. 2026-11-20에 1스테이지에 적용했다.

| 자료 | 파일 | 내용 |
| --- | --- | --- |
| 확정 일러스트 | [illustration_concept.png](illustration_concept.png) | 백발과 수염, 청백색 도복을 입은 종남 장로 |
| 기본 스프라이트 | [남서쪽](export/Idle/rotations/south-west.png) | 96×96px, 제자와 마주 보는 방향 |
| 크기 비교 | [height_comparison.png](height_comparison.png) | 주인공과 발을 맞춘 5배 정수 확대 |
| 스킬 참고 | [snowstorm_reference.png](snowstorm_reference.png) | 사용자가 제공한 청백색 눈꽃 회오리 참고 이미지 |
| PixelLab 원본 | [기본 그림 ZIP](pixellab_original_export.zip) / [애니메이션 ZIP](pixellab_animations_retry_export.zip) | 8방향 기본 그림, 남서쪽 애니메이션 원본과 재시도 |

## 애니메이션

각 24프레임, 남서 방향. 미리보기는 12fps다. PNG는 원본 프레임을 가로 한 줄로 배치했다. 게임의 재생 속도와 피벗은 [에셋 목록](../../../docs/dev/assets.md)을 따른다.

2026-11-18 사용자는 대기·걷기·스킬과 화면 오른쪽 아래에서 왼쪽 위로 베는 일반 공격을 확정했다. 나머지 일반 공격은 미선택 시안이다.

| 동작 | 미리보기 | 시트 | 칸 크기 | PixelLab 그룹 |
| --- | --- | --- | --- | --- |
| 대기 | [GIF](Elder_idle_loop_24f.gif) | [PNG](Elder_idle_loop_24f.png) | 96×100 | `d6f40af6-26c1-4a85-8167-a718387a061f` |
| 걷기 | [GIF](Elder_walk_loop_24f.gif) | [PNG](Elder_walk_loop_24f.png) | 96×104 | `9d9c327d-0fbe-4698-ac2d-f84da8e13c17` |
| 일반 공격 | [GIF](Elder_sword_attack_24f.gif) | [PNG](Elder_sword_attack_24f.png) | 100×100 | `c17e58cf-1587-427c-b6cf-74b56edfe863` |
| 일반 공격 수정 시안 (올려 베기) | [GIF](Elder_rising_slash_palette_24f.gif) | [PNG](Elder_rising_slash_palette_24f.png) | 100×100 | `9c7fb9c7-c3cf-4729-aa35-dc299d594362` |
| 일반 공격 새 시안 (왼쪽 아래 → 오른쪽 위) | [GIF](Elder_diagonal_low_left_to_high_right_24f.gif) | [PNG](Elder_diagonal_low_left_to_high_right_24f.png) | 100×100 | `2e6f6d50-1b2d-49b1-a344-9116b70a0cf0` |
| **일반 공격 확정 (화면 오른쪽 아래 → 왼쪽 위)** | [GIF](Elder_bottom_right_to_top_left_24f.gif) | [PNG](Elder_bottom_right_to_top_left_24f.png) | 100×100 | `1b460697-74e5-4c86-bb51-a915d3fdfd77` |
| 눈꽃 회오리 스킬 | [GIF](Elder_snowstorm_skill_24f.gif) | [PNG](Elder_snowstorm_skill_24f.png) | 100×100 | `5d707eb9-2b6c-43a8-a1ca-627a9c4b4cc1` |
| 일반 공격 재시도 | [GIF](Elder_sword_attack_retry_24f.gif) | [PNG](Elder_sword_attack_retry_24f.png) | 100×100 | `be47ede1-47d8-48b0-b6a4-011a5c60c0f1` |

- PixelLab PixMiniMax 커스텀 동작으로 제작. 대기·걷기는 시작 그림을 종료 목표로 지정했다. 반복 경계의 픽셀 변화 비율은 각각 0.64%, 2.42%이며 각 동작의 일반 프레임 간 중앙값 1.22%, 4.44%보다 작다.
- 스킬은 작은 청백색 회오리 검기를 표현한다. 참고 이미지처럼 뚜렷한 눈꽃 입자는 충분히 표현되지 않았다.
- 이전 일반 공격과 재시도에는 빨강·초록 잡점 또는 붉은 섬광이 있었다. 이후 팔레트를 지정해 생성한 미선택 시안의 원본은 [ZIP](pixellab_palette_attack_export.zip)에 보관한다.
- 왼쪽 아래에서 오른쪽 위로 베는 미선택 시안의 원본은 [ZIP](pixellab_diagonal_attack_export.zip)에 보관한다.
- 확정된 일반 공격은 이미지 기준 오른쪽 아래에서 왼쪽 위로 벤다. 24프레임 원본은 [ZIP](pixellab_right_to_left_export.zip)에 보관한다.
- 생성 요청: 대기는 제자리 호흡과 옷·수염의 작은 흔들림, 걷기는 남서 방향 제자리 보행, 일반 공격은 검을 들어 한 번 베고 복귀, 스킬은 검으로 기를 모아 작은 청백색 바람을 휘감고 복귀하도록 지정했다. 재시도는 색 섬광과 입자를 넣지 말도록 추가했다.

## 생성 기록

- PixelLab 캐릭터 `Jongnam Elder Boss`: `0db24a81-6d08-4887-8eaf-504bc24d0817`. Pro, 96×96px, 8방향, low top-down.
- 디자인 참고: 확정 일러스트. 스타일 참고: 주인공 PixelLab 캐릭터 `6c24a0f7-bff6-4d33-88cc-a7839592c1dd`.
- 생성 문구: Honorable elderly Jongnam martial sword master, matching the concept: white long hair in topknot, long white pointed beard and moustache, ivory layered robes, navy crossed lapels and sash, cloud ornament at belt, carries a straight sword. Compact SD chibi game sprite with large head and readable silhouette, matching the reference disciple pixel style. Taller slender stature, not wider. Calm combat pose.
- 남서쪽 원본의 투명하지 않은 픽셀 높이는 70px, 제자 남동쪽 기본 그림은 53px로 약 1.32배다. 둘 다 장식과 무기를 포함한 외곽 경계 기준이다. 이미지의 가로·세로를 따로 변형하지 않았다. 보스 도복과 검 때문에 가로 실루엣은 다르다.
- 사용자가 기본 그림과 대기·걷기·일반 공격·스킬 애니메이션을 확정했다.
