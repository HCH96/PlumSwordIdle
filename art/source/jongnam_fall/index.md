# 종남 캐릭터 쓰러짐 시안

2026-11-20 PixelLab PixMiniMax로 기존 종남 캐릭터의 남서쪽 기본 그림에서 생성한 쓰러짐 원본이다. 사용자가 남녀 제자와 장로의 오른쪽으로 쓰러짐 재생성 시안을 선택해 게임에 적용했다. 각 동작은 시작 그림 1장과 생성 프레임 16장으로 구성된다. 미리보기는 10fps 반복이며 게임에서는 한 번 재생한다.

| 캐릭터 | 미리보기 | 가로 시트 | 원본 프레임 | 칸 크기 | PixelLab 그룹 ID |
| --- | --- | --- | --- | --- | --- |
| 남자 제자 | [GIF](male_fall_preview.gif) | [PNG](male_fall_sheet.png) | [프레임](male_frames/) | 76×76 | `93630f8c-d192-4572-b189-9c8092ed8584` |
| 여자 제자 | [GIF](female_fall_preview.gif) | [PNG](female_fall_sheet.png) | [프레임](female_frames/) | 76×76 | `dd452c97-a0c5-4d2b-a00c-3ea206db9725` |
| 종남 장로 | [GIF](boss_fall_preview.gif) | [PNG](boss_fall_sheet.png) | [프레임](boss_frames/) | 100×100 | `57245ede-16bf-4497-84a7-d41d98f76ec4` |
| 종남 장로 오른쪽으로 쓰러짐 재생성 | [GIF](boss_right_fall_preview.gif) | [PNG](boss_right_fall_sheet.png) | [프레임](boss_right_frames/) | 100×100 | `8883c10d-13a1-402a-b510-26959493395e` |

- 생성 지시: 즉시 균형을 잃고 화면 오른쪽 뒤로 쓰러져 바닥에 누운 뒤 움직이지 않는다. 검과 복장을 유지하며 효과는 넣지 않는다.
- 가로 시트는 원본 프레임을 확대·축소 없이 순서대로 배치했다. 게임 피벗과 재생 속도는 [에셋 목록](../../../docs/dev/assets.md)을 따른다.
- 장로의 최초 시안은 머리가 화면 왼쪽에 놓여 방향이 반대였다. 재생성 시안은 왼쪽을 바라보며 시작해 머리가 화면 오른쪽에 놓이도록 쓰러진다. 쓰러지기 전 초반에 잠시 서 있는 구간이 있다.
- 원본 캐릭터와 다른 동작은 [종남 제자](../jongnam/index.md), [종남 장로](../jongnam_boss/index.md) 색인을 따른다.
