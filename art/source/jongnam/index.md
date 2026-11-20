# 종남파 일반 대련 상대 에셋 색인

쓰러짐 신규 시안은 [종남 캐릭터 쓰러짐 시안](../jongnam_fall/index.md)에 보관한다.

2026-11-18 사용자가 선택한 남녀 제자 일러스트, 기본 스프라이트와 애니메이션. 남녀 각 대기·걷기·공격을 1스테이지 일반 대련 상대에 적용한다.

| 자료 | 파일 | 크기 | 상태 |
| --- | --- | --- | --- |
| 남녀 합본 원본 | [concept_pair.png](concept_pair.png) | 1402×1122px | 선택됨 |
| 남자 일러스트 | [concept_male.png](concept_male.png) | 700×1122px | 선택됨, 합본에서 잘라냄 |
| 여자 일러스트 | [concept_female.png](concept_female.png) | 702×1122px | 선택됨, 합본에서 잘라냄 |

- 두 사람은 흰색 도복과 남색 깃·허리띠를 함께 쓰며, 남자가 더 크다. 한 장으로 생성한 원본을 확대·축소 없이 분리했다.
- Codex 내장 이미지 생성으로 제작. 사용자가 제공한 남녀 참고 이미지의 도복 색상과 [화산 제자 일러스트](../disciple/illustration_concept.png)의 분위기를 참고했다.
- 정지 일러스트라 방향·프레임 수·재생 속도는 해당 없다.

## 선택한 기본 스프라이트와 애니메이션

남녀 모두 남서쪽(왼쪽 앞), low top-down. 기본 스프라이트와 프레임은 PixelLab 원본이며, 시트는 원본을 가로 한 줄로 배치했다. 미리보기는 10fps다. 게임용 시트는 `Assets/Resources/Art/Enemies/`에 별도로 보관한다.

| 대상 | 기본 스프라이트 | 동작 | 시트 / 개별 프레임 / 미리보기 | 칸(px) | 프레임 | 게임 재생 속도 |
| --- | --- | --- | --- | --- | --- | --- |
| 남자 | [기본](male/base_south-west.png) (64×72) | 대기 | [시트](male/idle_sheet.png) / [프레임](male/idle_frames/) / [미리보기](male/idle_preview.gif) | 68×76 | 16 | 10fps |
| 남자 | 위와 같음 | 걷기 | [시트](male/walk_sheet.png) / [프레임](male/walk_frames/) / [미리보기](male/walk_preview.gif) | 72×84 | 24 | 12fps |
| 남자 | 위와 같음 | 공격 | [시트](male/attack_sheet.png) / [프레임](male/attack_frames/) / [미리보기](male/attack_preview.gif) | 92×96 | 24 | 24 ÷ 공격 간격 fps |
| 여자 | [기본](female/base_south-west.png) (56×56) | 대기 | [시트](female/idle_sheet.png) / [프레임](female/idle_frames/) / [미리보기](female/idle_preview.gif) | 76×76 | 9 | 10fps |
| 여자 | 위와 같음 | 걷기 | [시트](female/walk_sheet.png) / [프레임](female/walk_frames/) / [미리보기](female/walk_preview.gif) | 72×84 | 24 | 12fps |
| 여자 | 위와 같음 | 공격 | [시트](female/attack_sheet.png) / [프레임](female/attack_frames/) / [미리보기](female/attack_preview.gif) | 96×92 | 24 | 24 ÷ 공격 간격 fps |

- PixelLab 캐릭터 ID: 남자 `123a16e4-d602-43cb-b0e1-73243147e0eb`, 여자 `32dc293d-3307-47ed-906b-9087979aae9b`. 스타일 참고: 화산 제자 `6c24a0f7-bff6-4d33-88cc-a7839592c1dd`.
- 선택한 애니메이션 그룹 ID: 남자 대기 `299ff77e-d531-4f8e-a83d-a9ef6dab7e6f`, 걷기 `bcb857ab-4125-46d6-8ded-6861af8a3ac3`, 공격 `5589ce82-c147-4ffe-80fa-9008e4e64c96`; 여자 대기 `0da2745e-4ecf-4526-9ed2-ea6119e7502f`, 걷기 `ff8774b0-518b-4ff4-b7b2-0d4732ffe46c`, 공격 `680a6520-b93c-4123-8e6c-30e9f8eb8cf5`.
- 생성 지시 요약: 대기는 제자리에서 작은 움직임(남자는 숨·입김·연기·먼지 효과 없음), 걷기는 왼쪽으로 천천히 발을 번갈아 디디며 반복, 공격은 왼쪽을 향한 한 번의 검 휘두르기와 흰 검기 및 자세 회복. 남자 대기·여자 대기는 v3, 나머지는 PixMiniMax. 걷기와 공격의 마지막 자세는 기본 자세를 목표로 했다.
- 여자 공격은 선택 당시 흰 검기가 두 구간에 나타나 두 번의 검격처럼 보일 수 있다고 알렸으며, 사용자가 이 시안을 선택했다. 재생 속도와 화면 배치는 게임에서 확인한다.
